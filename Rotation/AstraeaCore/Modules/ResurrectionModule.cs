using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific resurrection module.
/// Uses base resurrection logic without job-specific buff synergies.
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<IAstraeaContext>, IAstraeaModule
{
    protected override ActionDefinition RaiseAction => ASTActions.Ascend;
    protected override ActionDefinition SwiftcastAction => ASTActions.Swiftcast;
    protected override int RaiseMpCost => ASTActions.Ascend.MpCost;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(IAstraeaContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(IAstraeaContext context) => context.HasSwiftcast;

    protected override void SetRaiseState(IAstraeaContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(IAstraeaContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(IAstraeaContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(IAstraeaContext context, string action) => context.Debug.PlannedAction = action;
    protected override IPartyCoordinationService? GetPartyCoordinationService(IAstraeaContext context) => context.PartyCoordinationService;

    // Astrologian doesn't have Thin Air equivalent, so no pre-raise buff waiting

    /// <summary>
    /// Records training explanation for raise decisions.
    /// </summary>
    protected override void RecordRaiseTraining(IAstraeaContext context, string targetName, bool hasSwiftcast, bool isHardcast)
    {
        if (context.TrainingService?.IsTrainingEnabled != true)
            return;

        var mpPercent = (float)context.Player.CurrentMp / context.Player.MaxMp;
        var hasLightspeed = context.HasLightspeed;

        string shortReason = hasSwiftcast
            ? $"Swiftcast Ascend on {targetName}"
            : hasLightspeed
                ? $"Lightspeed Ascend on {targetName}"
                : $"Hardcast Ascend on {targetName}";

        var factors = new[]
        {
            hasSwiftcast ? "Swiftcast active - instant cast" : hasLightspeed ? "Lightspeed active - instant cast" : "No instant cast available - hardcasting (8s)",
            $"MP: {mpPercent:P0} (2400 MP cost)",
            $"Target: {targetName} (dead party member)",
            "Dead party members = 0 contribution",
            "Raising has highest priority after emergency heals",
        };

        var alternatives = new[]
        {
            hasSwiftcast ? "Nothing - Swiftcast raise is optimal" : hasLightspeed ? "Nothing - Lightspeed raise is optimal" : "Wait for Swiftcast/Lightspeed",
            "Let co-healer raise",
            "DPS first if party is stable",
        };

        string tip = hasSwiftcast
            ? "Always use Swiftcast for raises when available. It lets you continue healing/DPSing immediately."
            : hasLightspeed
                ? "Lightspeed makes Ascend instant! Great alternative to Swiftcast for raises."
                : "Hardcast raises are expensive (2400 MP, 8s cast). AST has Lightspeed as an alternative to Swiftcast for instant raises!";

        var detailedReason = $"Raised {targetName} using " +
            (hasSwiftcast ? "Swiftcast (instant)" : hasLightspeed ? "Lightspeed (instant)" : "hardcast (8 second cast)") +
            $" at {mpPercent:P0} MP. Dead party members contribute nothing to the fight, so resurrection is always high priority. " +
            (hasSwiftcast || hasLightspeed
                ? "Instant raise is ideal because it doesn't interrupt your rotation."
                : "Hardcast is used when Swiftcast and Lightspeed are both on cooldown and the situation is stable enough to cast.");

        context.TrainingService.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = ASTActions.Ascend.ActionId,
            ActionName = "Ascend",
            Category = "Resurrection",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = AstConcepts.RaiseDecision,
            Priority = ExplanationPriority.High,
        });
    }
}
