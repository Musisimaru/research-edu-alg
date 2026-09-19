using Crawler.Cli;
using Crawler.Cli.Report;
using Crawler.Cli.Viewers;
using Crawler.Core;

// Субкоманды:
//   (пусто) | watch   — окно, ручное управление (Q/A, W/S, R, Esc)
//   watch-random      — окно, случайная политика
//   random  [--budget N] [--batch N] [--hold N] [--top N] [--no-traj]
//                     — headless baseline: ран пишется в runs/<algo>_<timestamp>/
//   replay  <run> [episode-id]        — реплей одного эпизода (по умолчанию лучший)
//   ghosts  <run> [--top N] [--episodes 1,2,3]  — несколько эпизодов в одном мире
//   grid    <run> [--top N]           — сетка ячеек, по эпизоду в каждой
//   report  [-o out.html]             — HTML-дашборд по всем ранам
// <run> = полный путь | имя каталога под runs/ | latest
//
// Позже добавятся: train-ga, train-neat, train-ppo, watch <brain.json>, compare.

switch (args.FirstOrDefault() ?? "watch")
{
    case "watch":
        Watch.Run(policy: null);
        break;

    case "watch-random":
        Watch.Run(new RandomPolicy(seed: 1, actionSize: 2, holdSteps: 10));
        break;

    case "random":
        RandomBaseline.Run(
            stepBudget: CliArgs.GetLong(args, "--budget", 2_000_000),
            batchSize: CliArgs.GetInt(args, "--batch", 100),
            holdSteps: CliArgs.GetInt(args, "--hold", 10),
            topN: CliArgs.GetInt(args, "--top", 3),
            recordTraj: !CliArgs.HasFlag(args, "--no-traj"));
        break;

    case "replay":
    {
        var runDir = RunStore.ResolveRunDir(CliArgs.GetPositional(args, 0) ?? "latest");
        int? episodeId = CliArgs.GetPositional(args, 1) is { } s ? int.Parse(s) : null;
        ReplayViewer.Run(runDir, episodeId);
        break;
    }

    case "ghosts":
    {
        var runDir = RunStore.ResolveRunDir(CliArgs.GetPositional(args, 0) ?? "latest");
        GhostsViewer.Run(runDir,
            topN: CliArgs.GetInt(args, "--top", 6),
            explicitIds: CliArgs.GetIntList(args, "--episodes"));
        break;
    }

    case "grid":
    {
        var runDir = RunStore.ResolveRunDir(CliArgs.GetPositional(args, 0) ?? "latest");
        GridViewer.Run(runDir, topN: CliArgs.GetInt(args, "--top", 16));
        break;
    }

    case "report":
    {
        var outPath = ReportGenerator.Generate(RunStore.ResolveRunsRoot(), CliArgs.GetString(args, "-o"));
        Console.WriteLine($"Report: {outPath}");
        break;
    }

    default:
        Console.WriteLine("Usage: crawler [watch|watch-random|random|replay|ghosts|grid|report]");
        break;
}
