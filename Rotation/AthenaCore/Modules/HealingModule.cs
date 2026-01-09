using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles all healing logic for the Scholar rotation.
/// Includes GCD heals, oGCD heals, shields, and Aetherflow healing abilities.
/// </summary>
public sealed class HealingModule : IAthenaModule
{
    public int Priority => 10; // High priority for healing
    public string Name => "Healing";

    public bool TryExecute(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
            return false;

        // oGCD heals first (free healing, no GCD cost)
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Recitation + Excogitation combo
            if (TryRecitationCombo(context))
                return true;

            // Priority 2: Excogitation (proactive)
            if (TryExcogitation(context))
                return true;

            // Priority 3: Lustrate (emergency single-target)
            if (TryLustrate(context))
                return true;

            // Priority 4: Indomitability (AoE oGCD)
            if (TryIndomitability(context))
                return true;

            // Priority 5: Sacred Soil
            if (TrySacredSoil(context))
                return true;

            // Priority 6: Protraction
            if (TryProtraction(context))
                return true;

            // Priority 7: Emergency Tactics (before GCD heal)
            if (TryEmergencyTactics(context))
                return true;
        }

        // GCD heals
        if (context.CanExecuteGcd && !isMoving)
        {
            // Priority 8: AoE healing (Succor/Concitation)
            if (TryAoEHeal(context))
                return true;

            // Priority 9: Single-target healing (Adloquium/Physick)
            if (TrySingleTargetHeal(context))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(AthenaContext context)
    {
        context.Debug.AetherflowStacks = context.AetherflowService.CurrentStacks;
    }

    #region oGCD Healing

    private bool TryRecitationCombo(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableRecitation)
            return false;

        if (player.Level < SCHActions.Recitation.MinLevel)
            return false;

        // Already have Recitation active - handled by other methods
        if (context.StatusHelper.HasRecitation(player))
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Recitation.ActionId))
            return false;

        // Check if we have a good target for the follow-up
        bool shouldUseRecitation = config.RecitationPriority switch
        {
            RecitationPriority.Excogitation => ShouldUseExcogitation(context),
            RecitationPriority.Indomitability => ShouldUseIndomitability(context),
            RecitationPriority.Adloquium => ShouldUseSingleTargetHeal(context),
            RecitationPriority.Succor => ShouldUseAoEHeal(context),
            _ => false
        };

        if (!shouldUseRecitation)
            return false;

        var action = SCHActions.Recitation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Recitation";
            return true;
        }

        return false;
    }

    private bool TryExcogitation(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableExcogitation)
            return false;

        if (player.Level < SCHActions.Excogitation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Excogitation.ActionId))
            return false;

        // Skip if no Aetherflow (unless Recitation is active)
        if (!context.StatusHelper.HasRecitation(player))
        {
            if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
                return false;
        }

        var target = context.PartyHelper.FindExcogitationTarget(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.ExcogitationThreshold)
            return false;

        var action = SCHActions.Excogitation;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            if (!context.StatusHelper.HasRecitation(player))
                context.AetherflowService.ConsumeStack();

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Excogitation";
            return true;
        }

        return false;
    }

    private bool TryLustrate(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableLustrate)
            return false;

        if (player.Level < SCHActions.Lustrate.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Lustrate.ActionId))
            return false;

        // Respect Aetherflow reserve
        if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.LustrateThreshold)
            return false;

        var action = SCHActions.Lustrate;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Lustrate";
            return true;
        }

        return false;
    }

    private bool TryIndomitability(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableIndomitability)
            return false;

        if (player.Level < SCHActions.Indomitability.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Indomitability.ActionId))
            return false;

        // Skip if no Aetherflow (unless Recitation is active)
        if (!context.StatusHelper.HasRecitation(player))
        {
            if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
                return false;
        }

        if (!ShouldUseIndomitability(context))
            return false;

        var action = SCHActions.Indomitability;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            if (!context.StatusHelper.HasRecitation(player))
                context.AetherflowService.ConsumeStack();

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Indomitability";
            return true;
        }

        return false;
    }

    private bool TrySacredSoil(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSacredSoil)
            return false;

        if (player.Level < SCHActions.SacredSoil.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.SacredSoil.ActionId))
            return false;

        // Respect Aetherflow reserve
        if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
            return false;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.SacredSoilThreshold)
            return false;

        // Count party members in range
        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= SCHActions.SacredSoil.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < config.SacredSoilMinTargets)
            return false;

        // Place at player's position (ground target at self)
        var action = SCHActions.SacredSoil;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Sacred Soil";
            return true;
        }

        return false;
    }

    private bool TryProtraction(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableProtraction)
            return false;

        if (player.Level < SCHActions.Protraction.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Protraction.ActionId))
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.ProtractionThreshold)
            return false;

        var action = SCHActions.Protraction;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Protraction";
            return true;
        }

        return false;
    }

    private bool TryEmergencyTactics(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableEmergencyTactics)
            return false;

        if (player.Level < SCHActions.EmergencyTactics.MinLevel)
            return false;

        // Already have Emergency Tactics active
        if (context.StatusHelper.HasEmergencyTactics(player))
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.EmergencyTactics.ActionId))
            return false;

        // Use when we need raw healing, not shields
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.EmergencyTacticsThreshold)
            return false;

        // Check if target already has Galvanize (shield would be wasted)
        if (context.StatusHelper.HasGalvanize(target))
        {
            var action = SCHActions.EmergencyTactics;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Emergency Tactics";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region GCD Healing

    private bool TryAoEHeal(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSuccor)
            return false;

        if (!ShouldUseAoEHeal(context))
            return false;

        // Choose between Succor and Concitation (Seraphism upgrade)
        ActionDefinition action;
        if (context.FairyStateManager.IsSeraphOrSeraphismActive && player.Level >= SCHActions.Concitation.MinLevel)
        {
            action = SCHActions.Concitation;
        }
        else if (player.Level >= SCHActions.Succor.MinLevel)
        {
            action = SCHActions.Succor;
        }
        else
        {
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "AoE Heal";
            return true;
        }

        return false;
    }

    private bool TrySingleTargetHeal(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        // Check if at least one GCD heal is enabled
        if (!config.EnableAdloquium && !config.EnablePhysick)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Choose between Adloquium and Physick
        ActionDefinition? action = null;

        // Check for Manifestation (Seraphism upgrade of Adlo) - controlled by EnableAdloquium
        if (config.EnableAdloquium && context.FairyStateManager.IsSeraphOrSeraphismActive && player.Level >= SCHActions.Manifestation.MinLevel)
        {
            if (hpPercent <= config.AdloquiumThreshold)
            {
                // Avoid overwriting Sage shields
                if (!config.AvoidOverwritingSageShields || !HasSageShield(context, target))
                {
                    action = SCHActions.Manifestation;
                }
            }
        }
        // Adloquium for shields
        else if (config.EnableAdloquium && player.Level >= SCHActions.Adloquium.MinLevel && hpPercent <= config.AdloquiumThreshold)
        {
            // Avoid overwriting existing Galvanize or Sage shields
            if (!context.StatusHelper.HasGalvanize(target))
            {
                if (!config.AvoidOverwritingSageShields || !HasSageShield(context, target))
                {
                    action = SCHActions.Adloquium;
                }
            }
        }

        // Fall back to Physick for raw healing
        if (action == null && config.EnablePhysick && hpPercent <= config.PhysickThreshold)
        {
            action = SCHActions.Physick;
        }

        if (action == null)
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Single Heal";
            return true;
        }

        return false;
    }

    #endregion

    #region Helper Methods

    private bool ShouldUseExcogitation(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (player.Level < SCHActions.Excogitation.MinLevel)
            return false;

        var target = context.PartyHelper.FindExcogitationTarget(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        return hpPercent <= config.ExcogitationThreshold;
    }

    private bool ShouldUseIndomitability(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        return avgHp <= config.AoEHealThreshold && injuredCount >= config.AoEHealMinTargets;
    }

    private bool ShouldUseSingleTargetHeal(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        return hpPercent <= config.AdloquiumThreshold;
    }

    private bool ShouldUseAoEHeal(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var (count, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, 0);
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        return avgHp <= config.AoEHealThreshold && count >= config.AoEHealMinTargets;
    }

    private static bool HasSageShield(AthenaContext context, IBattleChara target)
    {
        // Eukrasian Diagnosis status ID
        const ushort EukrasianDiagnosisStatusId = 2607;
        // Eukrasian Prognosis status ID
        const ushort EukrasianPrognosisStatusId = 2609;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == EukrasianDiagnosisStatusId ||
                status.StatusId == EukrasianPrognosisStatusId)
                return true;
        }
        return false;
    }

    #endregion
}
