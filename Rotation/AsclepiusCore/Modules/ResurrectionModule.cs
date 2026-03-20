using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// SGE-specific resurrection module.
/// Uses Egeiro (Sage's raise spell) with Swiftcast.
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<IAsclepiusContext>, IAsclepiusModule
{
    protected override ActionDefinition RaiseAction => RoleActions.Egeiro;
    protected override ActionDefinition SwiftcastAction => RoleActions.Swiftcast;
    protected override int RaiseMpCost => 2400;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(IAsclepiusContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(IAsclepiusContext context) => context.HasSwiftcast;

    protected override void SetRaiseState(IAsclepiusContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(IAsclepiusContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(IAsclepiusContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(IAsclepiusContext context, string action) => context.Debug.PlannedAction = action;
    protected override IPartyCoordinationService? GetPartyCoordinationService(IAsclepiusContext context) => context.PartyCoordinationService;

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

    /// <summary>
    /// Records training explanation for raise decisions.
    /// </summary>
    protected override void RecordRaiseTraining(IAsclepiusContext context, string targetName, bool hasSwiftcast, bool isHardcast)
    {
        if (context.TrainingService?.IsTrainingEnabled != true)
            return;

        var mpPercent = (float)context.Player.CurrentMp / context.Player.MaxMp;

        string shortReason = hasSwiftcast
            ? $"Swiftcast Egeiro on {targetName}"
            : $"Hardcast Egeiro on {targetName}";

        var factors = new[]
        {
            hasSwiftcast ? "Swiftcast active - instant cast" : "No Swiftcast - hardcasting (8s)",
            $"MP: {mpPercent:P0} (2400 MP cost)",
            $"Target: {targetName} (dead party member)",
            "Dead party members = 0 contribution",
            "Raising has highest priority after emergency heals",
        };

        var alternatives = new[]
        {
            hasSwiftcast ? "Nothing - Swiftcast raise is optimal" : "Wait for Swiftcast (60s CD)",
            "Let co-healer raise",
            "DPS first if party is stable",
        };

        string tip = hasSwiftcast
            ? "Always use Swiftcast for raises when available. It lets you continue healing/DPSing immediately."
            : "Hardcast raises are expensive (2400 MP, 8s cast). Unlike WHM, SGE doesn't have Thin Air to reduce cost. Save Swiftcast for raises when possible!";

        var detailedReason = $"Raised {targetName} using " +
            (hasSwiftcast ? "Swiftcast (instant)" : "hardcast (8 second cast)") +
            $" at {mpPercent:P0} MP. Dead party members contribute nothing to the fight, so resurrection is always high priority. " +
            (hasSwiftcast
                ? "Instant raise is ideal because it doesn't interrupt your rotation."
                : "Hardcast is used when Swiftcast is on cooldown and the situation is stable enough to cast.");

        context.TrainingService.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.UtcNow,
            ActionId = RoleActions.Egeiro.ActionId,
            ActionName = "Egeiro",
            Category = "Resurrection",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = SgeConcepts.RaiseDecision,
            Priority = ExplanationPriority.High,
        });
    }
}
