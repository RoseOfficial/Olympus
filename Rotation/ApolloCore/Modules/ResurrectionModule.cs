using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// WHM-specific resurrection module.
/// Extends base resurrection with Thin Air synergy for free raises.
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<ApolloContext>, IApolloModule
{
    protected override ActionDefinition RaiseAction => WHMActions.Raise;
    protected override ActionDefinition SwiftcastAction => WHMActions.Swiftcast;
    protected override int RaiseMpCost => 2400;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(ApolloContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(ApolloContext context) => context.HasSwiftcast;

    protected override void SetRaiseState(ApolloContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(ApolloContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(ApolloContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(ApolloContext context, string action) => context.Debug.PlannedAction = action;

    /// <summary>
    /// WHM should wait for Thin Air before raising if it's available and not already active.
    /// This provides a free 2400 MP raise.
    /// </summary>
    protected override bool ShouldWaitForPreRaiseBuff(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir || player.Level < WHMActions.ThinAir.MinLevel)
            return false;

        if (context.HasThinAir)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId))
            return false;

        return true;
    }

    /// <summary>
    /// Include Thin Air status in raise success notes.
    /// </summary>
    protected override string GetRaiseSuccessNote(ApolloContext context, bool hasSwiftcast)
    {
        var hasThinAir = context.HasThinAir;

        if (hasSwiftcast && hasThinAir)
            return " (Swiftcast + Thin Air)";
        if (hasSwiftcast)
            return " (Swiftcast)";
        if (hasThinAir)
            return " (Thin Air)";

        return "";
    }
}
