using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific buff module.
/// Extends base buff logic with Dissipation for Aetherflow management.
/// </summary>
public sealed class BuffModule : BaseBuffModule<AthenaContext>, IAthenaModule
{
    public override string Name => "Buff"; // SCH uses "Buff" instead of "Buffs"

    #region Base Class Overrides - Configuration

    protected override bool IsLucidDreamingEnabled(AthenaContext context) =>
        context.Configuration.Scholar.EnableLucidDreaming;

    protected override ActionDefinition GetLucidDreamingAction() =>
        SCHActions.LucidDreaming;

    protected override bool HasLucidDreaming(AthenaContext context) =>
        context.StatusHelper.HasLucidDreaming(context.Player);

    protected override float GetLucidDreamingThreshold(AthenaContext context) =>
        context.Configuration.Scholar.LucidDreamingThreshold;

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetLucidState(AthenaContext context, string state) =>
        context.Debug.LucidState = state;

    protected override void SetPlannedAction(AthenaContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SCH requires combat for buff usage.
    /// </summary>
    protected override bool RequiresCombat => true;

    /// <summary>
    /// SCH-specific buffs: Dissipation (sacrifice fairy for Aetherflow).
    /// Called after Lucid Dreaming since it's more situational.
    /// </summary>
    protected override bool TryJobSpecificUtilities(AthenaContext context, bool isMoving)
    {
        if (TryDissipation(context))
            return true;

        return false;
    }

    #endregion

    #region SCH-Specific Methods

    private bool TryDissipation(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableDissipation)
            return false;

        if (player.Level < SCHActions.Dissipation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Dissipation.ActionId))
            return false;

        // Don't use if fairy is not present (need Eos to sacrifice)
        if (!context.FairyStateManager.IsFairyAvailable)
            return false;

        // Don't use if we already have stacks (would waste it)
        if (context.AetherflowService.CurrentStacks > 0)
            return false;

        // Check fairy gauge - don't waste high gauge
        if (context.FairyGaugeService.CurrentGauge > config.DissipationMaxFairyGauge)
            return false;

        // Check party health - only use when party is healthy
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp < config.DissipationSafePartyHp)
            return false;

        if (context.ActionService.ExecuteOgcd(SCHActions.Dissipation, player.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.Dissipation.Name);
            context.Debug.PlanningState = "Dissipation";
            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(AthenaContext context)
    {
        var player = context.Player;
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        context.Debug.LucidState = mpPercent < context.Configuration.Scholar.LucidDreamingThreshold
            ? $"Low MP ({mpPercent:P0})"
            : $"OK ({mpPercent:P0})";
    }
}
