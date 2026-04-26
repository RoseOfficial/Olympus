using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HecateCore.Abilities;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.HecateCore.Modules;

/// <summary>
/// Handles the Black Mage damage rotation (scheduler-driven).
/// Manages Fire/Ice phase transitions, Polyglot spending, proc usage, Flare Star.
/// </summary>
public sealed class DamageModule : IHecateModule
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

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    private const int DespairMpCost = 800;
    private const float ElementRefreshThreshold = 6f;

    public bool TryExecute(IHecateContext context, bool isMoving) => false;

    public void UpdateDebugState(IHecateContext context) { }

    public void CollectCandidates(IHecateContext context, RotationScheduler scheduler, bool isMoving)
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

        var aoeEnabled = context.Configuration.BlackMage.EnableAoERotation;
        var aoeThreshold = context.Configuration.BlackMage.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(8f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;
        var useAoe = enemyCount >= aoeThreshold;

        // Addle (oGCD utility, fires regardless of phase or movement)
        TryPushAddle(context, scheduler, target);

        // Movement handling first
        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            TryPushMovementAction(context, scheduler, target, useAoe);
            return; // Movement preempts all other GCDs
        }

        // Flare Star (Lv.100 finisher) — push, but only fires when 6 stacks
        TryPushFlareStar(context, scheduler, target);

        // Procs (Firestarter expiring, Thunderhead expiring or DoT refresh)
        TryPushExpiringProcs(context, scheduler, target, useAoe);

        // Polyglot spending (cap avoidance + movement fallback)
        TryPushPolyglot(context, scheduler, target, useAoe, isMoving);

        // Phase rotation
        if (context.InAstralFire)
            TryPushFirePhase(context, scheduler, target, useAoe);
        else if (context.InUmbralIce)
            TryPushIcePhase(context, scheduler, target, useAoe);
        else
            TryPushStartRotation(context, scheduler, target);
    }

    #region Movement

    private void TryPushMovementAction(IHecateContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        if (context.Configuration.BlackMage.EnableXenoglossy && context.PolyglotStacks > 0
            && level >= BLMActions.Xenoglossy.MinLevel && !useAoe)
        {
            scheduler.PushGcd(HecateAbilities.Xenoglossy, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Xenoglossy.Name;
                    context.Debug.DamageState = "Xenoglossy (movement)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Xenoglossy.ActionId, BLMActions.Xenoglossy.Name)
                        .AsMovement().Target(target.Name?.TextValue)
                        .Reason("Xenoglossy for movement",
                            "Xenoglossy is an instant-cast high-potency spell that spends 1 Polyglot stack. " +
                            "It's ideal for movement because it deals strong damage without requiring a cast time.")
                        .Factors("Moving", $"Polyglot: {context.PolyglotStacks}", "Single target")
                        .Alternatives("Use Triplecast", "Slidecast")
                        .Tip("Xenoglossy is your best movement tool - save Polyglot for movement-heavy phases.")
                        .Concept(BlmConcepts.XenoglossyUsage)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.XenoglossyUsage, true, "Movement Xenoglossy");
                });
            return;
        }

        if (context.Configuration.BlackMage.EnableFoul && context.PolyglotStacks > 0
            && level >= BLMActions.Foul.MinLevel && useAoe)
        {
            scheduler.PushGcd(HecateAbilities.Foul, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Foul.Name;
                    context.Debug.DamageState = "Foul (movement AoE)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Foul.ActionId, BLMActions.Foul.Name)
                        .AsMovement().Target(target.Name?.TextValue)
                        .Reason("Foul for AoE movement",
                            "Foul is the AoE version of Xenoglossy, spending 1 Polyglot for instant AoE damage.")
                        .Factors("Moving", $"Polyglot: {context.PolyglotStacks}", "AoE situation")
                        .Alternatives("Use Xenoglossy on priority target")
                        .Tip("In AoE situations, Foul is better than Xenoglossy for movement.")
                        .Concept(BlmConcepts.AoeRotation)
                        .Record();
                });
            return;
        }

        if (context.HasFirestarter)
        {
            scheduler.PushGcd(HecateAbilities.Fire3, target.GameObjectId, priority: 2,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = "Fire III (Firestarter)";
                    context.Debug.DamageState = "Firestarter proc (movement)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Fire3.ActionId, "Fire III (Firestarter)")
                        .AsCasterProc("Firestarter").Target(target.Name?.TextValue)
                        .Reason("Firestarter proc for movement",
                            "Firestarter makes Fire III instant. Use it during movement to maintain DPS.")
                        .Factors("Moving", "Firestarter active", $"Remaining: {context.FirestarterRemaining:F1}s")
                        .Alternatives("Save for later movement")
                        .Tip("Firestarter is great for movement but also useful for weaving oGCDs.")
                        .Concept(BlmConcepts.FirestarterProc)
                        .Record();
                });
            return;
        }

        if (context.HasThunderhead
            && (context.Configuration.BlackMage.UseThunderheadImmediately
                || context.ThunderDoTRemaining < context.Configuration.BlackMage.ThunderRefreshThreshold))
        {
            var thunderAction = useAoe ? BLMActions.GetThunderAoe(context.Player.Level) : BLMActions.GetThunderST(context.Player.Level);
            var ability = MapThunderAbility(thunderAction);
            scheduler.PushGcd(ability, target.GameObjectId, priority: 2,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = thunderAction.Name;
                    context.Debug.DamageState = "Thunderhead proc (movement)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(thunderAction.ActionId, thunderAction.Name)
                        .AsCasterProc("Thunderhead").Target(target.Name?.TextValue)
                        .Reason("Thunderhead proc for movement",
                            "Thunderhead makes Thunder instant and refreshes the DoT. Use during movement " +
                            "for instant damage plus DoT application/refresh.")
                        .Factors("Moving", "Thunderhead active", $"Remaining: {context.ThunderheadRemaining:F1}s")
                        .Alternatives("Save for DoT refresh timing")
                        .Tip("Thunderhead is flexible - use for movement or optimized DoT refresh timing.")
                        .Concept(BlmConcepts.ThunderheadProc)
                        .Record();
                });
            return;
        }

        if (context.Configuration.BlackMage.EnableParadox && context.HasParadox && context.InUmbralIce
            && context.UmbralIceStacks == 3 && level >= BLMActions.Paradox.MinLevel)
        {
            scheduler.PushGcd(HecateAbilities.Paradox, target.GameObjectId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Paradox.Name;
                    context.Debug.DamageState = "Paradox (movement)";
                });
            return;
        }

        if (context.Configuration.BlackMage.UseScatheForMovement && level >= BLMActions.Scathe.MinLevel)
        {
            scheduler.PushGcd(HecateAbilities.Scathe, target.GameObjectId, priority: 9,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Scathe.Name;
                    context.Debug.DamageState = "Scathe (emergency movement)";
                });
        }
    }

    #endregion

    #region GCDs

    private void TryPushAddle(IHecateContext context, RotationScheduler scheduler, IBattleChara target)
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

        scheduler.PushOgcd(HecateAbilities.Addle, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Addle.Name;
                partyCoord?.OnCooldownUsed(RoleActions.Addle.ActionId, 90_000);
            });
    }

    private void TryPushFlareStar(IHecateContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.BlackMage.EnableFlareStar) return;
        var player = context.Player;
        if (player.Level < BLMActions.FlareStar.MinLevel) return;
        if (context.AstralSoulStacks < 6) return;
        if (!context.ActionService.IsActionReady(BLMActions.FlareStar.ActionId)) return;
        var castTime = context.HasInstantCast ? 0f : BLMActions.FlareStar.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(HecateAbilities.FlareStar, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BLMActions.FlareStar.Name;
                context.Debug.DamageState = "Flare Star (6 stacks)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.FlareStar.ActionId, BLMActions.FlareStar.Name)
                    .AsCasterBurst().Target(target.Name?.TextValue)
                    .Reason("Flare Star at 6 Astral Soul stacks",
                        "Flare Star is a powerful AoE finisher that requires 6 Astral Soul stacks. Stacks are built by " +
                        "casting Fire IV. At 6 stacks, immediately cast Flare Star for massive damage.")
                    .Factors("6 Astral Soul stacks", "In Astral Fire")
                    .Alternatives("Continue Fire IV spam")
                    .Tip("Flare Star at 6 stacks is mandatory - never let stacks go to waste.")
                    .Concept(BlmConcepts.AstralSoul)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralSoul, true, "Spent 6 Astral Soul stacks");
            });
    }

    private void TryPushExpiringProcs(IHecateContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;

        if (context.HasFirestarter && context.FirestarterRemaining < 5f)
        {
            scheduler.PushGcd(HecateAbilities.Fire3, target.GameObjectId, priority: 2,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = "Fire III (Firestarter)";
                    context.Debug.DamageState = $"Firestarter expiring ({context.FirestarterRemaining:F1}s)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Fire3.ActionId, "Fire III (Firestarter)")
                        .AsCasterProc("Firestarter").Target(target.Name?.TextValue)
                        .Reason("Firestarter proc expiring - must use now",
                            "Firestarter proc is about to expire. Use it immediately to avoid wasting the instant Fire III.")
                        .Factors($"Firestarter: {context.FirestarterRemaining:F1}s remaining", "Will expire soon")
                        .Alternatives("Would lose the proc")
                        .Tip("Watch proc timers - don't let them expire.")
                        .Concept(BlmConcepts.FirestarterProc)
                        .Record();
                });
        }

        var thunderRefresh = context.Configuration.BlackMage.ThunderRefreshThreshold;
        if (context.HasThunderhead
            && (context.ThunderheadRemaining < 5f || context.ThunderDoTRemaining < thunderRefresh))
        {
            var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
            var ability = MapThunderAbility(thunderAction);
            scheduler.PushGcd(ability, target.GameObjectId, priority: 2,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = thunderAction.Name;
                    context.Debug.DamageState = $"Thunderhead expiring ({context.ThunderheadRemaining:F1}s)";
                    var reason = context.ThunderDoTRemaining < thunderRefresh ? "DoT needs refresh" : "Proc expiring";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(thunderAction.ActionId, thunderAction.Name)
                        .AsCasterProc("Thunderhead").Target(target.Name?.TextValue)
                        .Reason($"Thunderhead - {reason}",
                            "Thunderhead proc is expiring. Use it to avoid losing the instant Thunder cast.")
                        .Factors($"Thunderhead: {context.ThunderheadRemaining:F1}s", $"DoT: {context.ThunderDoTRemaining:F1}s")
                        .Alternatives("Would lose proc/DoT uptime")
                        .Tip("Balance proc usage between movement and DoT maintenance.")
                        .Concept(BlmConcepts.ThunderheadProc)
                        .Record();
                });
        }
    }

    private void TryPushPolyglot(IHecateContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe, bool isMoving)
    {
        var level = context.Player.Level;
        if (context.PolyglotStacks == 0) return;
        var maxPolyglot = level >= 98 ? 3 : 2;
        var cfg = context.Configuration.BlackMage;

        if (context.PolyglotStacks >= maxPolyglot)
        {
            var (action, ability) = SelectPolyglotAction(context, level, useAoe);
            if (action == null) return;
            if (level < action.MinLevel) return;

            scheduler.PushGcd(ability!, target.GameObjectId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = $"{action.Name} (cap avoidance)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsCasterResource("Polyglot", context.PolyglotStacks)
                        .Reason($"{action.Name} - avoiding Polyglot overcap",
                            "Polyglot stacks are at maximum capacity. Using Xenoglossy/Foul now to make room.")
                        .Factors($"Polyglot: {context.PolyglotStacks}/{maxPolyglot}", "At cap, must spend")
                        .Alternatives("Would overcap next Polyglot")
                        .Tip("Spend Polyglot before reaching max to avoid wasting stacks.")
                        .Concept(BlmConcepts.GaugeOvercapping)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.GaugeOvercapping, true, "Avoided Polyglot overcap");
                });
            return;
        }

        // Respect minimum stack threshold before spending non-cap stacks
        if (context.PolyglotStacks < cfg.PolyglotMinStacks) return;

        if (cfg.EnableBurstPooling && ShouldHoldForBurst(8f) && context.PolyglotStacks < 2) return;

        if (isMoving && !context.HasInstantCast)
        {
            // Player wants to reserve stacks specifically for movement
            if (!cfg.SavePolyglotForMovement) return;
            if (context.PolyglotStacks <= cfg.PolyglotMovementReserve) return;

            var (action, ability) = SelectPolyglotAction(context, level, useAoe);
            if (action == null || level < action.MinLevel) return;
            scheduler.PushGcd(ability!, target.GameObjectId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = $"{action.Name} (movement)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsMovement().Target(target.Name?.TextValue)
                        .Reason($"{action.Name} for movement",
                            $"{action.Name} provides instant damage during movement.")
                        .Factors("Moving", $"Polyglot: {context.PolyglotStacks}", "No other instant available")
                        .Alternatives("Use Triplecast")
                        .Tip("Save Polyglot for movement-heavy phases.")
                        .Concept(BlmConcepts.PolyglotStacks)
                        .Record();
                });
        }
    }

    private static (ActionDefinition? action, AbilityBehavior? ability) SelectPolyglotAction(IHecateContext context, byte level, bool useAoe)
    {
        if (context.Configuration.BlackMage.EnableFoul && useAoe && level >= BLMActions.Foul.MinLevel)
            return (BLMActions.Foul, HecateAbilities.Foul);
        if (context.Configuration.BlackMage.EnableXenoglossy && level >= BLMActions.Xenoglossy.MinLevel)
            return (BLMActions.Xenoglossy, HecateAbilities.Xenoglossy);
        if (context.Configuration.BlackMage.EnableFoul && level >= BLMActions.Foul.MinLevel)
            return (BLMActions.Foul, HecateAbilities.Foul);
        return (null, null);
    }

    private void TryPushFirePhase(IHecateContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;
        context.Debug.Phase = "Fire";

        if (context.ElementTimer < 3f && context.ElementTimer > 0)
        {
            TryPushIceTransition(context, scheduler, target);
            return;
        }

        if (context.Configuration.BlackMage.EnableParadox && context.HasParadox
            && context.ElementTimer < ElementRefreshThreshold && level >= BLMActions.Paradox.MinLevel)
        {
            var castTime = context.HasInstantCast ? 0f : BLMActions.Paradox.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(HecateAbilities.Paradox, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Paradox.Name;
                    context.Debug.DamageState = "Paradox (timer refresh)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Paradox.ActionId, BLMActions.Paradox.Name)
                        .AsCasterResource("Element Timer", (int)context.ElementTimer)
                        .Reason("Paradox - refreshing element timer",
                            "Element timer is getting low. Paradox refreshes the timer while dealing damage.")
                        .Factors($"Element timer: {context.ElementTimer:F1}s", "Paradox ready")
                        .Alternatives("Transition to Ice early")
                        .Tip("Use Paradox in Fire phase to extend your damage window.")
                        .Concept(BlmConcepts.ElementTimer)
                        .Record();
                });
            return;
        }

        if (useAoe) TryPushFireAoe(context, scheduler, target);
        else TryPushFireSingleTarget(context, scheduler, target);
    }

    private void TryPushFireSingleTarget(IHecateContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;

        // Despair finisher window
        var fire4MinMp = context.Configuration.BlackMage.FireIVMinMp;
        // Prefer Despair when configured Fire IV count has been reached (tracked via Astral Soul stacks)
        var fireIVsReached = context.AstralSoulStacks >= context.Configuration.BlackMage.FireIVsBeforeDespair;
        if (fireIVsReached && context.Configuration.BlackMage.EnableDespair && level >= BLMActions.Despair.MinLevel
            && context.CurrentMp >= DespairMpCost)
        {
            if (context.HasFirestarter)
            {
                scheduler.PushGcd(HecateAbilities.Fire3, target.GameObjectId, priority: 5,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = "Fire III (Firestarter)";
                        context.Debug.DamageState = "Firestarter before Despair (FireIVs reached)";
                    });
                return;
            }
            var castTimeD = context.HasInstantCast || level >= 100 ? 0f : BLMActions.Despair.CastTime;
            if (!MechanicCastGate.ShouldBlock(context, castTimeD))
            {
                scheduler.PushGcd(HecateAbilities.Despair, target.GameObjectId, priority: 5,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = BLMActions.Despair.Name;
                        context.Debug.DamageState = "Despair (FireIVs before Despair reached)";
                    });
                return;
            }
        }

        if (context.Configuration.BlackMage.EnableDespair && level >= BLMActions.Despair.MinLevel
            && context.CurrentMp >= DespairMpCost && context.CurrentMp < fire4MinMp * 2)
        {
            if (context.HasFirestarter)
            {
                scheduler.PushGcd(HecateAbilities.Fire3, target.GameObjectId, priority: 5,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = "Fire III (Firestarter)";
                        context.Debug.DamageState = "Firestarter before Despair";
                    });
                return;
            }

            var castTime = context.HasInstantCast || level >= 100 ? 0f : BLMActions.Despair.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(HecateAbilities.Despair, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Despair.Name;
                    context.Debug.DamageState = "Despair (finisher)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Despair.ActionId, BLMActions.Despair.Name)
                        .AsCasterDamage().Target(target.Name?.TextValue)
                        .Reason("Despair - Fire phase finisher",
                            "Despair is the Fire phase finisher that consumes all remaining MP for high damage.")
                        .Factors($"MP: {context.CurrentMp}", "Can't cast more Fire IV")
                        .Alternatives("Force transition now")
                        .Tip("Despair should always end your Fire phase before Ice transition.")
                        .Concept(BlmConcepts.DespairTiming)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.DespairTiming, true, "Proper Despair timing");
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.FirePhase, true, "Fire phase finisher");
                });
            return;
        }

        if (level >= BLMActions.Fire4.MinLevel && context.CurrentMp >= fire4MinMp)
        {
            var castTime = context.HasInstantCast ? 0f : BLMActions.Fire4.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(HecateAbilities.Fire4, target.GameObjectId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Fire4.Name;
                    context.Debug.DamageState = $"Fire IV (MP: {context.CurrentMp})";
                    if (context.TrainingService?.IsTrainingEnabled == true && context.CurrentMp > 6000)
                    {
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(BLMActions.Fire4.ActionId, BLMActions.Fire4.Name)
                            .AsCasterDamage().Target(target.Name?.TextValue)
                            .Reason("Fire IV - main damage spell",
                                "Fire IV is your primary damage spell in Astral Fire. Spam it while you have MP. " +
                                "Each cast builds 1 Astral Soul stack (for Flare Star at 6).")
                            .Factors($"MP: {context.CurrentMp}", "In Astral Fire", $"Astral Soul: {context.AstralSoulStacks}/6")
                            .Alternatives("Use Paradox for timer", "Despair as finisher")
                            .Tip("Fire IV is your bread and butter - maximize casts before transitioning.")
                            .Concept(BlmConcepts.FireIvSpam)
                            .Record();
                        context.TrainingService.RecordConceptApplication(BlmConcepts.FireIvSpam, true, "Fire IV cast");
                    }
                });
            return;
        }

        // Low level: Fire I
        if (level < BLMActions.Fire4.MinLevel)
        {
            var castTime = context.HasInstantCast ? 0f : BLMActions.Fire.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(HecateAbilities.Fire, target.GameObjectId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Fire.Name;
                    context.Debug.DamageState = "Fire (low level)";
                });
            return;
        }

        if (context.CurrentMp < DespairMpCost)
            TryPushIceTransition(context, scheduler, target);
    }

    private void TryPushFireAoe(IHecateContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level >= BLMActions.Flare.MinLevel && context.CurrentMp >= context.Configuration.BlackMage.FireIVMinMp)
        {
            var castTime = context.HasInstantCast ? 0f : BLMActions.Flare.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(HecateAbilities.Flare, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Flare.Name;
                    context.Debug.DamageState = "Flare (AoE)";
                });
            return;
        }

        var fireAoe = BLMActions.GetFireAoe(level);
        var fireAoeAbility = fireAoe == BLMActions.HighFire2 ? HecateAbilities.HighFire2 : HecateAbilities.Fire2;
        if (level >= fireAoe.MinLevel && context.CurrentMp >= fireAoe.MpCost)
        {
            var castTime = context.HasInstantCast ? 0f : fireAoe.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(fireAoeAbility, target.GameObjectId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = fireAoe.Name;
                    context.Debug.DamageState = $"{fireAoe.Name} (AoE)";
                });
            return;
        }

        TryPushIceTransition(context, scheduler, target);
    }

    private void TryPushIceTransition(IHecateContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;
        var iceTransition = context.Configuration.BlackMage.UseBlizzardIIITransition
            ? BLMActions.GetIceTransition(level)
            : BLMActions.Blizzard;
        var ability = iceTransition == BLMActions.Blizzard3 ? HecateAbilities.Blizzard3 : HecateAbilities.Blizzard;
        var castTime = context.HasInstantCast ? 0f : iceTransition.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }
        scheduler.PushGcd(ability, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = iceTransition.Name;
                context.Debug.DamageState = "Transition to Ice";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(iceTransition.ActionId, iceTransition.Name)
                    .AsPhase("Astral Fire", "Umbral Ice")
                    .Reason("Transitioning to Umbral Ice",
                        "Moving from Astral Fire to Umbral Ice to recover MP.")
                    .Factors($"MP: {context.CurrentMp}", "Fire phase exhausted")
                    .Alternatives("Should have used Despair/Flare first")
                    .Tip("Always use your finisher (Despair/Flare) before transitioning to Ice.")
                    .Concept(BlmConcepts.ElementTransitions)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ElementTransitions, true, "Fire → Ice transition");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.UmbralIce, true, "Entering Ice phase");
            });
    }

    private void TryPushIcePhase(IHecateContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;
        context.Debug.Phase = "Ice";

        // Build to UI3 if at lower stacks
        if (context.UmbralIceStacks < 3)
        {
            var iceUpgrade = context.Configuration.BlackMage.UseBlizzardIIITransition
                ? BLMActions.GetIceTransition(level)
                : BLMActions.Blizzard;
            var ability = iceUpgrade == BLMActions.Blizzard3 ? HecateAbilities.Blizzard3 : HecateAbilities.Blizzard;
            var castTime = context.HasInstantCast ? 0f : iceUpgrade.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(ability, target.GameObjectId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = iceUpgrade.Name;
                    context.Debug.DamageState = $"{iceUpgrade.Name} (build UI3)";
                });
            return;
        }

        // Generate Umbral Hearts with Blizzard IV
        if (context.UmbralHearts < 3 && context.UmbralIceStacks == 3 && level >= BLMActions.Blizzard4.MinLevel)
        {
            var castTime = context.HasInstantCast ? 0f : BLMActions.Blizzard4.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }
            scheduler.PushGcd(HecateAbilities.Blizzard4, target.GameObjectId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Blizzard4.Name;
                    context.Debug.DamageState = "Blizzard IV (hearts)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Blizzard4.ActionId, BLMActions.Blizzard4.Name)
                        .AsCasterResource("Umbral Hearts", context.UmbralHearts)
                        .Reason("Blizzard IV - generating Umbral Hearts",
                            "Blizzard IV generates 3 Umbral Hearts in Umbral Ice III. These hearts reduce " +
                            "Fire IV MP cost by 1/3 each.")
                        .Factors("In Umbral Ice III", $"Hearts: {context.UmbralHearts} → 3")
                        .Alternatives("Skip hearts (suboptimal)")
                        .Tip("Never skip Blizzard IV - Umbral Hearts are essential for Fire phase efficiency.")
                        .Concept(BlmConcepts.UmbralHearts)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.UmbralHearts, true, "Generated Umbral Hearts");
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.IcePhase, true, "Ice phase hearts");
                });
            return;
        }

        // Apply/refresh Thunder
        if (!context.Configuration.BlackMage.MaintainThunder)
        {
            // Thunder maintenance is disabled; fall through to transition/filler
        }
        else if (!context.HasThunderDoT || context.ThunderDoTRemaining < context.Configuration.BlackMage.ThunderRefreshThreshold)
        {
            var thunderTargetHpPercent = target != null && target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
            if (target != null && thunderTargetHpPercent < context.Configuration.BlackMage.ThunderMinTargetHp)
            {
                // Target too low HP — skip Thunder
            }
            else if (context.HasThunderhead)
            {
                var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
                var ability = MapThunderAbility(thunderAction);
                scheduler.PushGcd(ability, target.GameObjectId, priority: 7,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = thunderAction.Name;
                        context.Debug.DamageState = "Thunder (DoT refresh)";
                    });
                return;
            }
            else if (level >= BLMActions.Thunder.MinLevel)
            {
                var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
                var ability = MapThunderAbility(thunderAction);
                var castTime = context.HasInstantCast ? 0f : thunderAction.CastTime;
                if (MechanicCastGate.ShouldBlock(context, castTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return;
                }
                scheduler.PushGcd(ability, target.GameObjectId, priority: 7,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = thunderAction.Name;
                        context.Debug.DamageState = "Thunder (hard cast)";
                    });
                return;
            }
        }

        // Paradox in Ice
        if (context.Configuration.BlackMage.EnableParadox && context.HasParadox && level >= BLMActions.Paradox.MinLevel)
        {
            scheduler.PushGcd(HecateAbilities.Paradox, target.GameObjectId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Paradox.Name;
                    context.Debug.DamageState = "Paradox (Ice phase)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Paradox.ActionId, BLMActions.Paradox.Name)
                        .AsCasterResource("Paradox", 1)
                        .Reason("Paradox in Ice phase (instant)",
                            "Paradox is instant in Umbral Ice III. Use it for free damage during Ice phase.")
                        .Factors("In Umbral Ice III", "Paradox ready", "Instant cast")
                        .Alternatives("Save for Fire phase timer refresh")
                        .Tip("Paradox in Ice is always instant and grants Firestarter - use it freely.")
                        .Concept(BlmConcepts.ParadoxMechanic)
                        .Record();
                });
            return;
        }

        if (context.MpPercent >= 0.99f && context.UmbralHearts >= 3)
        {
            TryPushFireTransition(context, scheduler, target);
            return;
        }
        if (context.MpPercent >= 0.99f && level < BLMActions.Blizzard4.MinLevel)
        {
            TryPushFireTransition(context, scheduler, target);
            return;
        }

        if (useAoe && context.UmbralHearts < 3)
        {
            var iceAoe = BLMActions.GetIceAoe(level);
            var ability = iceAoe == BLMActions.Freeze ? HecateAbilities.Freeze
                        : iceAoe == BLMActions.HighBlizzard2 ? HecateAbilities.HighBlizzard2
                        : HecateAbilities.Blizzard2;
            if (level >= iceAoe.MinLevel)
            {
                var castTime = context.HasInstantCast ? 0f : iceAoe.CastTime;
                if (MechanicCastGate.ShouldBlock(context, castTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return;
                }
                scheduler.PushGcd(ability, target.GameObjectId, priority: 8,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = iceAoe.Name;
                        context.Debug.DamageState = $"{iceAoe.Name} (hearts)";
                    });
                return;
            }
        }

        context.Debug.DamageState = $"Waiting for MP ({context.MpPercent * 100:F0}%)";
    }

    private void TryPushFireTransition(IHecateContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;

        if (context.HasFirestarter)
        {
            scheduler.PushGcd(HecateAbilities.Fire3, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = "Fire III (Firestarter)";
                    context.Debug.DamageState = "Transition to Fire (Firestarter)";
                });
            return;
        }

        var fireTransition = context.Configuration.BlackMage.UseFireIIITransition
            ? BLMActions.GetFireTransition(level)
            : BLMActions.Fire;
        var ability = fireTransition == BLMActions.Fire3 ? HecateAbilities.Fire3 : HecateAbilities.Fire;
        var castTime = context.HasInstantCast ? 0f : fireTransition.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }
        scheduler.PushGcd(ability, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = fireTransition.Name;
                context.Debug.DamageState = "Transition to Fire";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(fireTransition.ActionId, fireTransition.Name)
                    .AsPhase("Umbral Ice", "Astral Fire")
                    .Reason("Transitioning to Astral Fire",
                        "Moving from Umbral Ice to Astral Fire with full MP and 3 Umbral Hearts.")
                    .Factors("Full MP", $"Hearts: {context.UmbralHearts}", "Ice phase complete")
                    .Alternatives("Use Firestarter for instant transition")
                    .Tip("Always transition to Fire with full MP and 3 Umbral Hearts.")
                    .Concept(BlmConcepts.ElementTransitions)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ElementTransitions, true, "Ice → Fire transition");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralFire, true, "Entering Fire phase");
            });
    }

    private void TryPushStartRotation(IHecateContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;
        context.Debug.Phase = "Starting";

        var fireStarter = context.Configuration.BlackMage.UseFireIIITransition
            ? BLMActions.GetFireTransition(level)
            : BLMActions.Fire;
        var ability = fireStarter == BLMActions.Fire3 ? HecateAbilities.Fire3 : HecateAbilities.Fire;
        var castTime = context.HasInstantCast ? 0f : fireStarter.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }
        scheduler.PushGcd(ability, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = fireStarter.Name;
                context.Debug.DamageState = "Start rotation (Fire)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(fireStarter.ActionId, fireStarter.Name)
                    .AsPhase("None", "Astral Fire")
                    .Reason("Starting rotation with Fire III",
                        "Beginning the rotation by entering Astral Fire with Fire III.")
                    .Factors("Combat started", "No element active", "Full MP")
                    .Alternatives("Start with Ice (suboptimal)")
                    .Tip("Always start your rotation with Fire III for immediate full Astral Fire.")
                    .Concept(BlmConcepts.AstralFire)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralFire, true, "Rotation start");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.Enochian, true, "Enochian activated");
            });
    }

    private static AbilityBehavior MapThunderAbility(ActionDefinition action)
    {
        if (action == BLMActions.Thunder) return HecateAbilities.Thunder;
        if (action == BLMActions.Thunder3) return HecateAbilities.Thunder3;
        if (action == BLMActions.HighThunder) return HecateAbilities.HighThunder;
        if (action == BLMActions.Thunder2) return HecateAbilities.Thunder2;
        if (action == BLMActions.Thunder4) return HecateAbilities.Thunder4;
        if (action == BLMActions.HighThunder2) return HecateAbilities.HighThunder2;
        return HecateAbilities.Thunder;
    }

    #endregion
}
