namespace Crawler.Core;

/// <summary>
/// Одна строка episodes.jsonl: итог эпизода + всё, что нужно для реплея.
/// Traj — относительный путь к записанной траектории (только топ-N за батч),
/// null => реплей через детерминированную ре-симуляцию по Seed + Policy.
/// Batch для GA/NEAT играет роль номера поколения.
/// </summary>
public sealed record EpisodeRecord(
    int Id,
    int Batch,
    int Seed,
    float Reward,
    int Steps,
    bool Reached,
    PolicySpec Policy,
    string? Traj);
