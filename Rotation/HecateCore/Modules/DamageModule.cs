using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.HecateCore.Modules;

/// <summary>
/// Handles the Black Mage damage rotation.
/// Manages Fire/Ice phase transitions, Polyglot spending, and proc usage.
/// </summary>
public sealed class DamageModule : IHecateModule
{
    public int Priority => 30; // Lower priority than buffs (higher number = lower priority)
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    // MP thresholds
    private const int Fire4MpCost = 800;
    private const int DespairMpCost = 800;

    // Timer thresholds
    private const float ElementRefreshThreshold = 6f;
    private const float ThunderRefreshThreshold = 3f;

    public bool TryExecute(IHecateContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(8f, player);
        context.Debug.NearbyEnemies = enemyCount;

        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        var useAoe = enemyCount >= AoeThreshold;

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

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IHecateContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Movement Handling

    private bool TryMovementAction(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Xenoglossy (instant, high damage)
        if (context.PolyglotStacks > 0 && level >= BLMActions.Xenoglossy.MinLevel && !useAoe)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Xenoglossy, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Xenoglossy.Name;
                context.Debug.DamageState = "Xenoglossy (movement)";

                // Training: Record movement Xenoglossy
                CasterTrainingHelper.RecordMovementDecision(
                    context.TrainingService,
                    BLMActions.Xenoglossy.ActionId,
                    BLMActions.Xenoglossy.Name,
                    target.Name?.TextValue,
                    "Xenoglossy for movement",
                    "Xenoglossy is an instant-cast high-potency spell that spends 1 Polyglot stack. " +
                    "It's ideal for movement because it deals strong damage without requiring a cast time. " +
                    "Save Polyglot stacks for movement when possible.",
                    new[] { "Moving", $"Polyglot: {context.PolyglotStacks}", "Single target" },
                    new[] { "Use Triplecast", "Slidecast" },
                    "Xenoglossy is your best movement tool - save Polyglot for movement-heavy phases.",
                    BlmConcepts.XenoglossyUsage);
                context.TrainingService?.RecordConceptApplication(BlmConcepts.XenoglossyUsage, true, "Movement Xenoglossy");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.MovementOptimization, true, "Instant cast while moving");

                return true;
            }
        }

        // Priority 2: Foul for AoE
        if (context.PolyglotStacks > 0 && level >= BLMActions.Foul.MinLevel && useAoe)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Foul, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Foul.Name;
                context.Debug.DamageState = "Foul (movement AoE)";

                // Training: Record movement Foul
                CasterTrainingHelper.RecordMovementDecision(
                    context.TrainingService,
                    BLMActions.Foul.ActionId,
                    BLMActions.Foul.Name,
                    target.Name?.TextValue,
                    "Foul for AoE movement",
                    "Foul is the AoE version of Xenoglossy, spending 1 Polyglot for instant AoE damage. " +
                    "Use during movement when there are 3+ enemies.",
                    new[] { "Moving", $"Polyglot: {context.PolyglotStacks}", "AoE situation" },
                    new[] { "Use Xenoglossy on priority target" },
                    "In AoE situations, Foul is better than Xenoglossy for movement.",
                    BlmConcepts.AoeRotation);
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
                CasterTrainingHelper.RecordProcDecision(
                    context.TrainingService,
                    BLMActions.Fire3.ActionId,
                    "Fire III (Firestarter)",
                    "Firestarter",
                    target.Name?.TextValue,
                    "Firestarter proc for movement",
                    "Firestarter makes Fire III instant. Use it during movement to maintain DPS. " +
                    "The proc has a 30-second duration, so you have flexibility on when to use it.",
                    new[] { "Moving", "Firestarter active", $"Remaining: {context.FirestarterRemaining:F1}s" },
                    new[] { "Save for later movement" },
                    "Firestarter is great for movement but also useful for weaving oGCDs.",
                    BlmConcepts.FirestarterProc);
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
                CasterTrainingHelper.RecordProcDecision(
                    context.TrainingService,
                    thunderAction.ActionId,
                    thunderAction.Name,
                    "Thunderhead",
                    target.Name?.TextValue,
                    "Thunderhead proc for movement",
                    "Thunderhead makes Thunder instant and refreshes the DoT. Use during movement " +
                    "for instant damage plus DoT application/refresh.",
                    new[] { "Moving", "Thunderhead active", $"Remaining: {context.ThunderheadRemaining:F1}s" },
                    new[] { "Save for DoT refresh timing" },
                    "Thunderhead is flexible - use for movement or optimized DoT refresh timing.",
                    BlmConcepts.ThunderheadProc);
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ThunderheadProc, true, "Movement Thunderhead");

                return true;
            }
        }

        // Priority 5: Paradox in Umbral Ice III (instant)
        if (context.HasParadox && context.InUmbralIce && context.UmbralIceStacks == 3 && level >= BLMActions.Paradox.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (movement)";

                // Training: Record movement Paradox
                CasterTrainingHelper.RecordMovementDecision(
                    context.TrainingService,
                    BLMActions.Paradox.ActionId,
                    BLMActions.Paradox.Name,
                    target.Name?.TextValue,
                    "Paradox for movement (Ice phase)",
                    "Paradox is instant when cast in Umbral Ice III. This makes it perfect for " +
                    "movement during Ice phase while also refreshing your element timer.",
                    new[] { "Moving", "In Umbral Ice III", "Paradox ready" },
                    new[] { "Save for element refresh" },
                    "Paradox in Ice phase is always instant - use it for movement.",
                    BlmConcepts.ParadoxMechanic);
                context.TrainingService?.RecordConceptApplication(BlmConcepts.ParadoxMechanic, true, "Movement Paradox");

                return true;
            }
        }

        // Priority 6: Scathe (last resort)
        if (level >= BLMActions.Scathe.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Scathe, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Scathe.Name;
                context.Debug.DamageState = "Scathe (emergency movement)";

                // Training: Record emergency Scathe
                CasterTrainingHelper.RecordMovementDecision(
                    context.TrainingService,
                    BLMActions.Scathe.ActionId,
                    BLMActions.Scathe.Name,
                    target.Name?.TextValue,
                    "Scathe - emergency movement filler",
                    "Scathe is a weak instant-cast spell used only as a last resort when all other " +
                    "movement options are exhausted. Its low potency means you should avoid using it " +
                    "by better managing Triplecast, Xenoglossy, and procs.",
                    new[] { "Moving", "No better options", "All instants exhausted" },
                    new[] { "Should have saved Triplecast/Polyglot" },
                    "Avoid Scathe by planning movement tools ahead of mechanics.",
                    BlmConcepts.MovementOptimization);
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
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.FlareStar.MinLevel)
            return false;

        // Requires 6 Astral Soul stacks
        if (context.AstralSoulStacks < 6)
            return false;

        if (!context.ActionService.IsActionReady(BLMActions.FlareStar.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BLMActions.FlareStar, target.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.FlareStar.Name;
            context.Debug.DamageState = "Flare Star (6 stacks)";

            // Training: Record Flare Star usage
            CasterTrainingHelper.RecordBurstDecision(
                context.TrainingService,
                BLMActions.FlareStar.ActionId,
                BLMActions.FlareStar.Name,
                target.Name?.TextValue,
                "Flare Star at 6 Astral Soul stacks",
                "Flare Star is a powerful AoE finisher that requires 6 Astral Soul stacks. Stacks are built by " +
                "casting Fire IV. At 6 stacks, immediately cast Flare Star for massive damage. " +
                "This is your highest potency spell when fully charged.",
                new[] { "6 Astral Soul stacks", "In Astral Fire" },
                new[] { "Continue Fire IV spam" },
                "Flare Star at 6 stacks is mandatory - never let stacks go to waste.",
                BlmConcepts.AstralSoul);
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
                CasterTrainingHelper.RecordProcDecision(
                    context.TrainingService,
                    BLMActions.Fire3.ActionId,
                    "Fire III (Firestarter)",
                    "Firestarter",
                    target.Name?.TextValue,
                    "Firestarter proc expiring - must use now",
                    "Firestarter proc is about to expire. Use it immediately to avoid wasting the instant Fire III. " +
                    "Procs should generally be used before they expire unless specifically saving for movement.",
                    new[] { $"Firestarter: {context.FirestarterRemaining:F1}s remaining", "Will expire soon" },
                    new[] { "Would lose the proc" },
                    "Watch proc timers - don't let them expire. Use them for movement or weaving when possible.",
                    BlmConcepts.FirestarterProc);
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
                CasterTrainingHelper.RecordProcDecision(
                    context.TrainingService,
                    thunderAction.ActionId,
                    thunderAction.Name,
                    "Thunderhead",
                    target.Name?.TextValue,
                    $"Thunderhead - {reason}",
                    context.ThunderDoTRemaining < ThunderRefreshThreshold
                        ? "Thunder DoT is about to fall off. Thunderhead proc provides instant reapplication. " +
                          "Keeping Thunder DoT active is important for sustained damage."
                        : "Thunderhead proc is expiring. Use it to avoid losing the instant Thunder cast. " +
                          "Even if DoT has time remaining, the proc value is worth using.",
                    new[] { $"Thunderhead: {context.ThunderheadRemaining:F1}s", $"DoT: {context.ThunderDoTRemaining:F1}s" },
                    new[] { "Would lose proc/DoT uptime" },
                    "Balance proc usage between movement and DoT maintenance.",
                    BlmConcepts.ThunderheadProc);
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
            var action = (useAoe && level >= BLMActions.Foul.MinLevel) ? BLMActions.Foul :
                         (level >= BLMActions.Xenoglossy.MinLevel) ? BLMActions.Xenoglossy : BLMActions.Foul;

            if (level >= action.MinLevel && context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (cap avoidance)";

                // Training: Record cap avoidance
                CasterTrainingHelper.RecordResourceDecision(
                    context.TrainingService,
                    action.ActionId,
                    action.Name,
                    "Polyglot",
                    context.PolyglotStacks,
                    $"{action.Name} - avoiding Polyglot overcap",
                    "Polyglot stacks are at maximum capacity. Using Xenoglossy/Foul now to make room for " +
                    "incoming Polyglot generation. Overcapping wastes potential damage.",
                    new[] { $"Polyglot: {context.PolyglotStacks}/{maxPolyglot}", "At cap, must spend" },
                    new[] { "Would overcap next Polyglot" },
                    "Spend Polyglot before reaching max to avoid wasting stacks from Amplifier or natural generation.",
                    BlmConcepts.GaugeOvercapping);
                context.TrainingService?.RecordConceptApplication(BlmConcepts.GaugeOvercapping, true, "Avoided Polyglot overcap");

                return true;
            }
        }

        // Use for movement if needed
        if (isMoving && !context.HasInstantCast)
        {
            var action = (useAoe && level >= BLMActions.Foul.MinLevel) ? BLMActions.Foul :
                         (level >= BLMActions.Xenoglossy.MinLevel) ? BLMActions.Xenoglossy : BLMActions.Foul;

            if (level >= action.MinLevel && context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (movement)";

                // Training: Record movement usage (already covered in TryMovementAction, but this is fallback)
                CasterTrainingHelper.RecordMovementDecision(
                    context.TrainingService,
                    action.ActionId,
                    action.Name,
                    target.Name?.TextValue,
                    $"{action.Name} for movement",
                    $"{action.Name} provides instant damage during movement. Polyglot abilities are " +
                    "your best movement tools as they deal high damage without cast time.",
                    new[] { "Moving", $"Polyglot: {context.PolyglotStacks}", "No other instant available" },
                    new[] { "Use Triplecast" },
                    "Save Polyglot for movement-heavy phases of the fight.",
                    BlmConcepts.PolyglotStacks);
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
        if (context.HasParadox && context.ElementTimer < ElementRefreshThreshold && level >= BLMActions.Paradox.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (timer refresh)";

                // Training: Record timer refresh Paradox
                CasterTrainingHelper.RecordResourceDecision(
                    context.TrainingService,
                    BLMActions.Paradox.ActionId,
                    BLMActions.Paradox.Name,
                    "Element Timer",
                    (int)context.ElementTimer,
                    "Paradox - refreshing element timer",
                    "Element timer is getting low. Paradox refreshes the timer while dealing damage, " +
                    "allowing you to continue the Fire phase. Without Paradox, you'd need to use Fire/Blizzard " +
                    "to refresh the timer, which costs more MP or loses Fire phase.",
                    new[] { $"Element timer: {context.ElementTimer:F1}s", "Paradox ready", "Would drop Enochian" },
                    new[] { "Transition to Ice early" },
                    "Use Paradox in Fire phase to extend your damage window before transitioning to Ice.",
                    BlmConcepts.ElementTimer);
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
        if (level >= BLMActions.Despair.MinLevel && context.CurrentMp >= DespairMpCost && context.CurrentMp < Fire4MpCost * 2)
        {
            // Use Firestarter first if we have it
            if (context.HasFirestarter)
            {
                if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
                {
                    context.Debug.PlannedAction = "Fire III (Firestarter)";
                    context.Debug.DamageState = "Firestarter before Despair";

                    // Training: Record Firestarter before Despair
                    CasterTrainingHelper.RecordProcDecision(
                        context.TrainingService,
                        BLMActions.Fire3.ActionId,
                        "Fire III (Firestarter)",
                        "Firestarter",
                        target.Name?.TextValue,
                        "Firestarter before Despair finisher",
                        "Using Firestarter proc before Despair to maximize damage. The instant Fire III " +
                        "provides damage without spending the last of your MP, then Despair consumes remaining MP.",
                        new[] { $"MP: {context.CurrentMp}", "Firestarter active", "About to Despair" },
                        new[] { "Skip to Despair" },
                        "Weave procs when possible before phase-ending abilities.",
                        BlmConcepts.FirestarterProc);

                    return true;
                }
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Despair, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Despair.Name;
                context.Debug.DamageState = "Despair (finisher)";

                // Training: Record Despair finisher
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    BLMActions.Despair.ActionId,
                    BLMActions.Despair.Name,
                    target.Name?.TextValue,
                    "Despair - Fire phase finisher",
                    "Despair is the Fire phase finisher that consumes all remaining MP for high damage. " +
                    "Cast it when you can't afford another Fire IV. After Despair, transition to Umbral Ice.",
                    new[] { $"MP: {context.CurrentMp}", "Can't cast more Fire IV", "Fire phase ending" },
                    new[] { "Force transition now" },
                    "Despair should always end your Fire phase before Ice transition.",
                    BlmConcepts.DespairTiming);
                context.TrainingService?.RecordConceptApplication(BlmConcepts.DespairTiming, true, "Proper Despair timing");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.FirePhase, true, "Fire phase finisher");

                return true;
            }
        }

        // Spam Fire IV while we have MP
        if (level >= BLMActions.Fire4.MinLevel && context.CurrentMp >= Fire4MpCost)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire4, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Fire4.Name;
                context.Debug.DamageState = $"Fire IV (MP: {context.CurrentMp})";

                // Training: Record Fire IV (only occasionally to avoid spam)
                if (context.TrainingService?.IsTrainingEnabled == true && context.CurrentMp > 6000)
                {
                    CasterTrainingHelper.RecordDamageDecision(
                        context.TrainingService,
                        BLMActions.Fire4.ActionId,
                        BLMActions.Fire4.Name,
                        target.Name?.TextValue,
                        "Fire IV - main damage spell",
                        "Fire IV is your primary damage spell in Astral Fire. Spam it while you have MP. " +
                        "Each cast builds 1 Astral Soul stack (for Flare Star at 6). Watch element timer " +
                        "and use Paradox or transition before Enochian drops.",
                        new[] { $"MP: {context.CurrentMp}", "In Astral Fire", $"Astral Soul: {context.AstralSoulStacks}/6" },
                        new[] { "Use Paradox for timer", "Despair as finisher" },
                        "Fire IV is your bread and butter - maximize casts before transitioning to Ice.",
                        BlmConcepts.FireIvSpam);
                    context.TrainingService.RecordConceptApplication(BlmConcepts.FireIvSpam, true, "Fire IV cast");
                }

                return true;
            }
        }

        // Fallback: transition to Ice if out of MP
        if (context.CurrentMp < DespairMpCost)
        {
            return TryTransitionToIce(context, target, false);
        }

        // Low level: use Fire I
        if (level < BLMActions.Fire4.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Fire.Name;
                context.Debug.DamageState = "Fire (low level)";
                return true;
            }
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
            if (context.ActionService.ExecuteGcd(BLMActions.Flare, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Flare.Name;
                context.Debug.DamageState = "Flare (AoE)";

                // Training: Record Flare
                CasterTrainingHelper.RecordAoeDecision(
                    context.TrainingService,
                    BLMActions.Flare.ActionId,
                    BLMActions.Flare.Name,
                    enemyCount,
                    "Flare - AoE Fire finisher",
                    "Flare is a massive AoE that consumes all MP. In AoE situations, it replaces Despair " +
                    "as your Fire phase finisher. After Flare, transition to Ice to recover MP.",
                    new[] { $"Enemies: {enemyCount}", $"MP: {context.CurrentMp}", "In Astral Fire" },
                    new[] { "Continue Fire II spam" },
                    "Flare is your AoE phase finisher - use when you can't cast more Fire II.",
                    BlmConcepts.AoeRotation);
                context.TrainingService?.RecordConceptApplication(BlmConcepts.AoeRotation, true, "AoE Flare");

                return true;
            }
        }

        // High Fire II / Fire II
        var fireAoe = BLMActions.GetFireAoe(level);
        if (level >= fireAoe.MinLevel && context.CurrentMp >= fireAoe.MpCost)
        {
            if (context.ActionService.ExecuteGcd(fireAoe, target.GameObjectId))
            {
                context.Debug.PlannedAction = fireAoe.Name;
                context.Debug.DamageState = $"{fireAoe.Name} (AoE)";

                // Training: Record Fire II/High Fire II
                CasterTrainingHelper.RecordAoeDecision(
                    context.TrainingService,
                    fireAoe.ActionId,
                    fireAoe.Name,
                    enemyCount,
                    $"{fireAoe.Name} - AoE filler",
                    "Fire II/High Fire II is your AoE filler in Astral Fire. Spam it to deal AoE damage " +
                    "while building toward Flare finisher.",
                    new[] { $"Enemies: {enemyCount}", $"MP: {context.CurrentMp}", "AoE rotation" },
                    new[] { "Use Flare as finisher" },
                    "In AoE, replace Fire IV spam with Fire II/High Fire II spam.",
                    BlmConcepts.AoeRotation);

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
        if (context.ActionService.ExecuteGcd(iceTransition, target.GameObjectId))
        {
            context.Debug.PlannedAction = iceTransition.Name;
            context.Debug.DamageState = "Transition to Ice";

            // Training: Record phase transition
            CasterTrainingHelper.RecordPhaseDecision(
                context.TrainingService,
                iceTransition.ActionId,
                iceTransition.Name,
                "Astral Fire",
                "Umbral Ice",
                "Transitioning to Umbral Ice",
                "Moving from Astral Fire to Umbral Ice to recover MP. In Ice phase, you'll cast " +
                "Blizzard IV for Umbral Hearts, refresh Thunder DoT, and use Paradox if available. " +
                "Once MP is full and hearts are ready, transition back to Fire.",
                new[] { $"MP: {context.CurrentMp}", "Fire phase exhausted" },
                new[] { "Should have used Despair/Flare first" },
                "Always use your finisher (Despair/Flare) before transitioning to Ice.",
                BlmConcepts.ElementTransitions);
            context.TrainingService?.RecordConceptApplication(BlmConcepts.ElementTransitions, true, "Fire → Ice transition");
            context.TrainingService?.RecordConceptApplication(BlmConcepts.UmbralIce, true, "Entering Ice phase");

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

        // Priority 1: Get Umbral Hearts with Blizzard IV (requires UI3)
        if (context.UmbralHearts < 3 && context.UmbralIceStacks == 3 && level >= BLMActions.Blizzard4.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Blizzard4, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Blizzard4.Name;
                context.Debug.DamageState = "Blizzard IV (hearts)";

                // Training: Record Umbral Hearts generation
                CasterTrainingHelper.RecordResourceDecision(
                    context.TrainingService,
                    BLMActions.Blizzard4.ActionId,
                    BLMActions.Blizzard4.Name,
                    "Umbral Hearts",
                    context.UmbralHearts,
                    "Blizzard IV - generating Umbral Hearts",
                    "Blizzard IV generates 3 Umbral Hearts in Umbral Ice III. These hearts reduce " +
                    "Fire IV MP cost by 1/3 each, allowing more Fire IVs before running out of MP. " +
                    "Always get full hearts before transitioning to Fire.",
                    new[] { "In Umbral Ice III", $"Hearts: {context.UmbralHearts} → 3" },
                    new[] { "Skip hearts (suboptimal)" },
                    "Never skip Blizzard IV - Umbral Hearts are essential for Fire phase efficiency.",
                    BlmConcepts.UmbralHearts);
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
                if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = thunderAction.Name;
                    context.Debug.DamageState = "Thunder (DoT refresh)";

                    // Training: Record Thunder DoT refresh
                    CasterTrainingHelper.RecordDotDecision(
                        context.TrainingService,
                        thunderAction.ActionId,
                        thunderAction.Name,
                        target.Name?.TextValue,
                        context.ThunderDoTRemaining,
                        "Thunder DoT refresh with Thunderhead",
                        "Refreshing Thunder DoT using Thunderhead proc for instant cast. Ice phase is " +
                        "the ideal time to refresh DoT since you're recovering MP anyway.",
                        new[] { $"DoT: {context.ThunderDoTRemaining:F1}s remaining", "Thunderhead active" },
                        new[] { "Save proc for movement" },
                        "Ice phase is the natural time to refresh Thunder - no damage loss.",
                        BlmConcepts.ThunderDot);
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.ThunderDot, true, "DoT refreshed");

                    return true;
                }
            }
            else if (level >= BLMActions.Thunder.MinLevel)
            {
                // Hard cast Thunder if no Thunderhead proc
                var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
                if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = thunderAction.Name;
                    context.Debug.DamageState = "Thunder (hard cast)";

                    // Training: Record hard cast Thunder
                    CasterTrainingHelper.RecordDotDecision(
                        context.TrainingService,
                        thunderAction.ActionId,
                        thunderAction.Name,
                        target.Name?.TextValue,
                        context.ThunderDoTRemaining,
                        "Thunder DoT refresh (hard cast)",
                        "Hard casting Thunder to refresh DoT. No Thunderhead proc available, but DoT needs " +
                        "refresh. Ice phase is acceptable for hard cast since you're waiting for MP.",
                        new[] { $"DoT: {context.ThunderDoTRemaining:F1}s remaining", "No Thunderhead", "Ice phase" },
                        new[] { "Wait for Thunderhead proc" },
                        "Hard cast Thunder only in Ice phase or when DoT would fall off.",
                        BlmConcepts.ThunderDot);
                    context.TrainingService?.RecordConceptApplication(BlmConcepts.ThunderDot, true, "DoT maintained");

                    return true;
                }
            }
        }

        // Priority 3: Use Paradox if available
        if (context.HasParadox && level >= BLMActions.Paradox.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (Ice phase)";

                // Training: Record Ice phase Paradox
                CasterTrainingHelper.RecordResourceDecision(
                    context.TrainingService,
                    BLMActions.Paradox.ActionId,
                    BLMActions.Paradox.Name,
                    "Paradox",
                    1,
                    "Paradox in Ice phase (instant)",
                    "Paradox is instant in Umbral Ice III, making it ideal for maintaining DPS " +
                    "while recovering MP. It also grants a Firestarter proc for later use. " +
                    "Use it during Ice phase for free damage.",
                    new[] { "In Umbral Ice III", "Paradox ready", "Instant cast" },
                    new[] { "Save for Fire phase timer refresh" },
                    "Paradox in Ice is always instant and grants Firestarter - use it freely.",
                    BlmConcepts.ParadoxMechanic);
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
            if (level >= iceAoe.MinLevel && context.ActionService.ExecuteGcd(iceAoe, target.GameObjectId))
            {
                context.Debug.PlannedAction = iceAoe.Name;
                context.Debug.DamageState = $"{iceAoe.Name} (hearts)";

                // Training: Record AoE hearts
                var enemyCount = context.TargetingService.CountEnemiesInRange(8f, player);
                CasterTrainingHelper.RecordAoeDecision(
                    context.TrainingService,
                    iceAoe.ActionId,
                    iceAoe.Name,
                    enemyCount,
                    $"{iceAoe.Name} - AoE Umbral Hearts",
                    "Using AoE ice spell to generate Umbral Hearts in multi-target situations. " +
                    "This replaces Blizzard IV for AoE Ice phase.",
                    new[] { $"Enemies: {enemyCount}", "AoE rotation", $"Hearts: {context.UmbralHearts}" },
                    new[] { "Single-target Blizzard IV" },
                    "In AoE, use Freeze/High Blizzard II for hearts instead of Blizzard IV.",
                    BlmConcepts.AoeRotation);

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
                CasterTrainingHelper.RecordPhaseDecision(
                    context.TrainingService,
                    BLMActions.Fire3.ActionId,
                    "Fire III (Firestarter)",
                    "Umbral Ice",
                    "Astral Fire",
                    "Instant transition to Fire (Firestarter)",
                    "Using Firestarter proc for an instant transition from Ice to Fire. This is optimal " +
                    "because it saves a GCD of cast time. MP is full and hearts are ready.",
                    new[] { "Firestarter active", "Full MP", "Hearts ready" },
                    new[] { "Hard cast Fire III" },
                    "Use Firestarter for instant transitions when available.",
                    BlmConcepts.ElementTransitions);
                context.TrainingService?.RecordConceptApplication(BlmConcepts.FirestarterProc, true, "Transition Firestarter");
                context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralFire, true, "Entering Fire phase");

                return true;
            }
        }

        var fireTransition = BLMActions.GetFireTransition(level);
        if (context.ActionService.ExecuteGcd(fireTransition, target.GameObjectId))
        {
            context.Debug.PlannedAction = fireTransition.Name;
            context.Debug.DamageState = "Transition to Fire";

            // Training: Record Fire phase transition
            CasterTrainingHelper.RecordPhaseDecision(
                context.TrainingService,
                fireTransition.ActionId,
                fireTransition.Name,
                "Umbral Ice",
                "Astral Fire",
                "Transitioning to Astral Fire",
                "Moving from Umbral Ice to Astral Fire with full MP and 3 Umbral Hearts. " +
                "Hearts will reduce Fire IV MP cost, allowing more casts before running out. " +
                "Begin Fire IV spam immediately after transition.",
                new[] { "Full MP", $"Hearts: {context.UmbralHearts}", "Ice phase complete" },
                new[] { "Use Firestarter for instant transition" },
                "Always transition to Fire with full MP and 3 Umbral Hearts.",
                BlmConcepts.ElementTransitions);
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
        if (context.ActionService.ExecuteGcd(fireStarter, target.GameObjectId))
        {
            context.Debug.PlannedAction = fireStarter.Name;
            context.Debug.DamageState = "Start rotation (Fire)";

            // Training: Record rotation start
            CasterTrainingHelper.RecordPhaseDecision(
                context.TrainingService,
                fireStarter.ActionId,
                fireStarter.Name,
                "None",
                "Astral Fire",
                "Starting rotation with Fire III",
                "Beginning the rotation by entering Astral Fire with Fire III. This gives full AF3 stacks " +
                "immediately. From here, spam Fire IV until MP runs low, use Despair as finisher, " +
                "then transition to Ice phase.",
                new[] { "Combat started", "No element active", "Full MP" },
                new[] { "Start with Ice (suboptimal)" },
                "Always start your rotation with Fire III for immediate full Astral Fire.",
                BlmConcepts.AstralFire);
            context.TrainingService?.RecordConceptApplication(BlmConcepts.AstralFire, true, "Rotation start");
            context.TrainingService?.RecordConceptApplication(BlmConcepts.Enochian, true, "Enochian activated");

            return true;
        }

        return false;
    }

    #endregion
}
