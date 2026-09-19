namespace Crawler.Core;

/// <summary>
/// Сериализуемое описание политики — достаточно, чтобы воссоздать её
/// для детерминированного реплея. Для GA/NEAT позже добавится
/// type="mlp"/"neat" с File, указывающим на файл генома, — схема не меняется.
/// </summary>
public sealed record PolicySpec(string Type, int? Seed = null, int? HoldSteps = null, string? File = null)
{
    public static PolicySpec Random(int seed, int holdSteps) => new("random", seed, holdSteps);
}

public static class PolicyFactory
{
    public static IPolicy Create(PolicySpec spec, int actionSize) => spec.Type switch
    {
        "random" => new RandomPolicy(
            spec.Seed ?? throw new InvalidDataException("random policy spec without seed"),
            actionSize,
            spec.HoldSteps ?? 10),
        _ => throw new NotSupportedException($"Unknown policy type '{spec.Type}' — add it to PolicyFactory."),
    };
}
