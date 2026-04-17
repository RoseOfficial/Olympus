namespace Olympus.Config;

public sealed class ActionFeedConfig
{
    public bool IsVisible { get; set; } = true;
    public float X { get; set; } = 400f;
    public float Y { get; set; } = 200f;
    public int MaxIcons { get; set; } = 6;
    public float DurationSeconds { get; set; } = 2.0f;
    public float IconSize { get; set; } = 40f;
    public bool ShowLabels { get; set; } = false;
}
