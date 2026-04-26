using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.IrisCore.Abilities;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.IrisCore.Modules;

/// <summary>
/// Handles Pictomancer GCD rotation (scheduler-driven).
/// Includes pre-pull motif painting (out-of-combat) and in-combat damage rotation.
/// </summary>
public sealed class DamageModule : IIrisModule
{
    public int Priority => 50;
    public string Name => "Damage";

    private readonly IBurstWindowService? _burstWindowService;
    private readonly ISmartAoEService? _smartAoEService;

    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null)
    {
        _burstWindowService = burstWindowService;
        _smartAoEService = smartAoEService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IIrisContext context, bool isMoving) => false;

    public void UpdateDebugState(IIrisContext context) { }

    public void CollectCandidates(IIrisContext context, RotationScheduler scheduler, bool isMoving)
    {
        // Pre-pull motif painting (out-of-combat)
        if (!context.InCombat)
        {
            TryPushPrepaintMotif(context, scheduler);
            return;
        }

        if (context.IsCasting && !context.CanSlidecast) return;

        if (context.TargetingService.IsDamageTargetingPaused())
        {
            context.Debug.DamageState = "Paused (no target)";
            return;
        }
        if (context.Configuration.Targeting.SuppressDamageOnForcedMovement
            && PlayerSafetyHelper.IsForcedMovementActive(context.Player))
        {
            context.Debug.DamageState = "Paused (forced movement)";
            return;
        }

        var player = context.Player;
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        // Hyperphantasia treats GCDs as instant — disable movement-only branches
        if (context.HasHyperphantasia) isMoving = false;

        // Addle (party mit utility)
        TryPushAddle(context, scheduler, target);

        TryPushMotifDuringInspiration(context, scheduler, isMoving);
        TryPushStarPrism(context, scheduler, target);
        TryPushRainbowDrip(context, scheduler, target, isMoving);
        TryPushHammerCombo(context, scheduler, target);
        TryPushCometInBlack(context, scheduler, target);
        TryPushHolyInWhite(context, scheduler, target, isMoving);
        TryPushSubtractiveCombo(context, scheduler, target);
        TryPushBaseCombo(context, scheduler, target);
        if (!isMoving) TryPushRepaintMotif(context, scheduler);
    }

    private void TryPushAddle(IIrisContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Addle.MinLevel) return;
        if (!context.ActionService.IsActionReady(RoleActions.Addle.ActionId)) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasActionUsedByOther(RoleActions.Addle.ActionId, 15f) == true)
        {
            return;
        }

        scheduler.PushOgcd(IrisAbilities.Addle, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Addle.Name;
                partyCoord?.OnCooldownUsed(RoleActions.Addle.ActionId, 90_000);
            });
    }

    #region Pre-pull / Inspiration motif painting

    private void TryPushPrepaintMotif(IIrisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Pictomancer.PrepaintMotifs) return;
        var player = context.Player;
        var level = player.Level;
        if (context.IsCasting) return;

        var prepaintOption = context.Configuration.Pictomancer.PrepaintOption;

        var paintLandscape = prepaintOption == PrepaintOption.All || prepaintOption == PrepaintOption.LandscapeOnly;
        if (paintLandscape && context.Configuration.Pictomancer.EnableLandscapeMotif && context.NeedsLandscapeMotif
            && level >= PCTActions.StarrySkyMotif.MinLevel)
        {
            scheduler.PushGcd(IrisAbilities.StarrySkyMotif, player.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PCTActions.StarrySkyMotif.Name;
                    context.Debug.DamageState = "Painting Starry Sky";
                });
            return;
        }

        var paintCreature = prepaintOption == PrepaintOption.All || prepaintOption == PrepaintOption.CreatureOnly;
        if (paintCreature && context.Configuration.Pictomancer.EnableCreatureMotif && context.NeedsCreatureMotif
            && level >= PCTActions.CreatureMotif.MinLevel)
        {
            var motif = GetCreatureMotifOrdered(context, level);
            if (IsCreatureMotifEnabled(context, motif))
            {
                var ability = MapCreatureMotifAbility(motif);
                scheduler.PushGcd(ability, player.GameObjectId, priority: 1,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = motif.Name;
                        context.Debug.DamageState = $"Painting {motif.Name}";
                    });
                return;
            }
        }

        var paintWeapon = prepaintOption == PrepaintOption.All || prepaintOption == PrepaintOption.WeaponOnly;
        if (paintWeapon && context.Configuration.Pictomancer.EnableWeaponMotif && context.NeedsWeaponMotif
            && level >= PCTActions.WeaponMotif.MinLevel)
        {
            scheduler.PushGcd(IrisAbilities.HammerMotif, player.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PCTActions.HammerMotif.Name;
                    context.Debug.DamageState = "Painting Hammer";
                });
        }
    }

    private void TryPushMotifDuringInspiration(IIrisContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (isMoving) return;
        if (!context.HasInspiration) return;
        if (context.IsCasting && !context.CanSlidecast) return;
        var player = context.Player;
        var level = player.Level;

        if (context.Configuration.Pictomancer.EnableLandscapeMotif && context.NeedsLandscapeMotif
            && level >= PCTActions.StarrySkyMotif.MinLevel)
        {
            if (MechanicCastGate.ShouldBlock(context, PCTActions.StarrySkyMotif.CastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(IrisAbilities.StarrySkyMotif, player.GameObjectId, priority: 0,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PCTActions.StarrySkyMotif.Name;
                    context.Debug.DamageState = "Inspiration: Painting Starry Sky";
                });
            return;
        }

        if (context.Configuration.Pictomancer.EnableCreatureMotif && context.NeedsCreatureMotif
            && level >= PCTActions.CreatureMotif.MinLevel)
        {
            var motif = GetCreatureMotifOrdered(context, level);
            if (IsCreatureMotifEnabled(context, motif))
            {
                if (MechanicCastGate.ShouldBlock(context, motif.CastTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return;
                }
                var ability = MapCreatureMotifAbility(motif);
                scheduler.PushGcd(ability, player.GameObjectId, priority: 0,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = motif.Name;
                        context.Debug.DamageState = $"Inspiration: Painting {motif.Name}";
                    });
                return;
            }
        }

        if (context.Configuration.Pictomancer.EnableWeaponMotif && context.NeedsWeaponMotif
            && level >= PCTActions.WeaponMotif.MinLevel)
        {
            if (MechanicCastGate.ShouldBlock(context, PCTActions.HammerMotif.CastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(IrisAbilities.HammerMotif, player.GameObjectId, priority: 0,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PCTActions.HammerMotif.Name;
                    context.Debug.DamageState = "Inspiration: Painting Hammer";
                });
        }
    }

    private void TryPushRepaintMotif(IIrisContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;
        if (context.IsCasting) return;

        if (context.Configuration.Pictomancer.EnableCreatureMotif && context.NeedsCreatureMotif
            && level >= PCTActions.CreatureMotif.MinLevel)
        {
            var motif = GetCreatureMotifOrdered(context, level);
            if (IsCreatureMotifEnabled(context, motif))
            {
                if (MechanicCastGate.ShouldBlock(context, motif.CastTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return;
                }
                var ability = MapCreatureMotifAbility(motif);
                scheduler.PushGcd(ability, player.GameObjectId, priority: 9,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = motif.Name;
                        context.Debug.DamageState = $"Repainting {motif.Name}";
                    });
                return;
            }
        }

        if (context.Configuration.Pictomancer.EnableWeaponMotif && context.NeedsWeaponMotif
            && level >= PCTActions.WeaponMotif.MinLevel)
        {
            if (MechanicCastGate.ShouldBlock(context, PCTActions.HammerMotif.CastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(IrisAbilities.HammerMotif, player.GameObjectId, priority: 9,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PCTActions.HammerMotif.Name;
                    context.Debug.DamageState = "Repainting Hammer";
                });
            return;
        }

        if (context.Configuration.Pictomancer.EnableLandscapeMotif && context.NeedsLandscapeMotif
            && level >= PCTActions.StarrySkyMotif.MinLevel)
        {
            if (MechanicCastGate.ShouldBlock(context, PCTActions.StarrySkyMotif.CastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(IrisAbilities.StarrySkyMotif, player.GameObjectId, priority: 9,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PCTActions.StarrySkyMotif.Name;
                    context.Debug.DamageState = "Repainting Starry Sky";
                });
        }
    }

    private static ActionDefinition GetCreatureMotifOrdered(IIrisContext context, byte level)
    {
        var count = context.LivingMuseCharges;
        if (context.Configuration.Pictomancer.CreatureMotifOrder == CreatureOrder.MawClawWingPom)
            count ^= 1;
        return PCTActions.GetCreatureMotif(level, count);
    }

    private static bool IsCreatureMotifEnabled(IIrisContext context, ActionDefinition motif)
    {
        var config = context.Configuration.Pictomancer;
        if (motif.ActionId == PCTActions.PomMotif.ActionId) return config.EnablePomMotif;
        if (motif.ActionId == PCTActions.WingMotif.ActionId) return config.EnableWingMotif;
        if (motif.ActionId == PCTActions.ClawMotif.ActionId) return config.EnableClawMotif;
        if (motif.ActionId == PCTActions.MawMotif.ActionId) return config.EnableMawMotif;
        return true;
    }

    private static AbilityBehavior MapCreatureMotifAbility(ActionDefinition motif)
    {
        if (motif.ActionId == PCTActions.PomMotif.ActionId) return IrisAbilities.PomMotif;
        if (motif.ActionId == PCTActions.WingMotif.ActionId) return IrisAbilities.WingMotif;
        if (motif.ActionId == PCTActions.ClawMotif.ActionId) return IrisAbilities.ClawMotif;
        if (motif.ActionId == PCTActions.MawMotif.ActionId) return IrisAbilities.MawMotif;
        return IrisAbilities.PomMotif;
    }

    #endregion

    #region GCDs

    private void TryPushStarPrism(IIrisContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Pictomancer.EnableStarPrism) return;
        if (context.Player.Level < PCTActions.StarPrism.MinLevel) return;
        if (!context.HasStarstruck) return;

        scheduler.PushGcd(IrisAbilities.StarPrism, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.StarPrism.Name;
                context.Debug.DamageState = "Star Prism";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PCTActions.StarPrism.ActionId, PCTActions.StarPrism.Name)
                    .AsCasterBurst().Target(target.Name?.TextValue)
                    .Reason("Star Prism - burst finisher",
                        "Star Prism is your highest potency single-target GCD, available during Starstruck buff.")
                    .Factors("Starstruck active")
                    .Alternatives("None - must use before buff expires")
                    .Tip("Star Prism is your biggest hit. Never let Starstruck expire without using it.")
                    .Concept(PctConcepts.FinisherPriority)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PctConcepts.FinisherPriority, true, "Star Prism finisher used");
            });
    }

    private void TryPushRainbowDrip(IIrisContext context, RotationScheduler scheduler, IBattleChara target, bool isMoving)
    {
        if (!context.Configuration.Pictomancer.EnableRainbowDrip) return;
        if (context.Player.Level < PCTActions.RainbowDrip.MinLevel) return;

        if (!context.HasRainbowBright)
        {
            if (isMoving) return;
            if (!context.IsInBurstWindow) return;
            if (MechanicCastGate.ShouldBlock(context, PCTActions.RainbowDrip.CastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
        }

        scheduler.PushGcd(IrisAbilities.RainbowDrip, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.RainbowDrip.Name;
                context.Debug.DamageState = context.HasRainbowBright ? "Rainbow Drip (instant)" : "Rainbow Drip (hardcast)";
            });
    }

    private void TryPushHammerCombo(IIrisContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Pictomancer.EnableWeaponMotif) return;
        var level = context.Player.Level;
        if (level < PCTActions.HammerStamp.MinLevel) return;
        if (!context.HasHammerTime && !context.IsInHammerCombo) return;
        if (context.Configuration.Pictomancer.EnableBurstPooling && ShouldHoldForBurst(8f) && context.HammerComboStep == 0) return;
        if (!context.Configuration.Pictomancer.UseHammerDuringBurst && context.IsInBurstWindow) return;

        var hammerAction = PCTActions.GetHammerComboAction(context.HammerComboStep, level);
        if (hammerAction == null) return;
        var ability = hammerAction.ActionId switch
        {
            var id when id == PCTActions.HammerStamp.ActionId => IrisAbilities.HammerStamp,
            var id when id == PCTActions.HammerBrush.ActionId => IrisAbilities.HammerBrush,
            var id when id == PCTActions.PolishingHammer.ActionId => IrisAbilities.PolishingHammer,
            _ => IrisAbilities.HammerStamp,
        };

        scheduler.PushGcd(ability, target.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = hammerAction.Name;
                context.Debug.DamageState = $"Hammer ({hammerAction.Name})";
                var stepName = context.HammerComboStep switch
                {
                    0 => "Stamp (step 1)",
                    1 => "Brush (step 2)",
                    2 => "Polish (step 3)",
                    _ => "Unknown"
                };
                TrainingHelper.Decision(context.TrainingService)
                    .Action(hammerAction.ActionId, hammerAction.Name)
                    .AsCasterBurst().Target(target.Name?.TextValue)
                    .Reason($"Hammer combo - {stepName}",
                        $"Hammer combo {stepName} is instant cast and high damage. Complete all 3 hits.")
                    .Factors($"Hammer Step: {context.HammerComboStep}", $"Hammer Time Stacks: {context.HammerTimeStacks}")
                    .Alternatives("Don't drop combo")
                    .Tip("Complete the hammer combo before Hammer Time expires.")
                    .Concept(PctConcepts.HammerCombo)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PctConcepts.HammerCombo, true, $"Hammer {stepName} executed");
            });
    }

    private void TryPushCometInBlack(IIrisContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Pictomancer.EnableCometInBlack) return;
        if (context.Player.Level < PCTActions.CometInBlack.MinLevel) return;
        if (!context.HasBlackPaint) return;

        scheduler.PushGcd(IrisAbilities.CometInBlack, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.CometInBlack.Name;
                context.Debug.DamageState = "Comet in Black";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PCTActions.CometInBlack.ActionId, PCTActions.CometInBlack.Name)
                    .AsCasterBurst().Target(target.Name?.TextValue)
                    .Reason("Comet in Black - black paint spender",
                        "Comet in Black consumes Black Paint for high instant damage.")
                    .Factors("Black Paint available")
                    .Alternatives("None - use Black Paint when available")
                    .Tip("Always use Comet in Black when you have Black Paint.")
                    .Concept(PctConcepts.CometInBlack)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PctConcepts.CometInBlack, true, "Black Paint consumed");
            });
    }

    private void TryPushHolyInWhite(IIrisContext context, RotationScheduler scheduler, IBattleChara target, bool isMoving)
    {
        if (!context.Configuration.Pictomancer.EnableHolyInWhite) return;
        if (context.Player.Level < PCTActions.HolyInWhite.MinLevel) return;
        if (!context.HasWhitePaint) return;
        if (!isMoving && context.WhitePaint < 4 && !context.IsInBurstWindow) return;
        if (!isMoving && context.PaletteGauge < context.Configuration.Pictomancer.HolyMinPalette && !context.IsInBurstWindow) return;

        scheduler.PushGcd(IrisAbilities.HolyInWhite, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.HolyInWhite.Name;
                context.Debug.DamageState = $"Holy in White ({context.WhitePaint - 1} paint)";
            });
    }

    private void TryPushSubtractiveCombo(IIrisContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Pictomancer.EnableSubtractiveCombo) return;
        if (context.Player.Level < PCTActions.BlizzardInCyan.MinLevel) return;
        if (!context.HasSubtractivePalette && !context.HasSubtractiveSpectrum) return;

        var comboAction = PCTActions.GetSubtractiveComboAction(context.BaseComboStep, context.ShouldUseAoe, context.Player.Level);
        var ability = MapSubtractiveCombo(comboAction);
        var castTime = context.HasInstantCast ? 0f : comboAction.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(ability, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = comboAction.Name;
                context.Debug.DamageState = $"Subtractive ({comboAction.Name})";
            });
    }

    private void TryPushBaseCombo(IIrisContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var comboAction = PCTActions.GetBaseComboAction(context.BaseComboStep, context.ShouldUseAoe, context.Player.Level);
        var ability = MapBaseCombo(comboAction);
        var castTime = context.HasInstantCast ? 0f : comboAction.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(ability, target.GameObjectId, priority: 8,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = comboAction.Name;
                context.Debug.DamageState = $"Base Combo ({comboAction.Name})";
            });
    }

    private static AbilityBehavior MapBaseCombo(ActionDefinition action)
    {
        if (action == PCTActions.FireInRed) return IrisAbilities.FireInRed;
        if (action == PCTActions.AeroInGreen) return IrisAbilities.AeroInGreen;
        if (action == PCTActions.WaterInBlue) return IrisAbilities.WaterInBlue;
        if (action == PCTActions.Fire2InRed) return IrisAbilities.Fire2InRed;
        if (action == PCTActions.Aero2InGreen) return IrisAbilities.Aero2InGreen;
        if (action == PCTActions.Water2InBlue) return IrisAbilities.Water2InBlue;
        return IrisAbilities.FireInRed;
    }

    private static AbilityBehavior MapSubtractiveCombo(ActionDefinition action)
    {
        if (action == PCTActions.BlizzardInCyan) return IrisAbilities.BlizzardInCyan;
        if (action == PCTActions.StoneInYellow) return IrisAbilities.StoneInYellow;
        if (action == PCTActions.ThunderInMagenta) return IrisAbilities.ThunderInMagenta;
        if (action == PCTActions.Blizzard2InCyan) return IrisAbilities.Blizzard2InCyan;
        if (action == PCTActions.Stone2InYellow) return IrisAbilities.Stone2InYellow;
        if (action == PCTActions.Thunder2InMagenta) return IrisAbilities.Thunder2InMagenta;
        return IrisAbilities.BlizzardInCyan;
    }

    #endregion
}
