using System.Text.Json;
using System.Text.Json.Serialization;

namespace Crawler.Core;

/// <summary>Общие настройки JSON для всех файлов рана.</summary>
public static class RunJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static readonly JsonSerializerOptions Indented = new(Options)
    {
        WriteIndented = true,
    };
}
