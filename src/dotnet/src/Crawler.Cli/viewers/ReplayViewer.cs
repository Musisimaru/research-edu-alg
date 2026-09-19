using Crawler.Core;
using Raylib_cs;
using static Crawler.Cli.Viewers.ReplayCommon;

namespace Crawler.Cli.Viewers;

/// <summary>
/// Реплей одного эпизода: записанная траектория, либо детерминированная
/// ре-симуляция (если траектории нет). Камера следит за червяком.
/// </summary>
public static class ReplayViewer
{
    private const int ScreenW = 1920;
    private const int ScreenH = 600;
    private const float Scale = 120f;
    private const float GroundY = 480f;

    public static void Run(string runDir, int? episodeId)
    {
        var episodes = ReplayLoader.LoadEpisodes(runDir);
        if (episodes.Count == 0)
        {
            Console.WriteLine($"No episodes in {runDir}");
            return;
        }

        var ep = episodeId is { } id
            ? episodes.FirstOrDefault(e => e.Id == id)
              ?? throw new ArgumentException($"Episode {id} not found in {runDir}")
            : episodes.MaxBy(e => e.Reward)!;

        var traj = ReplayLoader.GetTrajectory(runDir, ep, out bool resim);
        float replayedReward = traj.Sum(s => s.Reward);
        Console.WriteLine($"Episode #{ep.Id} (batch {ep.Batch}, seed {ep.Seed}): " +
                          $"recorded reward={ep.Reward:F4}, replayed reward={replayedReward:F4} " +
                          $"[{(resim ? "re-simulated" : "recorded")}]");

        int lastFrame = traj.Length - 1;
        int frame = 0, speed = 1;
        bool paused = false;

        Raylib.InitWindow(ScreenW, ScreenH, $"Crawler — replay #{ep.Id}");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            frame = UpdatePlayback(frame, lastFrame, ref paused, ref speed);
            var s = traj[frame];

            var cam = new Camera2Dish(s.X1 + 5f, Scale, GroundY, new Rectangle(0, 0, ScreenW, ScreenH));

            float cumReward = 0f;
            for (int i = 0; i <= frame; i++) cumReward += traj[i].Reward;

            var color = StatusColor(ep, frame, lastFrame, Color.Blue);
            var mid = StatusColor(ep, frame, lastFrame, Color.DarkBlue);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            DrawGroundAndFlag(cam);
            DrawWorm(cam, s, color, mid);

            Raylib.DrawText(PlaybackHelp, 10, 10, 20, Color.Black);
            Raylib.DrawText($"episode #{ep.Id}   seed {ep.Seed}   [{(resim ? "re-simulated" : "recorded")}]", 10, 40, 20, Color.Black);
            Raylib.DrawText($"frame: {frame}/{lastFrame}   speed: x{speed}{(paused ? "   PAUSED" : "")}", 10, 65, 20, Color.Black);
            Raylib.DrawText($"x: {s.X1:F2} m   target: {CrawlerEnv.TargetX} m", 10, 90, 20, Color.Black);
            Raylib.DrawText($"reward: {cumReward:F3} / {ep.Reward:F3}", 10, 115, 20, Color.Black);

            if (frame >= lastFrame)
            {
                var verdict = ep.Reached ? "REACHED TARGET" : "TIME OUT";
                Raylib.DrawText(verdict, ScreenW / 2 - 120, 30, 24, ep.Reached ? Color.Green : Color.Red);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
