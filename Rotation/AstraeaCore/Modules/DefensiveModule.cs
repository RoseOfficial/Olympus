using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific defensive module.
/// Handles Neutral Sect for enhanced healing + shields.
/// </summary>
public sealed class DefensiveModule : BaseDefensiveModule<AstraeaContext>, IAstraeaModule
{
    #region Base Class Overrides - Debug State

    protected override void SetDefensiveState(AstraeaContext context, string state) =>
        context.Debug.PlanningState = state;

    protected override void SetPlannedAction(AstraeaContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(AstraeaContext context) =>
        context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// AST-specific defensives: Neutral Sect and Collective Unconscious.
    /// </summary>
    protected override bool TryJobSpecificDefensives(AstraeaContext context, bool isMoving)
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

    private bool TryNeutralSect(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableNeutralSect)
            return false;

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
            return true;
        }

        return false;
    }

    private bool ShouldUseForDamage(AstraeaContext context, AstrologianConfig config)
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

    private bool TrySunSign(AstraeaContext context)
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
            return true;
        }

        return false;
    }

    private bool TryCollectiveUnconscious(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCollectiveUnconscious)
            return false;

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
            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(AstraeaContext context)
    {
        context.Debug.NeutralSectState = context.HasNeutralSect ? "Active" : "Idle";
    }
}
