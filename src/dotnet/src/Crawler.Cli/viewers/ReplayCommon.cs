using System.Numerics;
using Crawler.Core;
using Raylib_cs;

namespace Crawler.Cli.Viewers;

/// <summary>
/// Общий рендер для всех вьюверов реплеев: камера-на-ячейку, земля с разметкой,
/// флажок цели, отрисовка червяка из TrajectoryStep и единое правило подсветки:
/// дошёл до цели — зелёный, вышло время — красный.
/// Вьюверы — чистое воспроизведение: 1 TrajectoryStep = 1 кадр, физики в цикле нет.
/// </summary>
public static class ReplayCommon
{
    /// <summary>Камера/вьюпорт одной ячейки. GroundScreenY — экранный Y линии пола.</summary>
    public record struct Camera2Dish(float CamX, float Scale, float GroundScreenY, Rectangle Viewport);

    public static Vector2 ToScreen(in Camera2Dish cam, float wx, float wy) =>
        new(cam.Viewport.X + (wx - cam.CamX) * cam.Scale + cam.Viewport.Width / 2f,
            cam.GroundScreenY - wy * cam.Scale);

    public static void DrawGroundAndFlag(in Camera2Dish cam)
    {
        Raylib.DrawLineEx(ToScreen(cam, -50f, 0f), ToScreen(cam, 200f, 0f), 3f, Color.DarkGray);

        // Метровая разметка
        float left = cam.Viewport.X - 20f;
        float right = cam.Viewport.X + cam.Viewport.Width + 20f;
        for (int m = -50; m <= 200; m++)
        {
            var t = ToScreen(cam, m, 0f);
            if (t.X < left || t.X > right) continue;
            Raylib.DrawLine((int)t.X, (int)cam.GroundScreenY, (int)t.X, (int)(cam.GroundScreenY + 6), Color.Gray);
        }

        // Точка Б — флажок
        var flagBase = ToScreen(cam, CrawlerEnv.TargetX, 0f);
        var flagTop = ToScreen(cam, CrawlerEnv.TargetX, 1.2f);
        Raylib.DrawLineEx(flagBase, flagTop, 3f, Color.Red);
        float fs = cam.Scale / 120f; // масштаб флажка относительно базового 120 px/м
        Raylib.DrawTriangle(flagTop, flagTop + new Vector2(0, 25 * fs), flagTop + new Vector2(40 * fs, 12 * fs), Color.Red);
    }

    /// <summary>Червяк из одного шага траектории. mid — цвет среднего звена.</summary>
    public static void DrawWorm(in Camera2Dish cam, in TrajectoryStep s, Color body, Color mid)
    {
        Span<(float X, float Y, float R)> links = stackalloc (float, float, float)[]
        {
            (s.X0, s.Y0, s.R0), (s.X1, s.Y1, s.R1), (s.X2, s.Y2, s.R2),
        };

        for (int i = 0; i < 3; i++)
        {
            var c = ToScreen(cam, links[i].X, links[i].Y);
            float w = CrawlerEnv.LinkLength * cam.Scale;
            float h = CrawlerEnv.LinkThickness * cam.Scale;
            var rect = new Rectangle(c.X, c.Y, w, h);
            var origin = new Vector2(w / 2f, h / 2f);
            // Физический угол — CCW при Y-вверх; у raylib Y вниз => знак меняем.
            float rotationDeg = -links[i].R * (180f / MathF.PI);
            Raylib.DrawRectanglePro(rect, origin, rotationDeg, i == 1 ? mid : body);
        }
    }

    /// <summary>
    /// Единая точка правды подсветки: пока играет — базовый цвет;
    /// закончил и дошёл — зелёный; закончил по лимиту шагов — красный.
    /// </summary>
    public static Color StatusColor(EpisodeRecord ep, int frame, int lastFrame, Color playing)
    {
        if (frame < lastFrame) return playing;
        if (ep.Reached) return Color.Green;
        if (ep.Steps >= CrawlerEnv.MaxSteps) return Color.Red;
        return playing;
    }

    /// <summary>~8 различимых оттенков для режима призраков (alpha накладывает вызывающий).</summary>
    public static readonly Color[] GhostPalette =
    {
        new(0, 82, 172, 255),    // синий
        new(230, 126, 34, 255),  // оранжевый
        new(142, 68, 173, 255),  // фиолетовый
        new(22, 160, 133, 255),  // бирюзовый
        new(192, 57, 43, 255),   // кирпичный
        new(41, 128, 185, 255),  // голубой
        new(211, 84, 0, 255),    // тыквенный
        new(39, 174, 96, 255),   // изумрудный
    };

    public static Color WithAlpha(Color c, byte a) => new(c.R, c.G, c.B, a);

    /// <summary>
    /// Общее управление воспроизведением: Space — пауза, стрелки — кадр (в паузе),
    /// +/- — скорость (кадров за рендер), R — рестарт. Возвращает новый кадр.
    /// </summary>
    public static int UpdatePlayback(int frame, int lastFrame, ref bool paused, ref int speed)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space)) paused = !paused;
        if (Raylib.IsKeyPressed(KeyboardKey.R)) return 0;
        if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
            speed = Math.Min(speed * 2, 16);
        if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
            speed = Math.Max(speed / 2, 1);

        if (paused)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Right)) frame = Math.Min(frame + 1, lastFrame);
            if (Raylib.IsKeyPressed(KeyboardKey.Left)) frame = Math.Max(frame - 1, 0);
            return frame;
        }

        return Math.Min(frame + speed, lastFrame);
    }

    public const string PlaybackHelp = "Space — pause   ←/→ — frame   +/- — speed   R — restart   Esc — quit";
}
