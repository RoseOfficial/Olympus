using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific resurrection module.
/// Uses base resurrection logic without job-specific buff synergies.
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<AstraeaContext>, IAstraeaModule
{
    protected override ActionDefinition RaiseAction => ASTActions.Ascend;
    protected override ActionDefinition SwiftcastAction => ASTActions.Swiftcast;
    protected override int RaiseMpCost => ASTActions.Ascend.MpCost;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(AstraeaContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(AstraeaContext context) => context.HasSwiftcast;

    protected override void SetRaiseState(AstraeaContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(AstraeaContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(AstraeaContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(AstraeaContext context, string action) => context.Debug.PlannedAction = action;

    // Astrologian doesn't have Thin Air equivalent, so no pre-raise buff waiting
}
