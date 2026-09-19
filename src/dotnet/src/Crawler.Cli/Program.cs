using System.Numerics;
using Crawler.Cli;
using Crawler.Core;


// ============================================================
// Просмотрщик: ручное управление червяком.
//   Q / A — мотор первого сустава (вперёд / назад)
//   W / S — мотор второго сустава
//   R     — reset
//   Esc   — выход
// Пока это единственный режим; позже сюда добавятся
// субкоманды train-ga / train-neat / train-ppo / watch <brain>.
// ============================================================

//Watch.Run(null);


// Субкоманды:
//   (пусто) | watch   — окно, ручное управление
//   watch-random      — окно, случайная политика
//   random            — headless baseline: random policy + CSV в runs/
//
// Позже добавятся: train-ga, train-neat, train-ppo, watch <brain.json>, compare.
 
switch (args.FirstOrDefault() ?? "watch")
{
    case "watch":
        Watch.Run(policy: null);
        break;
 
    case "watch-random":
        Watch.Run(new RandomPolicy(seed: 1, actionSize: 2, holdSteps: 10));
        break;
 
    case "random":
        RandomBaseline.Run();
        break;
 
    default:
        Console.WriteLine("Usage: crawler [watch|watch-random|random]");
        break;
}
