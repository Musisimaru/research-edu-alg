using System.Globalization;
using System.Text.Json;
using Crawler.Core;

namespace Crawler.Cli.Report;

/// <summary>
/// Генерирует один самодостаточный HTML-дашборд по всем ранам:
/// кривые обучения (best/mean по env-шагам), распределения наград по батчам
/// (медиана + полоса p25–p75), таблица ранов. Данные инлайнятся JSON'ом,
/// графики — Chart.js с CDN (единственное место, где нужна сеть; при желании
/// библиотеку можно завендорить рядом и поменять src).
/// </summary>
public static class ReportGenerator
{
    public static string Generate(string runsRoot, string? outPath)
    {
        var runs = new List<object>();

        foreach (var dir in Directory.EnumerateDirectories(runsRoot).OrderBy(Path.GetFileName))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            var manifest = ReplayLoader.LoadManifest(dir);
            var name = Path.GetFileName(dir);

            // Кривая обучения из metrics.csv
            var curve = new List<object>();
            var metricsPath = Path.Combine(dir, "metrics.csv");
            if (File.Exists(metricsPath))
            {
                foreach (var line in File.ReadLines(metricsPath).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 3) continue;
                    curve.Add(new
                    {
                        steps = long.Parse(parts[0], CultureInfo.InvariantCulture),
                        best = float.Parse(parts[1], CultureInfo.InvariantCulture),
                        mean = float.Parse(parts[2], CultureInfo.InvariantCulture),
                    });
                }
            }

            // Распределения по батчам из episodes.jsonl
            var batches = new List<object>();
            int episodeCount = 0, reachedCount = 0;
            float bestReward = float.MinValue;
            var episodesPath = Path.Combine(dir, "episodes.jsonl");
            if (File.Exists(episodesPath))
            {
                var episodes = ReplayLoader.LoadEpisodes(dir);
                episodeCount = episodes.Count;
                reachedCount = episodes.Count(e => e.Reached);
                if (episodes.Count > 0) bestReward = episodes.Max(e => e.Reward);

                foreach (var group in episodes.GroupBy(e => e.Batch).OrderBy(g => g.Key))
                {
                    var sorted = group.Select(e => e.Reward).OrderBy(r => r).ToArray();
                    batches.Add(new
                    {
                        batch = group.Key,
                        min = sorted[0],
                        p25 = Percentile(sorted, 0.25f),
                        median = Percentile(sorted, 0.5f),
                        p75 = Percentile(sorted, 0.75f),
                        max = sorted[^1],
                        reached = group.Count(e => e.Reached),
                        count = sorted.Length,
                    });
                }
            }

            runs.Add(new
            {
                name,
                algo = manifest.Algo,
                started = manifest.StartedUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                commit = manifest.GitCommit,
                episodes = episodeCount,
                reached = reachedCount,
                best = bestReward == float.MinValue ? (float?)null : bestReward,
                curve,
                batches,
            });
        }

        var dataJson = JsonSerializer.Serialize(runs, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var html = Template.Replace("/*__DATA__*/[]", dataJson);
        outPath ??= Path.Combine(runsRoot, "report.html");
        File.WriteAllText(outPath, html);
        return outPath;
    }

    private static float Percentile(float[] sorted, float p)
    {
        if (sorted.Length == 1) return sorted[0];
        float pos = p * (sorted.Length - 1);
        int lo = (int)pos;
        int hi = Math.Min(lo + 1, sorted.Length - 1);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (pos - lo);
    }

    private const string Template = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Crawler runs</title>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4"></script>
<style>
  body { font-family: -apple-system, system-ui, sans-serif; margin: 24px auto; max-width: 1200px; color: #222; }
  h1 { font-size: 22px; } h2 { font-size: 17px; margin-top: 32px; }
  .chart-box { position: relative; height: 380px; margin-bottom: 16px; }
  table { border-collapse: collapse; width: 100%; font-size: 14px; }
  th, td { border: 1px solid #ddd; padding: 6px 10px; text-align: left; }
  th { background: #f5f5f5; }
  .muted { color: #888; }
</style>
</head>
<body>
<h1>Crawler — experiment runs</h1>

<h2>Learning curves (reward vs env steps)</h2>
<div class="chart-box"><canvas id="curves"></canvas></div>

<h2>Reward distribution per batch (median, p25–p75 band)</h2>
<div id="distributions"></div>

<h2>Runs</h2>
<table id="runsTable">
  <thead><tr><th>Run</th><th>Algo</th><th>Started (UTC)</th><th>Episodes</th><th>Best</th><th>% reached</th><th>Commit</th></tr></thead>
  <tbody></tbody>
</table>

<script>
const RUNS = /*__DATA__*/[];
const PALETTE = ['#0052ac','#e67e22','#8e44ad','#16a085','#c0392b','#2980b9','#d35400','#27ae60'];

// --- Learning curves: one best + one mean dataset per run ---
const curveDatasets = [];
RUNS.forEach((run, i) => {
  const color = PALETTE[i % PALETTE.length];
  curveDatasets.push({
    label: run.name + ' (best)', data: run.curve.map(p => ({x: p.steps, y: p.best})),
    borderColor: color, backgroundColor: color, pointRadius: 0, borderWidth: 2,
  });
  curveDatasets.push({
    label: run.name + ' (mean)', data: run.curve.map(p => ({x: p.steps, y: p.mean})),
    borderColor: color, backgroundColor: color, pointRadius: 0, borderWidth: 1, borderDash: [6, 4],
  });
});
new Chart(document.getElementById('curves'), {
  type: 'line',
  data: { datasets: curveDatasets },
  options: {
    responsive: true, maintainAspectRatio: false, animation: false,
    scales: {
      x: { type: 'linear', title: { display: true, text: 'total env steps' } },
      y: { title: { display: true, text: 'reward' } },
    },
    plugins: { legend: { labels: { boxWidth: 18 } } },
  },
});

// --- Per-run distribution charts ---
const distRoot = document.getElementById('distributions');
RUNS.forEach((run, i) => {
  if (!run.batches.length) return;
  const color = PALETTE[i % PALETTE.length];
  const h = document.createElement('h3'); h.textContent = run.name; distRoot.appendChild(h);
  const box = document.createElement('div'); box.className = 'chart-box'; distRoot.appendChild(box);
  const canvas = document.createElement('canvas'); box.appendChild(canvas);
  const labels = run.batches.map(b => b.batch);
  new Chart(canvas, {
    type: 'line',
    data: {
      labels,
      datasets: [
        { label: 'p75', data: run.batches.map(b => b.p75), borderColor: 'transparent',
          backgroundColor: color + '33', fill: '+2', pointRadius: 0 },
        { label: 'median', data: run.batches.map(b => b.median), borderColor: color,
          borderWidth: 2, pointRadius: 0 },
        { label: 'p25', data: run.batches.map(b => b.p25), borderColor: 'transparent', pointRadius: 0 },
        { label: 'max', data: run.batches.map(b => b.max), borderColor: color + '77',
          borderWidth: 1, borderDash: [3, 3], pointRadius: 0 },
        { label: 'min', data: run.batches.map(b => b.min), borderColor: color + '77',
          borderWidth: 1, borderDash: [3, 3], pointRadius: 0 },
      ],
    },
    options: {
      responsive: true, maintainAspectRatio: false, animation: false,
      scales: {
        x: { title: { display: true, text: 'batch / generation' } },
        y: { title: { display: true, text: 'reward' } },
      },
      plugins: { legend: { display: false } },
    },
  });
});

// --- Table ---
const tbody = document.querySelector('#runsTable tbody');
RUNS.forEach(run => {
  const tr = document.createElement('tr');
  const pct = run.episodes ? (100 * run.reached / run.episodes).toFixed(1) + '%' : '—';
  tr.innerHTML = `<td>${run.name}</td><td>${run.algo}</td><td>${run.started}</td>` +
    `<td>${run.episodes}</td><td>${run.best?.toFixed(3) ?? '—'}</td><td>${pct}</td>` +
    `<td class="muted">${run.commit ?? '—'}</td>`;
  tbody.appendChild(tr);
});
</script>
</body>
</html>
""";
}
