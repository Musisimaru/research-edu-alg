using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Joints;

namespace Crawler.Core;

/// <summary>
/// Трёхзвенный "червяк" на Aether.Physics2D.
/// Мир: метры, ось Y вверх, гравитация (0, -9.8).
/// На каждый Reset мир пересоздаётся с нуля — так проще гарантировать детерминизм.
/// </summary>
public sealed class CrawlerEnv : ICrawlerEnv
{
    // --- Геометрия и физика (метры, ньютоны) ---
    public const float LinkLength = 0.5f;
    public const float LinkThickness = 0.1f;
    public const float LinkDensity = 1.0f;
    public const float GroundFriction = 0.9f;
    public const float LinkFriction = 0.9f;

    public const float JointLimit = MathF.PI / 2f;   // ±90°
    public const float MaxMotorSpeed = 4f;           // рад/с — до этого масштабируется action
    public const float MaxMotorTorque = 60f;         // Н·м

    public const float TargetX = 8f;                 // точка Б
    public const int MaxSteps = 1500;                // лимит эпизода (25 сек при 60 Гц)
    public const float Dt = 1f / 60f;

    public int ObservationSize => 11;
    public int ActionSize => 2;

    private World _world = null!;
    private readonly Body[] _links = new Body[3];
    private readonly RevoluteJoint[] _joints = new RevoluteJoint[2];

    private int _steps;
    private float _prevX;

    // --- Доступ для рендера / отладки (только чтение) ---
    public IReadOnlyList<Body> Links => _links;
    public IReadOnlyList<RevoluteJoint> Joints => _joints;
    public int Steps => _steps;

    public float[] Reset(int seed)
    {
        _world = new World(new Vector2(0f, -9.8f));

        // Пол: длинная статичная кромка
        var ground = _world.CreateBody(Vector2.Zero, 0f, BodyType.Static);
        var groundFixture = ground.CreateEdge(new Vector2(-50f, 0f), new Vector2(200f, 0f));
        groundFixture.Friction = GroundFriction;

        // Три звена цепочкой, центр среднего в x=0, чуть выше пола
        float y = LinkThickness / 2f + 0.01f;
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * LinkLength;
            var body = _world.CreateBody(new Vector2(x, y), 0f, BodyType.Dynamic);
            var fixture = body.CreateRectangle(LinkLength, LinkThickness, LinkDensity, Vector2.Zero);
            fixture.Friction = LinkFriction;
            body.SleepingAllowed = false; // иначе моторы "не будят" уснувшие тела
            _links[i] = body;
        }

        // Два revolute-сустава на стыках звеньев, с моторами и лимитами
        for (int i = 0; i < 2; i++)
        {
            var anchor = new Vector2((i == 0 ? -1 : 1) * LinkLength / 2f, y); // мировые координаты стыка
            var joint = new RevoluteJoint(_links[i], _links[i + 1], anchor, useWorldCoordinates: true)
            {
                LimitEnabled = true,
                LowerLimit = -JointLimit,
                UpperLimit = JointLimit,
                MotorEnabled = true,
                MaxMotorTorque = MaxMotorTorque,
                MotorSpeed = 0f,
            };
            _world.Add(joint);
            _joints[i] = joint;
        }

        // Маленький случайный "пинок", чтобы сломать идеальную симметрию.
        // Весь рандом — только от seed => воспроизводимость.
        var rng = new Random(seed);
        foreach (var link in _links)
            link.ApplyAngularImpulse((float)(rng.NextDouble() - 0.5) * 0.02f);

        _steps = 0;
        _prevX = BodyX();
        return BuildObservation();
    }

    public (float[] Obs, float Reward, bool Done) Step(float[] actions)
    {
        if (_world is null)
            throw new InvalidOperationException("Call Reset() before Step().");

        for (int i = 0; i < 2; i++)
        {
            float a = Math.Clamp(actions[i], -1f, 1f);
            _joints[i].MotorSpeed = a * MaxMotorSpeed;
        }

        _world.Step(Dt);
        _steps++;

        float x = BodyX();
        float progress = x - _prevX;
        _prevX = x;

        float energyPenalty = 0f;
        for (int i = 0; i < 2; i++)
        {
            float a = Math.Clamp(actions[i], -1f, 1f);
            energyPenalty += a * a;
        }

        float reward = progress - 0.0005f * energyPenalty;

        bool reached = x >= TargetX;
        if (reached) reward += 10f;

        bool done = reached || _steps >= MaxSteps;
        return (BuildObservation(), reward, done);
    }

    private float BodyX() => _links[1].Position.X;

    /// <summary>
    /// 11 наблюдений, все грубо нормированы к ~[-1, 1]:
    /// [0..1]  углы суставов / (π/2)
    /// [2..3]  скорости суставов / MaxMotorSpeed
    /// [4]     угол среднего звена / π
    /// [5]     угловая скорость среднего звена / 10
    /// [6..7]  линейная скорость среднего звена (x, y) / 5
    /// [8]     высота центра среднего звена / 1
    /// [9]     остаток пути до Б по x / TargetX (кламп в [-1, 1])
    /// [10]    знак направления до Б (+1 — Б справа)
    /// </summary>
    private float[] BuildObservation()
    {
        var body = _links[1];
        float dx = TargetX - body.Position.X;

        return new float[]
        {
            _joints[0].JointAngle / JointLimit,
            _joints[1].JointAngle / JointLimit,
            _joints[0].JointSpeed / MaxMotorSpeed,
            _joints[1].JointSpeed / MaxMotorSpeed,
            NormalizeAngle(body.Rotation) / MathF.PI,
            body.AngularVelocity / 10f,
            body.LinearVelocity.X / 5f,
            body.LinearVelocity.Y / 5f,
            body.Position.Y / 1f,
            Math.Clamp(dx / TargetX, -1f, 1f),
            MathF.Sign(dx),
        };
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        return angle;
    }
}