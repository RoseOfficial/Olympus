using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles Lustrate for Scholar. Priority 20 in the oGCD list.
/// Costs 1 Aetherflow stack.
/// </summary>
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

    public bool TryExecute(IAthenaContext context, bool isMoving)
        => TryLustrate(context);

    private bool TryLustrate(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableLustrate)
            return false;

        if (player.Level < SCHActions.Lustrate.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Lustrate.ActionId))
            return false;

        // Respect Aetherflow reserve
        if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.LustrateThreshold)
            return false;

        var action = SCHActions.Lustrate;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();

            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Lustrate";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var shortReason = hpPercent < 0.3f
                    ? $"Emergency Lustrate on {targetName}!"
                    : $"Lustrate on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.LustrateThreshold:P0}",
                    $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                    "600 potency instant heal",
                    "oGCD - can weave without clipping",
                };

                var alternatives = _lustrateAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Lustrate",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Lustrate on {targetName} at {hpPercent:P0} HP. Lustrate is SCH's emergency single-target oGCD heal at 600 potency. Used 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining). Lustrate is for reactive healing when someone is already low.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Lustrate is best for emergencies. For planned damage, Excogitation is usually better since it's proactive and higher potency (800). Save at least 1 Aetherflow for emergencies!",
                    ConceptId = SchConcepts.LustrateUsage,
                    Priority = hpPercent < 0.3f ? ExplanationPriority.Critical : ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
