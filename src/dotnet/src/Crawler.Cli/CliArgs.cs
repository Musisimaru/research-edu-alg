using System.Globalization;

namespace Crawler.Cli;

/// <summary>Мини-разбор аргументов вида "--name value" и флагов — без пакетов-парсеров.</summary>
public static class CliArgs
{
    public static string? GetString(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    public static int GetInt(string[] args, string name, int def)
        => GetString(args, name) is { } s ? int.Parse(s, CultureInfo.InvariantCulture) : def;

    public static long GetLong(string[] args, string name, long def)
        => GetString(args, name) is { } s ? long.Parse(s, CultureInfo.InvariantCulture) : def;

    public static bool HasFlag(string[] args, string name) => Array.IndexOf(args, name) >= 0;

    /// <summary>Список вида "1,2,3" -> int[]; null, если аргумент не задан.</summary>
    public static int[]? GetIntList(string[] args, string name)
        => GetString(args, name)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.Parse(s, CultureInfo.InvariantCulture))
            .ToArray();

    /// <summary>Первый позиционный аргумент после субкоманды (не начинающийся с "-").</summary>
    public static string? GetPositional(string[] args, int index)
    {
        int seen = 0;
        for (int i = 1; i < args.Length; i++) // args[0] — субкоманда
        {
            if (args[i].StartsWith('-'))
            {
                i++; // пропускаем и значение опции
                continue;
            }
            if (seen++ == index) return args[i];
        }
        return null;
    }
}
