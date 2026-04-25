using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.CirceCore.Abilities;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// Handles the Red Mage damage rotation (scheduler-driven).
/// Manages Dualcast flow, melee combo, finishers, and mana balance.
/// Combo step is computed via action replacement on Enchanted Riposte
/// (see CLAUDE.md: GetAdjustedActionId-based melee combo tracking).
/// </summary>
public sealed class DamageModule : ICirceModule
{
    public int Priority => 30;
    public string Name => "Damage";

    private readonly IBurstWindowService? _burstWindowService;
    private readonly ISmartAoEService? _smartAoEService;

    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null)
    {
        _burstWindowService = burstWindowService;
        _smartAoEService = smartAoEService;
    }

    private bool IsInBurst => BurstHoldHelper.IsInBurst(_burstWindowService);
    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(ICirceContext context, bool isMoving) => false;

    public void UpdateDebugState(ICirceContext context) { }

    public void CollectCandidates(ICirceContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return;
        }
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

        var aoeEnabled = context.Configuration.RedMage.EnableAoERotation;
        var aoeThreshold = context.Configuration.RedMage.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;
        var useAoe = enemyCount >= aoeThreshold;
        var level = player.Level;

        if (context.IsResolutionReady && level >= RDMActions.Resolution.MinLevel)
            TryPushResolution(context, scheduler, target);

        if (context.IsScorchReady && level >= RDMActions.Scorch.MinLevel)
            TryPushScorch(context, scheduler, target);

        if (context.IsGrandImpactReady && level >= RDMActions.GrandImpact.MinLevel)
            TryPushGrandImpact(context, scheduler, target);

        if (context.IsFinisherReady && level >= RDMActions.Verflare.MinLevel)
            TryPushFinisher(context, scheduler, target);

        if (context.IsInMeleeCombo)
            TryPushMeleeCombo(context, scheduler, target);

        if (context.IsInMoulinetCombo)
            TryPushMoulinetCombo(context, scheduler, target);

        if (context.CanStartMeleeCombo && !context.IsInMeleeCombo && !context.IsInMoulinetCombo)
        {
            if (useAoe && level >= RDMActions.EnchantedMoulinet.MinLevel)
                TryPushStartMoulinet(context, scheduler, target);
            else
                TryPushStartMeleeCombo(context, scheduler, target);
        }

        if (context.HasDualcast || context.HasSwiftcast || context.HasAcceleration)
            TryPushDualcastConsumer(context, scheduler, target, useAoe);

        TryPushHardcastFiller(context, scheduler, target, useAoe, isMoving);
    }

    #region Finisher sequence

    private void TryPushResolution(ICirceContext context, RotationScheduler scheduler, IBattleChara target)
    {
        scheduler.PushGcd(CirceAbilities.Resolution, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RDMActions.Resolution.Name;
                context.Debug.DamageState = "Resolution (final finisher)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RDMActions.Resolution.ActionId, RDMActions.Resolution.Name)
                    .AsCasterBurst().Target(target.Name?.TextValue)
                    .Reason("Resolution - final finisher",
                        "Resolution is the final GCD in the finisher sequence after Scorch.")
                    .Factors("After Scorch", "Finisher sequence complete")
                    .Alternatives("Must use - combo will drop")
                    .Tip("Resolution is your finisher's finisher.")
                    .Concept(RdmConcepts.ScorchResolution)
                    .Record();
                context.TrainingService?.RecordConceptApplication(RdmConcepts.ScorchResolution, true, "Resolution completed finisher");
            });
    }

    private void TryPushScorch(ICirceContext context, RotationScheduler scheduler, IBattleChara target)
    {
        scheduler.PushGcd(CirceAbilities.Scorch, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RDMActions.Scorch.Name;
                context.Debug.DamageState = "Scorch (after Verflare/Verholy)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RDMActions.Scorch.ActionId, RDMActions.Scorch.Name)
                    .AsCasterBurst().Target(target.Name?.TextValue)
                    .Reason("Scorch - post-finisher burst",
                        "Scorch becomes available after using Verflare or Verholy. It leads into Resolution.")
                    .Factors("After Verflare/Verholy", "Resolution follows")
                    .Alternatives("Must use - combo will drop")
                    .Tip("Scorch is mandatory after Verflare/Verholy.")
                    .Concept(RdmConcepts.ScorchResolution)
                    .Record();
                context.TrainingService?.RecordConceptApplication(RdmConcepts.ScorchResolution, true, "Scorch used in finisher");
            });
    }

    private void TryPushGrandImpact(ICirceContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RedMage.EnableGrandImpact) return;

        scheduler.PushGcd(CirceAbilities.GrandImpact, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RDMActions.GrandImpact.Name;
                context.Debug.DamageState = "Grand Impact";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RDMActions.GrandImpact.ActionId, RDMActions.GrandImpact.Name)
                    .AsCasterBurst().Target(target.Name?.TextValue)
                    .Reason("Grand Impact - special proc",
                        "Grand Impact becomes available from Acceleration III procs.")
                    .Factors("Grand Impact Ready active")
                    .Alternatives("Must use before proc expires")
                    .Tip("Grand Impact is free damage from Acceleration.")
                    .Concept(RdmConcepts.GrandImpact)
                    .Record();
                context.TrainingService?.RecordConceptApplication(RdmConcepts.GrandImpact, true, "Grand Impact proc consumed");
            });
    }

    private void TryPushFinisher(ICirceContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RedMage.EnableFinisherCombo) return;
        var level = context.Player.Level;
        var baseFinisher = RDMActions.GetFinisher(level, context.BlackMana, context.WhiteMana);
        // Apply FinisherPreference override when both finishers are available
        var finisher = context.Configuration.RedMage.FinisherPreference switch
        {
            FinisherPreference.PreferVerholy when level >= RDMActions.Verholy.MinLevel => RDMActions.Verholy,
            FinisherPreference.PreferVerflare when level >= RDMActions.Verflare.MinLevel => RDMActions.Verflare,
            _ => baseFinisher
        };
        var ability = finisher == RDMActions.Verflare ? CirceAbilities.Verflare : CirceAbilities.Verholy;

        scheduler.PushGcd(ability, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = finisher.Name;
                context.Debug.DamageState = $"{finisher.Name} (finisher)";
                var isVerflare = finisher.ActionId == RDMActions.Verflare.ActionId;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(finisher.ActionId, finisher.Name)
                    .AsCasterBurst().Target(target.Name?.TextValue)
                    .Reason($"{finisher.Name} - melee finisher",
                        $"{finisher.Name} is your melee combo finisher after Redoublement.")
                    .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                            isVerflare ? "Chose Verflare (Black lower)" : "Chose Verholy (White lower)")
                    .Alternatives(isVerflare ? "Use Verholy instead" : "Use Verflare instead")
                    .Tip("Pick the finisher that generates your lower mana type to maintain balance.")
                    .Concept(RdmConcepts.FinisherSelection)
                    .Record();
                context.TrainingService?.RecordConceptApplication(RdmConcepts.FinisherSelection, true, "Correct finisher chosen");
            });
    }

    #endregion

    #region Melee combo

    private void TryPushMeleeCombo(ICirceContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RedMage.EnableMeleeCombo) return;
        var level = context.Player.Level;

        switch (context.MeleeComboStep)
        {
            case 1:
                if (level < RDMActions.EnchantedZwerchhau.MinLevel) break;
                scheduler.PushGcd(CirceAbilities.Zwerchhau, target.GameObjectId, priority: 4,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RDMActions.EnchantedZwerchhau.Name;
                        context.Debug.DamageState = "Enchanted Zwerchhau (combo 2)";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(RDMActions.EnchantedZwerchhau.ActionId, RDMActions.EnchantedZwerchhau.Name)
                            .AsMeleeCombo(2).Target(target.Name?.TextValue)
                            .Reason("Zwerchhau - combo step 2",
                                "Enchanted Zwerchhau is the second hit in your melee combo.")
                            .Factors("Step 2 of 3", "Redoublement next")
                            .Alternatives("Continue combo")
                            .Tip("Always complete your melee combo.")
                            .Concept(RdmConcepts.ComboProgression)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(RdmConcepts.ComboProgression, true, "Combo step 2 executed");
                    });
                break;

            case 2:
                if (level < RDMActions.EnchantedRedoublement.MinLevel) break;
                scheduler.PushGcd(CirceAbilities.Redoublement, target.GameObjectId, priority: 4,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RDMActions.EnchantedRedoublement.Name;
                        context.Debug.DamageState = "Enchanted Redoublement (combo 3)";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(RDMActions.EnchantedRedoublement.ActionId, RDMActions.EnchantedRedoublement.Name)
                            .AsMeleeCombo(3).Target(target.Name?.TextValue)
                            .Reason("Redoublement - combo step 3",
                                "Enchanted Redoublement is the final hit in your melee combo.")
                            .Factors("Step 3 of 3", "Finisher next")
                            .Alternatives("Use finisher immediately")
                            .Tip("After Redoublement, use Verflare/Verholy based on your lower mana.")
                            .Concept(RdmConcepts.ComboProgression)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(RdmConcepts.ComboProgression, true, "Combo step 3 executed");
                    });
                break;
        }
    }

    private void TryPushStartMeleeCombo(ICirceContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RedMage.EnableMeleeCombo) return;

        var rdmCfg = context.Configuration.RedMage;
        var inBurst = context.HasEmbolden || context.HasManafication;

        // UseMeleeDuringBurst = false: skip melee entry when in a burst window
        if (!rdmCfg.UseMeleeDuringBurst && inBurst) return;

        var highMana = context.LowerMana >= rdmCfg.MeleeComboMinMana;
        var verySoon = context.LowerMana >= 90;
        if (!inBurst && !highMana && !verySoon)
        {
            if (context.EmboldenReady && rdmCfg.HoldMeleeForEmbolden)
            {
                // Only hold if Embolden is within the configured window
                var emboldenCd = context.ActionService.GetCooldownRemaining(RDMActions.Embolden.ActionId);
                if (emboldenCd <= rdmCfg.MeleeHoldForEmbolden)
                {
                    context.Debug.DamageState = "Hold melee for Embolden";
                    return;
                }
            }
        }
        if (rdmCfg.EnableBurstPooling && ShouldHoldForBurst(8f) && !IsInBurst && !verySoon) return;

        scheduler.PushGcd(CirceAbilities.Riposte, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RDMActions.EnchantedRiposte.Name;
                context.Debug.DamageState = "Enchanted Riposte (combo start)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RDMActions.EnchantedRiposte.ActionId, RDMActions.EnchantedRiposte.Name)
                    .AsMeleeCombo(1).Target(target.Name?.TextValue)
                    .Reason("Riposte - melee combo entry",
                        "Enchanted Riposte starts your melee combo. Enter at 50|50 mana or higher.")
                    .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                            inBurst ? "In burst window" : "Not in burst")
                    .Alternatives("Wait for Embolden", "Build more mana")
                    .Tip("Enter melee combo at 50|50+ mana. Ideally align with Embolden for maximum damage.")
                    .Concept(RdmConcepts.MeleeEntry)
                    .Record();
                context.TrainingService?.RecordConceptApplication(RdmConcepts.MeleeEntry, true, "Melee combo started");
            });
    }

    #endregion

    #region Moulinet combo

    private void TryPushMoulinetCombo(ICirceContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RedMage.EnableMeleeCombo) return;
        var level = context.Player.Level;

        switch (context.MoulinetStep)
        {
            case 1:
                if (level < RDMActions.EnchantedMoulinetDeux.MinLevel) break;
                scheduler.PushGcd(CirceAbilities.EnchantedMoulinetDeux, target.GameObjectId, priority: 4,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RDMActions.EnchantedMoulinetDeux.Name;
                        context.Debug.DamageState = "Moulinet Deux (AoE combo 2/3)";
                    });
                break;
            case 2:
                if (level < RDMActions.EnchantedMoulinetTrois.MinLevel) break;
                scheduler.PushGcd(CirceAbilities.EnchantedMoulinetTrois, target.GameObjectId, priority: 4,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RDMActions.EnchantedMoulinetTrois.Name;
                        context.Debug.DamageState = "Moulinet Trois (AoE combo 3/3)";
                    });
                break;
        }
    }

    private void TryPushStartMoulinet(ICirceContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RedMage.EnableMeleeCombo) return;
        var level = context.Player.Level;
        if (level < RDMActions.EnchantedMoulinet.MinLevel) return;

        var inBurst = context.HasEmbolden || context.HasManafication;
        var highMana = context.LowerMana >= 80;
        var verySoon = context.LowerMana >= 90;
        if (!inBurst && !highMana && !verySoon && context.EmboldenReady)
        {
            context.Debug.DamageState = "Hold Moulinet for Embolden";
            return;
        }
        if (context.Configuration.RedMage.EnableBurstPooling && ShouldHoldForBurst(8f) && !IsInBurst && !verySoon) return;

        scheduler.PushGcd(CirceAbilities.EnchantedMoulinet, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RDMActions.EnchantedMoulinet.Name;
                context.Debug.DamageState = "Enchanted Moulinet (AoE combo start)";
            });
    }

    #endregion

    #region Dualcast consumer & filler

    private void TryPushDualcastConsumer(ICirceContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;
        if (!context.Configuration.RedMage.EnableProcs)
        {
            TryPushLongSpell(context, scheduler, target, useAoe);
            return;
        }

        var procSpell = RDMActions.GetProcSpell(
            level, context.HasVerfire, context.HasVerstone,
            context.VerfireRemaining, context.VerstoneRemaining,
            context.BlackMana, context.WhiteMana);

        if (context.HasAcceleration && !context.HasAnyProc)
        {
            TryPushLongSpell(context, scheduler, target, useAoe);
            return;
        }

        if (procSpell != null)
        {
            var procExpiring = (context.HasVerfire && context.VerfireRemaining < 5f)
                            || (context.HasVerstone && context.VerstoneRemaining < 5f);
            if (procExpiring)
            {
                var ability = procSpell == RDMActions.Verfire ? CirceAbilities.Verfire : CirceAbilities.Verstone;
                scheduler.PushGcd(ability, target.GameObjectId, priority: 7,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = procSpell.Name;
                        context.Debug.DamageState = $"{procSpell.Name} (expiring proc)";
                        var isVerfire = procSpell.ActionId == RDMActions.Verfire.ActionId;
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(procSpell.ActionId, procSpell.Name)
                            .AsCasterProc(isVerfire ? "Verfire" : "Verstone")
                            .Target(target.Name?.TextValue)
                            .Reason($"{procSpell.Name} - expiring proc usage",
                                $"{procSpell.Name} is about to expire. Using it now to avoid wasting the proc.")
                            .Factors($"Proc remaining: {(isVerfire ? context.VerfireRemaining : context.VerstoneRemaining):F1}s")
                            .Alternatives("Let it expire (waste)")
                            .Tip("Always use procs before they expire.")
                            .Concept(isVerfire ? RdmConcepts.VerfireProc : RdmConcepts.VerstoneProc)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(
                            isVerfire ? RdmConcepts.VerfireProc : RdmConcepts.VerstoneProc, true, "Expiring proc consumed");
                    });
                return;
            }
        }

        TryPushLongSpell(context, scheduler, target, useAoe);
    }

    private void TryPushLongSpell(ICirceContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;

        if (useAoe && level >= RDMActions.Impact.MinLevel)
        {
            scheduler.PushGcd(CirceAbilities.Impact, target.GameObjectId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RDMActions.Impact.Name;
                    context.Debug.DamageState = "Impact (AoE)";
                });
            return;
        }

        // Force deficit color when imbalance exceeds threshold (StrictManaBalance or imbalanced)
        ActionDefinition longSpell;
        if (context.AbsoluteManaImbalance >= context.Configuration.RedMage.ManaImbalanceThreshold
            || context.Configuration.RedMage.StrictManaBalance && context.AbsoluteManaImbalance > 0)
        {
            // Pick the spell that restores the lower mana color
            longSpell = context.BlackMana < context.WhiteMana
                ? RDMActions.GetVerthunderSpell(level)
                : RDMActions.GetVeraeroSpell(level);
        }
        else
        {
            longSpell = RDMActions.GetBalancedLongSpell(level, context.BlackMana, context.WhiteMana);
        }
        var ability = longSpell == RDMActions.Verthunder3 ? CirceAbilities.Verthunder3
                    : longSpell == RDMActions.Veraero3 ? CirceAbilities.Veraero3
                    : longSpell == RDMActions.Verthunder ? CirceAbilities.Verthunder
                    : CirceAbilities.Veraero;

        scheduler.PushGcd(ability, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = longSpell.Name;
                context.Debug.DamageState = $"{longSpell.Name} (Dualcast)";
                var isVerthunder = longSpell.ActionId == RDMActions.Verthunder3.ActionId
                                || longSpell.ActionId == RDMActions.Verthunder.ActionId;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(longSpell.ActionId, longSpell.Name)
                    .AsCasterDamage().Target(target.Name?.TextValue)
                    .Reason($"{longSpell.Name} - Dualcast consumer",
                        $"Using {longSpell.Name} with Dualcast for instant cast.")
                    .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                            isVerthunder ? "Chose Verthunder (Black lower)" : "Chose Veraero (White lower)")
                    .Alternatives(isVerthunder ? "Use Veraero instead" : "Use Verthunder instead")
                    .Tip("Always pick the spell that generates your lower mana type.")
                    .Concept(RdmConcepts.DualcastConsumption)
                    .Record();
                context.TrainingService?.RecordConceptApplication(RdmConcepts.DualcastConsumption, true, "Dualcast consumed correctly");
            });
    }

    private void TryPushHardcastFiller(ICirceContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            // Try proc first
            var procSpell = context.Configuration.RedMage.EnableProcs ? RDMActions.GetProcSpell(
                level, context.HasVerfire, context.HasVerstone,
                context.VerfireRemaining, context.VerstoneRemaining,
                context.BlackMana, context.WhiteMana) : null;
            if (procSpell != null)
            {
                var ability = procSpell == RDMActions.Verfire ? CirceAbilities.Verfire : CirceAbilities.Verstone;
                scheduler.PushGcd(ability, target.GameObjectId, priority: 8,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = procSpell.Name;
                        context.Debug.DamageState = $"{procSpell.Name} (movement)";
                    });
                return;
            }

            if (context.SwiftcastReady)
            {
                scheduler.PushOgcd(CirceAbilities.Swiftcast, player.GameObjectId, priority: 7,
                    onDispatched: _ => context.Debug.DamageState = "Swiftcast for movement");
            }
            context.Debug.DamageState = "Moving, need instant";
            return;
        }

        var fillerProc = context.Configuration.RedMage.EnableProcs ? RDMActions.GetProcSpell(
            level, context.HasVerfire, context.HasVerstone,
            context.VerfireRemaining, context.VerstoneRemaining,
            context.BlackMana, context.WhiteMana) : null;
        if (fillerProc != null)
        {
            var ability = fillerProc == RDMActions.Verfire ? CirceAbilities.Verfire : CirceAbilities.Verstone;
            scheduler.PushGcd(ability, target.GameObjectId, priority: 8,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = fillerProc.Name;
                    context.Debug.DamageState = $"{fillerProc.Name} (proc filler)";
                    var isVerfire = fillerProc.ActionId == RDMActions.Verfire.ActionId;
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(fillerProc.ActionId, fillerProc.Name)
                        .AsCasterProc(isVerfire ? "Verfire" : "Verstone")
                        .Target(target.Name?.TextValue)
                        .Reason($"{fillerProc.Name} - proc as hardcast filler",
                            $"Using {fillerProc.Name} as your hardcast filler. Procs are instant and grant Dualcast.")
                        .Factors(isVerfire ? "Verfire available" : "Verstone available", "Grants Dualcast")
                        .Alternatives("Use Jolt instead")
                        .Tip("Procs are the best hardcast filler.")
                        .Concept(RdmConcepts.ProcPriority)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(RdmConcepts.ProcPriority, true, "Proc used as filler");
                });
            return;
        }

        if (useAoe)
        {
            var aoeHardcast = RDMActions.GetAoeHardcast(level, context.BlackMana, context.WhiteMana);
            var ability = aoeHardcast == RDMActions.Verthunder2 ? CirceAbilities.Verthunder2 : CirceAbilities.Veraero2;
            var castTime = context.HasInstantCast ? 0f : aoeHardcast.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(ability, target.GameObjectId, priority: 9,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = aoeHardcast.Name;
                    context.Debug.DamageState = $"{aoeHardcast.Name} (AoE hardcast)";
                });
            return;
        }

        var jolt = RDMActions.GetJoltSpell(level);
        var joltAbility = jolt == RDMActions.Jolt3 ? CirceAbilities.Jolt3
                        : jolt == RDMActions.Jolt2 ? CirceAbilities.Jolt2
                        : CirceAbilities.Jolt;
        var joltCastTime = context.HasInstantCast ? 0f : jolt.CastTime;
        if (MechanicCastGate.ShouldBlock(context, joltCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }
        scheduler.PushGcd(joltAbility, target.GameObjectId, priority: 9,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = jolt.Name;
                context.Debug.DamageState = jolt.Name;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(jolt.ActionId, jolt.Name)
                    .AsCasterDamage().Target(target.Name?.TextValue)
                    .Reason("Jolt - Dualcast starter",
                        "Jolt is your default hardcast filler when no procs are available.")
                    .Factors("No procs available", "Grants Dualcast")
                    .Alternatives("Use proc if available")
                    .Tip("Jolt is the fallback filler.")
                    .Concept(RdmConcepts.DualcastMechanic)
                    .Record();
                context.TrainingService?.RecordConceptApplication(RdmConcepts.DualcastMechanic, true, "Dualcast generated");
            });
    }

    #endregion
}
