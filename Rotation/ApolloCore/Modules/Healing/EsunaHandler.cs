using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Debuff;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles debuff cleansing with Esuna.
/// Uses priority-based debuff detection for lethal and high-priority debuffs.
/// </summary>
public sealed class EsunaHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Esuna;
    public string Name => "Esuna";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.RoleActions.EnableEsuna)
        {
            context.Debug.EsunaState = "Disabled";
            return false;
        }

        if (player.Level < WHMActions.Esuna.MinLevel)
        {
            context.Debug.EsunaState = $"Level {player.Level} < {WHMActions.Esuna.MinLevel}";
            return false;
        }

        if (player.CurrentMp < WHMActions.Esuna.MpCost)
        {
            context.Debug.EsunaState = $"MP {player.CurrentMp} < {WHMActions.Esuna.MpCost}";
            return false;
        }

        var (target, statusId, priority) = FindBestEsunaTarget(context);
        if (target is null)
        {
            context.Debug.EsunaState = "No target";
            context.Debug.EsunaTarget = "None";
            return false;
        }

        if (priority != DebuffPriority.Lethal && (int)priority > config.RoleActions.EsunaPriorityThreshold)
        {
            context.Debug.EsunaState = $"Priority {priority} > threshold {config.RoleActions.EsunaPriorityThreshold}";
            return false;
        }

        if (isMoving)
        {
            context.Debug.EsunaState = "Moving";
            return false;
        }

        context.Debug.EsunaTarget = target.Name?.TextValue ?? "Unknown";
        context.Debug.EsunaState = $"Cleansing {priority} debuff";

        if (ActionExecutor.ExecuteGcd(context, WHMActions.Esuna, target.GameObjectId,
            target.Name?.TextValue ?? "Unknown", target.CurrentHp, "Esuna",
            appendThinAirNote: false))
        {
            return true;
        }

        return false;
    }

    private static (IBattleChara? target, uint statusId, DebuffPriority priority) FindBestEsunaTarget(ApolloContext context)
    {
        var player = context.Player;
        IBattleChara? bestTarget = null;
        uint bestStatusId = 0;
        var bestPriority = DebuffPriority.None;
        float bestRemainingTime = float.MaxValue;

        foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            if (!DistanceHelper.IsInRange(player, member, WHMActions.Esuna.Range))
                continue;

            var (statusId, priority, remainingTime) = context.DebuffDetectionService.FindHighestPriorityDebuff(member);

            if (priority == DebuffPriority.None)
                continue;

            if (priority < bestPriority ||
                (priority == bestPriority && remainingTime < bestRemainingTime))
            {
                bestTarget = member;
                bestStatusId = statusId;
                bestPriority = priority;
                bestRemainingTime = remainingTime;
            }
        }

        return (bestTarget, bestStatusId, bestPriority);
    }
}
