using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.ZeusCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Dragoon
/// rotation fires. Modules push these into the scheduler; the scheduler runs
/// gates and dispatches the winner.
/// </summary>
public static class ZeusAbilities
{
    // --- Single-target combo GCDs ---

    public static readonly AbilityBehavior TrueThrust = new()
    {
        Action = DRGActions.TrueThrust,
        Toggle = null,
    };

    public static readonly AbilityBehavior VorpalThrust = new()
    {
        Action = DRGActions.VorpalThrust,
        Toggle = null,
    };

    public static readonly AbilityBehavior Disembowel = new()
    {
        Action = DRGActions.Disembowel,
        Toggle = null,
    };

    // FullThrust → HeavensThrust at Lv.86
    public static readonly AbilityBehavior VorpalFinisher = new()
    {
        Action = DRGActions.FullThrust,
        Toggle = null,
        LevelReplacements = new[] { ((byte)86, DRGActions.HeavensThrust) },
    };

    // ChaosThrust → ChaoticSpring at Lv.86
    public static readonly AbilityBehavior DisembowelFinisher = new()
    {
        Action = DRGActions.ChaosThrust,
        Toggle = null,
        LevelReplacements = new[] { ((byte)86, DRGActions.ChaoticSpring) },
    };

    // --- Positional procs ---

    public static readonly AbilityBehavior FangAndClaw = new()
    {
        Action = DRGActions.FangAndClaw,
        Toggle = null,
        ProcBuff = DRGActions.StatusIds.FangAndClawBared,
    };

    public static readonly AbilityBehavior WheelingThrust = new()
    {
        Action = DRGActions.WheelingThrust,
        Toggle = null,
        ProcBuff = DRGActions.StatusIds.WheelInMotion,
    };

    public static readonly AbilityBehavior Drakesbane = new()
    {
        Action = DRGActions.Drakesbane,
        Toggle = null,
    };

    // --- AoE combo ---

    public static readonly AbilityBehavior DoomSpike = new()
    {
        Action = DRGActions.DoomSpike,
        Toggle = cfg => cfg.Dragoon.EnableAoERotation,
    };

    public static readonly AbilityBehavior SonicThrust = new()
    {
        Action = DRGActions.SonicThrust,
        Toggle = cfg => cfg.Dragoon.EnableAoERotation,
    };

    public static readonly AbilityBehavior CoerthanTorment = new()
    {
        Action = DRGActions.CoerthanTorment,
        Toggle = cfg => cfg.Dragoon.EnableAoERotation,
    };

    // --- Jump line (Jump → HighJump at Lv.74) ---

    public static readonly AbilityBehavior Jump = new()
    {
        Action = DRGActions.Jump,
        Toggle = cfg => cfg.Dragoon.EnableJumps,
        LevelReplacements = new[] { ((byte)74, DRGActions.HighJump) },
    };

    public static readonly AbilityBehavior MirageDive = new()
    {
        Action = DRGActions.MirageDive,
        Toggle = cfg => cfg.Dragoon.EnableMirageDive,
        ProcBuff = DRGActions.StatusIds.DiveReady,
    };

    public static readonly AbilityBehavior SpineshatterDive = new()
    {
        Action = DRGActions.SpineshatterDive,
        Toggle = cfg => cfg.Dragoon.EnableSpineshatterDive,
    };

    public static readonly AbilityBehavior DragonfireDive = new()
    {
        Action = DRGActions.DragonfireDive,
        Toggle = cfg => cfg.Dragoon.EnableDragonfireDive,
    };

    public static readonly AbilityBehavior RiseOfTheDragon = new()
    {
        Action = DRGActions.RiseOfTheDragon,
        Toggle = cfg => cfg.Dragoon.EnableDragonfireDive,
        ProcBuff = DRGActions.StatusIds.DraconianFire,
    };

    // --- Life of the Dragon ---

    public static readonly AbilityBehavior Geirskogul = new()
    {
        Action = DRGActions.Geirskogul,
        Toggle = cfg => cfg.Dragoon.EnableGeirskogul,
    };

    public static readonly AbilityBehavior Nastrond = new()
    {
        Action = DRGActions.Nastrond,
        Toggle = cfg => cfg.Dragoon.EnableNastrond,
    };

    public static readonly AbilityBehavior Stardiver = new()
    {
        Action = DRGActions.Stardiver,
        Toggle = cfg => cfg.Dragoon.EnableStardiver,
    };

    public static readonly AbilityBehavior Starcross = new()
    {
        Action = DRGActions.Starcross,
        Toggle = cfg => cfg.Dragoon.EnableStardiver,
        ProcBuff = DRGActions.StatusIds.StarcrossReady,
    };

    public static readonly AbilityBehavior WyrmwindThrust = new()
    {
        Action = DRGActions.WyrmwindThrust,
        Toggle = cfg => cfg.Dragoon.EnableWyrmwindThrust,
    };

    // --- Buffs ---

    public static readonly AbilityBehavior LifeSurge = new()
    {
        Action = DRGActions.LifeSurge,
        Toggle = cfg => cfg.Dragoon.EnableLifeSurge,
    };

    public static readonly AbilityBehavior LanceCharge = new()
    {
        Action = DRGActions.LanceCharge,
        Toggle = cfg => cfg.Dragoon.EnableLanceCharge,
    };

    public static readonly AbilityBehavior BattleLitany = new()
    {
        Action = DRGActions.BattleLitany,
        Toggle = cfg => cfg.Dragoon.EnableBattleLitany,
    };

    // --- Ranged filler ---

    public static readonly AbilityBehavior PiercingTalon = new()
    {
        Action = DRGActions.PiercingTalon,
        Toggle = null,
    };

    // --- Role ---
    public static readonly AbilityBehavior Feint = new() { Action = RoleActions.Feint, Toggle = cfg => cfg.Dragoon.EnableFeint };
}
