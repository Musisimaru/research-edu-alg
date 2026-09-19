namespace Crawler.Core;

/// <summary>
/// Случайная политика. holdSteps — сколько шагов держать одно действие:
/// при holdSteps=1 моторы дёргаются на частоте физики и в среднем стоят
/// на месте; при 10-15 возникают макро-движения — честный baseline.
/// </summary>
public sealed class RandomPolicy : IPolicy
{
    private readonly Random _rng;
    private readonly int _holdSteps;
    private readonly float[] _current;
    private int _counter;

    public RandomPolicy(int seed, int actionSize, int holdSteps = 10)
    {
        _rng = new(seed);
        _holdSteps = Math.Max(1, holdSteps);
        _current = new float[actionSize];
    }

    public float[] Act(float[] obs)
    {
        if (_counter++ % _holdSteps != 0)
        {
            return _current;
        }

        for (int i = 0; i < _current.Length; i++)
        {
            _current[i] = (float)(_rng.NextDouble() * 2.0 - 1.0);
        }
        return _current;
    }
}