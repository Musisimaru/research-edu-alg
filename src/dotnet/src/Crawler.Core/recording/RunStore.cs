namespace Crawler.Core;

/// <summary>
/// Где лежат раны. Корень runs/ ищется от каталога бинарника вверх:
/// приоритет — корень git-репозитория, затем каталог с *.slnx, иначе cwd.
/// Переопределяется переменной окружения CRAWLER_RUNS_DIR.
/// </summary>
public static class RunStore
{
    public static string ResolveRunsRoot()
    {
        var env = Environment.GetEnvironmentVariable("CRAWLER_RUNS_DIR");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        string? gitRoot = null;
        string? slnRoot = null;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (gitRoot is null && Directory.Exists(Path.Combine(dir.FullName, ".git")))
                gitRoot = dir.FullName;
            if (slnRoot is null && dir.EnumerateFiles("*.slnx").Any())
                slnRoot = dir.FullName;
        }

        var root = gitRoot ?? slnRoot ?? Environment.CurrentDirectory;
        return Path.Combine(root, "runs");
    }

    /// <summary>Самый свежий ран (имена содержат timestamp, сортировка по имени = по времени).</summary>
    public static string? LatestRunDir(string runsRoot)
    {
        if (!Directory.Exists(runsRoot)) return null;
        return Directory.EnumerateDirectories(runsRoot)
            .Where(d => File.Exists(Path.Combine(d, "manifest.json")))
            .OrderByDescending(Path.GetFileName)
            .FirstOrDefault();
    }

    /// <summary>Принимает полный путь | имя каталога под runs/ | "latest".</summary>
    public static string ResolveRunDir(string arg)
    {
        if (arg == "latest")
            return LatestRunDir(ResolveRunsRoot())
                   ?? throw new DirectoryNotFoundException($"No runs found under {ResolveRunsRoot()}");

        if (Directory.Exists(arg))
            return Path.GetFullPath(arg);

        var candidate = Path.Combine(ResolveRunsRoot(), arg);
        if (Directory.Exists(candidate))
            return candidate;

        throw new DirectoryNotFoundException($"Run not found: '{arg}' (looked in {ResolveRunsRoot()})");
    }
}
