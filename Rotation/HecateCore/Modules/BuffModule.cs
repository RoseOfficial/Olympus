using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.HecateCore.Modules;

/// <summary>
/// Handles Black Mage oGCD buffs and cooldowns.
/// Manages Ley Lines, Triplecast, Amplifier, and Manafont.
/// </summary>
public sealed class BuffModule : IHecateModule
{
    public int Priority => 20; // Higher priority than damage (lower number = higher priority)
    public string Name => "Buff";

    public bool TryExecute(IHecateContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Amplifier - Generate Polyglot stack (don't overcap)
        if (TryAmplifier(context))
            return true;

        // Priority 2: Ley Lines - During burst windows
        if (TryLeyLines(context, isMoving))
            return true;

        // Priority 3: Triplecast - For movement or burst
        if (TryTriplecast(context, isMoving))
            return true;

        // Priority 4: Manafont - During Fire phase when low MP
        if (TryManafont(context))
            return true;

        context.Debug.BuffState = "No oGCD needed";
        return false;
    }

    public void UpdateDebugState(IHecateContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IHecateContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    /// <summary>
    /// Checks if movement is imminent (for Triplecast/Swiftcast planning).
    /// Returns true if a movement mechanic is expected soon.
    /// </summary>
    private bool IsMovementImminent(IHecateContext context, float windowSeconds = 5f)
    {
        var nextMovement = context.TimelineService?.GetNextMechanic(TimelineEntryType.Movement);
        if (nextMovement?.IsSoon != true || !nextMovement.Value.IsHighConfidence)
            return false;

        return nextMovement.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region oGCD Actions

    private bool TryAmplifier(IHecateContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.Amplifier.MinLevel)
            return false;

        if (!context.AmplifierReady)
            return false;

        // Don't use if we'd overcap Polyglot (max 3 at Lv.100, 2 before)
        var maxPolyglot = level >= 98 ? 3 : 2;
        if (context.PolyglotStacks >= maxPolyglot)
        {
            context.Debug.BuffState = "Polyglot full, hold Amplifier";

            // Training: Record held decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BLMActions.Amplifier.ActionId, BLMActions.Amplifier.Name)
                .AsCasterResource("Polyglot", context.PolyglotStacks)
                .Reason("Holding Amplifier - Polyglot at max",
                    "Amplifier generates 1 Polyglot stack instantly. Using it at max stacks would waste the resource. " +
                    "Spend Polyglot with Xenoglossy (single target) or Foul (AoE) before using Amplifier.")
                .Factors($"Polyglot: {context.PolyglotStacks}/{maxPolyglot}", "Would overcap")
                .Alternatives("Use Xenoglossy/Foul first")
                .Tip("Always spend Polyglot before using Amplifier to avoid overcapping.")
                .Concept(BlmConcepts.GaugeOvercapping)
                .Record();
            context.TrainingService?.RecordConceptApplication(BlmConcepts.GaugeOvercapping, true, "Avoided Polyglot overcap");

            return false;
        }

        // Only use during Enochian (Polyglot only generates with active element)
        if (!context.IsEnochianActive)
        {
            context.Debug.BuffState = "No Enochian, skip Amplifier";

            // Training: Record skipped decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BLMActions.Amplifier.ActionId, BLMActions.Amplifier.Name)
                .AsCasterResource("Polyglot", context.PolyglotStacks)
                .Reason("Skipping Amplifier - no Enochian",
                    "Amplifier requires an active element state (Astral Fire or Umbral Ice) to function. " +
                    "Without Enochian active, Polyglot stacks cannot be generated or maintained.")
                .Factors("No Enochian active", "Element timer expired")
                .Alternatives("Cast Fire III/Blizzard III first")
                .Tip("Maintain your element rotation to keep Enochian active for Polyglot generation.")
                .Concept(BlmConcepts.Enochian)
                .Record();

            return false;
        }

        if (context.ActionService.ExecuteOgcd(BLMActions.Amplifier, player.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.Amplifier.Name;
            context.Debug.BuffState = "Amplifier (+1 Polyglot)";

            // Training: Record usage
            TrainingHelper.Decision(context.TrainingService)
                .Action(BLMActions.Amplifier.ActionId, BLMActions.Amplifier.Name)
                .AsCasterResource("Polyglot", context.PolyglotStacks)
                .Reason("Amplifier - instant Polyglot stack",
                    "Amplifier grants 1 Polyglot stack instantly on a 120s cooldown. Use it on cooldown when " +
                    "you have room to gain stacks. Polyglot is spent on Xenoglossy (high single-target damage) " +
                    "or Foul (AoE damage).")
                .Factors($"Polyglot: {context.PolyglotStacks} → {context.PolyglotStacks + 1}", "Enochian active")
                .Alternatives("Hold for emergency movement")
                .Tip("Use Amplifier on cooldown but only when you have room for more Polyglot stacks.")
                .Concept(BlmConcepts.PolyglotStacks)
                .Record();
            context.TrainingService?.RecordConceptApplication(BlmConcepts.PolyglotStacks, true, "Generated Polyglot via Amplifier");

            return true;
        }

        return false;
    }

    private bool TryLeyLines(IHecateContext context, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.LeyLines.MinLevel)
            return false;

        if (!context.LeyLinesReady)
            return false;

        // Don't use Ley Lines while moving
        if (isMoving)
        {
            context.Debug.BuffState = "Moving, skip Ley Lines";
            return false;
        }

        // Don't use if already active
        if (context.HasLeyLines)
            return false;

        // Timeline: Don't waste burst before phase transition or movement
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Ley Lines (phase soon)";

            // Training: Record held decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BLMActions.LeyLines.ActionId, BLMActions.LeyLines.Name)
                .AsCasterBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason("Holding Ley Lines - phase transition soon",
                    "Ley Lines provides 15% spell speed for 30 seconds but requires you to stay in the circle. " +
                    "Placing it before a phase transition or forced movement wastes the buff duration.")
                .Factors("Phase transition imminent", "Would waste uptime")
                .Alternatives("Use after phase resolves")
                .Tip("Save Ley Lines for windows where you can stay stationary.")
                .Concept(BlmConcepts.LeyLines)
                .Record();

            return false;
        }

        if (IsMovementImminent(context))
        {
            context.Debug.BuffState = "Holding Ley Lines (movement soon)";

            // Training: Record held decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(BLMActions.LeyLines.ActionId, BLMActions.LeyLines.Name)
                .AsCasterBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason("Holding Ley Lines - movement mechanic soon",
                    "Ley Lines provides 15% spell speed but only while standing in the circle. " +
                    "Movement mechanics force you to leave, wasting the buff. Wait until after movement resolves.")
                .Factors("Movement mechanic soon", "Would lose uptime")
                .Alternatives("Wait for stationary window")
                .Tip("Plan Ley Lines around known movement mechanics in the fight.")
                .Concept(BlmConcepts.LeyLines)
                .Record();

            return false;
        }

        // Use during Fire phase for maximum DPS
        // Or at the start of combat
        if (!context.InAstralFire && context.InCombat)
        {
            context.Debug.BuffState = "Not in Fire, hold Ley Lines";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(BLMActions.LeyLines, player.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.LeyLines.Name;
            context.Debug.BuffState = "Ley Lines placed";

            // Training: Record usage
            TrainingHelper.Decision(context.TrainingService)
                .Action(BLMActions.LeyLines.ActionId, BLMActions.LeyLines.Name)
                .AsCasterBurst()
                .Priority(ExplanationPriority.High)
                .Target(player.Name?.TextValue ?? "Self")
                .Reason("Ley Lines - 15% spell speed buff",
                    "Ley Lines provides 15% spell speed for 30 seconds. Use during Astral Fire phase " +
                    "to maximize Fire IV casts. Avoid placing before phase transitions or forced movement mechanics.")
                .Factors("In Astral Fire", "Stationary window", "No phase transition soon")
                .Alternatives("Hold for burst alignment")
                .Tip("Place Ley Lines early in Fire phase for maximum uptime on your highest damage spells.")
                .Concept(BlmConcepts.LeyLines)
                .Record();
            context.TrainingService?.RecordConceptApplication(BlmConcepts.LeyLines, true, "Burst buff placed");

            return true;
        }

        return false;
    }

    private bool TryTriplecast(IHecateContext context, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.Triplecast.MinLevel)
            return false;

        // Need at least 1 charge
        if (context.TriplecastCharges == 0)
            return false;

        // Don't use if already have stacks
        if (context.TriplecastStacks > 0)
            return false;

        // Use for movement
        if (isMoving && !context.HasInstantCast)
        {
            if (context.ActionService.ExecuteOgcd(BLMActions.Triplecast, player.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                context.Debug.BuffState = "Triplecast (movement)";

                // Training: Record movement usage
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Triplecast.ActionId, BLMActions.Triplecast.Name)
                    .AsMovement()
                    .Reason("Triplecast for movement",
                        "Triplecast makes your next 3 spells instant. This is essential for maintaining DPS while " +
                        "handling movement mechanics. Using it reactively when forced to move.")
                    .Factors("Currently moving", "No instant cast available", $"Charges: {context.TriplecastCharges}")
                    .Alternatives("Use Xenoglossy instead", "Slidecast")
                    .Tip("Save at least one Triplecast charge for unexpected movement.")
                    .Concept(BlmConcepts.Triplecast)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.Triplecast, true, "Movement Triplecast");

                return true;
            }
        }

        // Timeline: Use proactively for upcoming movement
        if (IsMovementImminent(context) && !context.HasInstantCast)
        {
            if (context.ActionService.ExecuteOgcd(BLMActions.Triplecast, player.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                context.Debug.BuffState = "Triplecast (prepping for movement)";

                // Training: Record proactive usage
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Triplecast.ActionId, BLMActions.Triplecast.Name)
                    .AsMovement()
                    .Reason("Triplecast - preparing for movement",
                        "Using Triplecast proactively before an expected movement mechanic. This allows you to " +
                        "continue casting Fire IV while moving instead of losing GCDs to movement.")
                    .Factors("Movement mechanic soon", "No instant cast ready", "Preparing in advance")
                    .Alternatives("Wait for actual movement")
                    .Tip("Learning fight timelines helps you use Triplecast proactively for better uptime.")
                    .Concept(BlmConcepts.Triplecast)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.MovementOptimization, true, "Proactive Triplecast");

                return true;
            }
        }

        // Use during burst (Fire phase with Ley Lines or high stacks available)
        if (context.InAstralFire && context.TriplecastCharges >= 2)
        {
            if (context.ActionService.ExecuteOgcd(BLMActions.Triplecast, player.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                context.Debug.BuffState = "Triplecast (burst)";

                // Training: Record burst usage
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BLMActions.Triplecast.ActionId, BLMActions.Triplecast.Name)
                    .AsCasterBurst()
                    .Reason("Triplecast for burst DPS",
                        "Using Triplecast during Astral Fire phase to cast more Fire IVs faster. With 2 charges, " +
                        "we can afford to use one for DPS while keeping one for movement. Instant Fire IVs mean " +
                        "higher spell speed during burst windows.")
                    .Factors("In Astral Fire", "2 charges available", "Burst window")
                    .Alternatives("Save for movement")
                    .Tip("Balance Triplecast between movement utility and burst damage optimization.")
                    .Concept(BlmConcepts.Triplecast)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BlmConcepts.Triplecast, true, "Burst Triplecast");

                return true;
            }
        }

        return false;
    }

    private bool TryManafont(IHecateContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.Manafont.MinLevel)
            return false;

        if (!context.ManafontReady)
            return false;

        // Only use in Fire phase when MP is low
        if (!context.InAstralFire)
        {
            context.Debug.BuffState = "Not in Fire, skip Manafont";
            return false;
        }

        // Use when we've used all our Fire IV casts and need MP for Despair
        // Typically when MP is near 0 after Fire IVs
        if (context.CurrentMp > 1600)
        {
            context.Debug.BuffState = "MP too high, skip Manafont";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(BLMActions.Manafont, player.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.Manafont.Name;
            context.Debug.BuffState = "Manafont (MP restore)";

            // Training: Record usage
            TrainingHelper.Decision(context.TrainingService)
                .Action(BLMActions.Manafont.ActionId, BLMActions.Manafont.Name)
                .AsCasterResource("MP", context.CurrentMp)
                .Reason("Manafont - extending Fire phase",
                    "Manafont restores 10,000 MP and resets element timer. Use during Astral Fire when MP is low " +
                    "(after Fire IV spam) to cast additional Fire IV/Despair. This extends your Fire phase for " +
                    "more damage before transitioning to Umbral Ice.")
                .Factors("In Astral Fire", $"MP: {context.CurrentMp}", "Low MP threshold")
                .Alternatives("Transition to Ice instead")
                .Tip("Use Manafont after depleting MP with Fire IVs to squeeze more damage before Ice phase.")
                .Concept(BlmConcepts.Manafont)
                .Record();
            context.TrainingService?.RecordConceptApplication(BlmConcepts.Manafont, true, "MP restored in Fire phase");
            context.TrainingService?.RecordConceptApplication(BlmConcepts.MpManagement, true, "Extended Fire phase");

            return true;
        }

        return false;
    }

    #endregion
}
