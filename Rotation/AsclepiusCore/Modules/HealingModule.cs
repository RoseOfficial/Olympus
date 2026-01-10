using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Handles healing for Sage.
/// Priority order:
/// 1. Emergency healing (oGCD Addersgall spenders)
/// 2. Lucid Dreaming (MP management)
/// 3. Free oGCD heals (Physis II, Kerachole, etc.)
/// 4. AoE healing (Ixochole, Prognosis)
/// 5. Single-target healing (Druochole, Diagnosis)
/// 6. Shields (Haima, Panhaima, Eukrasian heals)
/// </summary>
public sealed class HealingModule : IAsclepiusModule
{
    public int Priority => 10; // High priority - healing is essential
    public string Name => "Healing";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        // oGCD: Emergency Addersgall heals
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Druochole for emergency single-target
            if (TryDruochole(context))
                return true;

            // Priority 2: Taurochole for tank healing + mit
            if (TryTaurochole(context))
                return true;

            // Priority 3: Ixochole for AoE emergency
            if (TryIxochole(context))
                return true;

            // Priority 4: Kerachole for AoE regen + mit
            if (TryKerachole(context))
                return true;

            // Priority 5: Physis II for AoE HoT
            if (TryPhysisII(context))
                return true;

            // Priority 6: Holos for emergency AoE heal + shield + mit
            if (TryHolos(context))
                return true;

            // Priority 7: Haima for single-target multi-hit shield
            if (TryHaima(context))
                return true;

            // Priority 8: Panhaima for AoE multi-hit shield
            if (TryPanhaima(context))
                return true;

            // Priority 9: Pepsis to consume shields for healing
            if (TryPepsis(context))
                return true;

            // Priority 10: Rhizomata for Addersgall management
            if (TryRhizomata(context))
                return true;

            // Priority 11: Krasis for healing boost
            if (TryKrasis(context))
                return true;

            // Priority 12: Zoe for next GCD heal boost
            if (TryZoe(context))
                return true;

            // Priority 13: Lucid Dreaming for MP
            if (TryLucidDreaming(context))
                return true;
        }

        // GCD: Healing spells
        if (context.CanExecuteGcd)
        {
            // Priority 1: Pneuma for AoE damage + heal
            if (!isMoving && TryPneuma(context))
                return true;

            // Priority 2: Eukrasian shields
            if (TryEukrasianHealing(context, isMoving))
                return true;

            // Priority 3: Prognosis for AoE
            if (!isMoving && TryPrognosis(context))
                return true;

            // Priority 4: Diagnosis for single-target
            if (!isMoving && TryDiagnosis(context))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(IAsclepiusContext context)
    {
        context.Debug.AddersgallStacks = context.AddersgallStacks;
        context.Debug.AddersgallTimer = context.AddersgallTimer;
        context.Debug.AdderstingStacks = context.AdderstingStacks;

        // Update healing state based on party health
        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        context.Debug.AoEInjuredCount = injuredCount;
        context.Debug.PlayerHpPercent = context.Player.MaxHp > 0
            ? (float)context.Player.CurrentHp / context.Player.MaxHp
            : 1f;
    }

    #region Addersgall Heals

    private bool TryDruochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (player.Level < SGEActions.Druochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.DruocholeState = "No Addersgall";
            return false;
        }

        // Reserve stacks if configured
        if (context.AddersgallStacks <= config.AddersgallReserve)
        {
            context.Debug.DruocholeState = $"Reserved ({config.AddersgallReserve})";
            return false;
        }

        // Find target needing healing
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
        {
            context.Debug.DruocholeState = "No target";
            return false;
        }

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.DruocholeThreshold)
        {
            context.Debug.DruocholeState = $"{hpPercent:P0} > {config.DruocholeThreshold:P0}";
            return false;
        }

        var action = SGEActions.Druochole;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Druochole";
            context.Debug.DruocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Target at {hpPercent:P0}");
            return true;
        }

        return false;
    }

    private bool TryTaurochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableTaurochole)
            return false;

        if (player.Level < SGEActions.Taurochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.TaurocholeState = "No Addersgall";
            return false;
        }

        // Check cooldown (shares with Kerachole)
        if (!context.ActionService.IsActionReady(SGEActions.Taurochole.ActionId))
        {
            context.Debug.TaurocholeState = "On CD";
            return false;
        }

        // Taurochole is best for tanks needing healing + mitigation
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null)
        {
            context.Debug.TaurocholeState = "No tank";
            return false;
        }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;
        if (hpPercent > config.TaurocholeThreshold)
        {
            context.Debug.TaurocholeState = $"Tank at {hpPercent:P0}";
            return false;
        }

        // Don't use if tank already has Kerachole/Taurochole mit
        if (AsclepiusStatusHelper.HasKerachole(tank))
        {
            context.Debug.TaurocholeState = "Already has mit";
            return false;
        }

        var action = SGEActions.Taurochole;
        if (context.ActionService.ExecuteOgcd(action, tank.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Taurochole";
            context.Debug.TaurocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Tank at {hpPercent:P0}");
            return true;
        }

        return false;
    }

    private bool TryIxochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableIxochole)
            return false;

        if (player.Level < SGEActions.Ixochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.IxocholeState = "No Addersgall";
            return false;
        }

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Ixochole.ActionId))
        {
            context.Debug.IxocholeState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.IxocholeState = $"{injuredCount} < {config.AoEHealMinTargets} injured";
            return false;
        }

        if (avgHp > config.AoEHealThreshold)
        {
            context.Debug.IxocholeState = $"Avg HP {avgHp:P0} > {config.AoEHealThreshold:P0}";
            return false;
        }

        var action = SGEActions.Ixochole;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Ixochole";
            context.Debug.IxocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"{injuredCount} injured");
            return true;
        }

        return false;
    }

    private bool TryKerachole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableKerachole)
            return false;

        if (player.Level < SGEActions.Kerachole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.KeracholeState = "No Addersgall";
            return false;
        }

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Kerachole.ActionId))
        {
            context.Debug.KeracholeState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Kerachole is best value - use it liberally for regen + mit
        if (injuredCount < 2)
        {
            context.Debug.KeracholeState = $"{injuredCount} injured";
            return false;
        }

        if (avgHp > config.KeracholeThreshold)
        {
            context.Debug.KeracholeState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Kerachole;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Kerachole";
            context.Debug.KeracholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Party regen + mit");
            return true;
        }

        return false;
    }

    #endregion

    #region Free oGCD Heals

    private bool TryPhysisII(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhysisII)
            return false;

        if (player.Level < SGEActions.PhysisII.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.PhysisII.ActionId))
        {
            context.Debug.PhysisIIState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.PhysisIIState = $"{injuredCount} injured";
            return false;
        }

        if (avgHp > config.PhysisIIThreshold)
        {
            context.Debug.PhysisIIState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.PhysisII;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Physis II";
            context.Debug.PhysisIIState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryHolos(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableHolos)
            return false;

        if (player.Level < SGEActions.Holos.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Holos.ActionId))
        {
            context.Debug.HolosState = "On CD";
            return false;
        }

        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Holos is a 2-minute CD - save for emergencies
        if (lowestHp > config.HolosThreshold)
        {
            context.Debug.HolosState = $"Lowest HP {lowestHp:P0}";
            return false;
        }

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.HolosState = $"{injuredCount} injured";
            return false;
        }

        var action = SGEActions.Holos;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Holos";
            context.Debug.HolosState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryHaima(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableHaima)
            return false;

        if (player.Level < SGEActions.Haima.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Haima.ActionId))
        {
            context.Debug.HaimaState = "On CD";
            return false;
        }

        // Haima is best for tanks taking consistent damage
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null)
        {
            context.Debug.HaimaState = "No tank";
            return false;
        }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;

        // Don't use if tank already has Haima
        if (AsclepiusStatusHelper.HasHaima(tank))
        {
            context.Debug.HaimaState = "Already has Haima";
            return false;
        }

        if (hpPercent > config.HaimaThreshold)
        {
            context.Debug.HaimaState = $"Tank at {hpPercent:P0}";
            return false;
        }

        var action = SGEActions.Haima;
        if (context.ActionService.ExecuteOgcd(action, tank.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Haima";
            context.Debug.HaimaState = "Executing";
            context.Debug.HaimaTarget = tank.Name?.TextValue ?? "Unknown";
            return true;
        }

        return false;
    }

    private bool TryPanhaima(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePanhaima)
            return false;

        if (player.Level < SGEActions.Panhaima.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Panhaima.ActionId))
        {
            context.Debug.PanhaimaState = "On CD";
            return false;
        }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Panhaima is a 2-minute CD - save for raidwides
        if (avgHp > config.PanhaimaThreshold)
        {
            context.Debug.PanhaimaState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Panhaima;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Panhaima";
            context.Debug.PanhaimaState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryPepsis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePepsis)
            return false;

        if (player.Level < SGEActions.Pepsis.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Pepsis.ActionId))
        {
            context.Debug.PepsisState = "On CD";
            return false;
        }

        // Count party members with Eukrasian shields
        var shieldedCount = 0;
        foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
        {
            if (AsclepiusStatusHelper.HasEukrasianDiagnosisShield(member) ||
                AsclepiusStatusHelper.HasEukrasianPrognosisShield(member))
            {
                shieldedCount++;
            }
        }

        if (shieldedCount < config.AoEHealMinTargets)
        {
            context.Debug.PepsisState = $"{shieldedCount} shielded";
            return false;
        }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PepsisThreshold)
        {
            context.Debug.PepsisState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Pepsis;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Pepsis";
            context.Debug.PepsisState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryRhizomata(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableRhizomata)
            return false;

        if (player.Level < SGEActions.Rhizomata.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Rhizomata.ActionId))
        {
            context.Debug.RhizomataState = "On CD";
            return false;
        }

        // Don't overcap Addersgall
        if (context.AddersgallStacks >= 3)
        {
            context.Debug.RhizomataState = "At max stacks";
            return false;
        }

        // Use proactively to prevent overcapping
        if (config.PreventAddersgallCap && context.AddersgallStacks >= 2 && context.AddersgallTimer < 5f)
        {
            // About to cap, use Rhizomata to bank a stack
            var action = SGEActions.Rhizomata;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Rhizomata";
                context.Debug.RhizomataState = "Preventing cap";
                return true;
            }
        }

        // Use when low on Addersgall
        if (context.AddersgallStacks == 0)
        {
            var action = SGEActions.Rhizomata;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Rhizomata";
                context.Debug.RhizomataState = "Out of Addersgall";
                return true;
            }
        }

        context.Debug.RhizomataState = "Saving";
        return false;
    }

    private bool TryKrasis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableKrasis)
            return false;

        if (player.Level < SGEActions.Krasis.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Krasis.ActionId))
        {
            context.Debug.KrasisState = "On CD";
            return false;
        }

        // Find a target that needs healing boost
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
        {
            context.Debug.KrasisState = "No target";
            return false;
        }

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.KrasisThreshold)
        {
            context.Debug.KrasisState = $"Target at {hpPercent:P0}";
            return false;
        }

        // Don't stack with existing Krasis
        if (AsclepiusStatusHelper.HasKrasis(target))
        {
            context.Debug.KrasisState = "Already has Krasis";
            return false;
        }

        var action = SGEActions.Krasis;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Krasis";
            context.Debug.KrasisState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryZoe(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableZoe)
            return false;

        if (player.Level < SGEActions.Zoe.MinLevel)
            return false;

        // Already have Zoe active
        if (context.HasZoe)
        {
            context.Debug.ZoeState = "Active";
            return false;
        }

        if (!context.ActionService.IsActionReady(SGEActions.Zoe.ActionId))
        {
            context.Debug.ZoeState = "On CD";
            return false;
        }

        // Use Zoe before a big heal
        var (avgHp, lowestHp, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Use when someone is critically low and we'll need a big heal
        if (lowestHp > config.DiagnosisThreshold)
        {
            context.Debug.ZoeState = $"Lowest HP {lowestHp:P0}";
            return false;
        }

        var action = SGEActions.Zoe;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Zoe";
            context.Debug.ZoeState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryLucidDreaming(IAsclepiusContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableLucidDreaming)
        {
            context.Debug.LucidState = "Disabled";
            return false;
        }

        if (player.Level < SGEActions.LucidDreaming.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.LucidDreaming.ActionId))
        {
            context.Debug.LucidState = "On CD";
            return false;
        }

        var mpPercent = (float)player.CurrentMp / player.MaxMp;
        if (mpPercent > config.LucidDreamingThreshold)
        {
            context.Debug.LucidState = $"MP {mpPercent:P0}";
            return false;
        }

        var action = SGEActions.LucidDreaming;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Lucid Dreaming";
            context.Debug.LucidState = "Executing";
            return true;
        }

        return false;
    }

    #endregion

    #region GCD Heals

    private bool TryPneuma(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePneuma)
            return false;

        if (player.Level < SGEActions.Pneuma.MinLevel)
        {
            context.Debug.PneumaState = "Level too low";
            return false;
        }

        // Pneuma has a 2-minute cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Pneuma.ActionId))
        {
            context.Debug.PneumaState = "On CD";
            return false;
        }

        // Check if we have a target
        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SGEActions.Pneuma.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PneumaState = "No enemy";
            return false;
        }

        // Use Pneuma when party needs healing AND we can hit an enemy
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PneumaThreshold && injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.PneumaState = $"Party HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Pneuma;
        if (context.ActionService.ExecuteGcd(action, enemy.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Pneuma";
            context.Debug.PneumaState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryEukrasianHealing(IAsclepiusContext context, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (player.Level < SGEActions.Eukrasia.MinLevel)
            return false;

        // If we already have Eukrasia active, use it for a heal
        if (context.HasEukrasia)
        {
            return TryEukrasianHealSpell(context);
        }

        // Decide if we should activate Eukrasia for healing
        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // AoE shield if multiple people need shields
        if (config.EnableEukrasianPrognosis && injuredCount >= config.AoEHealMinTargets &&
            avgHp < config.AoEHealThreshold)
        {
            return TryActivateEukrasia(context);
        }

        // Single-target shield for tank or low HP member
        if (config.EnableEukrasianDiagnosis && lowestHp < config.EukrasianDiagnosisThreshold)
        {
            return TryActivateEukrasia(context);
        }

        return false;
    }

    private bool TryActivateEukrasia(IAsclepiusContext context)
    {
        var player = context.Player;

        var action = SGEActions.Eukrasia;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Eukrasia";
            context.Debug.EukrasiaState = "Activating";
            return true;
        }

        return false;
    }

    private bool TryEukrasianHealSpell(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Prefer AoE if multiple injured
        if (config.EnableEukrasianPrognosis && injuredCount >= config.AoEHealMinTargets)
        {
            var action = player.Level >= SGEActions.EukrasianPrognosisII.MinLevel
                ? SGEActions.EukrasianPrognosisII
                : SGEActions.EukrasianPrognosis;

            if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "E.Prognosis";
                context.Debug.EukrasianPrognosisState = "Executing";
                return true;
            }
        }

        // Single-target shield
        if (config.EnableEukrasianDiagnosis)
        {
            var target = context.PartyHelper.FindLowestHpPartyMember(player);
            if (target == null)
                return false;

            // Don't stack shields
            if (AsclepiusStatusHelper.HasEukrasianDiagnosisShield(target))
            {
                context.Debug.EukrasianDiagnosisState = "Already shielded";
                return false;
            }

            var action = SGEActions.EukrasianDiagnosis;
            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "E.Diagnosis";
                context.Debug.EukrasianDiagnosisState = "Executing";
                return true;
            }
        }

        return false;
    }

    private bool TryPrognosis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (player.Level < SGEActions.Prognosis.MinLevel)
            return false;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.AoEStatus = $"{injuredCount} < {config.AoEHealMinTargets} injured";
            return false;
        }

        if (avgHp > config.AoEHealThreshold)
        {
            context.Debug.AoEStatus = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Prognosis;
        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Prognosis";
            context.Debug.AoEStatus = "Executing";
            return true;
        }

        return false;
    }

    private bool TryDiagnosis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.DruocholeThreshold)
            return false;

        var action = SGEActions.Diagnosis;
        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Diagnosis";
            context.LogHealDecision(target.Name?.TextValue ?? "Unknown", hpPercent, action.Name, action.HealPotency, "Low HP");
            return true;
        }

        return false;
    }

    #endregion
}
