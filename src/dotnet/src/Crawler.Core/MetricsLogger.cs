using System.Globalization;

namespace Crawler.Core;

/// <summary>
/// CSV-лог обучения, общий формат для всех алгоритмов:
///   total_env_steps,best_reward,mean_reward
/// Сравнение алгоритмов идёт по потраченным шагам среды (ось X),
/// а не по поколениям/итерациям — они между алгоритмами несравнимы.
/// Все числа пишутся в InvariantCulture: на ru-локали float.ToString()
/// даёт запятую и ломает CSV.
/// </summary>
public sealed class MetricsLogger :  IDisposable
{
    private readonly StreamWriter _writer;
    
    public string FilePath { get; }

    public MetricsLogger(string algo, string? dir = null)
        : this(MakePath(algo, dir ?? RunStore.ResolveRunsRoot()))
    {
    }

    private MetricsLogger(string filePath)
    {
        FilePath = filePath;
        _writer = new StreamWriter(FilePath);
        _writer.WriteLine("total_env_steps,best_reward,mean_reward");
    }

    /// <summary>Лог ровно по этому пути (используется RunRecorder для metrics.csv рана).</summary>
    public static MetricsLogger AtPath(string filePath) => new(filePath);

    private static string MakePath(string algo, string dir)
    {
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{algo}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    public void Log(long totalEnvSteps, float bestReward, float meanReward)
    {
        _writer.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{totalEnvSteps},{bestReward:F4},{meanReward:F4}"));
        _writer.Flush(); // чтобы кривую можно было смотреть прямо во время обучения
    }
    
    public void Dispose() => _writer.Dispose();
}