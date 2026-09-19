using System.Text.Json;

namespace Crawler.Core;

/// <summary>
/// Читающая сторона рана. GetTrajectory отдаёт записанный .traj, а если его нет —
/// детерминированно ре-симулирует эпизод по seed + PolicySpec (мир пересоздаётся
/// в Reset с нуля, вся случайность от seed, так что результат бит-в-бит тот же).
/// 1500 headless-шагов — десятки миллисекунд, можно делать до открытия окна.
/// </summary>
public static class ReplayLoader
{
    public static RunManifest LoadManifest(string runDir)
    {
        var json = File.ReadAllText(Path.Combine(runDir, "manifest.json"));
        return JsonSerializer.Deserialize<RunManifest>(json, RunJson.Options)
               ?? throw new InvalidDataException($"Empty manifest in {runDir}");
    }

    public static List<EpisodeRecord> LoadEpisodes(string runDir)
    {
        var path = Path.Combine(runDir, "episodes.jsonl");
        var result = new List<EpisodeRecord>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var rec = JsonSerializer.Deserialize<EpisodeRecord>(line, RunJson.Options);
            if (rec != null) result.Add(rec);
        }
        return result;
    }

    public static TrajectoryStep[] GetTrajectory(string runDir, EpisodeRecord ep)
        => GetTrajectory(runDir, ep, out _);

    /// <param name="resimulated">true, если траектория получена ре-симуляцией, а не из файла.</param>
    public static TrajectoryStep[] GetTrajectory(string runDir, EpisodeRecord ep, out bool resimulated)
    {
        if (ep.Traj != null)
        {
            var path = Path.Combine(runDir, ep.Traj);
            if (File.Exists(path))
            {
                resimulated = false;
                return TrajectoryIO.Read(path);
            }
        }

        resimulated = true;
        var env = new CrawlerEnv();
        var policy = PolicyFactory.Create(ep.Policy, env.ActionSize);
        var record = new List<TrajectoryStep>(CrawlerEnv.MaxSteps);
        EpisodeRunner.Run(env, policy, ep.Seed, record);
        return record.ToArray();
    }
}
