namespace Crawler.Core;

/// <summary>
/// Прогон одного эпизода: env + policy + seed -> суммарная награда.
/// Для GA/NEAT это и есть fitness-функция.
/// Перегрузка с record дополнительно пишет по-шаговую траекторию
/// (позы звеньев + применённые действия + награда) для реплея.
/// </summary>
public static class EpisodeRunner
{
    public static EpisodeResult Run(ICrawlerEnv env, IPolicy policy, int seed)
        => Run(env, policy, seed, record: null);

    public static EpisodeResult Run(ICrawlerEnv env, IPolicy policy, int seed,
                                    List<TrajectoryStep>? record)
    {
        var obs = env.Reset(seed);
        float total = 0f;
        int steps = 0;
        bool done = false;

        while (!done)
        {
            var actions = policy.Act(obs);
            (obs, float reward, done) = env.Step(actions);
            total += reward;
            steps++;

            if (record != null)
            {
                var p0 = env.GetLinkPose(0);
                var p1 = env.GetLinkPose(1);
                var p2 = env.GetLinkPose(2);
                record.Add(new TrajectoryStep(
                    p0.X, p0.Y, p0.Rotation,
                    p1.X, p1.Y, p1.Rotation,
                    p2.X, p2.Y, p2.Rotation,
                    Math.Clamp(actions[0], -1f, 1f),
                    Math.Clamp(actions[1], -1f, 1f),
                    reward));
            }
        }

        return new EpisodeResult(total, steps, env.ReachedTarget);
    }
}
