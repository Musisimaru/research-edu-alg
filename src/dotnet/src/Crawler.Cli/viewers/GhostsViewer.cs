using Crawler.Core;
using Raylib_cs;
using static Crawler.Cli.Viewers.ReplayCommon;

namespace Crawler.Cli.Viewers;

/// <summary>
/// «Призраки»: несколько эпизодов в одном мире полупрозрачными цветами.
/// Камера следит за лидером по X среди ещё играющих. Закончившие замирают
/// на последнем кадре и тонируются зелёным/красным по исходу.
/// </summary>
public static class GhostsViewer
{
    private const int ScreenW = 1920;
    private const int ScreenH = 600;
    private const float Scale = 120f;
    private const float GroundY = 480f;
    private const byte GhostAlpha = 130;

    public static void Run(string runDir, int topN, int[]? explicitIds)
    {
        var episodes = ReplayLoader.LoadEpisodes(runDir);
        var selected = explicitIds is { Length: > 0 }
            ? explicitIds.Select(id => episodes.FirstOrDefault(e => e.Id == id)
                ?? throw new ArgumentException($"Episode {id} not found in {runDir}")).ToList()
            : episodes.OrderByDescending(e => e.Reward).Take(topN).ToList();

        if (selected.Count == 0)
        {
            Console.WriteLine($"No episodes to show in {runDir}");
            return;
        }

        Console.WriteLine($"Loading {selected.Count} trajectories...");
        var ghosts = selected
            .Select((ep, i) =>
            {
                var traj = ReplayLoader.GetTrajectory(runDir, ep, out bool resim);
                return (Ep: ep, Traj: traj, Color: GhostPalette[i % GhostPalette.Length], Resim: resim);
            })
            .ToArray();

        int globalLast = ghosts.Max(g => g.Traj.Length - 1);
        int frame = 0, speed = 1;
        bool paused = false;

        Raylib.InitWindow(ScreenW, ScreenH, $"Crawler — ghosts ({ghosts.Length})");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            frame = UpdatePlayback(frame, globalLast, ref paused, ref speed);

            // Камера — за лидером среди играющих; все финишировали => за лучшим финальным X.
            float camTargetX = float.MinValue;
            bool anyPlaying = false;
            foreach (var g in ghosts)
            {
                int last = g.Traj.Length - 1;
                if (frame < last)
                {
                    anyPlaying = true;
                    camTargetX = Math.Max(camTargetX, g.Traj[frame].X1);
                }
            }
            if (!anyPlaying)
                camTargetX = ghosts.Max(g => g.Traj[^1].X1);

            var cam = new Camera2Dish(camTargetX + 5f, Scale, GroundY, new Rectangle(0, 0, ScreenW, ScreenH));

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            DrawGroundAndFlag(cam);

            for (int i = 0; i < ghosts.Length; i++)
            {
                var g = ghosts[i];
                int last = g.Traj.Length - 1;
                var s = g.Traj[Math.Min(frame, last)];
                var baseColor = StatusColor(g.Ep, frame, last, g.Color);
                var body = WithAlpha(baseColor, GhostAlpha);
                DrawWorm(cam, s, body, body);
            }

            // HUD + легенда
            Raylib.DrawText(PlaybackHelp, 10, 10, 20, Color.Black);
            Raylib.DrawText($"frame: {frame}/{globalLast}   speed: x{speed}{(paused ? "   PAUSED" : "")}", 10, 40, 20, Color.Black);
            for (int i = 0; i < ghosts.Length; i++)
            {
                var g = ghosts[i];
                int y = 70 + i * 24;
                Raylib.DrawRectangle(10, y + 4, 14, 14, g.Color);
                var status = frame >= g.Traj.Length - 1 ? (g.Ep.Reached ? "  reached" : "  timeout") : "";
                Raylib.DrawText($"#{g.Ep.Id}  r={g.Ep.Reward:F2}{(g.Resim ? " (resim)" : "")}{status}", 32, y, 20, Color.Black);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
