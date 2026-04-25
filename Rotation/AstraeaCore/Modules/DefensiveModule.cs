using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific defensive module (scheduler-driven).
/// </summary>
public sealed class DefensiveModule : BaseDefensiveModule<IAstraeaContext>, IAstraeaModule
{
    private static readonly string[] _neutralSectAlternatives =
    {
        "Save for predictable heavy damage",
        "Coordinate with co-healer",
        "Use other defensives first",
    };

    private static readonly string[] _sunSignAlternatives =
    {
        "Wait for more party members",
        "Save for imminent damage",
        "Let Neutral Sect expire (wastes Sun Sign)",
    };

    private static readonly string[] _collectiveUnconsciousAlternatives =
    {
        "Celestial Opposition (doesn't require channeling)",
        "Earthly Star (if placed)",
        "Neutral Sect + Helios (shields)",
    };

    protected override void SetDefensiveState(IAstraeaContext context, string state) =>
        context.Debug.PlanningState = state;
    protected override void SetPlannedAction(IAstraeaContext context, string action) =>
        context.Debug.PlannedAction = action;
    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IAstraeaContext context) =>
        context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
    protected override bool TryJobSpecificDefensives(IAstraeaContext context, bool isMoving) => false;

    public override bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;

        TryPushNeutralSect(context, scheduler);
        TryPushSunSign(context, scheduler);
        if (!isMoving)
            TryPushCollectiveUnconscious(context, scheduler);
    }

    public override void UpdateDebugState(IAstraeaContext context)
    {
        context.Debug.NeutralSectState = context.HasNeutralSect ? "Active" : "Idle";
    }

    private void TryPushNeutralSect(IAstraeaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableNeutralSect) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.NeutralSectState = "Skipped (remote mit)";
            return;
        }

        if (coordConfig.EnableHealerBurstAwareness && coordConfig.DelayMitigationsDuringBurst && partyCoord != null)
        {
            var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHp > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.NeutralSectState = "Delayed (burst active)";
                return;
            }
        }

        if (player.Level < ASTActions.NeutralSect.MinLevel) return;
        if (!context.ActionService.IsActionReady(ASTActions.NeutralSect.ActionId)) return;
        if (context.HasNeutralSect) return;

        bool shouldUse = config.NeutralSectStrategy switch
        {
            NeutralSectUsageStrategy.OnCooldown => true,
            NeutralSectUsageStrategy.SaveForDamage => ShouldUseForDamage(context, config),
            NeutralSectUsageStrategy.Manual => false,
            _ => false
        };

        if (!shouldUse && config.NeutralSectStrategy != NeutralSectUsageStrategy.Manual)
        {
            shouldUse = TimelineHelper.IsRaidwideImminent(
                context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);
        }

        if (!shouldUse) return;

        scheduler.PushOgcd(AstraeaAbilities.NeutralSect, player.GameObjectId, priority: 75,
            onDispatched: _ =>
            {
                SetPlannedAction(context, ASTActions.NeutralSect.Name);
                context.Debug.NeutralSectState = "Active";
                partyCoord?.OnCooldownUsed(ASTActions.NeutralSect.ActionId, 120_000);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var (avgHp, _, injured) = context.PartyHelper.CalculatePartyHealthMetrics(player);
                    var shortReason = $"Neutral Sect - enhanced healing + shields ({config.NeutralSectStrategy})";
                    var factors = new[]
                    {
                        $"Strategy: {config.NeutralSectStrategy}",
                        $"Party avg HP: {avgHp:P0}",
                        $"Injured count: {injured}",
                        "20% healing buff for 20s",
                        "Aspected Benefic/Helios gain shields",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = ASTActions.NeutralSect.ActionId,
                        ActionName = "Neutral Sect",
                        Category = "Defensive",
                        TargetName = "Self",
                        ShortReason = shortReason,
                        DetailedReason = $"Neutral Sect activated ({config.NeutralSectStrategy} strategy). Party at {avgHp:P0} avg HP with {injured} injured. For 20 seconds: +20% healing potency AND Aspected Benefic/Helios gain shields equal to heal amount.",
                        Factors = factors,
                        Alternatives = _neutralSectAlternatives,
                        Tip = "Neutral Sect turns your GCD heals into shield heals! Cast Aspected Helios under Neutral Sect for party-wide shields.",
                        ConceptId = AstConcepts.NeutralSectUsage,
                        Priority = ExplanationPriority.High,
                    });
                }
            });
    }

    private bool ShouldUseForDamage(IAstraeaContext context, AstrologianConfig config)
    {
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        if (avgHp < config.NeutralSectThreshold) return true;
        if (injuredCount >= 3) return true;
        return false;
    }

    private void TryPushSunSign(IAstraeaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableSunSign) return;
        if (player.Level < ASTActions.SunSign.MinLevel) return;
        if (!context.HasNeutralSect) return;
        if (!context.ActionService.IsActionReady(ASTActions.SunSign.ActionId)) return;

        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= ASTActions.SunSign.RadiusSquared)
                membersInRange++;
        }
        if (membersInRange < 3) return;

        var capturedMembersInRange = membersInRange;

        scheduler.PushOgcd(AstraeaAbilities.SunSign, player.GameObjectId, priority: 78,
            onDispatched: _ =>
            {
                SetPlannedAction(context, ASTActions.SunSign.Name);
                context.Debug.SunSignState = "Used";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
                    var shortReason = $"Sun Sign - party shield ({capturedMembersInRange} in range)";
                    var factors = new[]
                    {
                        $"Party avg HP: {avgHp:P0}",
                        $"Members in range: {capturedMembersInRange}",
                        "Neutral Sect is active",
                        "400 potency AoE shield",
                        "15s duration",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = ASTActions.SunSign.ActionId,
                        ActionName = "Sun Sign",
                        Category = "Defensive",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Sun Sign used on {capturedMembersInRange} party members at {avgHp:P0} avg HP. Provides 400 potency AoE shield that lasts 15s. Only available during Neutral Sect.",
                        Factors = factors,
                        Alternatives = _sunSignAlternatives,
                        Tip = "Sun Sign is ONLY available during Neutral Sect! Make sure to use it before Neutral Sect expires.",
                        ConceptId = AstConcepts.SunSignUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }

    private void TryPushCollectiveUnconscious(IAstraeaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCollectiveUnconscious) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.CollectiveUnconsciousState = "Skipped (remote mit)";
            return;
        }

        if (coordConfig.EnableHealerBurstAwareness && coordConfig.DelayMitigationsDuringBurst && partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.CollectiveUnconsciousState = "Delayed (burst active)";
                return;
            }
        }

        if (player.Level < ASTActions.CollectiveUnconscious.MinLevel) return;
        if (!context.ActionService.IsActionReady(ASTActions.CollectiveUnconscious.ActionId)) return;

        var (avgHp, _, _) = context.PartyHealthMetrics;
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);
        if (avgHp > config.CollectiveUnconsciousThreshold && !raidwideImminent) return;

        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= ASTActions.CollectiveUnconscious.RadiusSquared)
                membersInRange++;
        }
        if (membersInRange < 3) return;

        var capturedAvgHp = avgHp;
        var capturedMembersInRange = membersInRange;

        scheduler.PushOgcd(AstraeaAbilities.CollectiveUnconscious, player.GameObjectId, priority: 82,
            onDispatched: _ =>
            {
                SetPlannedAction(context, ASTActions.CollectiveUnconscious.Name);
                context.Debug.CollectiveUnconsciousState = "Channeling";
                partyCoord?.OnCooldownUsed(ASTActions.CollectiveUnconscious.ActionId, 60_000);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Collective Unconscious - {capturedMembersInRange} in range at {capturedAvgHp:P0}";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Threshold: {config.CollectiveUnconsciousThreshold:P0}",
                        $"Members in range: {capturedMembersInRange}",
                        "10% damage reduction (channeled)",
                        "100 potency regen/tick",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = ASTActions.CollectiveUnconscious.ActionId,
                        ActionName = "Collective Unconscious",
                        Category = "Defensive",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Collective Unconscious used on {capturedMembersInRange} party members at {capturedAvgHp:P0} avg HP. Provides 10% damage reduction while channeling plus 100 potency regen/tick.",
                        Factors = factors,
                        Alternatives = _collectiveUnconsciousAlternatives,
                        Tip = "Collective Unconscious is tricky - the 10% mitigation requires you to stand still and channel.",
                        ConceptId = AstConcepts.CollectiveUnconsciousUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }
}
