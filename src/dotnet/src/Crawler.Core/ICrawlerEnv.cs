namespace Crawler.Core;

public interface ICrawlerEnv
{
    int ObservationSize { get; }
    int ActionSize { get; }
    float[] Reset(int seed);
    (float[] Obs, float Reward, bool Done) Step(float[] actions);
}