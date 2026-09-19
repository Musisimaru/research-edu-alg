namespace Crawler.Core;

 
/// <summary>
/// Минимальный интерфейс "мозга": наблюдения -> действия в [-1, 1].
/// Его реализуют все агенты (random, GA, NEAT, PPO), поэтому
/// просмотрщик и EpisodeRunner работают с любым из них.
/// </summary>
public interface IPolicy
{
    float[] Act(float[] obs);
}