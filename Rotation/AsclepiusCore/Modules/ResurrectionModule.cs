using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// SGE-specific resurrection module.
/// Uses Egeiro (Sage's raise spell) with Swiftcast.
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<IAsclepiusContext>, IAsclepiusModule
{
    protected override ActionDefinition RaiseAction => SGEActions.Egeiro;
    protected override ActionDefinition SwiftcastAction => SGEActions.Swiftcast;
    protected override int RaiseMpCost => 2400;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(IAsclepiusContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(IAsclepiusContext context) => context.HasSwiftcast;

    protected override void SetRaiseState(IAsclepiusContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(IAsclepiusContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(IAsclepiusContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(IAsclepiusContext context, string action) => context.Debug.PlannedAction = action;

    /// <summary>
    /// SGE doesn't have a pre-raise buff like WHM's Thin Air.
    /// </summary>
    protected override bool ShouldWaitForPreRaiseBuff(IAsclepiusContext context) => false;

    /// <summary>
    /// Basic success note for SGE raises.
    /// </summary>
    protected override string GetRaiseSuccessNote(IAsclepiusContext context, bool hasSwiftcast)
    {
        return hasSwiftcast ? " (Swiftcast)" : "";
    }
}
