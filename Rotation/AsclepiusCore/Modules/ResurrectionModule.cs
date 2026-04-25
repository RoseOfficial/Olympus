using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// SGE-specific resurrection module (scheduler-driven).
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

    protected override bool ShouldWaitForPreRaiseBuff(IAsclepiusContext context) => false;

    protected override string GetRaiseSuccessNote(IAsclepiusContext context, bool hasSwiftcast)
    {
        return hasSwiftcast ? " (Swiftcast)" : "";
    }

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

    public override bool TryExecute(IAsclepiusContext context, bool isMoving) => false;

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        TryPushSwiftcast(context, scheduler);
        TryPushRaise(context, scheduler, isMoving);
    }

    private void TryPushSwiftcast(IAsclepiusContext context, RotationScheduler scheduler)
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

        scheduler.PushOgcd(AsclepiusAbilities.Swiftcast, player.GameObjectId, priority: 1);
    }

    private void TryPushRaise(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
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

            scheduler.PushGcd(AsclepiusAbilities.Egeiro, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    var note = GetRaiseSuccessNote(context, hasSwiftcast: true);
                    SetRaiseState(context, "Swiftcast Raise");
                    SetPlanningState(context, "Raise");
                    SetPlannedAction(context, $"{RaiseAction.Name}{note}");
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

                scheduler.PushGcd(AsclepiusAbilities.Egeiro, target.GameObjectId, priority: 1,
                    onDispatched: _ =>
                    {
                        var note = GetRaiseSuccessNote(context, hasSwiftcast: false);
                        SetRaiseState(context, "Hardcast Raise");
                        SetPlanningState(context, "Raise");
                        SetPlannedAction(context, $"{RaiseAction.Name} (Hardcast){note}");
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
