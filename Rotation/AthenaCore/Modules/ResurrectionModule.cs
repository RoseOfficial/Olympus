using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific resurrection module (scheduler-driven).
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
            Timestamp = DateTime.UtcNow,
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

    public override bool TryExecute(IAthenaContext context, bool isMoving) => false;

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        TryPushSwiftcast(context, scheduler);
        TryPushRaise(context, scheduler, isMoving);
    }

    private void TryPushSwiftcast(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Resurrection.EnableRaise) return;
        if (player.Level < SwiftcastAction.MinLevel) return;
        if (HasSwiftcast(context)) return;

        var deadMember = FindDeadPartyMemberNeedingRaise(context);
        if (deadMember is null) return;

        if (player.CurrentMp < RaiseMpCost) return;
        if (!context.ActionService.IsActionReady(SwiftcastAction.ActionId)) return;

        scheduler.PushOgcd(AthenaAbilities.Swiftcast, player.GameObjectId, priority: 1);
    }

    private void TryPushRaise(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Resurrection.EnableRaise) { SetRaiseState(context, "Disabled"); return; }
        if (player.Level < RaiseAction.MinLevel) { SetRaiseState(context, $"Level {player.Level} < {RaiseAction.MinLevel}"); return; }

        var mpPercent = (float)player.CurrentMp / player.MaxMp;
        if (mpPercent < config.Resurrection.RaiseMpThreshold) { SetRaiseState(context, $"MP {mpPercent:P0} < {config.Resurrection.RaiseMpThreshold:P0}"); return; }
        if (player.CurrentMp < RaiseMpCost) { SetRaiseState(context, $"MP {player.CurrentMp} < {RaiseMpCost}"); return; }

        var target = FindDeadPartyMemberNeedingRaise(context);
        if (target is null) { SetRaiseState(context, "No target"); SetRaiseTarget(context, "None"); return; }

        var targetName = target.Name?.TextValue ?? "Unknown";
        SetRaiseTarget(context, targetName);

        var partyCoord = GetPartyCoordinationService(context);
        if (partyCoord?.IsRaiseTargetReservedByOther((uint)target.GameObjectId) == true)
        {
            SetRaiseState(context, "Reserved by other");
            return;
        }

        var hasSwiftcast = HasSwiftcast(context);

        if (hasSwiftcast)
        {
            if (partyCoord?.ReserveRaiseTarget((uint)target.GameObjectId, RaiseAction.ActionId, 0, usingSwiftcast: true) == false)
            {
                SetRaiseState(context, "Failed to reserve");
                return;
            }

            scheduler.PushGcd(AthenaAbilities.Resurrection, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    SetRaiseState(context, "Swiftcast Raise");
                    SetPlanningState(context, "Raise");
                    SetPlannedAction(context, $"{RaiseAction.Name} (Swiftcast)");
                    RecordRaiseTraining(context, targetName, hasSwiftcast: true, isHardcast: false);
                });
            return;
        }

        if (config.Resurrection.AllowHardcastRaise && !isMoving)
        {
            var swiftcastCooldown = context.ActionService.GetCooldownRemaining(SwiftcastAction.ActionId);
            if (swiftcastCooldown > 10f)
            {
                const int hardcastMs = 8000;
                if (partyCoord?.ReserveRaiseTarget((uint)target.GameObjectId, RaiseAction.ActionId, hardcastMs, usingSwiftcast: false) == false)
                {
                    SetRaiseState(context, "Failed to reserve");
                    return;
                }

                scheduler.PushGcd(AthenaAbilities.Resurrection, target.GameObjectId, priority: 1,
                    onDispatched: _ =>
                    {
                        SetRaiseState(context, "Hardcast Raise");
                        SetPlanningState(context, "Raise");
                        SetPlannedAction(context, $"{RaiseAction.Name} (Hardcast)");
                        RecordRaiseTraining(context, targetName, hasSwiftcast: false, isHardcast: true);
                    });
            }
            else
            {
                SetRaiseState(context, $"Waiting for Swiftcast ({swiftcastCooldown:F1}s)");
            }
        }
        else if (!hasSwiftcast && !config.Resurrection.AllowHardcastRaise)
        {
            SetRaiseState(context, "No Swiftcast (hardcast disabled)");
        }
        else if (isMoving)
        {
            SetRaiseState(context, "Moving (can't hardcast)");
        }
    }
}
