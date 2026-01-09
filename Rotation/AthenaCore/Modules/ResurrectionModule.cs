using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific resurrection module.
/// Uses base resurrection logic without job-specific buff synergies.
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<AthenaContext>, IAthenaModule
{
    protected override ActionDefinition RaiseAction => SCHActions.Resurrection;
    protected override ActionDefinition SwiftcastAction => SCHActions.Swiftcast;
    protected override int RaiseMpCost => SCHActions.Resurrection.MpCost;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(AthenaContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(AthenaContext context) => context.HasSwiftcast;

    protected override void SetRaiseState(AthenaContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(AthenaContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(AthenaContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(AthenaContext context, string action) => context.Debug.PlannedAction = action;

    // Scholar doesn't have Thin Air equivalent, so no pre-raise buff waiting
}
