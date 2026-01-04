namespace Olympus.Config;

/// <summary>
/// Configuration for DoT spells (Aero/Dia progression).
/// </summary>
public sealed class DotConfig
{
    public bool EnableAero { get; set; } = true;
    public bool EnableAeroII { get; set; } = true;
    public bool EnableDia { get; set; } = true;
}
