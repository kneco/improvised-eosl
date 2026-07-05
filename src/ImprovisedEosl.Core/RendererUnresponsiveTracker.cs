namespace ImprovisedEosl.Core;

public sealed class RendererUnresponsiveTracker
{
    private readonly int _threshold;

    public RendererUnresponsiveTracker(int threshold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(threshold, 1);
        _threshold = threshold;
    }

    public int Count { get; private set; }

    public int Threshold => _threshold;

    public bool Observe()
    {
        Count++;
        return Count >= _threshold;
    }

    public void MarkResponsive()
    {
        Count = 0;
    }
}
