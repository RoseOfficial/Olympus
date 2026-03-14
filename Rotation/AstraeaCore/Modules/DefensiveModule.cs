using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific defensive module.
/// Handles Neutral Sect for enhanced healing + shields.
/// </summary>
public sealed class DefensiveModule : BaseDefensiveModule<IAstraeaContext>, IAstraeaModule
{
    // Training explanation arrays
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

    #region Base Class Overrides - Debug State

    protected override void SetDefensiveState(IAstraeaContext context, string state) =>
        context.Debug.PlanningState = state;

    protected override void SetPlannedAction(IAstraeaContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IAstraeaContext context) =>
        context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// AST-specific defensives: Neutral Sect and Collective Unconscious.
    /// </summary>
    protected override bool TryJobSpecificDefensives(IAstraeaContext context, bool isMoving)
    {
        // Priority 1: Neutral Sect (party-wide shield enhancement)
        if (TryNeutralSect(context))
            return true;

        // Priority 2: Sun Sign (follow-up from Neutral Sect at level 100)
        if (TrySunSign(context))
            return true;

        // Priority 3: Collective Unconscious (channeled mitigation, optional)
        if (!isMoving && TryCollectiveUnconscious(context))
            return true;

        return false;
    }

    #endregion

    #region AST-Specific Methods

    private bool TryNeutralSect(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableNeutralSect)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.NeutralSectState = "Skipped (remote mit)";
            return false;
        }

        // Burst awareness: Delay mitigations during burst windows unless emergency
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHp > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.NeutralSectState = $"Delayed (burst active)";
                return false;
            }
        }

        if (player.Level < ASTActions.NeutralSect.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.NeutralSect.ActionId))
            return false;

        if (context.HasNeutralSect)
            return false;

        // Usage depends on strategy
        bool shouldUse = config.NeutralSectStrategy switch
        {
            NeutralSectUsageStrategy.OnCooldown => true,
            NeutralSectUsageStrategy.SaveForDamage => ShouldUseForDamage(context, config),
            NeutralSectUsageStrategy.Manual => false,
            _ => false
        };

        if (!shouldUse)
            return false;

        if (context.ActionService.ExecuteOgcd(ASTActions.NeutralSect, player.GameObjectId))
        {
            SetPlannedAction(context, ASTActions.NeutralSect.Name);
            context.Debug.NeutralSectState = "Active";
            partyCoord?.OnCooldownUsed(ASTActions.NeutralSect.ActionId, 120_000);

            // Training mode: capture explanation
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

                var alternatives = _neutralSectAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = ASTActions.NeutralSect.ActionId,
                    ActionName = "Neutral Sect",
                    Category = "Defensive",
                    TargetName = "Self",
                    ShortReason = shortReason,
                    DetailedReason = $"Neutral Sect activated ({config.NeutralSectStrategy} strategy). Party at {avgHp:P0} avg HP with {injured} injured. For 20 seconds: +20% healing potency AND Aspected Benefic/Helios gain shields equal to heal amount. This is AST's strongest defensive buff - use before heavy damage phases!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Neutral Sect turns your GCD heals into shield heals! Cast Aspected Helios under Neutral Sect for party-wide shields. Best used before known raidwides or when you need both healing AND shielding.",
                    ConceptId = AstConcepts.NeutralSectUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool ShouldUseForDamage(IAstraeaContext context, AstrologianConfig config)
    {
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

        // Use when party is taking significant damage
        if (avgHp < config.NeutralSectThreshold)
            return true;

        // Use when multiple people are injured
        if (injuredCount >= 3)
            return true;

        return false;
    }

    private bool TrySunSign(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableSunSign)
            return false;

        if (player.Level < ASTActions.SunSign.MinLevel)
            return false;

        // Sun Sign is only available when Neutral Sect is active
        if (!context.HasNeutralSect)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.SunSign.ActionId))
            return false;

        // Check if party would benefit from the shield
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > 0.85f)
            return false; // Party is healthy, save it

        // Count party members in range
        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= ASTActions.SunSign.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < 3)
            return false;

        if (context.ActionService.ExecuteOgcd(ASTActions.SunSign, player.GameObjectId))
        {
            SetPlannedAction(context, ASTActions.SunSign.Name);
            context.Debug.SunSignState = "Used";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Sun Sign - party shield ({membersInRange} in range)";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Members in range: {membersInRange}",
                    "Neutral Sect is active",
                    "400 potency AoE shield",
                    "15s duration",
                };

                var alternatives = _sunSignAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = ASTActions.SunSign.ActionId,
                    ActionName = "Sun Sign",
                    Category = "Defensive",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Sun Sign used on {membersInRange} party members at {avgHp:P0} avg HP. Provides 400 potency AoE shield that lasts 15s. Only available during Neutral Sect - don't waste it by letting Neutral Sect expire without using Sun Sign!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Sun Sign is ONLY available during Neutral Sect! Make sure to use it before Neutral Sect expires. It's a free 400 potency party shield - great before raidwides.",
                    ConceptId = AstConcepts.SunSignUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryCollectiveUnconscious(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCollectiveUnconscious)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.CollectiveUnconsciousState = "Skipped (remote mit)";
            return false;
        }

        // Burst awareness: Delay mitigations during burst windows unless emergency
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.CollectiveUnconsciousState = $"Delayed (burst active)";
                return false;
            }
        }

        if (player.Level < ASTActions.CollectiveUnconscious.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.CollectiveUnconscious.ActionId))
            return false;

        // Check party health - use when party is taking significant damage
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.CollectiveUnconsciousThreshold)
            return false;

        // Need multiple party members in range
        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= ASTActions.CollectiveUnconscious.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < 3)
            return false;

        if (context.ActionService.ExecuteOgcd(ASTActions.CollectiveUnconscious, player.GameObjectId))
        {
            SetPlannedAction(context, ASTActions.CollectiveUnconscious.Name);
            context.Debug.CollectiveUnconsciousState = "Channeling";
            partyCoord?.OnCooldownUsed(ASTActions.CollectiveUnconscious.ActionId, 60_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Collective Unconscious - {membersInRange} in range at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Threshold: {config.CollectiveUnconsciousThreshold:P0}",
                    $"Members in range: {membersInRange}",
                    "10% damage reduction (channeled)",
                    "100 potency regen/tick",
                };

                var alternatives = _collectiveUnconsciousAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = ASTActions.CollectiveUnconscious.ActionId,
                    ActionName = "Collective Unconscious",
                    Category = "Defensive",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Collective Unconscious used on {membersInRange} party members at {avgHp:P0} avg HP. Provides 10% damage reduction while channeling plus 100 potency regen/tick. The regen persists for 15s even after you stop channeling. Note: This is a channeled ability - you can't do other actions while holding it!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Collective Unconscious is tricky - the 10% mitigation requires you to stand still and channel. In practice, you often just tap it briefly to apply the regen, then cancel to keep DPSing. Full channel only for massive damage phases!",
                    ConceptId = AstConcepts.CollectiveUnconsciousUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(IAstraeaContext context)
    {
        context.Debug.NeutralSectState = context.HasNeutralSect ? "Active" : "Idle";
    }
}
