using System;

namespace Olympus.Services.Debug;

/// <summary>
/// Rolling per-frame wall-clock stats for the plugin's framework update (120-sample ring).
/// Record() is allocation-free; P95Ms allocates a sort copy and must only be read
/// while the debug window is open.
/// </summary>
public sealed class FrameTimeStats
{
    private readonly double[] _samplesMs = new double[120];
    private int _count;
    private int _next;

    public void Record(double elapsedMs)
    {
        _samplesMs[_next] = elapsedMs;
        _next = (_next + 1) % _samplesMs.Length;
        if (_count < _samplesMs.Length) _count++;
    }

    public double LastMs => _count == 0
        ? 0
        : _samplesMs[(_next - 1 + _samplesMs.Length) % _samplesMs.Length];

    public double P95Ms
    {
        get
        {
            if (_count == 0) return 0;
            var copy = new double[_count];
            Array.Copy(_samplesMs, copy, _count);
            Array.Sort(copy);
            return copy[(int)(0.95 * (_count - 1))];
        }
    }
}
