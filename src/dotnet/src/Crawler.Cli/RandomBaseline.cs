using Crawler.Core;

namespace Crawler.Cli;

/// <summary>
/// Headless-прогон случайной политики — нижняя планка для сравнения
/// с GA/NEAT/PPO. Батчи эпизодов гоняются параллельно: по одному
/// CrawlerEnv на итерацию (мир Aether не потокобезопасен) — этот же
/// паттерн затем переиспользует GA для оценки популяции.
/// Каждый эпизод пишется в episodes.jsonl; траектории — только топ-N батча
/// (остальные восстанавливаются детерминированным реплеем по seed).
/// </summary>
public static class RandomBaseline
{
    public static void Run(long stepBudget = 2_000_000, int batchSize = 100, int holdSteps = 10,
                           int topN = 3, bool recordTraj = true)
    {
        using var run = new RunRecorder("random",
            new Dictionary<string, object?>
            {
                ["stepBudget"] = stepBudget,
                ["batchSize"] = batchSize,
                ["holdSteps"] = holdSteps,
            },
            topNPerBatch: recordTraj ? topN : 0);

        Console.WriteLine($"Random baseline: budget={stepBudget:N0} env steps, batch={batchSize}, hold={holdSteps}, top={topN}");
        Console.WriteLine($"Run dir: {run.RunDir}");

        long totalSteps = 0;
        int episodeCounter = 0;
        int batchIndex = 0;
        float allTimeBest = float.MinValue;

        while (totalSteps < stepBudget)
        {
            var results = new EpisodeResult[batchSize];
            // Каждый воркер пишет в свою ячейку — блокировки на горячем пути не нужны.
            var trajs = new List<TrajectoryStep>?[batchSize];
            int baseSeed = episodeCounter;

            Parallel.For(0, batchSize, i =>
            {
                int seed = baseSeed + i;
                var env = new CrawlerEnv();
                var policy = new RandomPolicy(seed, env.ActionSize, holdSteps);
                var record = recordTraj ? new List<TrajectoryStep>(CrawlerEnv.MaxSteps) : null;
                results[i] = EpisodeRunner.Run(env, policy, seed, record);
                trajs[i] = record;
            });

            episodeCounter += batchSize;
            totalSteps += results.Sum(r => (long)r.Steps);

            // Весь файловый I/O — однопоточно, в join-точке после батча.
            var topSet = Enumerable.Range(0, batchSize)
                .OrderByDescending(i => results[i].TotalReward)
                .Take(recordTraj ? topN : 0)
                .ToHashSet();

            var records = new EpisodeRecord[batchSize];
            for (int i = 0; i < batchSize; i++)
            {
                int id = baseSeed + i; // id == seed — удобно для глаз и реплея
                string? trajPath = topSet.Contains(i) && trajs[i] is { } t
                    ? run.SaveTrajectory(id, t)
                    : null;
                records[i] = new EpisodeRecord(
                    id, batchIndex, id,
                    results[i].TotalReward, results[i].Steps, results[i].Reached,
                    PolicySpec.Random(id, holdSteps), trajPath);
            }
            run.AppendEpisodes(records);

            float best = results.Max(r => r.TotalReward);
            float mean = results.Average(r => r.TotalReward);
            allTimeBest = Math.Max(allTimeBest, best);

            run.Metrics.Log(totalSteps, best, mean);
            Console.WriteLine($"batch={batchIndex,4}  steps={totalSteps,12:N0}  best={best,8:F3}  mean={mean,8:F3}  all-time best={allTimeBest,8:F3}");
            batchIndex++;
        }
    }
}
