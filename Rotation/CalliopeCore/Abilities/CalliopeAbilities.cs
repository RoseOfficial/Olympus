using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.CalliopeCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Bard rotation fires.
/// </summary>
public static class CalliopeAbilities
{
    // --- Filler / starter shots ---
    public static readonly AbilityBehavior HeavyShot = new() { Action = BRDActions.HeavyShot };
    public static readonly AbilityBehavior BurstShot = new() { Action = BRDActions.BurstShot };

    // --- Proc shots ---
    public static readonly AbilityBehavior StraightShot = new() { Action = BRDActions.StraightShot, Toggle = cfg => cfg.Bard.EnableRefulgentArrow };
    public static readonly AbilityBehavior RefulgentArrow = new() { Action = BRDActions.RefulgentArrow, Toggle = cfg => cfg.Bard.EnableRefulgentArrow };

    // --- DoTs ---
    public static readonly AbilityBehavior VenomousBite = new() { Action = BRDActions.VenomousBite, Toggle = cfg => cfg.Bard.EnableCausticBite };
    public static readonly AbilityBehavior CausticBite = new() { Action = BRDActions.CausticBite, Toggle = cfg => cfg.Bard.EnableCausticBite };
    public static readonly AbilityBehavior Windbite = new() { Action = BRDActions.Windbite, Toggle = cfg => cfg.Bard.EnableStormbite };
    public static readonly AbilityBehavior Stormbite = new() { Action = BRDActions.Stormbite, Toggle = cfg => cfg.Bard.EnableStormbite };
    public static readonly AbilityBehavior IronJaws = new() { Action = BRDActions.IronJaws, Toggle = cfg => cfg.Bard.EnableIronJaws };

    // --- Apex chain ---
    public static readonly AbilityBehavior ApexArrow = new() { Action = BRDActions.ApexArrow, Toggle = cfg => cfg.Bard.EnableApexArrow };
    public static readonly AbilityBehavior BlastArrow = new() { Action = BRDActions.BlastArrow, Toggle = cfg => cfg.Bard.EnableBlastArrow };

    // --- Burst follow-ups ---
    public static readonly AbilityBehavior ResonantArrow = new() { Action = BRDActions.ResonantArrow, Toggle = cfg => cfg.Bard.EnableResonantArrow };
    public static readonly AbilityBehavior RadiantEncore = new() { Action = BRDActions.RadiantEncore, Toggle = cfg => cfg.Bard.EnableRadiantEncore };

    // --- AoE GCDs ---
    public static readonly AbilityBehavior QuickNock = new() { Action = BRDActions.QuickNock, Toggle = cfg => cfg.Bard.EnableAoERotation };
    public static readonly AbilityBehavior Ladonsbite = new() { Action = BRDActions.Ladonsbite, Toggle = cfg => cfg.Bard.EnableAoERotation };
    public static readonly AbilityBehavior Shadowbite = new() { Action = BRDActions.Shadowbite, Toggle = cfg => cfg.Bard.EnableAoERotation };
    public static readonly AbilityBehavior WideVolley = new() { Action = BRDActions.WideVolley, Toggle = cfg => cfg.Bard.EnableAoERotation };

    // --- Songs ---
    public static readonly AbilityBehavior MagesBallad = new() { Action = BRDActions.MagesBallad, Toggle = cfg => cfg.Bard.EnableSongRotation };
    public static readonly AbilityBehavior ArmysPaeon = new() { Action = BRDActions.ArmysPaeon, Toggle = cfg => cfg.Bard.EnableSongRotation };
    public static readonly AbilityBehavior WanderersMinuet = new() { Action = BRDActions.WanderersMinuet, Toggle = cfg => cfg.Bard.EnableSongRotation };

    // --- oGCDs ---
    public static readonly AbilityBehavior Bloodletter = new() { Action = BRDActions.Bloodletter, Toggle = cfg => cfg.Bard.EnableBloodletter };
    public static readonly AbilityBehavior HeartbreakShot = new() { Action = BRDActions.HeartbreakShot, Toggle = cfg => cfg.Bard.EnableBloodletter };
    public static readonly AbilityBehavior RainOfDeath = new() { Action = BRDActions.RainOfDeath, Toggle = cfg => cfg.Bard.EnableBloodletter };
    public static readonly AbilityBehavior EmpyrealArrow = new() { Action = BRDActions.EmpyrealArrow, Toggle = cfg => cfg.Bard.EnableEmpyrealArrow };
    public static readonly AbilityBehavior Sidewinder = new() { Action = BRDActions.Sidewinder, Toggle = cfg => cfg.Bard.EnableSidewinder };
    public static readonly AbilityBehavior PitchPerfect = new() { Action = BRDActions.PitchPerfect, Toggle = cfg => cfg.Bard.EnablePitchPerfect };

    // --- Buffs ---
    public static readonly AbilityBehavior RagingStrikes = new() { Action = BRDActions.RagingStrikes, Toggle = cfg => cfg.Bard.EnableRagingStrikes };
    public static readonly AbilityBehavior BattleVoice = new() { Action = BRDActions.BattleVoice, Toggle = cfg => cfg.Bard.EnableBattleVoice };
    public static readonly AbilityBehavior RadiantFinale = new() { Action = BRDActions.RadiantFinale, Toggle = cfg => cfg.Bard.EnableRadiantFinale };
    public static readonly AbilityBehavior Barrage = new() { Action = BRDActions.Barrage, Toggle = cfg => cfg.Bard.EnableBarrage };

    // --- Role ---
    public static readonly AbilityBehavior HeadGraze = new() { Action = RoleActions.HeadGraze, Toggle = cfg => cfg.RangedShared.EnableHeadGraze };
}
