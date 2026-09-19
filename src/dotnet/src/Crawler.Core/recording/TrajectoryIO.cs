using System.Text;

namespace Crawler.Core;

/// <summary>
/// Бинарный формат .traj (little-endian):
///   "CTRJ" | int32 version | int32 stepCount | stepCount * 12 float32
/// (см. TrajectoryStep — порядок полей совпадает). ~48 байт на шаг.
/// </summary>
public static class TrajectoryIO
{
    private static readonly byte[] Magic = "CTRJ"u8.ToArray();
    private const int Version = 1;

    public static void Write(string path, IReadOnlyList<TrajectoryStep> steps)
    {
        using var w = new BinaryWriter(File.Create(path), Encoding.UTF8);
        w.Write(Magic);
        w.Write(Version);
        w.Write(steps.Count);
        foreach (var s in steps)
        {
            w.Write(s.X0); w.Write(s.Y0); w.Write(s.R0);
            w.Write(s.X1); w.Write(s.Y1); w.Write(s.R1);
            w.Write(s.X2); w.Write(s.Y2); w.Write(s.R2);
            w.Write(s.A0); w.Write(s.A1);
            w.Write(s.Reward);
        }
    }

    public static TrajectoryStep[] Read(string path)
    {
        using var r = new BinaryReader(File.OpenRead(path), Encoding.UTF8);
        var magic = r.ReadBytes(4);
        if (!magic.AsSpan().SequenceEqual(Magic))
            throw new InvalidDataException($"Not a trajectory file: {path}");
        int version = r.ReadInt32();
        if (version != Version)
            throw new InvalidDataException($"Unsupported trajectory version {version} in {path}");

        int count = r.ReadInt32();
        var steps = new TrajectoryStep[count];
        for (int i = 0; i < count; i++)
        {
            steps[i] = new TrajectoryStep(
                r.ReadSingle(), r.ReadSingle(), r.ReadSingle(),
                r.ReadSingle(), r.ReadSingle(), r.ReadSingle(),
                r.ReadSingle(), r.ReadSingle(), r.ReadSingle(),
                r.ReadSingle(), r.ReadSingle(),
                r.ReadSingle());
        }
        return steps;
    }
}
