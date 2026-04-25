using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.HecateCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Black Mage rotation fires.
/// </summary>
public static class HecateAbilities
{
    // --- Fire family ---
    public static readonly AbilityBehavior Fire = new() { Action = BLMActions.Fire };
    public static readonly AbilityBehavior Fire3 = new() { Action = BLMActions.Fire3 };
    public static readonly AbilityBehavior Fire4 = new() { Action = BLMActions.Fire4 };
    public static readonly AbilityBehavior Despair = new() { Action = BLMActions.Despair, Toggle = cfg => cfg.BlackMage.EnableDespair };
    public static readonly AbilityBehavior Fire2 = new() { Action = BLMActions.Fire2, Toggle = cfg => cfg.BlackMage.EnableAoERotation };
    public static readonly AbilityBehavior HighFire2 = new() { Action = BLMActions.HighFire2, Toggle = cfg => cfg.BlackMage.EnableAoERotation };
    public static readonly AbilityBehavior Flare = new() { Action = BLMActions.Flare, Toggle = cfg => cfg.BlackMage.EnableAoERotation };
    public static readonly AbilityBehavior FlareStar = new() { Action = BLMActions.FlareStar, Toggle = cfg => cfg.BlackMage.EnableFlareStar };

    // --- Ice family ---
    public static readonly AbilityBehavior Blizzard = new() { Action = BLMActions.Blizzard };
    public static readonly AbilityBehavior Blizzard3 = new() { Action = BLMActions.Blizzard3 };
    public static readonly AbilityBehavior Blizzard4 = new() { Action = BLMActions.Blizzard4 };
    public static readonly AbilityBehavior Blizzard2 = new() { Action = BLMActions.Blizzard2, Toggle = cfg => cfg.BlackMage.EnableAoERotation };
    public static readonly AbilityBehavior HighBlizzard2 = new() { Action = BLMActions.HighBlizzard2, Toggle = cfg => cfg.BlackMage.EnableAoERotation };
    public static readonly AbilityBehavior Freeze = new() { Action = BLMActions.Freeze, Toggle = cfg => cfg.BlackMage.EnableAoERotation };

    // --- Thunder ---
    public static readonly AbilityBehavior Thunder = new() { Action = BLMActions.Thunder };
    public static readonly AbilityBehavior Thunder3 = new() { Action = BLMActions.Thunder3 };
    public static readonly AbilityBehavior HighThunder = new() { Action = BLMActions.HighThunder };
    public static readonly AbilityBehavior Thunder2 = new() { Action = BLMActions.Thunder2, Toggle = cfg => cfg.BlackMage.EnableAoERotation };
    public static readonly AbilityBehavior Thunder4 = new() { Action = BLMActions.Thunder4, Toggle = cfg => cfg.BlackMage.EnableAoERotation };
    public static readonly AbilityBehavior HighThunder2 = new() { Action = BLMActions.HighThunder2, Toggle = cfg => cfg.BlackMage.EnableAoERotation };

    // --- Polyglot ---
    public static readonly AbilityBehavior Xenoglossy = new() { Action = BLMActions.Xenoglossy, Toggle = cfg => cfg.BlackMage.EnableXenoglossy };
    public static readonly AbilityBehavior Foul = new() { Action = BLMActions.Foul, Toggle = cfg => cfg.BlackMage.EnableFoul };

    // --- Paradox / Transpose / Scathe ---
    public static readonly AbilityBehavior Paradox = new() { Action = BLMActions.Paradox, Toggle = cfg => cfg.BlackMage.EnableParadox };
    public static readonly AbilityBehavior Transpose = new() { Action = BLMActions.Transpose };
    public static readonly AbilityBehavior Scathe = new() { Action = BLMActions.Scathe };

    // --- oGCDs ---
    public static readonly AbilityBehavior Triplecast = new() { Action = BLMActions.Triplecast, Toggle = cfg => cfg.BlackMage.EnableTriplecast };
    public static readonly AbilityBehavior Manafont = new() { Action = BLMActions.Manafont, Toggle = cfg => cfg.BlackMage.EnableManafont };
    public static readonly AbilityBehavior Amplifier = new() { Action = BLMActions.Amplifier, Toggle = cfg => cfg.BlackMage.EnableAmplifier };
    public static readonly AbilityBehavior LeyLines = new() { Action = BLMActions.LeyLines, Toggle = cfg => cfg.BlackMage.EnableLeyLines };
}
