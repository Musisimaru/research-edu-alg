namespace Crawler.Core;

/// <summary>Поза звена в мировых координатах (для записи/рендера без типов Aether).</summary>
public readonly record struct LinkPose(float X, float Y, float Rotation);

public interface ICrawlerEnv
{
    int ObservationSize { get; }
    int ActionSize { get; }
    float[] Reset(int seed);
    (float[] Obs, float Reward, bool Done) Step(float[] actions);

    int LinkCount { get; }
    LinkPose GetLinkPose(int index);

    /// <summary>true, если в текущем эпизоде цель (TargetX) уже достигнута.</summary>
    bool ReachedTarget { get; }
}
