using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific resurrection module (scheduler-driven).
/// </summary>
public sealed class ResurrectionModule : BaseResurrectionModule<IAstraeaContext>, IAstraeaModule
{
    protected override ActionDefinition RaiseAction => RoleActions.Ascend;
    protected override ActionDefinition SwiftcastAction => RoleActions.Swiftcast;
    protected override int RaiseMpCost => RoleActions.Ascend.MpCost;

    protected override IBattleChara? FindDeadPartyMemberNeedingRaise(IAstraeaContext context)
        => context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);

    protected override bool HasSwiftcast(IAstraeaContext context) => context.HasSwiftcast || context.HasLightspeed;

    protected override void SetRaiseState(IAstraeaContext context, string state) => context.Debug.RaiseState = state;
    protected override void SetRaiseTarget(IAstraeaContext context, string target) => context.Debug.RaiseTarget = target;
    protected override void SetPlanningState(IAstraeaContext context, string state) => context.Debug.PlanningState = state;
    protected override void SetPlannedAction(IAstraeaContext context, string action) => context.Debug.PlannedAction = action;
    protected override IPartyCoordinationService? GetPartyCoordinationService(IAstraeaContext context) => context.PartyCoordinationService;

    protected override bool ShouldWaitForPreRaiseBuff(IAstraeaContext context)
    {
        if (context.HasSwiftcast || context.HasLightspeed)
            return false;

        var lightspeedCooldown = context.ActionService.GetCooldownRemaining(ASTActions.Lightspeed.ActionId);
        return lightspeedCooldown <= 10f;
    }

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
            Timestamp = DateTime.UtcNow,
            ActionId = RoleActions.Ascend.ActionId,
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

        context.TrainingService?.RecordConceptApplication(AstConcepts.RaiseDecision, wasSuccessful: true, hasSwiftcast || hasLightspeed ? "Instant raise" : "Hardcast raise");
    }

    public override bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        TryPushSwiftcast(context, scheduler);
        TryPushLightspeed(context, scheduler);
        TryPushRaise(context, scheduler, isMoving);
    }

    private void TryPushSwiftcast(IAstraeaContext context, RotationScheduler scheduler)
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

        scheduler.PushOgcd(AstraeaAbilities.Swiftcast, player.GameObjectId, priority: 1);
    }

    private void TryPushLightspeed(IAstraeaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Resurrection.EnableRaise) return;
        if (player.Level < ASTActions.Lightspeed.MinLevel) return;
        if (HasSwiftcast(context)) return;
        if (context.HasLightspeed) return;

        var deadMember = FindDeadPartyMemberNeedingRaise(context);
        if (deadMember is null) return;

        if (player.CurrentMp < RaiseMpCost) return;
        if (!context.ActionService.IsActionReady(ASTActions.Lightspeed.ActionId)) return;

        // Only push Lightspeed when Swiftcast unavailable — otherwise Swiftcast wins same-frame
        if (context.ActionService.IsActionReady(SwiftcastAction.ActionId)) return;

        scheduler.PushOgcd(AstraeaAbilities.Lightspeed, player.GameObjectId, priority: 2);
    }

    private void TryPushRaise(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
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

            scheduler.PushGcd(AstraeaAbilities.Ascend, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    SetRaiseState(context, "Instant Raise");
                    SetPlanningState(context, "Raise");
                    SetPlannedAction(context, $"{RaiseAction.Name} (Instant)");
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

                scheduler.PushGcd(AstraeaAbilities.Ascend, target.GameObjectId, priority: 1,
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
