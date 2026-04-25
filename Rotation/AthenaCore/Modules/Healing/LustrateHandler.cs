using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

public sealed class LustrateHandler : IHealingHandler
{
    public int Priority => 20;
    public string Name => "Lustrate";

    private static readonly string[] _lustrateAlternatives =
    {
        "Excogitation (proactive, auto-triggers)",
        "Adloquium (GCD, adds shield)",
        "Wait for fairy abilities",
    };

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableLustrate) return;
        if (player.Level < SCHActions.Lustrate.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Lustrate.ActionId)) return;
        if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve) return;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null) return;
        if (HealerPartyHelper.HasNoHealStatus(target)) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.LustrateThreshold) return;

        var action = SCHActions.Lustrate;
        var capturedTarget = target;
        var capturedHpPercent = hpPercent;

        scheduler.PushOgcd(AthenaAbilities.Lustrate, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.AetherflowService.ConsumeStack();

                var healAmount = action.HealPotency * 10;
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Lustrate";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var shortReason = capturedHpPercent < 0.3f
                        ? $"Emergency Lustrate on {targetName}!"
                        : $"Lustrate on {targetName} at {capturedHpPercent:P0}";

                    var factors = new[]
                    {
                        $"Target HP: {capturedHpPercent:P0}",
                        $"Threshold: {config.LustrateThreshold:P0}",
                        $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                        "600 potency instant heal",
                        "oGCD - can weave without clipping",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Lustrate",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Lustrate on {targetName} at {capturedHpPercent:P0} HP. Lustrate is SCH's emergency single-target oGCD heal at 600 potency. Used 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining). Lustrate is for reactive healing when someone is already low.",
                        Factors = factors,
                        Alternatives = _lustrateAlternatives,
                        Tip = "Lustrate is best for emergencies. For planned damage, Excogitation is usually better since it's proactive and higher potency (800). Save at least 1 Aetherflow for emergencies!",
                        ConceptId = SchConcepts.LustrateUsage,
                        Priority = capturedHpPercent < 0.3f ? ExplanationPriority.Critical : ExplanationPriority.High,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.LustrateUsage, wasSuccessful: true);
                }
            });
    }
}
