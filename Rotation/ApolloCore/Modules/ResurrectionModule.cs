using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// WHM-specific resurrection module (scheduler-driven).
/// Extends base resurrection with Thin Air synergy for free raises.
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<IApolloContext>, IApolloModule
{
    protected override ActionDefinition RaiseAction => RoleActions.Raise;
    protected override ActionDefinition SwiftcastAction => RoleActions.Swiftcast;
    protected override int RaiseMpCost => 2400;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(IApolloContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(IApolloContext context) => context.HasSwiftcast;

    protected override void SetRaiseState(IApolloContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(IApolloContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(IApolloContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(IApolloContext context, string action) => context.Debug.PlannedAction = action;
    protected override IPartyCoordinationService? GetPartyCoordinationService(IApolloContext context) => context.PartyCoordinationService;

    /// <summary>
    /// WHM should wait for Thin Air before raising if it's available and not already active.
    /// This provides a free 2400 MP raise.
    /// </summary>
    protected override bool ShouldWaitForPreRaiseBuff(IApolloContext context)
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

    protected override string GetRaiseSuccessNote(IApolloContext context, bool hasSwiftcast)
    {
        var hasThinAir = context.HasThinAir;
        if (hasSwiftcast && hasThinAir) return " (Swiftcast + Thin Air)";
        if (hasSwiftcast) return " (Swiftcast)";
        if (hasThinAir) return " (Thin Air)";
        return "";
    }

    protected override void RecordRaiseTraining(IApolloContext context, string targetName, bool hasSwiftcast, bool isHardcast)
    {
        if (context.TrainingService?.IsTrainingEnabled != true)
            return;

        var hasThinAir = context.HasThinAir;
        var mpPercent = (float)context.Player.CurrentMp / context.Player.MaxMp;

        string shortReason;
        if (hasSwiftcast && hasThinAir)
            shortReason = $"Swiftcast + Thin Air raise on {targetName}!";
        else if (hasSwiftcast)
            shortReason = $"Swiftcast raise on {targetName}";
        else if (isHardcast && hasThinAir)
            shortReason = $"Hardcast (Thin Air) raise on {targetName}";
        else
            shortReason = $"Hardcast raise on {targetName}";

        var factors = new[]
        {
            hasSwiftcast ? "Swiftcast active - instant cast" : "No Swiftcast - hardcasting",
            hasThinAir ? "Thin Air active - free MP cost!" : $"MP: {mpPercent:P0} (2400 MP cost)",
            $"Target: {targetName} (dead party member)",
            "Dead party members = 0 DPS contribution",
            "Raising has highest priority after emergency heals",
        };

        var alternatives = new[]
        {
            hasSwiftcast ? "Nothing - Swiftcast raise is optimal" : "Wait for Swiftcast (if available soon)",
            hasThinAir ? "Nothing - Thin Air makes this free" : "Use Thin Air first (if available)",
            "Let co-healer raise (if coordinating)",
        };

        string tip;
        if (hasSwiftcast && hasThinAir)
            tip = "Perfect raise! Swiftcast + Thin Air = instant free raise. This is the ideal combination.";
        else if (hasSwiftcast)
            tip = "Always use Swiftcast for raises when available. It lets you continue healing/DPSing immediately.";
        else if (hasThinAir)
            tip = "Thin Air saved 2400 MP on this hardcast. If Swiftcast comes up mid-cast, save it for next raise.";
        else
            tip = "Hardcast raises are expensive (2400 MP, 8s cast). Try to have Swiftcast or Thin Air ready for raises.";

        var detailedReason = $"Raised {targetName} using " +
            (hasSwiftcast ? "Swiftcast (instant)" : "hardcast (8 second cast)") +
            (hasThinAir ? " with Thin Air (free 2400 MP saved)" : $" at {mpPercent:P0} MP") +
            $". Dead party members contribute nothing to the fight, so resurrection is always high priority. " +
            (hasSwiftcast
                ? "Swiftcast is ideal because it's instant and doesn't interrupt your healing rotation."
                : "Hardcast is used when Swiftcast is on cooldown (>10s remaining) and the situation is stable enough to cast.");

        context.TrainingService.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.UtcNow,
            ActionId = RoleActions.Raise.ActionId,
            ActionName = "Raise",
            Category = "Resurrection",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = WhmConcepts.RaiseDecision,
            Priority = ExplanationPriority.High,
        });
    }

    public override bool TryExecute(IApolloContext context, bool isMoving) => false;

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        TryPushSwiftcast(context, scheduler);
        TryPushRaise(context, scheduler, isMoving);
    }

    private void TryPushSwiftcast(IApolloContext context, RotationScheduler scheduler)
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

        scheduler.PushOgcd(ApolloAbilities.Swiftcast, player.GameObjectId, priority: 1);
    }

    private void TryPushRaise(IApolloContext context, RotationScheduler scheduler, bool isMoving)
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
            if (ShouldWaitForPreRaiseBuff(context)) { SetRaiseState(context, "Waiting for buff"); return; }
            if (partyCoord?.ReserveRaiseTarget((uint)target.GameObjectId, RaiseAction.ActionId, 0, usingSwiftcast: true) == false)
            {
                SetRaiseState(context, "Failed to reserve");
                return;
            }

            scheduler.PushGcd(ApolloAbilities.Raise, target.GameObjectId, priority: 1,
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
                if (ShouldWaitForPreRaiseBuff(context)) { SetRaiseState(context, "Waiting for buff"); return; }
                const int hardcastMs = 8000;
                if (partyCoord?.ReserveRaiseTarget((uint)target.GameObjectId, RaiseAction.ActionId, hardcastMs, usingSwiftcast: false) == false)
                {
                    SetRaiseState(context, "Failed to reserve");
                    return;
                }

                scheduler.PushGcd(ApolloAbilities.Raise, target.GameObjectId, priority: 1,
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
