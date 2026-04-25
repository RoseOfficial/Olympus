using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.TerpsichoreCore.Abilities;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Handles the Dancer GCD damage rotation (scheduler-driven).
/// Procs, Esprit spenders, Tillana, filler GCDs, Head Graze.
/// </summary>
public sealed class DamageModule : ITerpsichoreModule
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

    public bool TryExecute(ITerpsichoreContext context, bool isMoving) => false;

    public void UpdateDebugState(ITerpsichoreContext context) { }

    public void CollectCandidates(ITerpsichoreContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return;
        }
        if (context.IsDancing)
        {
            context.Debug.DamageState = "Dancing...";
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
            FFXIVConstants.RangedTargetingRange,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        var aoeEnabled = context.Configuration.Dancer.EnableAoERotation;
        var aoeThreshold = context.Configuration.Dancer.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;

        // oGCD interrupt
        TryPushInterrupt(context, scheduler, target);

        // GCD priority
        TryPushStarfallDance(context, scheduler, target);
        TryPushFinishingMove(context, scheduler);
        TryPushLastDance(context, scheduler, target);
        TryPushDanceOfTheDawn(context, scheduler, target);
        TryPushSaberDance(context, scheduler, target);
        TryPushTillana(context, scheduler);
        TryPushFountainfall(context, scheduler, target, enemyCount);
        TryPushReverseCascade(context, scheduler, target, enemyCount);
        TryPushFountain(context, scheduler, target, enemyCount);
        TryPushCascade(context, scheduler, target, enemyCount);
    }

    #region GCDs

    private void TryPushStarfallDance(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableStarfallDance) return;
        var player = context.Player;
        if (player.Level < DNCActions.StarfallDance.MinLevel) return;
        if (!context.HasFlourishingStarfall) return;
        if (!context.ActionService.IsActionReady(DNCActions.StarfallDance.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : DNCActions.StarfallDance.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.StarfallDance, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.StarfallDance.Name;
                context.Debug.DamageState = "Starfall Dance";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.StarfallDance.ActionId, DNCActions.StarfallDance.Name)
                    .AsRangedBurst()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Flourishing Starfall proc - highest priority GCD",
                        "Starfall Dance is granted by Devilment (Flourishing Starfall buff) and expires when Devilment " +
                        "ends. It's your highest potency GCD and must be used during the Devilment window. Has fall-off " +
                        "damage for AoE.")
                    .Factors("Flourishing Starfall active", "Highest priority GCD", "Expires with Devilment")
                    .Alternatives("No Starfall proc")
                    .Tip("Never let Starfall Dance expire - use it immediately after Devilment.")
                    .Concept(DncConcepts.StarfallDance)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.StarfallDance, true, "Burst GCD used");
            });
    }

    private void TryPushFinishingMove(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dancer.EnableFinishingMove) return;
        var player = context.Player;
        if (player.Level < DNCActions.FinishingMove.MinLevel) return;
        if (!context.HasFinishingMoveReady) return;
        if (!context.ActionService.IsActionReady(DNCActions.FinishingMove.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : DNCActions.FinishingMove.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.FinishingMove, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.FinishingMove.Name;
                context.Debug.DamageState = "Finishing Move";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.FinishingMove.ActionId, DNCActions.FinishingMove.Name)
                    .AsRangedBurst()
                    .Reason("Finishing Move Ready proc",
                        "Finishing Move (Lv.96+) is granted after Standard Finish. It's a high-potency GCD that should " +
                        "be used before Standard Finish buff expires. Prioritize it over normal GCDs during your rotation.")
                    .Factors("Finishing Move Ready buff", "High potency GCD", "Granted by Standard Finish")
                    .Alternatives("No Finishing Move Ready proc")
                    .Tip("Use Finishing Move before the buff expires - don't let it fall off.")
                    .Concept(DncConcepts.FinishingMove)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.FinishingMove, true, "Proc consumed");
            });
    }

    private void TryPushLastDance(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableLastDance) return;
        var player = context.Player;
        if (player.Level < DNCActions.LastDance.MinLevel) return;
        if (!context.HasLastDanceReady) return;
        if (!context.ActionService.IsActionReady(DNCActions.LastDance.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : DNCActions.LastDance.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.LastDance, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.LastDance.Name;
                context.Debug.DamageState = "Last Dance";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.LastDance.ActionId, DNCActions.LastDance.Name)
                    .AsRangedBurst()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Last Dance Ready proc",
                        "Last Dance (Lv.92+) is granted after Technical Finish or Tillana. It's a high-potency GCD " +
                        "that extends your burst phase. Use before the buff expires.")
                    .Factors("Last Dance Ready buff", "High potency burst GCD", "Granted by Technical/Tillana")
                    .Alternatives("No Last Dance Ready proc")
                    .Tip("Use Last Dance during burst windows - it's part of your Technical Step sequence.")
                    .Concept(DncConcepts.LastDance)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.LastDance, true, "Proc consumed");
            });
    }

    private void TryPushDanceOfTheDawn(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableSaberDance) return;
        var player = context.Player;
        if (player.Level < DNCActions.DanceOfTheDawn.MinLevel) return;
        if (!context.HasDanceOfTheDawnReady) return;
        if (context.Esprit < 50) return;
        if (!context.ActionService.IsActionReady(DNCActions.DanceOfTheDawn.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : DNCActions.DanceOfTheDawn.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.DanceOfTheDawn, target.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.DanceOfTheDawn.Name;
                context.Debug.DamageState = $"Dance of the Dawn ({context.Esprit} Esprit)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.DanceOfTheDawn.ActionId, DNCActions.DanceOfTheDawn.Name)
                    .AsRangedResource("Esprit", context.Esprit)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Dance of the Dawn Ready - enhanced Esprit spender",
                        "Dance of the Dawn (Lv.100) is an enhanced version of Saber Dance granted during Technical Finish " +
                        "window. It costs 50 Esprit and has higher potency. Always use this over Saber Dance when available.")
                    .Factors("Dance of the Dawn Ready", $"Esprit: {context.Esprit}/100", "Higher potency than Saber Dance")
                    .Alternatives("No Dance of the Dawn Ready proc", "Insufficient Esprit")
                    .Tip("Use Dance of the Dawn during Technical Finish - it replaces Saber Dance with higher damage.")
                    .Concept(DncConcepts.EspritGauge)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.EspritGauge, true, "Esprit spent");
            });
    }

    private void TryPushSaberDance(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableSaberDance) return;
        var player = context.Player;
        var level = player.Level;
        if (level < DNCActions.SaberDance.MinLevel) return;
        if (context.HasDanceOfTheDawnReady && level >= DNCActions.DanceOfTheDawn.MinLevel) return;
        if (context.Esprit < 50) return;

        var espritConfig = context.Configuration.Dancer;
        bool inBurst = context.HasDevilment || context.HasTechnicalFinish || IsInBurst;
        bool shouldUse = inBurst
            ? context.Esprit >= espritConfig.SaberDanceMinGauge
            : context.Esprit >= espritConfig.EspritOvercapThreshold;
        if (!shouldUse) return;
        if (!context.ActionService.IsActionReady(DNCActions.SaberDance.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : DNCActions.SaberDance.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.SaberDance, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.SaberDance.Name;
                context.Debug.DamageState = $"Saber Dance ({context.Esprit} Esprit)";
                var saberReason = context.Esprit >= 80 ? "Preventing Esprit overcap"
                                : context.HasDevilment || context.HasTechnicalFinish ? "Burst window active" : "Esprit dump";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.SaberDance.ActionId, DNCActions.SaberDance.Name)
                    .AsRangedResource("Esprit", context.Esprit)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Saber Dance ({saberReason})",
                        "Saber Dance is the primary Esprit spender, costing 50 Esprit. Use at 80+ Esprit to prevent " +
                        "overcapping, or at 50+ during burst windows. Esprit is generated by you and your dance partner " +
                        "dealing damage.")
                    .Factors($"Esprit: {context.Esprit}/100", context.HasDevilment ? "Burst window" : "Preventing overcap", "50 Esprit cost")
                    .Alternatives("Esprit < 50", "Dance of the Dawn available")
                    .Tip("Never let Esprit overcap - use Saber Dance at 80+ or during burst at 50+.")
                    .Concept(DncConcepts.SaberDance)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.SaberDance, true, "Esprit spent");
                if (context.Esprit >= 80)
                    context.TrainingService?.RecordConceptApplication(DncConcepts.EspritOvercapping, true, "Prevented overcap");
            });
    }

    private void TryPushTillana(ITerpsichoreContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dancer.EnableTillana) return;
        var player = context.Player;
        if (player.Level < DNCActions.Tillana.MinLevel) return;
        if (!context.HasFlourishingFinish) return;
        if (!context.ActionService.IsActionReady(DNCActions.Tillana.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : DNCActions.Tillana.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.Tillana, player.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.Tillana.Name;
                context.Debug.DamageState = "Tillana";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.Tillana.ActionId, DNCActions.Tillana.Name)
                    .AsRangedBurst()
                    .Reason("Flourishing Finish proc - Technical sequence",
                        "Tillana is granted by Technical Finish (Flourishing Finish buff). It's a high-potency GCD " +
                        "that also grants Last Dance Ready. Use during the Technical Finish window to extend burst phase.")
                    .Factors("Flourishing Finish active", "Part of Technical sequence", "Grants Last Dance Ready")
                    .Alternatives("No Flourishing Finish proc")
                    .Tip("Tillana is part of your 2-minute burst - use it during Technical Finish window.")
                    .Concept(DncConcepts.Tillana)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.Tillana, true, "Burst GCD used");
            });
    }

    private void TryPushFountainfall(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Dancer.EnableProcs) return;
        var player = context.Player;
        var level = player.Level;
        if (!context.HasSilkenFlow) return;
        var aoeThreshold = context.Configuration.Dancer.AoEMinTargets;

        if (enemyCount >= aoeThreshold && level >= DNCActions.Bloodshower.MinLevel
            && context.ActionService.IsActionReady(DNCActions.Bloodshower.ActionId))
        {
            var aoeCastTime = context.HasSwiftcast ? 0f : DNCActions.Bloodshower.CastTime;
            if (MechanicCastGate.ShouldBlock(context, aoeCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(TerpsichoreAbilities.Bloodshower, player.GameObjectId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DNCActions.Bloodshower.Name;
                    context.Debug.DamageState = "Bloodshower (AoE Flow)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.Bloodshower.ActionId, DNCActions.Bloodshower.Name)
                        .AsProc("Silken Flow")
                        .Reason($"Silken Flow AoE proc ({enemyCount} targets)",
                            "Bloodshower is the AoE version of Fountainfall, consuming the Silken Flow proc. " +
                            "Use for 3+ targets to maximize damage. Consumes the same proc as Fountainfall.")
                        .Factors("Silken Flow proc active", $"{enemyCount} enemies", "AoE proc consumer")
                        .Alternatives("No Silken Flow proc", "Single target")
                        .Tip("Use Bloodshower at 3+ targets instead of Fountainfall.")
                        .Concept(DncConcepts.SilkenFlow)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.SilkenFlow, true, "Proc consumed");
                });
            return;
        }

        if (level < DNCActions.Fountainfall.MinLevel) return;
        if (!context.ActionService.IsActionReady(DNCActions.Fountainfall.ActionId)) return;
        var stCastTime = context.HasSwiftcast ? 0f : DNCActions.Fountainfall.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.Fountainfall, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.Fountainfall.Name;
                context.Debug.DamageState = "Fountainfall (Flow)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.Fountainfall.ActionId, DNCActions.Fountainfall.Name)
                    .AsProc("Silken Flow")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Silken Flow proc - higher potency than combo",
                        "Fountainfall consumes the Silken Flow proc from Fountain or Flourish. It has higher potency " +
                        "than the basic combo and should be used before the proc expires. Generates 1 Feather.")
                    .Factors("Silken Flow proc active", "Higher potency than Fountain", "Generates 1 Feather")
                    .Alternatives("No Silken Flow proc")
                    .Tip("Always consume Silken Flow procs - they have higher priority than basic combos.")
                    .Concept(DncConcepts.SilkenFlow)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.SilkenFlow, true, "Proc consumed");
            });
    }

    private void TryPushReverseCascade(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Dancer.EnableProcs) return;
        var player = context.Player;
        var level = player.Level;
        if (!context.HasSilkenSymmetry) return;
        var aoeThreshold = context.Configuration.Dancer.AoEMinTargets;

        if (enemyCount >= aoeThreshold && level >= DNCActions.RisingWindmill.MinLevel
            && context.ActionService.IsActionReady(DNCActions.RisingWindmill.ActionId))
        {
            var aoeCastTime = context.HasSwiftcast ? 0f : DNCActions.RisingWindmill.CastTime;
            if (MechanicCastGate.ShouldBlock(context, aoeCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(TerpsichoreAbilities.RisingWindmill, player.GameObjectId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DNCActions.RisingWindmill.Name;
                    context.Debug.DamageState = "Rising Windmill (AoE Symmetry)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.RisingWindmill.ActionId, DNCActions.RisingWindmill.Name)
                        .AsProc("Silken Symmetry")
                        .Reason($"Silken Symmetry AoE proc ({enemyCount} targets)",
                            "Rising Windmill is the AoE version of Reverse Cascade, consuming the Silken Symmetry proc. " +
                            "Use for 3+ targets to maximize damage. Consumes the same proc as Reverse Cascade.")
                        .Factors("Silken Symmetry proc active", $"{enemyCount} enemies", "AoE proc consumer")
                        .Alternatives("No Silken Symmetry proc", "Single target")
                        .Tip("Use Rising Windmill at 3+ targets instead of Reverse Cascade.")
                        .Concept(DncConcepts.SilkenSymmetry)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.SilkenSymmetry, true, "Proc consumed");
                });
            return;
        }

        if (level < DNCActions.ReverseCascade.MinLevel) return;
        if (!context.ActionService.IsActionReady(DNCActions.ReverseCascade.ActionId)) return;
        var stCastTime = context.HasSwiftcast ? 0f : DNCActions.ReverseCascade.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.ReverseCascade, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.ReverseCascade.Name;
                context.Debug.DamageState = "Reverse Cascade (Symmetry)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.ReverseCascade.ActionId, DNCActions.ReverseCascade.Name)
                    .AsProc("Silken Symmetry")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Silken Symmetry proc - higher potency than combo",
                        "Reverse Cascade consumes the Silken Symmetry proc from Cascade or Flourish. It has higher potency " +
                        "than the basic combo and should be used before the proc expires. Generates 1 Feather.")
                    .Factors("Silken Symmetry proc active", "Higher potency than Cascade", "Generates 1 Feather")
                    .Alternatives("No Silken Symmetry proc")
                    .Tip("Always consume Silken Symmetry procs - they have higher priority than basic combos.")
                    .Concept(DncConcepts.SilkenSymmetry)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.SilkenSymmetry, true, "Proc consumed");
            });
    }

    private void TryPushFountain(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var aoeThreshold = context.Configuration.Dancer.AoEMinTargets;

        if (enemyCount >= aoeThreshold && level >= DNCActions.Bladeshower.MinLevel
            && context.ComboTimeRemaining > 0 && context.LastComboAction == DNCActions.Windmill.ActionId
            && context.ActionService.IsActionReady(DNCActions.Bladeshower.ActionId))
        {
            var aoeCastTime = context.HasSwiftcast ? 0f : DNCActions.Bladeshower.CastTime;
            if (MechanicCastGate.ShouldBlock(context, aoeCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(TerpsichoreAbilities.Bladeshower, player.GameObjectId, priority: 8,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DNCActions.Bladeshower.Name;
                    context.Debug.DamageState = "Bladeshower (AoE combo)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.Bladeshower.ActionId, DNCActions.Bladeshower.Name)
                        .AsAoE(enemyCount)
                        .Reason("AoE combo finisher",
                            "Bladeshower is the AoE combo finisher after Windmill. It can proc Silken Flow " +
                            "for Bloodshower. Use for 3+ targets.")
                        .Factors("Windmill combo active", $"{enemyCount} enemies", "Can proc Silken Flow")
                        .Alternatives("Not in Windmill combo", "Single target")
                        .Tip("Complete the Windmill → Bladeshower combo for AoE damage.")
                        .Concept(DncConcepts.DanceExecution)
                        .Record();
                });
            return;
        }

        if (level < DNCActions.Fountain.MinLevel) return;
        if (context.ComboTimeRemaining <= 0 || context.LastComboAction != DNCActions.Cascade.ActionId) return;
        if (!context.ActionService.IsActionReady(DNCActions.Fountain.ActionId)) return;
        var stCastTime = context.HasSwiftcast ? 0f : DNCActions.Fountain.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.Fountain, target.GameObjectId, priority: 8,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.Fountain.Name;
                context.Debug.DamageState = "Fountain (combo)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.Fountain.ActionId, DNCActions.Fountain.Name)
                    .AsRangedDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Combo finisher after Cascade",
                        "Fountain is the single-target combo finisher after Cascade. It can proc Silken Flow for " +
                        "Fountainfall. Complete the combo to maximize damage and generate procs.")
                    .Factors("Cascade combo active", "Single target", "Can proc Silken Flow")
                    .Alternatives("Not in Cascade combo")
                    .Tip("Complete the Cascade → Fountain combo - don't break it for other GCDs.")
                    .Concept(DncConcepts.DanceExecution)
                    .Record();
            });
    }

    private void TryPushCascade(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var aoeThreshold = context.Configuration.Dancer.AoEMinTargets;

        if (enemyCount >= aoeThreshold && level >= DNCActions.Windmill.MinLevel
            && context.ActionService.IsActionReady(DNCActions.Windmill.ActionId))
        {
            var aoeCastTime = context.HasSwiftcast ? 0f : DNCActions.Windmill.CastTime;
            if (MechanicCastGate.ShouldBlock(context, aoeCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(TerpsichoreAbilities.Windmill, player.GameObjectId, priority: 9,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DNCActions.Windmill.Name;
                    context.Debug.DamageState = "Windmill (AoE filler)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.Windmill.ActionId, DNCActions.Windmill.Name)
                        .AsAoE(enemyCount)
                        .Reason("AoE combo starter",
                            "Windmill is the AoE combo starter. Follow with Bladeshower. It can proc Silken " +
                            "Symmetry for Rising Windmill. Use for 3+ targets.")
                        .Factors($"{enemyCount} enemies", "AoE combo starter", "Can proc Silken Symmetry")
                        .Alternatives("Single target (use Cascade)")
                        .Tip("Start the Windmill → Bladeshower combo for AoE situations.")
                        .Concept(DncConcepts.DanceExecution)
                        .Record();
                });
            return;
        }

        if (!context.ActionService.IsActionReady(DNCActions.Cascade.ActionId)) return;
        var stCastTime = context.HasSwiftcast ? 0f : DNCActions.Cascade.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(TerpsichoreAbilities.Cascade, target.GameObjectId, priority: 9,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DNCActions.Cascade.Name;
                context.Debug.DamageState = "Cascade (filler)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DNCActions.Cascade.ActionId, DNCActions.Cascade.Name)
                    .AsRangedDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Combo starter / filler GCD",
                        "Cascade is the single-target combo starter. Follow with Fountain. It can proc Silken " +
                        "Symmetry for Reverse Cascade. This is your basic filler when no procs are available.")
                    .Factors("Single target", "Combo starter", "Can proc Silken Symmetry")
                    .Alternatives("3+ enemies (use Windmill)")
                    .Tip("Cascade → Fountain is your basic combo - use when no procs are available.")
                    .Concept(DncConcepts.DanceExecution)
                    .Record();
            });
    }

    #endregion

    #region Interrupt

    private void TryPushInterrupt(ITerpsichoreContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RangedShared.EnableHeadGraze) return;
        var player = context.Player;
        if (player.Level < RoleActions.HeadGraze.MinLevel) return;
        if (!target.IsCasting) return;
        if (!target.IsCastInterruptible) return;

        var targetId = target.EntityId;
        var delaySeed = (int)(target.EntityId * 2654435761u ^ (uint)(target.TotalCastTime * 1000f));
        var interruptDelay = 0.3f + ((delaySeed & 0xFFFF) / 65535f) * 0.4f;
        if (target.CurrentCastTime < interruptDelay) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableInterruptCoordination
            && partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.DamageState = "Interrupt reserved by other";
            return;
        }
        if (!context.ActionService.IsActionReady(RoleActions.HeadGraze.ActionId)) return;

        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;
        if (coordConfig.EnableInterruptCoordination)
        {
            if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.HeadGraze.ActionId, castTimeMs) ?? false)
            {
                context.Debug.DamageState = "Failed to reserve interrupt";
                return;
            }
        }

        scheduler.PushOgcd(TerpsichoreAbilities.HeadGraze, target.GameObjectId, priority: 0,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.HeadGraze.Name;
                context.Debug.DamageState = "Interrupted cast";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.HeadGraze.ActionId, RoleActions.HeadGraze.Name)
                    .AsInterrupt()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Interrupted enemy cast",
                        "Head Graze interrupts interruptible enemy casts. Watch for the pulsing cast bar indicating " +
                        "an interruptible ability. Coordinated with other Olympus instances to avoid duplicate interrupts.")
                    .Factors("Enemy casting", "Cast is interruptible", "Not reserved by other player")
                    .Alternatives("Cast not interruptible", "Already interrupted")
                    .Tip("Watch for interruptible casts - Head Graze can prevent dangerous enemy abilities.")
                    .Concept(DncConcepts.PartyUtility)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.PartyUtility, true, "Interrupt used");
            });
    }

    #endregion
}
