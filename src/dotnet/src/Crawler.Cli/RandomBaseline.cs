using Crawler.Core;

namespace Crawler.Cli;

/// <summary>
/// Headless-прогон случайной политики — нижняя планка для сравнения
/// с GA/NEAT/PPO. Батчи эпизодов гоняются параллельно: по одному
/// CrawlerEnv на итерацию (мир Aether не потокобезопасен) — этот же
/// паттерн затем переиспользует GA для оценки популяции.
/// </summary>
public static class RandomBaseline
{
    public static void Run(long stepBudget = 2_000_000, int batchSize = 100, int holdSteps = 10)
    {
        using var log = new MetricsLogger("random");
        Console.WriteLine($"Random baseline: budget={stepBudget:N0} env steps, batch={batchSize}, hold={holdSteps}");
        Console.WriteLine($"CSV: {log.FilePath}");

        long totalSteps = 0;
        int episodeCounter = 0;
        float allTimeBest = float.MinValue;

        while (totalSteps < stepBudget)
        {
            var rewards = new float[batchSize];
            var steps = new int[batchSize];
            int baseSeed = episodeCounter;

            Parallel.For(0, batchSize, i =>
            {
                int seed = baseSeed + i;
                var env = new CrawlerEnv();
                var policy = new RandomPolicy(seed, env.ActionSize, holdSteps);
                var result = EpisodeRunner.Run(env, policy, seed);
                rewards[i] = result.TotalReward;
                steps[i] = result.Steps;
            });

            episodeCounter += batchSize;
            totalSteps += steps.Sum(s => (long)s);

            float best = rewards.Max();
            float mean = rewards.Average();
            allTimeBest = Math.Max(allTimeBest, best);

            log.Log(totalSteps, best, mean);
            Console.WriteLine($"steps={totalSteps,12:N0}  best={best,8:F3}  mean={mean,8:F3}  all-time best={allTimeBest,8:F3}");
        }
    }
}