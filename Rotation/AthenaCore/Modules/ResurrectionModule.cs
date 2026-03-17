using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific resurrection module.
/// Uses base resurrection logic without job-specific buff synergies.
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<IAthenaContext>, IAthenaModule
{
    protected override ActionDefinition RaiseAction => RoleActions.Resurrection;
    protected override ActionDefinition SwiftcastAction => RoleActions.Swiftcast;
    protected override int RaiseMpCost => RoleActions.Resurrection.MpCost;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(IAthenaContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(IAthenaContext context) => context.HasSwiftcast;

    protected override void SetRaiseState(IAthenaContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(IAthenaContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(IAthenaContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(IAthenaContext context, string action) => context.Debug.PlannedAction = action;
    protected override IPartyCoordinationService? GetPartyCoordinationService(IAthenaContext context) => context.PartyCoordinationService;

    // Scholar doesn't have Thin Air equivalent, so no pre-raise buff waiting

    /// <summary>
    /// Records training explanation for raise decisions.
    /// </summary>
    protected override void RecordRaiseTraining(IAthenaContext context, string targetName, bool hasSwiftcast, bool isHardcast)
    {
        if (context.TrainingService?.IsTrainingEnabled != true)
            return;

        var mpPercent = (float)context.Player.CurrentMp / context.Player.MaxMp;

        string shortReason = hasSwiftcast
            ? $"Swiftcast Resurrection on {targetName}"
            : $"Hardcast Resurrection on {targetName}";

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
            hasSwiftcast ? "Nothing - Swiftcast raise is optimal" : "Wait for Swiftcast (if available soon)",
            "Let co-healer raise",
            "DPS first if party is stable",
        };

        string tip = hasSwiftcast
            ? "Always use Swiftcast for raises when available. It lets you continue healing/DPSing immediately."
            : "Hardcast raises are expensive (2400 MP, 8s cast). Try to have Swiftcast ready for raises.";

        var detailedReason = $"Raised {targetName} using " +
            (hasSwiftcast ? "Swiftcast (instant)" : "hardcast (8 second cast)") +
            $" at {mpPercent:P0} MP. Dead party members contribute nothing to the fight, so resurrection is always high priority. " +
            (hasSwiftcast
                ? "Swiftcast is ideal because it's instant and doesn't interrupt your healing rotation."
                : "Hardcast is used when Swiftcast is on cooldown (>10s remaining) and the situation is stable enough to cast.");

        context.TrainingService.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = RoleActions.Resurrection.ActionId,
            ActionName = "Resurrection",
            Category = "Resurrection",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = SchConcepts.RaiseDecision,
            Priority = ExplanationPriority.High,
        });

        context.TrainingService.RecordConceptApplication(SchConcepts.RaiseDecision, wasSuccessful: true);
    }
}
