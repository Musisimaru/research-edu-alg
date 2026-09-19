namespace Crawler.Core;

/// <summary>
/// Прогон одного эпизода: env + policy + seed -> суммарная награда.
/// Для GA/NEAT это и есть fitness-функция.
/// </summary>
public static class EpisodeRunner
{
    public static EpisodeResult Run(ICrawlerEnv env, IPolicy policy, int seed)
    {
        var obs = env.Reset(seed);
        float total = 0f;
        int steps = 0;
        bool done = false;

        while (!done)
        {
            (obs, float reward, done) = env.Step(policy.Act(obs));
            total += reward;
            steps++;
        }
        
        return new EpisodeResult(total, steps);
    }
}