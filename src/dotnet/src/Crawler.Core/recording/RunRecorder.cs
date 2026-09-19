using System.Diagnostics;
using System.Text.Json;

namespace Crawler.Core;

/// <summary>
/// Пишущая сторона рана: создаёт каталог runs/&lt;algo&gt;_&lt;timestamp&gt;/,
/// сразу сохраняет manifest.json, ведёт episodes.jsonl и metrics.csv.
/// AppendEpisodes потокобезопасен, но задуман для вызова один раз на батч
/// из join-точки после Parallel.For.
/// </summary>
public sealed class RunRecorder : IDisposable
{
    private readonly StreamWriter _episodes;
    private readonly object _lock = new();

    public string RunDir { get; }
    public MetricsLogger Metrics { get; }

    public RunRecorder(string algo, Dictionary<string, object?> hyperparams, int topNPerBatch,
                       string? runsRoot = null)
    {
        runsRoot ??= RunStore.ResolveRunsRoot();
        RunDir = Path.Combine(runsRoot, $"{algo}_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(Path.Combine(RunDir, "trajectories"));

        var manifest = new RunManifest(
            SchemaVersion: 1,
            Algo: algo,
            StartedUtc: DateTime.UtcNow,
            GitCommit: TryGetGitCommit(runsRoot),
            Env: EnvConfig.Current(),
            Hyperparams: hyperparams,
            TopNPerBatch: topNPerBatch);
        File.WriteAllText(Path.Combine(RunDir, "manifest.json"),
            JsonSerializer.Serialize(manifest, RunJson.Indented));

        _episodes = new StreamWriter(Path.Combine(RunDir, "episodes.jsonl"));
        Metrics = MetricsLogger.AtPath(Path.Combine(RunDir, "metrics.csv"));
    }

    public void AppendEpisodes(IEnumerable<EpisodeRecord> records)
    {
        lock (_lock)
        {
            foreach (var rec in records)
                _episodes.WriteLine(JsonSerializer.Serialize(rec, RunJson.Options));
            _episodes.Flush(); // как и metrics.csv — чтобы ран можно было смотреть на ходу
        }
    }

    /// <summary>Сохраняет траекторию и возвращает относительный путь для EpisodeRecord.Traj.</summary>
    public string SaveTrajectory(int episodeId, IReadOnlyList<TrajectoryStep> steps)
    {
        var rel = Path.Combine("trajectories", $"ep_{episodeId:D6}.traj");
        TrajectoryIO.Write(Path.Combine(RunDir, rel), steps);
        return rel;
    }

    public void Dispose()
    {
        _episodes.Dispose();
        Metrics.Dispose();
    }

    private static string? TryGetGitCommit(string runsRoot)
    {
        try
        {
            var psi = new ProcessStartInfo("git", "rev-parse --short HEAD")
            {
                WorkingDirectory = Path.GetDirectoryName(runsRoot) ?? ".",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            string output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(3000);
            return p.ExitCode == 0 && output.Length > 0 ? output : null;
        }
        catch
        {
            return null; // git недоступен — не повод ронять ран
        }
    }
}
