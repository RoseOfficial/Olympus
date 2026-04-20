using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.JobGauge.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.HephaestusCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Gunbreaker
/// rotation fires. Modules push these into the scheduler; the scheduler runs
/// gates and dispatches the winner.
/// </summary>
public static class GnbAbilities
{
    // --- Basic ST combo ---

    public static readonly AbilityBehavior KeenEdge = new()
    {
        Action = GNBActions.KeenEdge,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior BrutalShell = new()
    {
        Action = GNBActions.BrutalShell,
        Toggle = cfg => cfg.Tank.EnableDamage,
        AdjustedActionProbe = GNBActions.KeenEdge.ActionId,
    };

    public static readonly AbilityBehavior SolidBarrel = new()
    {
        Action = GNBActions.SolidBarrel,
        Toggle = cfg => cfg.Tank.EnableDamage,
        AdjustedActionProbe = GNBActions.KeenEdge.ActionId,
    };

    // --- AoE combo ---

    public static readonly AbilityBehavior DemonSlice = new()
    {
        Action = GNBActions.DemonSlice,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    public static readonly AbilityBehavior DemonSlaughter = new()
    {
        Action = GNBActions.DemonSlaughter,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
        AdjustedActionProbe = GNBActions.DemonSlice.ActionId,
    };

    // --- Gnashing Fang chain (AmmoComboStep drives it) ---

    public static readonly AbilityBehavior GnashingFang = new()
    {
        Action = GNBActions.GnashingFang,
        Toggle = cfg => cfg.Tank.EnableDamage,
        ComboStep = g => g.Get<GNBGauge>().AmmoComboStep == 0,
    };

    public static readonly AbilityBehavior SavageClaw = new()
    {
        Action = GNBActions.SavageClaw,
        Toggle = cfg => cfg.Tank.EnableDamage,
        ComboStep = g => g.Get<GNBGauge>().AmmoComboStep == 1,
    };

    public static readonly AbilityBehavior WickedTalon = new()
    {
        Action = GNBActions.WickedTalon,
        Toggle = cfg => cfg.Tank.EnableDamage,
        ComboStep = g => g.Get<GNBGauge>().AmmoComboStep == 2,
    };

    // --- Continuations (proc-driven) ---

    public static readonly AbilityBehavior JugularRip = new()
    {
        Action = GNBActions.JugularRip,
        Toggle = cfg => cfg.Tank.EnableContinuation,
        ProcBuff = GNBActions.StatusIds.ReadyToRip,
    };

    public static readonly AbilityBehavior AbdomenTear = new()
    {
        Action = GNBActions.AbdomenTear,
        Toggle = cfg => cfg.Tank.EnableContinuation,
        ProcBuff = GNBActions.StatusIds.ReadyToTear,
    };

    public static readonly AbilityBehavior EyeGouge = new()
    {
        Action = GNBActions.EyeGouge,
        Toggle = cfg => cfg.Tank.EnableContinuation,
        ProcBuff = GNBActions.StatusIds.ReadyToGouge,
    };

    public static readonly AbilityBehavior Hypervelocity = new()
    {
        Action = GNBActions.Hypervelocity,
        Toggle = cfg => cfg.Tank.EnableContinuation,
        ProcBuff = GNBActions.StatusIds.ReadyToBlast,
    };

    public static readonly AbilityBehavior FatedBrand = new()
    {
        Action = GNBActions.FatedBrand,
        Toggle = cfg => cfg.Tank.EnableContinuation,
        ProcBuff = GNBActions.StatusIds.ReadyToBrand,
    };

    // --- Cartridge spenders ---

    public static readonly AbilityBehavior BurstStrike = new()
    {
        Action = GNBActions.BurstStrike,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior FatedCircle = new()
    {
        Action = GNBActions.FatedCircle,
        Toggle = cfg => cfg.Tank.EnableAoEDamage,
    };

    public static readonly AbilityBehavior DoubleDown = new()
    {
        Action = GNBActions.DoubleDown,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    public static readonly AbilityBehavior SonicBreak = new()
    {
        Action = GNBActions.SonicBreak,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- Reign chain ---
    // Steps 2-3 (Noble Blood, Lion Heart) execute via base ReignOfBeasts ID — the game resolves
    // replacements internally. AmmoComboStep 3/4 tracks the step; NobleBlood/LionHeart
    // definitions are present for reference only (UseAction must receive the base ID).

    public static readonly AbilityBehavior ReignOfBeasts = new()
    {
        Action = GNBActions.ReignOfBeasts,
        Toggle = cfg => cfg.Tank.EnableDamage,
        ProcBuff = GNBActions.StatusIds.ReadyToReign,
    };

    public static readonly AbilityBehavior NobleBlood = new()
    {
        Action = GNBActions.NobleBlood,
        Toggle = cfg => cfg.Tank.EnableDamage,
        ComboStep = g => g.Get<GNBGauge>().AmmoComboStep == 3,
        ReplacementBaseId = GNBActions.ReignOfBeasts.ActionId,
    };

    public static readonly AbilityBehavior LionHeart = new()
    {
        Action = GNBActions.LionHeart,
        Toggle = cfg => cfg.Tank.EnableDamage,
        ComboStep = g => g.Get<GNBGauge>().AmmoComboStep == 4,
        ReplacementBaseId = GNBActions.ReignOfBeasts.ActionId,
    };

    // --- oGCD damage ---
    // DangerZone/BlastingZone: no EnableDamage toggle in the current module (TryBlastingZone
    // checks level and cooldown only). Toggle = null here; module-level gating is handled by
    // the EnableDamage check at TryExecute entry in DamageModule.

    public static readonly AbilityBehavior BlastingZone = new()
    {
        Action = GNBActions.DangerZone,
        Toggle = null,
        LevelReplacements = new[] { ((byte)80, GNBActions.BlastingZone) },
    };

    public static readonly AbilityBehavior BowShock = new()
    {
        Action = GNBActions.BowShock,
        Toggle = cfg => cfg.Tank.EnableBowShock,
    };

    public static readonly AbilityBehavior Trajectory = new()
    {
        Action = GNBActions.Trajectory,
        Toggle = cfg => cfg.Tank.EnableTrajectory,
    };

    // --- Buffs ---

    public static readonly AbilityBehavior NoMercy = new()
    {
        Action = GNBActions.NoMercy,
        Toggle = cfg => cfg.Tank.EnableNoMercy,
    };

    public static readonly AbilityBehavior Bloodfest = new()
    {
        Action = GNBActions.Bloodfest,
        Toggle = cfg => cfg.Tank.EnableBloodfest,
    };

    // --- Ranged pull ---

    public static readonly AbilityBehavior LightningShot = new()
    {
        Action = GNBActions.LightningShot,
        Toggle = cfg => cfg.Tank.EnableDamage,
    };

    // --- Defensives (self) ---

    public static readonly AbilityBehavior Nebula = new()
    {
        Action = GNBActions.Nebula,
        Toggle = cfg => cfg.Tank.EnableNebula,
        LevelReplacements = new[] { ((byte)92, GNBActions.GreatNebula) },
    };

    public static readonly AbilityBehavior Camouflage = new()
    {
        Action = GNBActions.Camouflage,
        Toggle = cfg => cfg.Tank.EnableCamouflage,
    };

    public static readonly AbilityBehavior Superbolide = new()
    {
        Action = GNBActions.Superbolide,
        Toggle = cfg => cfg.Tank.EnableSuperbolide,
    };

    public static readonly AbilityBehavior Aurora = new()
    {
        Action = GNBActions.Aurora,
        Toggle = cfg => cfg.Tank.EnableAurora,
    };

    // --- Party defensives ---

    public static readonly AbilityBehavior HeartOfLight = new()
    {
        Action = GNBActions.HeartOfLight,
        Toggle = cfg => cfg.Tank.EnableHeartOfLight,
    };

    public static readonly AbilityBehavior HeartOfCorundum = new()
    {
        Action = GNBActions.HeartOfStone,
        Toggle = cfg => cfg.Tank.EnableHeartOfCorundum,
        LevelReplacements = new[] { ((byte)82, GNBActions.HeartOfCorundum) },
    };

    // --- Enmity ---

    public static readonly AbilityBehavior RoyalGuard = new()
    {
        Action = GNBActions.RoyalGuard,
    };
}
