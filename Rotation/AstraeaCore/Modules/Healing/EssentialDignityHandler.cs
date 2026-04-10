using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class EssentialDignityHandler : IHealingHandler
{
    public int Priority => 10;
    public string Name => "EssentialDignity";

    private static readonly string[] _alternatives =
    {
        "Celestial Intersection (heal + shield)",
        "Benefic II (GCD heal)",
        "Save charge for emergency",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryEssentialDignity(context);

    private bool TryEssentialDignity(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEssentialDignity)
            return false;

        if (player.Level < ASTActions.EssentialDignity.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.EssentialDignity.ActionId))
            return false;

        var target = context.PartyHelper.FindEssentialDignityTarget(player, config.EssentialDignityThreshold);
        if (target == null)
            return false;

        // Skip invuln/delayed-heal targets (Hallowed, Holmgang, Living Dead,
        // Superbolide, Excog, Catharsis) — a direct heal is guaranteed waste.
        if (HealerPartyHelper.HasNoHealStatus(target))
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var action = ASTActions.EssentialDignity;
        var hpPercent = context.PartyHelper.GetHpPercent(target);

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate (scales with low HP)
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.EssentialDignityState = "Used";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isEmergency = hpPercent < 0.3f;

                var shortReason = isEmergency
                    ? $"Emergency Dignity on {targetName} at {hpPercent:P0}!"
                    : $"Essential Dignity on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.EssentialDignityThreshold:P0}",
                    "Potency scales up to 1100 at low HP!",
                    "2 charges, 40s recharge",
                    "oGCD - can weave without clipping",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Essential Dignity",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Essential Dignity on {targetName} at {hpPercent:P0} HP. ED's potency scales from 400 at high HP to 1100 at very low HP, making it most efficient on low HP targets. {(isEmergency ? "Target was in critical condition!" : "Used proactively before HP dropped further.")} 2 charges with 40s recharge - don't sit on max charges!",
                    Factors = factors,
                    Alternatives = _alternatives,
                    Tip = "Essential Dignity is most efficient at low HP! Don't panic use it at 80% - wait until 50% or below for maximum value. But don't let anyone die holding charges either.",
                    ConceptId = AstConcepts.EssentialDignityUsage,
                    Priority = isEmergency ? ExplanationPriority.Critical : ExplanationPriority.High,
                });

                context.TrainingService?.RecordConceptApplication(AstConcepts.EssentialDignityUsage, wasSuccessful: true, isEmergency ? "Emergency heal" : "Proactive heal");
            }

            return true;
        }

        return false;
    }
}
