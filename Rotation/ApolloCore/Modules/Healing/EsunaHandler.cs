using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Debuff;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles debuff cleansing with Esuna.
/// </summary>
public sealed class EsunaHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Esuna;
    public string Name => "Esuna";

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.RoleActions.EnableEsuna) { context.Debug.EsunaState = "Disabled"; return; }
        if (player.Level < RoleActions.Esuna.MinLevel) { context.Debug.EsunaState = $"Level {player.Level} < {RoleActions.Esuna.MinLevel}"; return; }
        if (player.CurrentMp < RoleActions.Esuna.MpCost) { context.Debug.EsunaState = $"MP {player.CurrentMp} < {RoleActions.Esuna.MpCost}"; return; }

        var (target, statusId, priority) = EsunaHelper.FindBestTarget(
            player, context.PartyHelper.GetAllPartyMembers(player), context.DebuffDetectionService);
        if (target is null) { context.Debug.EsunaState = "No target"; context.Debug.EsunaTarget = "None"; return; }

        if (priority != DebuffPriority.Lethal && (int)priority > config.RoleActions.EsunaPriorityThreshold)
        {
            context.Debug.EsunaState = $"Priority {priority} > threshold {config.RoleActions.EsunaPriorityThreshold}";
            return;
        }

        if (isMoving && !context.HasSwiftcast) { context.Debug.EsunaState = "Moving (no Swiftcast)"; return; }

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;
        if (partyCoord?.IsCleanseTargetReservedByOther(targetEntityId) == true)
        {
            context.Debug.EsunaState = "Reserved by other";
            return;
        }

        if (partyCoord != null && !partyCoord.ReserveCleanseTarget(targetEntityId, statusId, RoleActions.Esuna.ActionId, (int)priority))
        {
            context.Debug.EsunaState = "Failed to reserve";
            return;
        }

        var capturedTarget = target;
        var capturedPriority = priority;
        var capturedStatusId = statusId;
        var capturedTargetEntityId = targetEntityId;

        var targetName = target.Name?.TextValue ?? "Unknown";
        context.Debug.EsunaTarget = targetName;
        context.Debug.EsunaState = $"Cleansing {priority} debuff";

        scheduler.PushGcd(ApolloAbilities.Esuna, target.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Esuna.Name;

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var priorityName = capturedPriority.ToString();
                    var name = capturedTarget.Name?.TextValue ?? "Unknown";
                    var shortReason = capturedPriority == DebuffPriority.Lethal
                        ? $"Lethal debuff on {name}!"
                        : $"Cleansing {priorityName} debuff on {name}";

                    var factors = new[]
                    {
                        $"Target: {name}",
                        $"Debuff priority: {priorityName}",
                        $"Status ID: {capturedStatusId}",
                        capturedPriority == DebuffPriority.Lethal ? "LETHAL - must cleanse immediately!" : "Dispellable debuff detected",
                    };

                    var alternatives = capturedPriority == DebuffPriority.Lethal
                        ? new[] { "Nothing - lethal debuffs must be cleansed" }
                        : new[] { "Wait for debuff to expire", "Focus on healing instead", "Let co-healer handle it" };

                    var tip = capturedPriority == DebuffPriority.Lethal
                        ? "Lethal debuffs kill if not cleansed! Always prioritize these over healing."
                        : "Look for the dispellable icon (white bar above debuff) to know what can be cleansed.";

                    var explanationPriority = capturedPriority == DebuffPriority.Lethal
                        ? ExplanationPriority.Critical
                        : ExplanationPriority.High;

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = RoleActions.Esuna.ActionId,
                        ActionName = "Esuna",
                        Category = "Utility",
                        TargetName = name,
                        ShortReason = shortReason,
                        DetailedReason = $"Esuna cleanses dispellable debuffs from party members. Used on {name} to remove a {priorityName} priority debuff. {(capturedPriority == DebuffPriority.Lethal ? "This debuff would kill the target if not removed!" : "Removing debuffs improves party performance and safety.")}",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = tip,
                        ConceptId = WhmConcepts.EsunaUsage,
                        Priority = explanationPriority,
                    });
                }
            });
    }
}
