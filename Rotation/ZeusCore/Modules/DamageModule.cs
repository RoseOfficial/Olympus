using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.ZeusCore.Modules;

/// <summary>
/// Handles the Dragoon damage rotation.
/// Manages combo execution, jump weaving, Life of the Dragon, and burst windows.
/// Extends BaseDpsDamageModule for shared damage module patterns.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<IZeusContext>, IZeusModule
{
    #region Abstract Method Implementations

    /// <summary>
    /// Melee targeting range (3y) — used as fallback only.
    /// </summary>
    protected override float GetTargetingRange() => FFXIVConstants.MeleeTargetingRange;

    /// <summary>
    /// Use True Thrust to check melee range via game API for maximum accuracy.
    /// </summary>
    protected override uint GetRangeCheckActionId() => DRGActions.TrueThrust.ActionId;

    /// <summary>
    /// AoE count range for DRG (5y for melee AoE abilities).
    /// </summary>
    protected override float GetAoECountRange() => 5f;

    /// <summary>
    /// Sets the damage state in the debug display.
    /// </summary>
    protected override void SetDamageState(IZeusContext context, string state) =>
        context.Debug.DamageState = state;

    /// <summary>
    /// Sets the nearby enemy count in the debug display.
    /// </summary>
    protected override void SetNearbyEnemies(IZeusContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    /// <summary>
    /// Sets the planned action name in the debug display.
    /// </summary>
    protected override void SetPlannedAction(IZeusContext context, string action) =>
        context.Debug.PlannedAction = action;

    /// <summary>
    /// oGCD damage for Dragoon - Life of the Dragon abilities, jumps, etc.
    /// </summary>
    protected override bool TryOgcdDamage(IZeusContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Mirage Dive (consume Dive Ready)
        if (TryMirageDive(context, target))
            return true;

        // Priority 2: Starcross (after Stardiver)
        if (TryStarcross(context, target))
            return true;

        // Priority 3: Rise of the Dragon (after Dragonfire Dive)
        if (TryRiseOfTheDragon(context, target))
            return true;

        // Priority 4: Wyrmwind Thrust (2 Firstmind stacks)
        if (TryWyrmwindThrust(context, target))
            return true;

        // Priority 5: Life of the Dragon actions
        if (context.IsLifeOfDragonActive)
        {
            // Nastrond (spam during Life)
            if (TryNastrond(context, target))
                return true;

            // Stardiver (Life finisher)
            if (TryStardiver(context, target))
                return true;
        }

        // Priority 6: Geirskogul (Life activation or just damage)
        if (TryGeirskogul(context, target))
            return true;

        // Priority 7: High Jump / Jump
        if (TryJump(context, target))
            return true;

        // Priority 8: Spineshatter Dive (gap closer / damage)
        if (TrySpineshatterDive(context, target))
            return true;

        // Priority 9: Dragonfire Dive (AoE damage)
        if (TryDragonfireDive(context, target))
            return true;

        return false;
    }

    private bool TryMirageDive(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.MirageDive.MinLevel)
            return false;

        if (!context.HasDiveReady)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.MirageDive.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.MirageDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.MirageDive.Name;
            context.Debug.DamageState = "Mirage Dive";
            return true;
        }

        return false;
    }

    private bool TryStarcross(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Starcross.MinLevel)
            return false;

        if (!context.HasStarcrossReady)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Starcross.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.Starcross, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Starcross.Name;
            context.Debug.DamageState = "Starcross";
            return true;
        }

        return false;
    }

    private bool TryRiseOfTheDragon(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.RiseOfTheDragon.MinLevel)
            return false;

        if (!context.HasDraconianFire)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.RiseOfTheDragon.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.RiseOfTheDragon, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.RiseOfTheDragon.Name;
            context.Debug.DamageState = "Rise of the Dragon";
            return true;
        }

        return false;
    }

    private bool TryWyrmwindThrust(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.WyrmwindThrust.MinLevel)
            return false;

        // Need 2 Firstmind's Focus
        if (context.FirstmindsFocus < 2)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.WyrmwindThrust.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.WyrmwindThrust, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.WyrmwindThrust.Name;
            context.Debug.DamageState = $"Wyrmwind Thrust ({context.FirstmindsFocus} focus)";
            return true;
        }

        return false;
    }

    private bool TryNastrond(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Nastrond.MinLevel)
            return false;

        if (!context.IsLifeOfDragonActive)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Nastrond.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.Nastrond, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Nastrond.Name;
            context.Debug.DamageState = $"Nastrond (Life: {context.LifeOfDragonRemaining:F1}s)";
            return true;
        }

        return false;
    }

    private bool TryStardiver(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Stardiver.MinLevel)
            return false;

        if (!context.IsLifeOfDragonActive)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Stardiver.ActionId))
            return false;

        // Use Stardiver during Life window
        // Prefer using it after Nastronds when possible
        if (context.ActionService.ExecuteOgcd(DRGActions.Stardiver, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Stardiver.Name;
            context.Debug.DamageState = $"Stardiver (Life: {context.LifeOfDragonRemaining:F1}s)";

            // Training: Record Stardiver decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.Stardiver.ActionId, DRGActions.Stardiver.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue)
                .Reason($"Stardiver during Life of Dragon ({context.LifeOfDragonRemaining:F1}s remaining)",
                    "Stardiver is DRG's highest potency single attack, available only during Life of the Dragon. " +
                    "This massive dive attack deals enormous AoE damage. At Lv.100, it also grants Starcross Ready " +
                    "for a follow-up attack. Time it within your Life window after using some Nastronds.")
                .Factors(new[] {
                    $"Life of Dragon active ({context.LifeOfDragonRemaining:F1}s)",
                    "Highest potency attack",
                    "Grants Starcross Ready at Lv.100"
                })
                .Alternatives(new[] { "Wait for more Nastronds (risk Life expiring)", "Use earlier (might miss buff alignment)" })
                .Tip("Stardiver is a long animation - don't use it if Life of Dragon is about to expire!")
                .Concept("drg_stardiver")
                .Record();
            context.TrainingService?.RecordConceptApplication("drg_stardiver", true, "Life of Dragon burst");

            return true;
        }

        return false;
    }

    private bool TryGeirskogul(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Geirskogul.MinLevel)
            return false;

        // Don't use during Life of the Dragon (Nastrond replaces it)
        if (context.IsLifeOfDragonActive)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Geirskogul.ActionId))
            return false;

        // Use Geirskogul - at 2 eyes, this enters Life of the Dragon
        if (context.ActionService.ExecuteOgcd(DRGActions.Geirskogul, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Geirskogul.Name;
            var eyeInfo = context.EyeCount >= 2 ? " (entering Life!)" : $" ({context.EyeCount} eyes)";
            context.Debug.DamageState = $"Geirskogul{eyeInfo}";

            // Training: Record Geirskogul decision
            var enteringLife = context.EyeCount >= 2;
            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.Geirskogul.ActionId, DRGActions.Geirskogul.Name)
                .AsMeleeResource("Dragon Eye", context.EyeCount)
                .Reason(enteringLife
                        ? "Geirskogul at 2 eyes - entering Life of the Dragon!"
                        : $"Geirskogul for damage ({context.EyeCount} eyes)",
                    enteringLife
                        ? "Geirskogul at 2 Dragon Eyes enters Life of the Dragon, a 30-second window " +
                          "where you can use Nastrond (line AoE damage) and Stardiver (massive dive attack). " +
                          "This is your strongest burst phase - try to align it with raid buffs!"
                        : "Geirskogul deals line AoE damage and adds 1 Dragon Eye. " +
                          "At 2 eyes, the next Geirskogul will enter Life of the Dragon.")
                .Factors(enteringLife
                    ? new[] { "2 Dragon Eyes ready", "Lance Charge and buffs aligned", "Entering Life of Dragon for Nastrond/Stardiver" }
                    : new[] { $"{context.EyeCount} Dragon Eye(s)", "Building toward Life of Dragon", "30s cooldown allows frequent use" })
                .Alternatives(new[] { "Hold for buff alignment (minor optimization)", "Use anyway for consistent damage" })
                .Tip("Life of the Dragon is your biggest damage window. Try to enter it during Lance Charge + Battle Litany.")
                .Concept(enteringLife ? "drg_life_of_dragon" : "drg_eye_gauge")
                .Record();
            if (enteringLife)
                context.TrainingService?.RecordConceptApplication("drg_life_of_dragon", true, "Entering Life of Dragon");
            else
                context.TrainingService?.RecordConceptApplication("drg_eye_gauge", true, "Building Dragon Eyes");

            return true;
        }

        return false;
    }

    private bool TryJump(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Jump.MinLevel)
            return false;

        var jumpAction = DRGActions.GetJumpAction((byte)level);

        if (!context.ActionService.IsActionReady(jumpAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(jumpAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = jumpAction.Name;
            context.Debug.DamageState = jumpAction.Name;

            // Training: Record High Jump decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(jumpAction.ActionId, jumpAction.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue)
                .Reason($"{jumpAction.Name} for damage and Dive Ready proc",
                    $"{jumpAction.Name} is DRG's signature ability that deals high damage and grants Dive Ready, " +
                    "allowing you to use Mirage Dive. At level 74+, High Jump replaces Jump with higher potency. " +
                    "Each Jump also grants 1 Dragon Eye toward Life of the Dragon.")
                .Factors(new[] { "30s cooldown ready", "Grants Dive Ready for Mirage Dive", "Builds Dragon Eye gauge" })
                .Alternatives(new[] { "Hold for better positioning (rarely worth it)", "Use other oGCDs first (might delay eye build)" })
                .Tip("Jump abilities are a key part of DRG's rotation. Use on cooldown to maximize Dragon Eye generation.")
                .Concept("drg_high_jump")
                .Record();
            context.TrainingService?.RecordConceptApplication("drg_high_jump", true, "Jump ability usage");

            return true;
        }

        return false;
    }

    private bool TrySpineshatterDive(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.SpineshatterDive.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.SpineshatterDive.ActionId))
            return false;

        // Use as damage oGCD (also works as gap closer)
        if (context.ActionService.ExecuteOgcd(DRGActions.SpineshatterDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.SpineshatterDive.Name;
            context.Debug.DamageState = "Spineshatter Dive";
            return true;
        }

        return false;
    }

    private bool TryDragonfireDive(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.DragonfireDive.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.DragonfireDive.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.DragonfireDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.DragonfireDive.Name;
            context.Debug.DamageState = "Dragonfire Dive";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Main GCD damage rotation for Dragoon.
    /// Handles positional procs and combo rotation (ST/AoE).
    /// </summary>
    protected override bool TryGcdDamage(IZeusContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        // Priority 1: Positional procs (Fang and Claw, Wheeling Thrust, Drakesbane)
        if (TryPositionalProc(context, target))
            return true;

        // Priority 2: Continue combo based on enemy count
        if (ShouldUseAoE(enemyCount))
        {
            if (TryAoeCombo(context, target))
                return true;
        }
        else
        {
            if (TrySingleTargetCombo(context, target))
                return true;
        }

        return false;
    }

    #endregion

    #region Positional Procs

    private bool TryPositionalProc(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // At level 92+, Drakesbane replaces both Fang and Claw and Wheeling Thrust
        if (level >= DRGActions.Drakesbane.MinLevel)
        {
            // Either proc grants Drakesbane
            if (context.HasFangAndClawBared || context.HasWheelInMotion)
            {
                if (context.ActionService.IsActionReady(DRGActions.Drakesbane.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.Drakesbane, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.Drakesbane.Name;
                        context.Debug.DamageState = "Drakesbane";
                        return true;
                    }
                }
            }
            return false;
        }

        // Fang and Claw (flank positional)
        if (context.HasFangAndClawBared && level >= DRGActions.FangAndClaw.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.FangAndClaw.ActionId))
            {
                var positionalOk = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(DRGActions.FangAndClaw, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.FangAndClaw.Name;
                    context.Debug.DamageState = $"Fang and Claw {(positionalOk ? "(flank OK)" : "(flank!)")}";
                    return true;
                }
            }
        }

        // Wheeling Thrust (rear positional)
        if (context.HasWheelInMotion && level >= DRGActions.WheelingThrust.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.WheelingThrust.ActionId))
            {
                var positionalOk = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(DRGActions.WheelingThrust, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.WheelingThrust.Name;
                    context.Debug.DamageState = $"Wheeling Thrust {(positionalOk ? "(rear OK)" : "(rear!)")}";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Single-Target Combo

    private bool TrySingleTargetCombo(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;
        var lastAction = context.LastComboAction;
        var comboActive = context.ComboTimeRemaining > 0;

        // After Vorpal Thrust -> Heavens' Thrust / Full Thrust
        if (comboActive && lastAction == DRGActions.VorpalThrust.ActionId)
        {
            var finisher = DRGActions.GetVorpalFinisher((byte)level);
            if (context.ActionService.IsActionReady(finisher.ActionId))
            {
                if (context.ActionService.ExecuteGcd(finisher, target.GameObjectId))
                {
                    context.Debug.PlannedAction = finisher.Name;
                    context.Debug.DamageState = finisher.Name;
                    return true;
                }
            }
        }

        // After Disembowel -> Chaotic Spring / Chaos Thrust (rear positional)
        if (comboActive && lastAction == DRGActions.Disembowel.ActionId)
        {
            var finisher = DRGActions.GetDisembowelFinisher((byte)level);
            if (context.ActionService.IsActionReady(finisher.ActionId))
            {
                var positionalOk = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(finisher, target.GameObjectId))
                {
                    context.Debug.PlannedAction = finisher.Name;
                    context.Debug.DamageState = $"{finisher.Name} {(positionalOk ? "(rear OK)" : "(rear!)")}";
                    return true;
                }
            }
        }

        // After True Thrust -> Choose Vorpal or Disembowel
        if (comboActive && lastAction == DRGActions.TrueThrust.ActionId)
        {
            // Prefer Disembowel line if:
            // 1. Power Surge is missing or about to expire (< 10s)
            // 2. DoT is missing or about to expire (< 5s)
            var needsPowerSurge = !context.HasPowerSurge || context.PowerSurgeRemaining < 10f;
            var needsDot = !context.HasDotOnTarget || context.DotRemaining < 5f;

            if ((needsPowerSurge || needsDot) && level >= DRGActions.Disembowel.MinLevel)
            {
                if (context.ActionService.IsActionReady(DRGActions.Disembowel.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.Disembowel, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.Disembowel.Name;
                        context.Debug.DamageState = $"Disembowel (PS: {context.PowerSurgeRemaining:F1}s)";
                        return true;
                    }
                }
            }

            // Otherwise use Vorpal Thrust for damage
            if (level >= DRGActions.VorpalThrust.MinLevel)
            {
                if (context.ActionService.IsActionReady(DRGActions.VorpalThrust.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.VorpalThrust, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.VorpalThrust.Name;
                        context.Debug.DamageState = "Vorpal Thrust";
                        return true;
                    }
                }
            }
        }

        // Start combo with True Thrust
        if (context.ActionService.IsActionReady(DRGActions.TrueThrust.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRGActions.TrueThrust, target.GameObjectId))
            {
                context.Debug.PlannedAction = DRGActions.TrueThrust.Name;
                context.Debug.DamageState = "True Thrust";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region AoE Combo

    private bool TryAoeCombo(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;
        var lastAction = context.LastComboAction;
        var comboActive = context.ComboTimeRemaining > 0;

        // After Sonic Thrust -> Coerthan Torment
        if (comboActive && lastAction == DRGActions.SonicThrust.ActionId &&
            level >= DRGActions.CoerthanTorment.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.CoerthanTorment.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.CoerthanTorment, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.CoerthanTorment.Name;
                    context.Debug.DamageState = "Coerthan Torment";
                    return true;
                }
            }
        }

        // After Doom Spike -> Sonic Thrust
        if (comboActive && lastAction == DRGActions.DoomSpike.ActionId &&
            level >= DRGActions.SonicThrust.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.SonicThrust.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.SonicThrust, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.SonicThrust.Name;
                    context.Debug.DamageState = "Sonic Thrust";
                    return true;
                }
            }
        }

        // Start AoE combo with Doom Spike
        if (level >= DRGActions.DoomSpike.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.DoomSpike.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.DoomSpike, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.DoomSpike.Name;
                    context.Debug.DamageState = "Doom Spike";
                    return true;
                }
            }
        }

        // Fall back to single target if AoE not available
        return TrySingleTargetCombo(context, target);
    }

    #endregion

    #region Smart AoE

    protected override uint GetNextDirectionalAoEActionId(IZeusContext context, IBattleChara target, int enemyCount)
    {
        if (enemyCount < AoeThreshold) return 0;
        if (context.Player.Level >= DRGActions.DoomSpike.MinLevel)
            return DRGActions.DoomSpike.ActionId; // Line AoE
        return 0;
    }

    #endregion
}
