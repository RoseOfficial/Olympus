using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HecateCore.Abilities;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Services;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.HecateCore.Modules;

/// <summary>
/// Handles Black Mage oGCD buffs and cooldowns (scheduler-driven).
/// Manages Ley Lines, Triplecast, Amplifier, Manafont, Transpose.
/// </summary>
public sealed class BuffModule : IHecateModule
{
    public int Priority => 20;
    public string Name => "Buff";

    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IHecateContext context, bool isMoving) => false;

    public void UpdateDebugState(IHecateContext context) { }

    public void CollectCandidates(IHecateContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        TryPushAmplifier(context, scheduler);
        TryPushLeyLines(context, scheduler, isMoving);
        TryPushTriplecast(context, scheduler, isMoving);
        TryPushManafont(context, scheduler);
        TryPushTranspose(context, scheduler);
    }

    private bool IsMovementImminent(IHecateContext context, float windowSeconds = 5f)
    {
        var nextMovement = context.TimelineService?.GetNextMechanic(TimelineEntryType.Movement);
        if (nextMovement?.IsSoon != true || !nextMovement.Value.IsHighConfidence)
            return false;
        return nextMovement.Value.SecondsUntil <= windowSeconds;
    }

    private void TryPushAmplifier(IHecateContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.BlackMage.EnableAmplifier) return;
        var player = context.Player;
        var level = player.Level;
        if (level < BLMActions.Amplifier.MinLevel) return;
        if (!context.AmplifierReady) return;

        var maxPolyglot = level >= 98 ? 3 : 2;
        if (context.PolyglotStacks >= maxPolyglot) return;
        if (!context.IsEnochianActive) return;
        if (ShouldHoldForBurst(8f)) return;

        scheduler.PushOgcd(HecateAbilities.Amplifier, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BLMActions.Amplifier.Name;
                context.Debug.BuffState = "Amplifier (+1 Polyglot)";
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
            });
    }

    private void TryPushLeyLines(IHecateContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.Configuration.BlackMage.EnableLeyLines) return;
        var player = context.Player;
        if (player.Level < BLMActions.LeyLines.MinLevel) return;
        if (!context.LeyLinesReady) return;
        if (isMoving) return;
        if (context.HasLeyLines) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Ley Lines (phase soon)";
            return;
        }
        if (IsMovementImminent(context))
        {
            context.Debug.BuffState = "Holding Ley Lines (movement soon)";
            return;
        }
        if (ShouldHoldForBurst(8f)) return;
        if (!context.InAstralFire && context.InCombat)
        {
            context.Debug.BuffState = "Not in Fire, hold Ley Lines";
            return;
        }

        scheduler.PushOgcd(HecateAbilities.LeyLines, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BLMActions.LeyLines.Name;
                context.Debug.BuffState = "Ley Lines placed";
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
            });
    }

    private void TryPushTriplecast(IHecateContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.Configuration.BlackMage.EnableTriplecast) return;
        var player = context.Player;
        if (player.Level < BLMActions.Triplecast.MinLevel) return;
        if (context.TriplecastCharges == 0) return;
        if (context.TriplecastStacks > 0) return;

        if (isMoving && !context.HasInstantCast)
        {
            scheduler.PushOgcd(HecateAbilities.Triplecast, player.GameObjectId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                    context.Debug.BuffState = "Triplecast (movement)";
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
                });
            return;
        }

        if (IsMovementImminent(context) && !context.HasInstantCast)
        {
            scheduler.PushOgcd(HecateAbilities.Triplecast, player.GameObjectId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                    context.Debug.BuffState = "Triplecast (prepping for movement)";
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
                });
            return;
        }

        if (context.InAstralFire && context.TriplecastCharges >= 2)
        {
            scheduler.PushOgcd(HecateAbilities.Triplecast, player.GameObjectId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                    context.Debug.BuffState = "Triplecast (burst)";
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
                });
        }
    }

    private void TryPushManafont(IHecateContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.BlackMage.EnableManafont) return;
        var player = context.Player;
        if (player.Level < BLMActions.Manafont.MinLevel) return;
        if (!context.ManafontReady) return;
        if (!context.InAstralFire) return;
        if (context.CurrentMp > 1600) return;

        scheduler.PushOgcd(HecateAbilities.Manafont, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BLMActions.Manafont.Name;
                context.Debug.BuffState = "Manafont (MP restore)";
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
            });
    }

    private void TryPushTranspose(IHecateContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;
        if (level < BLMActions.Transpose.MinLevel) return;
        if (level >= BLMActions.Fire4.MinLevel) return;
        if (!context.InAstralFire || context.AstralFireStacks < 3) return;
        if (context.CurrentMp >= 1600) return;
        if (!context.ActionService.IsActionReady(BLMActions.Transpose.ActionId)) return;

        scheduler.PushOgcd(HecateAbilities.Transpose, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BLMActions.Transpose.Name;
                context.Debug.BuffState = "Transpose (AF→UI, low MP)";
            });
    }
}
