using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.EchidnaCore.Context;

namespace Olympus.Rotation.EchidnaCore.Modules;

/// <summary>
/// Handles the Viper damage rotation.
/// Manages Reawaken sequences, twinblade combos, dual wield combos, and resource building.
/// </summary>
public sealed class DamageModule : IEchidnaModule
{
    public int Priority => 30; // Lowest priority - damage after utility
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IEchidnaContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // oGCD Phase - weave damage oGCDs during GCD
        if (context.CanExecuteOgcd)
        {
            if (TryOgcdDamage(context, target, enemyCount))
                return true;
        }

        // GCD Phase
        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // === REAWAKEN STATE ===
        if (context.IsReawakened)
        {
            // Reawaken sequence: Generation GCDs with Legacy oGCDs
            if (TryReawakenGcd(context, target))
                return true;
        }

        // === NORMAL STATE ===

        // Priority 1: Reawaken (enter burst mode)
        if (TryReawaken(context, target))
            return true;

        // Priority 2: Continue twinblade combo (DreadCombo in progress)
        if (TryTwinbladeCombo(context, target, enemyCount))
            return true;

        // Priority 3: Uncoiled Fury (use Rattling Coils)
        if (TryUncoiledFury(context, target))
            return true;

        // Priority 4: Vicewinder/Vicepit (start twinblade combo)
        if (TryVicewinder(context, target, enemyCount))
            return true;

        // Priority 5: Maintain Noxious Gnash debuff (via Vicewinder or combo)
        // Vicewinder applies it, but we check here for low duration
        if (ShouldRefreshNoxiousGnash(context))
        {
            // If Vicewinder available, use it to refresh
            if (TryVicewinder(context, target, enemyCount, forceUse: true))
                return true;
        }

        // Priority 6: Dual wield combo
        if (TryDualWieldCombo(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IEchidnaContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IEchidnaContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Poised oGCDs (from twinblade combos)
        if (TryPoisedOgcd(context, target, enemyCount))
            return true;

        // Priority 2: Uncoiled follow-ups
        if (TryUncoiledOgcd(context, target))
            return true;

        return false;
    }

    private bool TryPoisedOgcd(IEchidnaContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Twinfang (from Hunter's Coil or Hunter's Den)
        if (context.HasPoisedForTwinfang)
        {
            var action = useAoe ? VPRActions.TwinfangBite : VPRActions.Twinfang;
            if (level >= action.MinLevel && context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = action.Name;
                    return true;
                }
            }
        }

        // Twinblood (from Swiftskin's Coil or Swiftskin's Den)
        if (context.HasPoisedForTwinblood)
        {
            var action = useAoe ? VPRActions.TwinbloodBite : VPRActions.Twinblood;
            if (level >= action.MinLevel && context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = action.Name;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryUncoiledOgcd(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // After Uncoiled Fury: Twinfang -> Twinblood
        // Check for the follow-up state (tracked via status)

        // Uncoiled Twinfang first
        if (level >= VPRActions.UncoiledTwinfang.MinLevel)
        {
            if (context.ActionService.IsActionReady(VPRActions.UncoiledTwinfang.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(VPRActions.UncoiledTwinfang, target.GameObjectId))
                {
                    context.Debug.PlannedAction = VPRActions.UncoiledTwinfang.Name;
                    context.Debug.DamageState = "Uncoiled Twinfang";
                    return true;
                }
            }
        }

        // Uncoiled Twinblood second
        if (level >= VPRActions.UncoiledTwinblood.MinLevel)
        {
            if (context.ActionService.IsActionReady(VPRActions.UncoiledTwinblood.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(VPRActions.UncoiledTwinblood, target.GameObjectId))
                {
                    context.Debug.PlannedAction = VPRActions.UncoiledTwinblood.Name;
                    context.Debug.DamageState = "Uncoiled Twinblood";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Reawaken Sequence

    private bool TryReawaken(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < VPRActions.Reawaken.MinLevel)
            return false;

        // Already in Reawaken
        if (context.IsReawakened)
            return false;

        // Need 50 Serpent Offerings OR Ready to Reawaken buff
        if (context.SerpentOffering < 50 && !context.HasReadyToReawaken)
        {
            context.Debug.DamageState = $"Need 50 Offerings ({context.SerpentOffering}/50)";
            return false;
        }

        // Optimal timing: Have both buffs active with good duration
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < 10f)
        {
            context.Debug.DamageState = "Waiting for Hunter's Instinct";
            return false;
        }

        if (!context.HasSwiftscaled || context.SwiftscaledRemaining < 10f)
        {
            context.Debug.DamageState = "Waiting for Swiftscaled";
            return false;
        }

        // Make sure Noxious Gnash is on target
        if (!context.HasNoxiousGnash || context.NoxiousGnashRemaining < 10f)
        {
            context.Debug.DamageState = "Need Noxious Gnash refresh";
            return false;
        }

        if (!context.ActionService.IsActionReady(VPRActions.Reawaken.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(VPRActions.Reawaken, target.GameObjectId))
        {
            context.Debug.PlannedAction = VPRActions.Reawaken.Name;
            context.Debug.DamageState = "Entering Reawaken";
            return true;
        }

        return false;
    }

    private bool TryReawakenGcd(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the correct Generation based on Anguine Tribute count
        var action = VPRActions.GetGenerationGcd(context.AnguineTribute);

        // Ouroboros is the finisher at 1 tribute
        if (context.AnguineTribute == 1 && level >= VPRActions.Ouroboros.MinLevel)
        {
            action = VPRActions.Ouroboros;
        }

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Tribute: {context.AnguineTribute})";
            return true;
        }

        return false;
    }

    #endregion

    #region Twinblade Combo

    private bool TryTwinbladeCombo(IEchidnaContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Check DreadCombo state for twinblade continuation
        switch (context.DreadCombo)
        {
            case VPRActions.DreadCombo.DreadwindyReady:
            case VPRActions.DreadCombo.HunterCoilReady:
                // Use Hunter's Coil
                if (level >= VPRActions.HuntersCoil.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.HuntersCoil.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.HuntersCoil, target.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.HuntersCoil.Name;
                            context.Debug.DamageState = "Hunter's Coil (Twinblade)";
                            return true;
                        }
                    }
                }
                // Fallback to Swiftskin's Coil
                if (level >= VPRActions.SwiftskinsCoil.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.SwiftskinsCoil.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.SwiftskinsCoil, target.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsCoil.Name;
                            context.Debug.DamageState = "Swiftskin's Coil (Twinblade)";
                            return true;
                        }
                    }
                }
                break;

            case VPRActions.DreadCombo.SwiftskinCoilReady:
                // Use Swiftskin's Coil
                if (level >= VPRActions.SwiftskinsCoil.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.SwiftskinsCoil.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.SwiftskinsCoil, target.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsCoil.Name;
                            context.Debug.DamageState = "Swiftskin's Coil (Twinblade)";
                            return true;
                        }
                    }
                }
                break;

            case VPRActions.DreadCombo.PitReady:
            case VPRActions.DreadCombo.HunterDenReady:
                // AoE: Use Hunter's Den
                if (level >= VPRActions.HuntersDen.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.HuntersDen.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.HuntersDen, player.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.HuntersDen.Name;
                            context.Debug.DamageState = "Hunter's Den (AoE Twinblade)";
                            return true;
                        }
                    }
                }
                break;

            case VPRActions.DreadCombo.SwiftskinDenReady:
                // AoE: Use Swiftskin's Den
                if (level >= VPRActions.SwiftskinsDen.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.SwiftskinsDen.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.SwiftskinsDen, player.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsDen.Name;
                            context.Debug.DamageState = "Swiftskin's Den (AoE Twinblade)";
                            return true;
                        }
                    }
                }
                break;
        }

        return false;
    }

    private bool TryVicewinder(IEchidnaContext context, IBattleChara target, int enemyCount, bool forceUse = false)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Don't start new twinblade if DreadCombo is in progress
        if (context.DreadCombo != VPRActions.DreadCombo.None && !forceUse)
            return false;

        if (useAoe && level >= VPRActions.Vicepit.MinLevel)
        {
            if (context.ActionService.IsActionReady(VPRActions.Vicepit.ActionId))
            {
                if (context.ActionService.ExecuteGcd(VPRActions.Vicepit, player.GameObjectId))
                {
                    context.Debug.PlannedAction = VPRActions.Vicepit.Name;
                    context.Debug.DamageState = "Vicepit (AoE Twinblade start)";
                    return true;
                }
            }
        }

        if (level >= VPRActions.Vicewinder.MinLevel)
        {
            if (context.ActionService.IsActionReady(VPRActions.Vicewinder.ActionId))
            {
                if (context.ActionService.ExecuteGcd(VPRActions.Vicewinder, target.GameObjectId))
                {
                    context.Debug.PlannedAction = VPRActions.Vicewinder.Name;
                    context.Debug.DamageState = "Vicewinder (Twinblade start)";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Uncoiled Fury

    private bool TryUncoiledFury(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < VPRActions.UncoiledFury.MinLevel)
            return false;

        // Need Rattling Coils
        if (context.RattlingCoils <= 0)
            return false;

        // Don't use during Reawaken
        if (context.IsReawakened)
            return false;

        // Good to use when:
        // 1. At range (movement)
        // 2. Have max coils (would overcap)
        // 3. As filler when other options unavailable

        // Check if at range
        var dx = player.Position.X - target.Position.X;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dz * dz);

        // Use at range or if capped on coils
        bool shouldUse = distance > FFXIVConstants.MeleeTargetingRange ||
                         context.RattlingCoils >= 3 ||
                         context.IsMoving;

        if (!shouldUse)
            return false;

        if (!context.ActionService.IsActionReady(VPRActions.UncoiledFury.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(VPRActions.UncoiledFury, target.GameObjectId))
        {
            context.Debug.PlannedAction = VPRActions.UncoiledFury.Name;
            context.Debug.DamageState = $"Uncoiled Fury (Coils: {context.RattlingCoils})";
            return true;
        }

        return false;
    }

    #endregion

    #region Dual Wield Combo

    private bool TryDualWieldCombo(IEchidnaContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        if (useAoe && level >= VPRActions.SteelMaw.MinLevel)
        {
            return TryAoeDualWieldCombo(context, player, enemyCount);
        }

        return TrySingleTargetDualWieldCombo(context, target);
    }

    private bool TrySingleTargetDualWieldCombo(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine combo state and next action
        ActionDefinition action;
        string comboInfo;

        // Check for finisher (step 3) - based on venom buffs
        if (context.ComboStep == 2)
        {
            // From Hunter's Sting path
            if (context.LastComboAction == VPRActions.HuntersSting.ActionId)
            {
                // Check venom to determine positional
                if (context.HasHindstungVenom)
                {
                    action = VPRActions.HindstingStrike; // Rear
                    comboInfo = "Hindsting (rear)";
                }
                else if (context.HasFlankstungVenom)
                {
                    action = VPRActions.FlankstingStrike; // Flank
                    comboInfo = "Flanksting (flank)";
                }
                else
                {
                    // No venom - use flank as default
                    action = VPRActions.FlankstingStrike;
                    comboInfo = "Flanksting (default)";
                }
            }
            // From Swiftskin's Sting path
            else if (context.LastComboAction == VPRActions.SwiftskinsString.ActionId)
            {
                if (context.HasHindsbaneVenom)
                {
                    action = VPRActions.HindsbaneFang; // Rear
                    comboInfo = "Hindsbane (rear)";
                }
                else if (context.HasFlanksbaneVenom)
                {
                    action = VPRActions.FlanksbaneFang; // Flank
                    comboInfo = "Flanksbane (flank)";
                }
                else
                {
                    // No venom - use flank as default
                    action = VPRActions.FlanksbaneFang;
                    comboInfo = "Flanksbane (default)";
                }
            }
            else
            {
                // Unknown combo state, start fresh
                action = GetStarterAction(context);
                comboInfo = "Restart combo";
            }
        }
        // Second hit (step 2)
        else if (context.ComboStep == 1)
        {
            if (context.LastComboAction == VPRActions.SteelFangs.ActionId)
            {
                action = VPRActions.HuntersSting;
                comboInfo = "Hunter's Sting";
            }
            else if (context.LastComboAction == VPRActions.ReavingFangs.ActionId)
            {
                action = VPRActions.SwiftskinsString;
                comboInfo = "Swiftskin's Sting";
            }
            else
            {
                // Unknown combo state, start fresh
                action = GetStarterAction(context);
                comboInfo = "Restart combo";
            }
        }
        // Combo starter (step 1)
        else
        {
            action = GetStarterAction(context);
            comboInfo = action == VPRActions.SteelFangs ? "Steel Fangs" : "Reaving Fangs";
        }

        if (level < action.MinLevel)
        {
            // Fall back to basic action
            action = VPRActions.SteelFangs;
            comboInfo = "Steel Fangs (level)";
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{comboInfo} (combo {context.ComboStep + 1})";
            return true;
        }

        return false;
    }

    private bool TryAoeDualWieldCombo(IEchidnaContext context, IPlayerCharacter player, int enemyCount)
    {
        var level = player.Level;
        ActionDefinition action;
        string comboInfo;

        // Finisher (step 3)
        if (context.ComboStep == 2)
        {
            if (context.LastComboAction == VPRActions.HuntersBite.ActionId)
            {
                action = VPRActions.JaggedMaw;
                comboInfo = "Jagged Maw";
            }
            else if (context.LastComboAction == VPRActions.SwiftskinsBite.ActionId)
            {
                action = VPRActions.BloodiedMaw;
                comboInfo = "Bloodied Maw";
            }
            else
            {
                action = GetAoeStarterAction(context);
                comboInfo = "Restart AoE combo";
            }
        }
        // Second hit (step 2)
        else if (context.ComboStep == 1)
        {
            if (context.LastComboAction == VPRActions.SteelMaw.ActionId)
            {
                action = VPRActions.HuntersBite;
                comboInfo = "Hunter's Bite";
            }
            else if (context.LastComboAction == VPRActions.ReavingMaw.ActionId)
            {
                action = VPRActions.SwiftskinsBite;
                comboInfo = "Swiftskin's Bite";
            }
            else
            {
                action = GetAoeStarterAction(context);
                comboInfo = "Restart AoE combo";
            }
        }
        // Starter (step 1)
        else
        {
            action = GetAoeStarterAction(context);
            comboInfo = action == VPRActions.SteelMaw ? "Steel Maw" : "Reaving Maw";
        }

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{comboInfo} (AoE combo {context.ComboStep + 1})";
            return true;
        }

        return false;
    }

    private ActionDefinition GetStarterAction(IEchidnaContext context)
    {
        // Use enhanced version if available
        if (context.HasHonedReavers)
            return VPRActions.ReavingFangs;
        if (context.HasHonedSteel)
            return VPRActions.SteelFangs;

        // Alternate based on which buff we need
        // If missing Hunter's Instinct, use Steel Fangs path
        // If missing Swiftscaled, use Reaving Fangs path
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < context.SwiftscaledRemaining)
            return VPRActions.SteelFangs;

        return VPRActions.ReavingFangs;
    }

    private ActionDefinition GetAoeStarterAction(IEchidnaContext context)
    {
        // Use enhanced version if available
        if (context.HasHonedReavers)
            return VPRActions.ReavingMaw;
        if (context.HasHonedSteel)
            return VPRActions.SteelMaw;

        // Alternate based on which buff we need
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < context.SwiftscaledRemaining)
            return VPRActions.SteelMaw;

        return VPRActions.ReavingMaw;
    }

    #endregion

    #region Helpers

    private bool ShouldRefreshNoxiousGnash(IEchidnaContext context)
    {
        // Refresh if missing or about to expire
        return !context.HasNoxiousGnash || context.NoxiousGnashRemaining < 5f;
    }

    #endregion
}
