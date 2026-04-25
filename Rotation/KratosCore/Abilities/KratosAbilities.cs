using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.KratosCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Monk
/// rotation fires. Modules push these into the scheduler; the scheduler runs
/// gates and dispatches the winner.
/// </summary>
public static class KratosAbilities
{
    // --- Opo-opo form GCDs ---
    public static readonly AbilityBehavior DragonKick = new() { Action = MNKActions.DragonKick };
    public static readonly AbilityBehavior Bootshine = new() { Action = MNKActions.Bootshine };
    public static readonly AbilityBehavior LeapingOpo = new() { Action = MNKActions.LeapingOpo };

    // --- Raptor form GCDs ---
    public static readonly AbilityBehavior TwinSnakes = new() { Action = MNKActions.TwinSnakes };
    public static readonly AbilityBehavior TrueStrike = new() { Action = MNKActions.TrueStrike };
    public static readonly AbilityBehavior RisingRaptor = new() { Action = MNKActions.RisingRaptor };

    // --- Coeurl form GCDs ---
    public static readonly AbilityBehavior Demolish = new() { Action = MNKActions.Demolish };
    public static readonly AbilityBehavior SnapPunch = new() { Action = MNKActions.SnapPunch };
    public static readonly AbilityBehavior PouncingCoeurl = new() { Action = MNKActions.PouncingCoeurl };

    // --- AoE form GCDs ---
    public static readonly AbilityBehavior ArmOfTheDestroyer = new() { Action = MNKActions.ArmOfTheDestroyer, Toggle = cfg => cfg.Monk.EnableAoERotation };
    public static readonly AbilityBehavior ShadowOfTheDestroyer = new() { Action = MNKActions.ShadowOfTheDestroyer, Toggle = cfg => cfg.Monk.EnableAoERotation };
    public static readonly AbilityBehavior FourPointFury = new() { Action = MNKActions.FourPointFury, Toggle = cfg => cfg.Monk.EnableAoERotation };
    public static readonly AbilityBehavior Rockbreaker = new() { Action = MNKActions.Rockbreaker, Toggle = cfg => cfg.Monk.EnableAoERotation };

    // --- Rumination procs ---
    public static readonly AbilityBehavior FiresReply = new()
    {
        Action = MNKActions.FiresReply,
        Toggle = cfg => cfg.Monk.EnableFiresReply,
    };

    public static readonly AbilityBehavior WindsReply = new()
    {
        Action = MNKActions.WindsReply,
        Toggle = cfg => cfg.Monk.EnableWindsReply,
    };

    // --- Masterful Blitz finishers (each used directly by ID via base Action) ---
    public static readonly AbilityBehavior ElixirField = new() { Action = MNKActions.ElixirField, Toggle = cfg => cfg.Monk.EnableMasterfulBlitz };
    public static readonly AbilityBehavior FlintStrike = new() { Action = MNKActions.FlintStrike, Toggle = cfg => cfg.Monk.EnableMasterfulBlitz };
    public static readonly AbilityBehavior RisingPhoenix = new() { Action = MNKActions.RisingPhoenix, Toggle = cfg => cfg.Monk.EnableMasterfulBlitz };
    public static readonly AbilityBehavior PhantomRush = new() { Action = MNKActions.PhantomRush, Toggle = cfg => cfg.Monk.EnableMasterfulBlitz };
    public static readonly AbilityBehavior CelestialRevolution = new() { Action = MNKActions.CelestialRevolution, Toggle = cfg => cfg.Monk.EnableMasterfulBlitz };
    public static readonly AbilityBehavior ElixirBurst = new() { Action = MNKActions.ElixirBurst, Toggle = cfg => cfg.Monk.EnableMasterfulBlitz };

    // --- oGCDs ---
    public static readonly AbilityBehavior SteelPeak = new() { Action = MNKActions.SteelPeak, Toggle = cfg => cfg.Monk.EnableChakraSpenders };
    public static readonly AbilityBehavior TheForbiddenChakra = new() { Action = MNKActions.TheForbiddenChakra, Toggle = cfg => cfg.Monk.EnableChakraSpenders };
    public static readonly AbilityBehavior HowlingFist = new() { Action = MNKActions.HowlingFist, Toggle = cfg => cfg.Monk.EnableChakraSpenders };
    public static readonly AbilityBehavior Enlightenment = new() { Action = MNKActions.Enlightenment, Toggle = cfg => cfg.Monk.EnableChakraSpenders };
    public static readonly AbilityBehavior Thunderclap = new() { Action = MNKActions.Thunderclap, Toggle = cfg => cfg.Monk.EnableThunderclap };

    // --- Buffs ---
    public static readonly AbilityBehavior RiddleOfFire = new() { Action = MNKActions.RiddleOfFire, Toggle = cfg => cfg.Monk.EnableRiddleOfFire };
    public static readonly AbilityBehavior Brotherhood = new() { Action = MNKActions.Brotherhood, Toggle = cfg => cfg.Monk.EnableBrotherhood };
    public static readonly AbilityBehavior PerfectBalance = new() { Action = MNKActions.PerfectBalance, Toggle = cfg => cfg.Monk.EnablePerfectBalance };
    public static readonly AbilityBehavior RiddleOfWind = new() { Action = MNKActions.RiddleOfWind, Toggle = cfg => cfg.Monk.EnableRiddleOfWind };

    // --- Six-Sided Star ---
    public static readonly AbilityBehavior SixSidedStar = new() { Action = MNKActions.SixSidedStar, Toggle = cfg => cfg.Monk.EnableSixSidedStar };

    // --- Downtime ---
    public static readonly AbilityBehavior Meditation = new() { Action = MNKActions.Meditation };
}
