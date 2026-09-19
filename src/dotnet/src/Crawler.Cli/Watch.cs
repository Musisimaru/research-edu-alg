using System.Numerics;
using Crawler.Core;
using Raylib_cs;

namespace Crawler.Cli;

/// <summary>
/// Окно Raylib: рендер среды в реальном времени.
/// policy == null  -> ручное управление (Q/A, W/S)
/// policy != null  -> действия берутся у политики, клавиши только R/Esc.
/// </summary>
public static class Watch
{
    private const int ScreenW = 1920;
    private const int ScreenH = 600;
    private const float Scale = 120f;      // пикселей на метр
    private const float GroundScreenY = 480f;

    public static void Run(IPolicy? policy, int seed = 0)
    {
        var env = new CrawlerEnv();
        var obs = env.Reset(seed);

        float totalReward = 0f;
        bool done = false;
        var actions = new float[env.ActionSize];

        Raylib.InitWindow(ScreenW, ScreenH, policy is null ? "Crawler — manual" : "Crawler — policy");
        Raylib.SetTargetFPS(60); // 1 кадр = 1 шаг физики (1/60 c) => реальное время

        while (!Raylib.WindowShouldClose())
        {
            // --- Ввод / действия ---
            if (policy is null)
            {
                actions[0] = Raylib.IsKeyDown(KeyboardKey.Q) ? 1f : Raylib.IsKeyDown(KeyboardKey.A) ? -1f : 0f;
                actions[1] = Raylib.IsKeyDown(KeyboardKey.W) ? 1f : Raylib.IsKeyDown(KeyboardKey.S) ? -1f : 0f;
            }
            else
            {
                actions = policy.Act(obs);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                obs = env.Reset(++seed);
                totalReward = 0f;
                done = false;
            }

            // --- Шаг физики ---
            if (!done)
            {
                (obs, float reward, done) = env.Step(actions);
                totalReward += reward;
            }

            // Камера следит по X за средним звеном
            float camX = env.Links[1].Position.X + 5;

            Vector2 ToScreen(float wx, float wy) =>
                new((wx - camX) * Scale + ScreenW / 2f, GroundScreenY - wy * Scale);

            // --- Рендер ---
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            // Пол
            Raylib.DrawLineEx(ToScreen(-50f, 0f), ToScreen(200f, 0f), 3f, Color.DarkGray);

            // Метровая разметка — чтобы движение было видно глазом
            for (int m = -50; m <= 200; m++)
            {
                var t = ToScreen(m, 0f);
                if (t.X < -20 || t.X > ScreenW + 20) continue;
                Raylib.DrawLine((int)t.X, (int)GroundScreenY, (int)t.X, (int)(GroundScreenY + 8), Color.Gray);
            }

            // Точка Б — флажок
            var flagBase = ToScreen(CrawlerEnv.TargetX, 0f);
            var flagTop = ToScreen(CrawlerEnv.TargetX, 1.2f);
            Raylib.DrawLineEx(flagBase, flagTop, 3f, Color.Red);
            Raylib.DrawTriangle(flagTop, flagTop + new Vector2(0, 25), flagTop + new Vector2(40, 12), Color.Red);

            // Звенья. Физический угол — CCW при Y-вверх; у raylib положительное
            // вращение по часовой и Y вниз, поэтому знак угла меняем.
            for (int i = 0; i < env.Links.Count; i++)
            {
                var link = env.Links[i];
                var c = ToScreen(link.Position.X, link.Position.Y);
                float w = CrawlerEnv.LinkLength * Scale;
                float h = CrawlerEnv.LinkThickness * Scale;

                var rect = new Rectangle(c.X, c.Y, w, h);
                var origin = new Vector2(w / 2f, h / 2f);
                float rotationDeg = -link.Rotation * (180f / MathF.PI);

                Raylib.DrawRectanglePro(rect, origin, rotationDeg, i == 1 ? Color.DarkBlue : Color.Blue);
            }

            // Суставы
            foreach (var joint in env.Joints)
            {
                var a = joint.WorldAnchorA;
                Raylib.DrawCircleV(ToScreen(a.X, a.Y), 5f, Color.Orange);
            }

            // --- HUD ---
            Raylib.DrawText(policy is null
                ? "Q/A — joint 1   W/S — joint 2   R — reset"
                : "policy mode   R — reset", 10, 10, 20, Color.Black);
            Raylib.DrawText($"step: {env.Steps}/{CrawlerEnv.MaxSteps}", 10, 40, 20, Color.Black);
            Raylib.DrawText($"x: {env.Links[1].Position.X:F2} m   target: {CrawlerEnv.TargetX} m", 10, 65, 20, Color.Black);
            Raylib.DrawText($"total reward: {totalReward:F3}", 10, 90, 20, Color.Black);
            Raylib.DrawText($"actions: {actions[0],5:F2} {actions[1],5:F2}", 10, 115, 20, Color.Black);

            if (done)
                Raylib.DrawText("DONE — press R to reset", ScreenW / 2 - 150, 30, 24, Color.Red);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}