namespace Crawler.Core;

/// <summary>
/// Снимок конфигурации среды на момент рана — чтобы старые записи
/// оставались интерпретируемыми, если константы CrawlerEnv поменяются.
/// </summary>
public sealed record EnvConfig(float TargetX, int MaxSteps, float Dt, int ObservationSize, int ActionSize)
{
    public static EnvConfig Current() =>
        new(CrawlerEnv.TargetX, CrawlerEnv.MaxSteps, CrawlerEnv.Dt, 11, 2);
}

public sealed record RunManifest(
    int SchemaVersion,
    string Algo,
    DateTime StartedUtc,
    string? GitCommit,
    EnvConfig Env,
    Dictionary<string, object?> Hyperparams,
    int TopNPerBatch);
