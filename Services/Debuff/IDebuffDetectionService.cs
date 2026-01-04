using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services.Debuff;

/// <summary>
/// Interface for debuff detection and prioritization.
/// </summary>
public interface IDebuffDetectionService
{
    /// <summary>
    /// Checks if a status effect can be cleansed by Esuna.
    /// </summary>
    bool IsDispellable(uint statusId);

    /// <summary>
    /// Gets the priority tier for a dispellable debuff.
    /// </summary>
    DebuffPriority GetDebuffPriority(uint statusId);

    /// <summary>
    /// Finds the highest priority dispellable debuff on a target.
    /// </summary>
    (uint statusId, DebuffPriority priority, float remainingTime) FindHighestPriorityDebuff(IBattleChara target);
}
