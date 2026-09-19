namespace Crawler.Core;

/// <summary>
/// Один шаг записанной траектории: позы трёх звеньев (мировые координаты),
/// применённые (уже клампнутые) действия и награда за шаг.
/// Ровно то, что нужно для воспроизведения без физики.
/// </summary>
public readonly record struct TrajectoryStep(
    float X0, float Y0, float R0,
    float X1, float Y1, float R1,
    float X2, float Y2, float R2,
    float A0, float A1,
    float Reward);
