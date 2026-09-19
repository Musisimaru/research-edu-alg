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

    public MetricsLogger(string algo, string dir = "runs")
    {
        Directory.CreateDirectory(dir);
        FilePath = Path.Combine(dir, $"{algo}_{DateTime.Now:yyyMMdd_HHmmss}.csv");
        _writer = new StreamWriter(FilePath);
        _writer.WriteLine("total_env_steps,best_reward,mean_reward");
    }

    public void Log(long totalEnvSteps, float bestReward, float meanReward)
    {
        _writer.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{totalEnvSteps},{bestReward:F4},{meanReward:F4}"));
        _writer.Flush(); // чтобы кривую можно было смотреть прямо во время обучения
    }
    
    public void Dispose() => _writer.Dispose();
}