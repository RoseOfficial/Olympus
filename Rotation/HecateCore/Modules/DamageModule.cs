using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.HecateCore.Modules;

/// <summary>
/// Handles the Black Mage damage rotation.
/// Manages Fire/Ice phase transitions, Polyglot spending, and proc usage.
/// Extends BaseDpsDamageModule for shared damage module patterns.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<IHecateContext>, IHecateModule
{
    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null) : base(burstWindowService, smartAoEService) { }

    // MP thresholds
    private const int Fire4MpCost = 800;
    private const int DespairMpCost = 800;

    // Timer thresholds
    private const float ElementRefreshThreshold = 6f;
    private const float ThunderRefreshThreshold = 3f;

    #region Abstract Method Implementations

    /// <summary>
    /// Caster targeting range (25y).
    /// </summary>
    protected override float GetTargetingRange() => FFXIVConstants.CasterTargetingRange;

    /// <summary>
    /// AoE count range for BLM (8y for most AoE spells).
    /// </summary>
    protected override float GetAoECountRange() => 8f;

    /// <summary>
    /// Sets the damage state in the debug display.
    /// </summary>
    protected override void SetDamageState(IHecateContext context, string state) =>
        context.Debug.DamageState = state;

    /// <summary>
    /// Sets the nearby enemy count in the debug display.
    /// </summary>
    protected override void SetNearbyEnemies(IHecateContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    /// <summary>
    /// Sets the planned action name in the debug display.
    /// </summary>
    protected override void SetPlannedAction(IHecateContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override bool IsAoEEnabled(IHecateContext context) =>
        context.Configuration.BlackMage.EnableAoERotation;

    protected override int GetConfiguredAoEThreshold(IHecateContext context) =>
        context.Configuration.BlackMage.AoEMinTargets;

    /// <summary>
    /// BLM has no damage oGCDs - all oGCDs (Triplecast, Ley Lines, etc.) are in BuffModule.
    /// </summary>
    protected override bool TryOgcdDamage(IHecateContext context, IBattleChara target, int enemyCount) => false;

    /// <summary>
    /// Main GCD damage rotation for Black Mage.
    /// Handles movement, procs, Polyglot, and Fire/Ice phase transitions.
    /// </summary>
    protected override bool TryGcdDamage(IHecateContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        var useAoe = ShouldUseAoE(enemyCount);

        // === MOVEMENT HANDLING ===
        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            if (TryMovementAction(context, target, useAoe))
                return true;
        }

        // === FLARE STAR (Lv.100 finisher) ===
        if (TryFlareStar(context, target))
            return true;

        // === PROC HANDLING (Firestarter, Thunderhead) ===
        if (TryProcs(context, target, useAoe))
            return true;

        // === POLYGLOT SPENDING ===
        if (TryPolyglot(context, target, useAoe, isMoving))
            return true;

        // === MAIN ROTATION ===
        if (context.InAstralFire)
        {
            // Fire Phase
            if (TryFirePhase(context, target, useAoe))
                return true;
        }
        else if (context.InUmbralIce)
        {
            // Ice Phase
            if (TryIcePhase(context, target, useAoe))
                return true;
        }
        else
        {
            // No element - start Fire phase
            if (TryStartRotation(context, target, useAoe))
                return true;
        }

        return false;
    }

    #endregion

    #region Movement Handling

    private bool TryMovementAction(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Xenoglossy (instant, high damage)
        if (context.Configuration.BlackMage.EnableXenoglossy && context.PolyglotStacks > 0 && level >= BLMActions.Xenoglossy.MinLevel && !useAoe)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Xenoglossy, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Xenoglossy.Name;
                context.Debug.DamageState = "Xenoglossy (movement)";

                // Training: Record movement Xenoglossy
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Xenoglossy.ActionId, BLMActions.Xenoglossy.Name)
                    .AsMovement()
                    .Target(target.Name?.TextValue)
                    .Reason("Xenoglossy for movement",
                        "Xenoglossy is an instant-cast high-potency spell that spends 1 Polyglot stack. " +
                        "It's ideal for movement because it deals strong damage without requiring a cast time. " +
                        "Save Polyglot stacks for movement when possible.")
                    .Factors("Moving", $"Polyglot: {context.PolyglotStacks}", "Single target")
                    .Alternatives("Use Triplecast", "Slidecast")
                    .Tip("Xenoglossy is your best movement tool - save Polyglot for movement-heavy phases.")
                    .Concept(BlmConcepts.XenoglossyUsage)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.XenoglossyUsage, true, "Movement Xenoglossy");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.MovementOptimization, true, "Instant cast while moving");

                return true;
            }
        }

        // Priority 2: Foul for AoE
        if (context.Configuration.BlackMage.EnableFoul && context.PolyglotStacks > 0 && level >= BLMActions.Foul.MinLevel && useAoe)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Foul, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Foul.Name;
                context.Debug.DamageState = "Foul (movement AoE)";

                // Training: Record movement Foul
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Foul.ActionId, BLMActions.Foul.Name)
                    .AsMovement()
                    .Target(target.Name?.TextValue)
                    .Reason("Foul for AoE movement",
                        "Foul is the AoE version of Xenoglossy, spending 1 Polyglot for instant AoE damage. " +
                        "Use during movement when there are 3+ enemies.")
                    .Factors("Moving", $"Polyglot: {context.PolyglotStacks}", "AoE situation")
                    .Alternatives("Use Xenoglossy on priority target")
                    .Tip("In AoE situations, Foul is better than Xenoglossy for movement.")
                    .Concept(BlmConcepts.AoeRotation)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.AoeRotation, true, "AoE movement ability");

                return true;
            }
        }

        // Priority 3: Firestarter proc (instant Fire III)
        if (context.HasFirestarter)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
            {
                context.Debug.PlannedAction = "Fire III (Firestarter)";
                context.Debug.DamageState = "Firestarter proc (movement)";

                // Training: Record movement Firestarter
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Fire3.ActionId, "Fire III (Firestarter)")
                    .AsCasterProc("Firestarter")
                    .Target(target.Name?.TextValue)
                    .Reason("Firestarter proc for movement",
                        "Firestarter makes Fire III instant. Use it during movement to maintain DPS. " +
                        "The proc has a 30-second duration, so you have flexibility on when to use it.")
                    .Factors("Moving", "Firestarter active", $"Remaining: {context.FirestarterRemaining:F1}s")
                    .Alternatives("Save for later movement")
                    .Tip("Firestarter is great for movement but also useful for weaving oGCDs.")
                    .Concept(BlmConcepts.FirestarterProc)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.FirestarterProc, true, "Movement Firestarter");

                return true;
            }
        }

        // Priority 4: Thunderhead proc (instant Thunder)
        if (context.HasThunderhead)
        {
            var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
            if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
            {
                context.Debug.PlannedAction = thunderAction.Name;
                context.Debug.DamageState = "Thunderhead proc (movement)";

                // Training: Record movement Thunderhead
                TrainingHelper.Decision(context.TrainingService)
                    .Action(thunderAction.ActionId, thunderAction.Name)
                    .AsCasterProc("Thunderhead")
                    .Target(target.Name?.TextValue)
                    .Reason("Thunderhead proc for movement",
                        "Thunderhead makes Thunder instant and refreshes the DoT. Use during movement " +
                        "for instant damage plus DoT application/refresh.")
                    .Factors("Moving", "Thunderhead active", $"Remaining: {context.ThunderheadRemaining:F1}s")
                    .Alternatives("Save for DoT refresh timing")
                    .Tip("Thunderhead is flexible - use for movement or optimized DoT refresh timing.")
                    .Concept(BlmConcepts.ThunderheadProc)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ThunderheadProc, true, "Movement Thunderhead");

                return true;
            }
        }

        // Priority 5: Paradox in Umbral Ice III (instant)
        if (context.Configuration.BlackMage.EnableParadox && context.HasParadox && context.InUmbralIce && context.UmbralIceStacks == 3 && level >= BLMActions.Paradox.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (movement)";

                // Training: Record movement Paradox
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Paradox.ActionId, BLMActions.Paradox.Name)
                    .AsMovement()
                    .Target(target.Name?.TextValue)
                    .Reason("Paradox for movement (Ice phase)",
                        "Paradox is instant when cast in Umbral Ice III. This makes it perfect for " +
                        "movement during Ice phase while also refreshing your element timer.")
                    .Factors("Moving", "In Umbral Ice III", "Paradox ready")
                    .Alternatives("Save for element refresh")
                    .Tip("Paradox in Ice phase is always instant - use it for movement.")
                    .Concept(BlmConcepts.ParadoxMechanic)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ParadoxMechanic, true, "Movement Paradox");

                return true;
            }
        }

        // Priority 6: Scathe (last resort)
        if (context.Configuration.BlackMage.UseScatheForMovement && level >= BLMActions.Scathe.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Scathe, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Scathe.Name;
                context.Debug.DamageState = "Scathe (emergency movement)";

                // Training: Record emergency Scathe
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Scathe.ActionId, BLMActions.Scathe.Name)
                    .AsMovement()
                    .Target(target.Name?.TextValue)
                    .Reason("Scathe - emergency movement filler",
                        "Scathe is a weak instant-cast spell used only as a last resort when all other " +
                        "movement options are exhausted. Its low potency means you should avoid using it " +
                        "by better managing Triplecast, Xenoglossy, and procs.")
                    .Factors("Moving", "No better options", "All instants exhausted")
                    .Alternatives("Should have saved Triplecast/Polyglot")
                    .Tip("Avoid Scathe by planning movement tools ahead of mechanics.")
                    .Concept(BlmConcepts.MovementOptimization)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.MovementOptimization, false, "Had to use Scathe");

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Flare Star

    private bool TryFlareStar(IHecateContext context, IBattleChara target)
    {
        if (!context.Configuration.BlackMage.EnableFlareStar)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.FlareStar.MinLevel)
            return false;

        // Requires 6 Astral Soul stacks
        if (context.AstralSoulStacks < 6)
            return false;

        if (!context.ActionService.IsActionReady(BLMActions.FlareStar.ActionId))
            return false;

        var flareStarCastTime = context.HasInstantCast ? 0f : BLMActions.FlareStar.CastTime;
        if (MechanicCastGate.ShouldBlock(context, flareStarCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return false;
        }

        if (context.ActionService.ExecuteGcd(BLMActions.FlareStar, target.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.FlareStar.Name;
            context.Debug.DamageState = "Flare Star (6 stacks)";

            // Training: Record Flare Star usage
            TrainingHelper.Decision(context.TrainingService)
                .Action(BLMActions.FlareStar.ActionId, BLMActions.FlareStar.Name)
                .AsCasterBurst()
                .Target(target.Name?.TextValue)
                .Reason("Flare Star at 6 Astral Soul stacks",
                    "Flare Star is a powerful AoE finisher that requires 6 Astral Soul stacks. Stacks are built by " +
                    "casting Fire IV. At 6 stacks, immediately cast Flare Star for massive damage. " +
                    "This is your highest potency spell when fully charged.")
                .Factors("6 Astral Soul stacks", "In Astral Fire")
                .Alternatives("Continue Fire IV spam")
                .Tip("Flare Star at 6 stacks is mandatory - never let stacks go to waste.")
                .Concept(BlmConcepts.AstralSoul)
                .Record();
            context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralSoul, true, "Spent 6 Astral Soul stacks");

            return true;
        }

        return false;
    }

    #endregion

    #region Procs

    private bool TryProcs(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Use Firestarter if about to expire
        if (context.HasFirestarter && context.FirestarterRemaining < 5f)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
            {
                context.Debug.PlannedAction = "Fire III (Firestarter)";
                context.Debug.DamageState = $"Firestarter expiring ({context.FirestarterRemaining:F1}s)";

                // Training: Record expiring Firestarter
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Fire3.ActionId, "Fire III (Firestarter)")
                    .AsCasterProc("Firestarter")
                    .Target(target.Name?.TextValue)
                    .Reason("Firestarter proc expiring - must use now",
                        "Firestarter proc is about to expire. Use it immediately to avoid wasting the instant Fire III. " +
                        "Procs should generally be used before they expire unless specifically saving for movement.")
                    .Factors($"Firestarter: {context.FirestarterRemaining:F1}s remaining", "Will expire soon")
                    .Alternatives("Would lose the proc")
                    .Tip("Watch proc timers - don't let them expire. Use them for movement or weaving when possible.")
                    .Concept(BlmConcepts.FirestarterProc)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ProcPriority, true, "Used expiring proc");

                return true;
            }
        }

        // Use Thunderhead if about to expire or DoT needs refresh
        if (context.HasThunderhead && (context.ThunderheadRemaining < 5f || context.ThunderDoTRemaining < ThunderRefreshThreshold))
        {
            var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
            if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
            {
                context.Debug.PlannedAction = thunderAction.Name;
                context.Debug.DamageState = $"Thunderhead expiring ({context.ThunderheadRemaining:F1}s)";

                // Training: Record expiring Thunderhead
                var reason = context.ThunderDoTRemaining < ThunderRefreshThreshold
                    ? "DoT needs refresh"
                    : "Proc expiring";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(thunderAction.ActionId, thunderAction.Name)
                    .AsCasterProc("Thunderhead")
                    .Target(target.Name?.TextValue)
                    .Reason($"Thunderhead - {reason}",
                        context.ThunderDoTRemaining < ThunderRefreshThreshold
                            ? "Thunder DoT is about to fall off. Thunderhead proc provides instant reapplication. " +
                              "Keeping Thunder DoT active is important for sustained damage."
                            : "Thunderhead proc is expiring. Use it to avoid losing the instant Thunder cast. " +
                              "Even if DoT has time remaining, the proc value is worth using.")
                    .Factors($"Thunderhead: {context.ThunderheadRemaining:F1}s", $"DoT: {context.ThunderDoTRemaining:F1}s")
                    .Alternatives("Would lose proc/DoT uptime")
                    .Tip("Balance proc usage between movement and DoT maintenance.")
                    .Concept(BlmConcepts.ThunderheadProc)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ThunderheadProc, true, "Used expiring proc");

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Polyglot

    private bool TryPolyglot(IHecateContext context, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (context.PolyglotStacks == 0)
            return false;

        var maxPolyglot = level >= 98 ? 3 : 2;

        // Use Polyglot if at max stacks (avoid overcapping)
        if (context.PolyglotStacks >= maxPolyglot)
        {
            var action = (context.Configuration.BlackMage.EnableFoul && useAoe && level >= BLMActions.Foul.MinLevel) ? BLMActions.Foul :
                         (context.Configuration.BlackMage.EnableXenoglossy && level >= BLMActions.Xenoglossy.MinLevel) ? BLMActions.Xenoglossy :
                         (context.Configuration.BlackMage.EnableFoul && level >= BLMActions.Foul.MinLevel) ? BLMActions.Foul : null;

            if (action != null && level >= action.MinLevel && context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (cap avoidance)";

                // Training: Record cap avoidance
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsCasterResource("Polyglot", context.PolyglotStacks)
                    .Reason($"{action.Name} - avoiding Polyglot overcap",
                        "Polyglot stacks are at maximum capacity. Using Xenoglossy/Foul now to make room for " +
                        "incoming Polyglot generation. Overcapping wastes potential damage.")
                    .Factors($"Polyglot: {context.PolyglotStacks}/{maxPolyglot}", "At cap, must spend")
                    .Alternatives("Would overcap next Polyglot")
                    .Tip("Spend Polyglot before reaching max to avoid wasting stacks from Amplifier or natural generation.")
                    .Concept(BlmConcepts.GaugeOvercapping)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.GaugeOvercapping, true, "Avoided Polyglot overcap");

                return true;
            }
        }

        // Hold Polyglot for burst when not at risk of capping (cap = maxPolyglot, hold at 1 if burst imminent)
        if (context.Configuration.BlackMage.EnableBurstPooling && ShouldHoldForBurst(8f) && context.PolyglotStacks < 2)
        {
            context.Debug.DamageState = $"Holding Polyglot for burst ({context.PolyglotStacks}/{maxPolyglot})";
            return false;
        }

        // Use for movement if needed
        if (isMoving && !context.HasInstantCast)
        {
            var action = (context.Configuration.BlackMage.EnableFoul && useAoe && level >= BLMActions.Foul.MinLevel) ? BLMActions.Foul :
                         (context.Configuration.BlackMage.EnableXenoglossy && level >= BLMActions.Xenoglossy.MinLevel) ? BLMActions.Xenoglossy :
                         (context.Configuration.BlackMage.EnableFoul && level >= BLMActions.Foul.MinLevel) ? BLMActions.Foul : null;

            if (action != null && level >= action.MinLevel && context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (movement)";

                // Training: Record movement usage (already covered in TryMovementAction, but this is fallback)
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMovement()
                    .Target(target.Name?.TextValue)
                    .Reason($"{action.Name} for movement",
                        $"{action.Name} provides instant damage during movement. Polyglot abilities are " +
                        "your best movement tools as they deal high damage without cast time.")
                    .Factors("Moving", $"Polyglot: {context.PolyglotStacks}", "No other instant available")
                    .Alternatives("Use Triplecast")
                    .Tip("Save Polyglot for movement-heavy phases of the fight.")
                    .Concept(BlmConcepts.PolyglotStacks)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.PolyglotStacks, true, "Movement Polyglot");

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Fire Phase

    private bool TryFirePhase(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        context.Debug.Phase = "Fire";

        // Check element timer - transition to Ice if about to drop
        if (context.ElementTimer < 3f && context.ElementTimer > 0)
        {
            return TryTransitionToIce(context, target, useAoe);
        }

        // Use Paradox to refresh timer if available and timer is low
        if (context.Configuration.BlackMage.EnableParadox && context.HasParadox && context.ElementTimer < ElementRefreshThreshold && level >= BLMActions.Paradox.MinLevel)
        {
            // Paradox in Astral Fire has a 2.5s cast time (not instant like in Umbral Ice)
            var paradoxFireCastTime = context.HasInstantCast ? 0f : BLMActions.Paradox.CastTime;
            if (MechanicCastGate.ShouldBlock(context, paradoxFireCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (timer refresh)";

                // Training: Record timer refresh Paradox
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Paradox.ActionId, BLMActions.Paradox.Name)
                    .AsCasterResource("Element Timer", (int)context.ElementTimer)
                    .Reason("Paradox - refreshing element timer",
                        "Element timer is getting low. Paradox refreshes the timer while dealing damage, " +
                        "allowing you to continue the Fire phase. Without Paradox, you'd need to use Fire/Blizzard " +
                        "to refresh the timer, which costs more MP or loses Fire phase.")
                    .Factors($"Element timer: {context.ElementTimer:F1}s", "Paradox ready", "Would drop Enochian")
                    .Alternatives("Transition to Ice early")
                    .Tip("Use Paradox in Fire phase to extend your damage window before transitioning to Ice.")
                    .Concept(BlmConcepts.ElementTimer)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ParadoxMechanic, true, "Timer refresh Paradox");

                return true;
            }
        }

        // AoE rotation
        if (useAoe)
        {
            return TryFireAoe(context, target);
        }

        // Single target Fire rotation
        return TryFireSingleTarget(context, target);
    }

    private bool TryFireSingleTarget(IHecateContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Cast Despair when low MP (finisher)
        if (context.Configuration.BlackMage.EnableDespair && level >= BLMActions.Despair.MinLevel && context.CurrentMp >= DespairMpCost && context.CurrentMp < Fire4MpCost * 2)
        {
            // Use Firestarter first if we have it
            if (context.HasFirestarter)
            {
                if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
                {
                    context.Debug.PlannedAction = "Fire III (Firestarter)";
                    context.Debug.DamageState = "Firestarter before Despair";

                    // Training: Record Firestarter before Despair
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Fire3.ActionId, "Fire III (Firestarter)")
                        .AsCasterProc("Firestarter")
                        .Target(target.Name?.TextValue)
                        .Reason("Firestarter before Despair finisher",
                            "Using Firestarter proc before Despair to maximize damage. The instant Fire III " +
                            "provides damage without spending the last of your MP, then Despair consumes remaining MP.")
                        .Factors($"MP: {context.CurrentMp}", "Firestarter active", "About to Despair")
                        .Alternatives("Skip to Despair")
                        .Tip("Weave procs when possible before phase-ending abilities.")
                        .Concept(BlmConcepts.FirestarterProc)
                        .Record();

                    return true;
                }
            }

            var despairCastTime = context.HasInstantCast || level >= 100 ? 0f : BLMActions.Despair.CastTime;
            if (MechanicCastGate.ShouldBlock(context, despairCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Despair, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Despair.Name;
                context.Debug.DamageState = "Despair (finisher)";

                // Training: Record Despair finisher
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Despair.ActionId, BLMActions.Despair.Name)
                    .AsCasterDamage()
                    .Target(target.Name?.TextValue)
                    .Reason("Despair - Fire phase finisher",
                        "Despair is the Fire phase finisher that consumes all remaining MP for high damage. " +
                        "Cast it when you can't afford another Fire IV. After Despair, transition to Umbral Ice.")
                    .Factors($"MP: {context.CurrentMp}", "Can't cast more Fire IV", "Fire phase ending")
                    .Alternatives("Force transition now")
                    .Tip("Despair should always end your Fire phase before Ice transition.")
                    .Concept(BlmConcepts.DespairTiming)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.DespairTiming, true, "Proper Despair timing");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.FirePhase, true, "Fire phase finisher");

                return true;
            }
        }

        // Spam Fire IV while we have MP
        if (level >= BLMActions.Fire4.MinLevel && context.CurrentMp >= Fire4MpCost)
        {
            var fire4CastTime = context.HasInstantCast ? 0f : BLMActions.Fire4.CastTime;
            if (MechanicCastGate.ShouldBlock(context, fire4CastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Fire4, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Fire4.Name;
                context.Debug.DamageState = $"Fire IV (MP: {context.CurrentMp})";

                // Training: Record Fire IV (only occasionally to avoid spam)
                if (context.TrainingService?.IsTrainingEnabled == true && context.CurrentMp > 6000)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BLMActions.Fire4.ActionId, BLMActions.Fire4.Name)
                        .AsCasterDamage()
                        .Target(target.Name?.TextValue)
                        .Reason("Fire IV - main damage spell",
                            "Fire IV is your primary damage spell in Astral Fire. Spam it while you have MP. " +
                            "Each cast builds 1 Astral Soul stack (for Flare Star at 6). Watch element timer " +
                            "and use Paradox or transition before Enochian drops.")
                        .Factors($"MP: {context.CurrentMp}", "In Astral Fire", $"Astral Soul: {context.AstralSoulStacks}/6")
                        .Alternatives("Use Paradox for timer", "Despair as finisher")
                        .Tip("Fire IV is your bread and butter - maximize casts before transitioning to Ice.")
                        .Concept(BlmConcepts.FireIvSpam)
                        .Record();
                    context.TrainingService.RecordConceptApplication(BlmConcepts.FireIvSpam, true, "Fire IV cast");
                }

                return true;
            }
        }

        // Low level: use Fire I (before Fire IV is unlocked)
        if (level < BLMActions.Fire4.MinLevel)
        {
            var fireCastTime = context.HasInstantCast ? 0f : BLMActions.Fire.CastTime;
            if (MechanicCastGate.ShouldBlock(context, fireCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Fire, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Fire.Name;
                context.Debug.DamageState = "Fire (low level)";

                // Training: Record low-level Fire I
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Fire.ActionId, BLMActions.Fire.Name)
                    .AsCasterDamage()
                    .Target(target.Name?.TextValue)
                    .Reason("Fire - main filler at low level",
                        "Before unlocking Fire IV, Fire is your primary Astral Fire filler. " +
                        "Continue casting it while you have MP, then transition to Blizzard for recovery.")
                    .Factors("Low level", "In Astral Fire", $"MP: {context.CurrentMp}")
                    .Alternatives("Transition to Ice")
                    .Tip("Fire IV replaces Fire as your main filler once you reach the required level.")
                    .Concept(BlmConcepts.FirePhase)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.FirePhase, true, "Low-level Fire cast");

                return true;
            }

            // Fire failed (MP too low in AF to afford another cast) — transition to Ice
            return TryTransitionToIce(context, target, false);
        }

        // Fallback: transition to Ice if out of MP for Fire IV
        if (context.CurrentMp < DespairMpCost)
        {
            return TryTransitionToIce(context, target, false);
        }

        return false;
    }

    private bool TryFireAoe(IHecateContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var enemyCount = context.TargetingService.CountEnemiesInRange(8f, player);

        // Use Flare when possible
        if (level >= BLMActions.Flare.MinLevel && context.CurrentMp >= 800)
        {
            var flareCastTime = context.HasInstantCast ? 0f : BLMActions.Flare.CastTime;
            if (MechanicCastGate.ShouldBlock(context, flareCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Flare, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Flare.Name;
                context.Debug.DamageState = "Flare (AoE)";

                // Training: Record Flare
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Flare.ActionId, BLMActions.Flare.Name)
                    .AsAoE(enemyCount)
                    .Reason("Flare - AoE Fire finisher",
                        "Flare is a massive AoE that consumes all MP. In AoE situations, it replaces Despair " +
                        "as your Fire phase finisher. After Flare, transition to Ice to recover MP.")
                    .Factors($"Enemies: {enemyCount}", $"MP: {context.CurrentMp}", "In Astral Fire")
                    .Alternatives("Continue Fire II spam")
                    .Tip("Flare is your AoE phase finisher - use when you can't cast more Fire II.")
                    .Concept(BlmConcepts.AoeRotation)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.AoeRotation, true, "AoE Flare");

                return true;
            }
        }

        // High Fire II / Fire II
        var fireAoe = BLMActions.GetFireAoe(level);
        if (level >= fireAoe.MinLevel && context.CurrentMp >= fireAoe.MpCost)
        {
            var fireAoeCastTime = context.HasInstantCast ? 0f : fireAoe.CastTime;
            if (MechanicCastGate.ShouldBlock(context, fireAoeCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(fireAoe, target.GameObjectId))
            {
                context.Debug.PlannedAction = fireAoe.Name;
                context.Debug.DamageState = $"{fireAoe.Name} (AoE)";

                // Training: Record Fire II/High Fire II
                TrainingHelper.Decision(context.TrainingService)
                    .Action(fireAoe.ActionId, fireAoe.Name)
                    .AsAoE(enemyCount)
                    .Reason($"{fireAoe.Name} - AoE filler",
                        "Fire II/High Fire II is your AoE filler in Astral Fire. Spam it to deal AoE damage " +
                        "while building toward Flare finisher.")
                    .Factors($"Enemies: {enemyCount}", $"MP: {context.CurrentMp}", "AoE rotation")
                    .Alternatives("Use Flare as finisher")
                    .Tip("In AoE, replace Fire IV spam with Fire II/High Fire II spam.")
                    .Concept(BlmConcepts.AoeRotation)
                    .Record();

                return true;
            }
        }

        // Out of MP, transition to Ice
        return TryTransitionToIce(context, target, true);
    }

    private bool TryTransitionToIce(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        var iceTransition = BLMActions.GetIceTransition(level);
        var iceTransitionCastTime = context.HasInstantCast ? 0f : iceTransition.CastTime;
        if (MechanicCastGate.ShouldBlock(context, iceTransitionCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return false;
        }

        if (context.ActionService.ExecuteGcd(iceTransition, target.GameObjectId))
        {
            context.Debug.PlannedAction = iceTransition.Name;
            context.Debug.DamageState = "Transition to Ice";

            // Training: Record phase transition
            TrainingHelper.Decision(context.TrainingService)
                .Action(iceTransition.ActionId, iceTransition.Name)
                .AsPhase("Astral Fire", "Umbral Ice")
                .Reason("Transitioning to Umbral Ice",
                    "Moving from Astral Fire to Umbral Ice to recover MP. In Ice phase, you'll cast " +
                    "Blizzard IV for Umbral Hearts, refresh Thunder DoT, and use Paradox if available. " +
                    "Once MP is full and hearts are ready, transition back to Fire.")
                .Factors($"MP: {context.CurrentMp}", "Fire phase exhausted")
                .Alternatives("Should have used Despair/Flare first")
                .Tip("Always use your finisher (Despair/Flare) before transitioning to Ice.")
                .Concept(BlmConcepts.ElementTransitions)
                .Record();
            context.TrainingService?.RecordConceptApplication(BlmConcepts.ElementTransitions, true, "Fire → Ice transition");
            context.TrainingService?.RecordConceptApplication(BlmConcepts.UmbralIce, true, "Entering Ice phase");

            return true;
        }

        // Fallback: if the level-appropriate spell failed (e.g., Blizzard III not yet unlocked),
        // try basic Blizzard which is available from level 1 and always free in Astral Fire
        if (iceTransition.ActionId != BLMActions.Blizzard.ActionId &&
            context.ActionService.ExecuteGcd(BLMActions.Blizzard, target.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.Blizzard.Name;
            context.Debug.DamageState = "Transition to Ice (Blizzard)";
            return true;
        }

        return false;
    }

    #endregion

    #region Ice Phase

    private bool TryIcePhase(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        context.Debug.Phase = "Ice";

        // Priority 1a: Build to UI3 if entered at UI1 (e.g., via Transpose) for faster MP regen
        // and to prevent element timer expiry before MP recovers
        if (context.UmbralIceStacks < 3)
        {
            var iceUpgrade = BLMActions.GetIceTransition(level);
            var iceUpgradeCastTime = context.HasInstantCast ? 0f : iceUpgrade.CastTime;
            if (MechanicCastGate.ShouldBlock(context, iceUpgradeCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(iceUpgrade, target.GameObjectId))
            {
                context.Debug.PlannedAction = iceUpgrade.Name;
                context.Debug.DamageState = $"{iceUpgrade.Name} (build UI3)";
                return true;
            }

            // Fallback to basic Blizzard if level-appropriate spell unavailable
            if (iceUpgrade.ActionId != BLMActions.Blizzard.ActionId &&
                context.ActionService.ExecuteGcd(BLMActions.Blizzard, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Blizzard.Name;
                context.Debug.DamageState = "Blizzard (build UI stacks)";
                return true;
            }
        }

        // Priority 1b: Get Umbral Hearts with Blizzard IV (requires UI3)
        if (context.UmbralHearts < 3 && context.UmbralIceStacks == 3 && level >= BLMActions.Blizzard4.MinLevel)
        {
            var blizzard4CastTime = context.HasInstantCast ? 0f : BLMActions.Blizzard4.CastTime;
            if (MechanicCastGate.ShouldBlock(context, blizzard4CastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Blizzard4, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Blizzard4.Name;
                context.Debug.DamageState = "Blizzard IV (hearts)";

                // Training: Record Umbral Hearts generation
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Blizzard4.ActionId, BLMActions.Blizzard4.Name)
                    .AsCasterResource("Umbral Hearts", context.UmbralHearts)
                    .Reason("Blizzard IV - generating Umbral Hearts",
                        "Blizzard IV generates 3 Umbral Hearts in Umbral Ice III. These hearts reduce " +
                        "Fire IV MP cost by 1/3 each, allowing more Fire IVs before running out of MP. " +
                        "Always get full hearts before transitioning to Fire.")
                    .Factors("In Umbral Ice III", $"Hearts: {context.UmbralHearts} → 3")
                    .Alternatives("Skip hearts (suboptimal)")
                    .Tip("Never skip Blizzard IV - Umbral Hearts are essential for Fire phase efficiency.")
                    .Concept(BlmConcepts.UmbralHearts)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.UmbralHearts, true, "Generated Umbral Hearts");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.IcePhase, true, "Ice phase hearts");

                return true;
            }
        }

        // Priority 2: Apply/refresh Thunder if needed
        if (!context.HasThunderDoT || context.ThunderDoTRemaining < ThunderRefreshThreshold)
        {
            if (context.HasThunderhead)
            {
                var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
                // Thunderhead proc makes Thunder instant regardless of base cast time
                var thunderProcCastTime = context.HasInstantCast || context.HasThunderhead ? 0f : thunderAction.CastTime;
                if (MechanicCastGate.ShouldBlock(context, thunderProcCastTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return false;
                }

                if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = thunderAction.Name;
                    context.Debug.DamageState = "Thunder (DoT refresh)";

                    // Training: Record Thunder DoT refresh
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(thunderAction.ActionId, thunderAction.Name)
                        .AsCasterDot(context.ThunderDoTRemaining)
                        .Target(target.Name?.TextValue)
                        .Reason("Thunder DoT refresh with Thunderhead",
                            "Refreshing Thunder DoT using Thunderhead proc for instant cast. Ice phase is " +
                            "the ideal time to refresh DoT since you're recovering MP anyway.")
                        .Factors($"DoT: {context.ThunderDoTRemaining:F1}s remaining", "Thunderhead active")
                        .Alternatives("Save proc for movement")
                        .Tip("Ice phase is the natural time to refresh Thunder - no damage loss.")
                        .Concept(BlmConcepts.ThunderDot)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.ThunderDot, true, "DoT refreshed");

                    return true;
                }
            }
            else if (level >= BLMActions.Thunder.MinLevel)
            {
                // Hard cast Thunder if no Thunderhead proc
                var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
                var thunderHardCastTime = context.HasInstantCast ? 0f : thunderAction.CastTime;
                if (MechanicCastGate.ShouldBlock(context, thunderHardCastTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return false;
                }

                if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = thunderAction.Name;
                    context.Debug.DamageState = "Thunder (hard cast)";

                    // Training: Record hard cast Thunder
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(thunderAction.ActionId, thunderAction.Name)
                        .AsCasterDot(context.ThunderDoTRemaining)
                        .Target(target.Name?.TextValue)
                        .Reason("Thunder DoT refresh (hard cast)",
                            "Hard casting Thunder to refresh DoT. No Thunderhead proc available, but DoT needs " +
                            "refresh. Ice phase is acceptable for hard cast since you're waiting for MP.")
                        .Factors($"DoT: {context.ThunderDoTRemaining:F1}s remaining", "No Thunderhead", "Ice phase")
                        .Alternatives("Wait for Thunderhead proc")
                        .Tip("Hard cast Thunder only in Ice phase or when DoT would fall off.")
                        .Concept(BlmConcepts.ThunderDot)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.ThunderDot, true, "DoT maintained");

                    return true;
                }
            }
        }

        // Priority 3: Use Paradox if available
        if (context.Configuration.BlackMage.EnableParadox && context.HasParadox && level >= BLMActions.Paradox.MinLevel)
        {
            // Paradox is instant in Umbral Ice III (always the case in TryIcePhase)
            var paradoxIceCastTime = context.HasInstantCast || context.InUmbralIce ? 0f : BLMActions.Paradox.CastTime;
            if (MechanicCastGate.ShouldBlock(context, paradoxIceCastTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return false;
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (Ice phase)";

                // Training: Record Ice phase Paradox
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Paradox.ActionId, BLMActions.Paradox.Name)
                    .AsCasterResource("Paradox", 1)
                    .Reason("Paradox in Ice phase (instant)",
                        "Paradox is instant in Umbral Ice III, making it ideal for maintaining DPS " +
                        "while recovering MP. It also grants a Firestarter proc for later use. " +
                        "Use it during Ice phase for free damage.")
                    .Factors("In Umbral Ice III", "Paradox ready", "Instant cast")
                    .Alternatives("Save for Fire phase timer refresh")
                    .Tip("Paradox in Ice is always instant and grants Firestarter - use it freely.")
                    .Concept(BlmConcepts.ParadoxMechanic)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ParadoxMechanic, true, "Ice phase Paradox");

                return true;
            }
        }

        // Priority 4: Transition to Fire when MP is full and hearts are ready
        if (context.MpPercent >= 0.99f && context.UmbralHearts >= 3)
        {
            return TryTransitionToFire(context, target, useAoe);
        }

        // Transition to Fire when MP is full (lower level, no hearts mechanic)
        if (context.MpPercent >= 0.99f && level < BLMActions.Blizzard4.MinLevel)
        {
            return TryTransitionToFire(context, target, useAoe);
        }

        // AoE in Ice: Freeze or High Blizzard II for hearts
        if (useAoe && context.UmbralHearts < 3)
        {
            var iceAoe = BLMActions.GetIceAoe(level);
            if (level >= iceAoe.MinLevel)
            {
                var iceAoeCastTime = context.HasInstantCast ? 0f : iceAoe.CastTime;
                if (MechanicCastGate.ShouldBlock(context, iceAoeCastTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return false;
                }
            }
            if (level >= iceAoe.MinLevel && context.ActionService.ExecuteGcd(iceAoe, target.GameObjectId))
            {
                context.Debug.PlannedAction = iceAoe.Name;
                context.Debug.DamageState = $"{iceAoe.Name} (hearts)";

                // Training: Record AoE hearts
                var enemyCount = context.TargetingService.CountEnemiesInRange(8f, player);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(iceAoe.ActionId, iceAoe.Name)
                    .AsAoE(enemyCount)
                    .Reason($"{iceAoe.Name} - AoE Umbral Hearts",
                        "Using AoE ice spell to generate Umbral Hearts in multi-target situations. " +
                        "This replaces Blizzard IV for AoE Ice phase.")
                    .Factors($"Enemies: {enemyCount}", "AoE rotation", $"Hearts: {context.UmbralHearts}")
                    .Alternatives("Single-target Blizzard IV")
                    .Tip("In AoE, use Freeze/High Blizzard II for hearts instead of Blizzard IV.")
                    .Concept(BlmConcepts.AoeRotation)
                    .Record();

                return true;
            }
        }

        // Wait for MP regen - use filler if needed
        context.Debug.DamageState = $"Waiting for MP ({context.MpPercent * 100:F0}%)";
        return false;
    }

    private bool TryTransitionToFire(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Use Firestarter if available for instant transition
        if (context.HasFirestarter)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
            {
                context.Debug.PlannedAction = "Fire III (Firestarter)";
                context.Debug.DamageState = "Transition to Fire (Firestarter)";

                // Training: Record instant Fire transition
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Fire3.ActionId, "Fire III (Firestarter)")
                    .AsPhase("Umbral Ice", "Astral Fire")
                    .Reason("Instant transition to Fire (Firestarter)",
                        "Using Firestarter proc for an instant transition from Ice to Fire. This is optimal " +
                        "because it saves a GCD of cast time. MP is full and hearts are ready.")
                    .Factors("Firestarter active", "Full MP", "Hearts ready")
                    .Alternatives("Hard cast Fire III")
                    .Tip("Use Firestarter for instant transitions when available.")
                    .Concept(BlmConcepts.ElementTransitions)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.FirestarterProc, true, "Transition Firestarter");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralFire, true, "Entering Fire phase");

                return true;
            }
        }

        var fireTransition = BLMActions.GetFireTransition(level);
        var fireTransitionCastTime = context.HasInstantCast ? 0f : fireTransition.CastTime;
        if (MechanicCastGate.ShouldBlock(context, fireTransitionCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return false;
        }

        if (context.ActionService.ExecuteGcd(fireTransition, target.GameObjectId))
        {
            context.Debug.PlannedAction = fireTransition.Name;
            context.Debug.DamageState = "Transition to Fire";

            // Training: Record Fire phase transition
            TrainingHelper.Decision(context.TrainingService)
                .Action(fireTransition.ActionId, fireTransition.Name)
                .AsPhase("Umbral Ice", "Astral Fire")
                .Reason("Transitioning to Astral Fire",
                    "Moving from Umbral Ice to Astral Fire with full MP and 3 Umbral Hearts. " +
                    "Hearts will reduce Fire IV MP cost, allowing more casts before running out. " +
                    "Begin Fire IV spam immediately after transition.")
                .Factors("Full MP", $"Hearts: {context.UmbralHearts}", "Ice phase complete")
                .Alternatives("Use Firestarter for instant transition")
                .Tip("Always transition to Fire with full MP and 3 Umbral Hearts.")
                .Concept(BlmConcepts.ElementTransitions)
                .Record();
            context.TrainingService?.RecordConceptApplication(BlmConcepts.ElementTransitions, true, "Ice → Fire transition");
            context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralFire, true, "Entering Fire phase");

            return true;
        }

        return false;
    }

    #endregion

    #region Start Rotation

    private bool TryStartRotation(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        context.Debug.Phase = "Starting";

        // Start with Fire III for full AF3
        var fireStarter = BLMActions.GetFireTransition(level);
        var fireStarterCastTime = context.HasInstantCast ? 0f : fireStarter.CastTime;
        if (MechanicCastGate.ShouldBlock(context, fireStarterCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return false;
        }

        if (context.ActionService.ExecuteGcd(fireStarter, target.GameObjectId))
        {
            context.Debug.PlannedAction = fireStarter.Name;
            context.Debug.DamageState = "Start rotation (Fire)";

            // Training: Record rotation start
            TrainingHelper.Decision(context.TrainingService)
                .Action(fireStarter.ActionId, fireStarter.Name)
                .AsPhase("None", "Astral Fire")
                .Reason("Starting rotation with Fire III",
                    "Beginning the rotation by entering Astral Fire with Fire III. This gives full AF3 stacks " +
                    "immediately. From here, spam Fire IV until MP runs low, use Despair as finisher, " +
                    "then transition to Ice phase.")
                .Factors("Combat started", "No element active", "Full MP")
                .Alternatives("Start with Ice (suboptimal)")
                .Tip("Always start your rotation with Fire III for immediate full Astral Fire.")
                .Concept(BlmConcepts.AstralFire)
                .Record();
            context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralFire, true, "Rotation start");
            context.TrainingService?.RecordConceptApplication(BlmConcepts.Enochian, true, "Enochian activated");

            return true;
        }

        return false;
    }

    #endregion
}
