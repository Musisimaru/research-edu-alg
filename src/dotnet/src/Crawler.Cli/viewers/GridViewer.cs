using Crawler.Core;
using Raylib_cs;
using static Crawler.Cli.Viewers.ReplayCommon;

namespace Crawler.Cli.Viewers;

/// <summary>
/// Сетка реплеев: окно делится на ячейки, в каждой — свой эпизод со своей камерой.
/// Общий счётчик кадров; финишировавшие ячейки замирают, рамка ячейки
/// зелёная/красная по исходу. BeginScissorMode не даёт ячейкам протекать.
/// </summary>
public static class GridViewer
{
    private const int ScreenW = 1920;
    private const int ScreenH = 1080;

    public static void Run(string runDir, int topN)
    {
        var episodes = ReplayLoader.LoadEpisodes(runDir);
        var selected = episodes.OrderByDescending(e => e.Reward).Take(topN).ToList();
        if (selected.Count == 0)
        {
            Console.WriteLine($"No episodes to show in {runDir}");
            return;
        }

        Console.WriteLine($"Loading {selected.Count} trajectories...");
        var cells = selected
            .Select(ep => (Ep: ep, Traj: ReplayLoader.GetTrajectory(runDir, ep)))
            .ToArray();

        int n = cells.Length;
        int cols = (int)Math.Ceiling(Math.Sqrt(n));
        int rows = (int)Math.Ceiling((double)n / cols);
        float cellW = (float)ScreenW / cols;
        float cellH = (float)ScreenH / rows;
        // Видимая высота мира на ячейку ~3.5 м => масштаб от высоты ячейки.
        float scale = cellH / 3.5f;

        int globalLast = cells.Max(c => c.Traj.Length - 1);
        int frame = 0, speed = 1;
        bool paused = false;

        Raylib.InitWindow(ScreenW, ScreenH, $"Crawler — grid ({n})");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            frame = UpdatePlayback(frame, globalLast, ref paused, ref speed);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            for (int i = 0; i < n; i++)
            {
                var (ep, traj) = cells[i];
                int last = traj.Length - 1;
                var s = traj[Math.Min(frame, last)];

                var viewport = new Rectangle((i % cols) * cellW, (i / cols) * cellH, cellW, cellH);
                var cam = new Camera2Dish(s.X1, scale, viewport.Y + viewport.Height * 0.8f, viewport);

                Raylib.BeginScissorMode((int)viewport.X, (int)viewport.Y, (int)viewport.Width, (int)viewport.Height);
                DrawGroundAndFlag(cam);
                var color = StatusColor(ep, frame, last, Color.Blue);
                var mid = StatusColor(ep, frame, last, Color.DarkBlue);
                DrawWorm(cam, s, color, mid);
                Raylib.DrawText($"#{ep.Id}  r={ep.Reward:F2}  x={s.X1:F2}", (int)viewport.X + 8, (int)viewport.Y + 6, 18, Color.Black);
                Raylib.EndScissorMode();

                // Рамка: серой пока играет, зелёной/красной по финишу
                var border = frame >= last
                    ? (ep.Reached ? Color.Green : Color.Red)
                    : Color.LightGray;
                Raylib.DrawRectangleLinesEx(viewport, frame >= last ? 3f : 1f, border);
            }

            Raylib.DrawText($"{PlaybackHelp}   frame: {frame}/{globalLast}   x{speed}{(paused ? "  PAUSED" : "")}",
                10, ScreenH - 28, 20, Color.DarkGray);
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
