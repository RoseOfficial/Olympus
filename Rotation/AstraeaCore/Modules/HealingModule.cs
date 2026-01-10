using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Handles all healing logic for the Astrologian rotation.
/// Includes GCD heals, oGCD heals, Earthly Star, Horoscope, and Macrocosmos.
/// </summary>
public sealed class HealingModule : IAstraeaModule
{
    public int Priority => 10; // High priority for healing
    public string Name => "Healing";

    public bool TryExecute(AstraeaContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
            return false;

        // oGCD heals first (free healing, no GCD cost)
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Essential Dignity (emergency single-target, scales with low HP)
            if (TryEssentialDignity(context))
                return true;

            // Priority 2: Celestial Intersection (single-target heal + shield)
            if (TryCelestialIntersection(context))
                return true;

            // Priority 3: Celestial Opposition (AoE heal + regen)
            if (TryCelestialOpposition(context))
                return true;

            // Priority 4: Exaltation (damage reduction + delayed heal)
            if (TryExaltation(context))
                return true;

            // Priority 5: Horoscope detonation (if active and party needs healing)
            if (TryHoroscopeDetonation(context))
                return true;

            // Priority 6: Microcosmos (Macrocosmos detonation)
            if (TryMicrocosmos(context))
                return true;

            // Priority 7: Earthly Star detonation
            if (TryEarthlyStarDetonation(context))
                return true;

            // Priority 8: Synastry (buff for sustained healing)
            if (TrySynastry(context))
                return true;

            // Priority 9: Earthly Star placement (proactive)
            if (TryEarthlyStarPlacement(context))
                return true;

            // Priority 10: Lady of Crowns (Minor Arcana AoE heal)
            if (TryLadyOfCrowns(context))
                return true;
        }

        // GCD heals
        if (context.CanExecuteGcd)
        {
            // Priority 11: Horoscope preparation (setup for later detonation)
            if (TryHoroscopePreparation(context))
                return true;

            // Priority 12: Macrocosmos preparation (setup for damage absorption)
            if (!isMoving && TryMacrocosmosPreparation(context))
                return true;

            // Priority 13: AoE healing (Helios/Aspected Helios/Helios Conjunction)
            if (!isMoving && TryAoEHeal(context))
                return true;

            // Priority 14: Aspected Benefic (instant, can use while moving)
            if (TryAspectedBenefic(context))
                return true;

            // Priority 15: Single-target healing (Benefic II/Benefic)
            if (!isMoving && TrySingleTargetHeal(context))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(AstraeaContext context)
    {
        var (avgHp, lowestHp, injured) = context.PartyHealthMetrics;
        context.Debug.AoEInjuredCount = injured;
        context.Debug.PlayerHpPercent = context.Player.MaxHp > 0
            ? (float)context.Player.CurrentHp / context.Player.MaxHp
            : 1f;
    }

    #region oGCD Healing

    private bool TryEssentialDignity(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEssentialDignity)
            return false;

        if (player.Level < ASTActions.EssentialDignity.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.EssentialDignity.ActionId))
            return false;

        var target = context.PartyHelper.FindEssentialDignityTarget(player, config.EssentialDignityThreshold);
        if (target == null)
            return false;

        var action = ASTActions.EssentialDignity;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.EssentialDignityState = "Used";
            return true;
        }

        return false;
    }

    private bool TryCelestialIntersection(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCelestialIntersection)
            return false;

        if (player.Level < ASTActions.CelestialIntersection.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.CelestialIntersection.ActionId))
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.CelestialIntersectionThreshold)
            return false;

        var action = ASTActions.CelestialIntersection;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.CelestialIntersectionState = "Used";
            return true;
        }

        return false;
    }

    private bool TryCelestialOpposition(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCelestialOpposition)
            return false;

        if (player.Level < ASTActions.CelestialOpposition.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.CelestialOpposition.ActionId))
            return false;

        if (!ShouldUseAoEHeal(context))
            return false;

        var action = ASTActions.CelestialOpposition;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.CelestialOppositionState = "Used";
            return true;
        }

        return false;
    }

    private bool TryExaltation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableExaltation)
            return false;

        if (player.Level < ASTActions.Exaltation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Exaltation.ActionId))
            return false;

        var target = context.PartyHelper.FindExaltationTarget(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.ExaltationThreshold)
            return false;

        var action = ASTActions.Exaltation;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.ExaltationState = "Used";
            return true;
        }

        return false;
    }

    private bool TryHoroscopeDetonation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableHoroscope)
            return false;

        // Need Horoscope or Horoscope Helios buff active to detonate
        if (!context.HasHoroscope && !context.HasHoroscopeHelios)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.HoroscopeEnd.ActionId))
            return false;

        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > config.HoroscopeThreshold)
            return false;

        if (injured < config.HoroscopeMinTargets)
            return false;

        var action = ASTActions.HoroscopeEnd;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.HoroscopeState = "Detonated";
            return true;
        }

        return false;
    }

    private bool TryMicrocosmos(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMacrocosmos)
            return false;

        // Need Macrocosmos buff active to use Microcosmos
        if (!context.HasMacrocosmos)
            return false;

        if (player.Level < ASTActions.Microcosmos.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Microcosmos.ActionId))
            return false;

        // Detonate when party needs healing
        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > 0.70f && injured < 3)
            return false;

        var action = ASTActions.Microcosmos;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.MacrocosmosState = "Detonated";
            return true;
        }

        return false;
    }

    private bool TryEarthlyStarDetonation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEarthlyStar)
            return false;

        if (!context.IsStarPlaced)
            return false;

        if (player.Level < ASTActions.StellarDetonation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.StellarDetonation.ActionId))
            return false;

        var (avgHp, lowestHp, injured) = context.PartyHealthMetrics;

        // Check if star is mature (Giant Dominance)
        bool isMature = context.IsStarMature;

        // Determine if we should detonate
        bool shouldDetonate = false;

        if (isMature)
        {
            // Mature star: detonate when party needs healing
            if (avgHp <= config.EarthlyStarDetonateThreshold || injured >= config.EarthlyStarMinTargets)
                shouldDetonate = true;
        }
        else if (!config.WaitForGiantDominance)
        {
            // Immature star allowed: detonate if party needs healing
            if (avgHp <= config.EarthlyStarDetonateThreshold || injured >= config.EarthlyStarMinTargets)
                shouldDetonate = true;
        }
        else
        {
            // Emergency detonation even if immature
            if (avgHp <= config.EarthlyStarEmergencyThreshold)
                shouldDetonate = true;
        }

        if (!shouldDetonate)
            return false;

        var action = ASTActions.StellarDetonation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            // Notify service that star was detonated
            context.EarthlyStarService.OnStarDetonated();

            context.Debug.PlannedAction = action.Name;
            context.Debug.EarthlyStarState = isMature ? "Detonated (Mature)" : "Detonated (Immature)";
            context.LogEarthlyStarDecision("Detonated", isMature ? "Mature star, party needs healing" : "Emergency detonate");
            return true;
        }

        return false;
    }

    private bool TrySynastry(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableSynastry)
            return false;

        if (player.Level < ASTActions.Synastry.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Synastry.ActionId))
            return false;

        // Already have Synastry active
        if (context.HasSynastry)
            return false;

        var target = context.PartyHelper.FindSynastryTarget(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.SynastryThreshold)
            return false;

        var action = ASTActions.Synastry;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.SynastryState = "Active";
            context.Debug.SynastryTarget = target.Name.TextValue;
            return true;
        }

        return false;
    }

    private bool TryEarthlyStarPlacement(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEarthlyStar)
            return false;

        if (config.StarPlacement == Config.EarthlyStarPlacementStrategy.Manual)
            return false;

        if (player.Level < ASTActions.EarthlyStar.MinLevel)
            return false;

        // Don't place if star is already active
        if (context.IsStarPlaced)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.EarthlyStar.ActionId))
            return false;

        // Determine placement position (Earthly Star is ground-targeted, not entity-targeted)
        var targetPosition = player.Position;
        var targetName = "Self";

        if (config.StarPlacement == Config.EarthlyStarPlacementStrategy.OnMainTank)
        {
            var tank = context.PartyHelper.FindTankInParty(player);
            if (tank != null)
            {
                targetPosition = tank.Position;
                targetName = tank.Name.TextValue;
            }
        }

        var action = ASTActions.EarthlyStar;
        if (context.ActionService.ExecuteGroundTargetedOgcd(action, targetPosition))
        {
            // Notify service for state tracking
            context.EarthlyStarService.OnStarPlaced(targetPosition);

            context.Debug.PlannedAction = action.Name;
            context.Debug.EarthlyStarState = "Placed";
            context.LogEarthlyStarDecision("Placed", $"{config.StarPlacement} ({targetName})");
            return true;
        }

        return false;
    }

    private bool TryLadyOfCrowns(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMinorArcana)
            return false;

        // Only use Lady for emergency healing
        if (config.MinorArcanaStrategy != Config.MinorArcanaUsageStrategy.EmergencyOnly)
            return false;

        // Check if we have Lady card
        if (!context.CardService.HasLady)
            return false;

        if (player.Level < ASTActions.LadyOfCrowns.MinLevel)
            return false;

        var (avgHp, _, injured) = context.PartyHealthMetrics;
        if (avgHp > config.LadyOfCrownsThreshold)
            return false;

        if (injured < 2)
            return false;

        var action = ASTActions.LadyOfCrowns;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.CardState = "Lady Used";
            return true;
        }

        return false;
    }

    #endregion

    #region GCD Healing

    private bool TryHoroscopePreparation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableHoroscope || !config.AutoCastHoroscope)
            return false;

        if (player.Level < ASTActions.Horoscope.MinLevel)
            return false;

        // Already have Horoscope buff
        if (context.HasHoroscope || context.HasHoroscopeHelios)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Horoscope.ActionId))
            return false;

        // Only prepare if party might need healing soon (proactive)
        var (avgHp, _, _) = context.PartyHealthMetrics;
        if (avgHp > 0.85f)
            return false;

        var action = ASTActions.Horoscope;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.HoroscopeState = "Prepared";
            return true;
        }

        return false;
    }

    private bool TryMacrocosmosPreparation(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableMacrocosmos || !config.AutoUseMacrocosmos)
            return false;

        if (player.Level < ASTActions.Macrocosmos.MinLevel)
            return false;

        // Already have Macrocosmos buff
        if (context.HasMacrocosmos)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Macrocosmos.ActionId))
            return false;

        // Count party members in range
        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= ASTActions.Macrocosmos.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < config.MacrocosmosMinTargets)
            return false;

        // Use proactively when party is taking damage
        var (avgHp, _, _) = context.PartyHealthMetrics;
        if (avgHp > config.MacrocosmosThreshold)
            return false;

        // Macrocosmos is a GCD that deals damage and applies the buff
        var action = ASTActions.Macrocosmos;
        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.MacrocosmosState = "Applied";
            return true;
        }

        return false;
    }

    private bool TryAoEHeal(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableHelios && !config.EnableAspectedHelios)
            return false;

        if (!ShouldUseAoEHeal(context))
            return false;

        // Choose appropriate AoE heal based on level and config
        ActionDefinition? action = null;

        // Helios Conjunction (level 96) - upgraded Aspected Helios
        if (config.EnableAspectedHelios && player.Level >= ASTActions.HeliosConjunction.MinLevel)
        {
            action = ASTActions.HeliosConjunction;
        }
        // Aspected Helios (level 42)
        else if (config.EnableAspectedHelios && player.Level >= ASTActions.AspectedHelios.MinLevel)
        {
            action = ASTActions.AspectedHelios;
        }
        // Basic Helios (level 10)
        else if (config.EnableHelios && player.Level >= ASTActions.Helios.MinLevel)
        {
            action = ASTActions.Helios;
        }

        if (action == null)
            return false;

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.AoEHealState = "Casting";
            return true;
        }

        return false;
    }

    private bool TryAspectedBenefic(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableAspectedBenefic)
            return false;

        if (player.Level < ASTActions.AspectedBenefic.MinLevel)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.AspectedBeneficThreshold)
            return false;

        // Skip if target already has Aspected Benefic regen
        if (context.StatusHelper.HasAspectedBenefic(target))
            return false;

        var action = ASTActions.AspectedBenefic;
        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.SingleHealState = "Aspected Benefic";
            return true;
        }

        return false;
    }

    private bool TrySingleTargetHeal(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableBenefic && !config.EnableBeneficII)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Choose between Benefic II and Benefic
        ActionDefinition? action = null;

        // Benefic II (level 26) for higher healing
        if (config.EnableBeneficII && player.Level >= ASTActions.BeneficII.MinLevel && hpPercent <= config.BeneficIIThreshold)
        {
            action = ASTActions.BeneficII;
        }
        // Fall back to Benefic
        else if (config.EnableBenefic && hpPercent <= config.BeneficThreshold)
        {
            action = ASTActions.Benefic;
        }

        if (action == null)
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.SingleHealState = action.Name;
            return true;
        }

        return false;
    }

    #endregion

    #region Helper Methods

    private bool ShouldUseAoEHeal(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        var (count, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, 0);
        var (avgHp, _, _) = context.PartyHealthMetrics;

        return avgHp <= config.AoEHealThreshold && count >= config.AoEHealMinTargets;
    }

    #endregion
}
