using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.IrisCore.Context;

namespace Olympus.Rotation.IrisCore.Modules;

/// <summary>
/// Handles Pictomancer GCD rotation.
/// Manages base combo, subtractive combo, hammer combo, paint spenders, and finishers.
/// </summary>
public sealed class DamageModule : IIrisModule
{
    public int Priority => 30; // Lower priority than buffs
    public string Name => "Damage";

    public bool TryExecute(IIrisContext context, bool isMoving)
    {
        // Allow painting motifs out of combat
        if (!context.InCombat)
        {
            // Try to prepaint motifs before combat
            if (TryPrepaintMotif(context))
                return true;

            context.Debug.DamageState = "Not in combat";
            return false;
        }

        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // Don't interrupt casts unless we can slidecast
        if (context.IsCasting && !context.CanSlidecast)
        {
            context.Debug.DamageState = "Casting";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target for damage GCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Priority 1: Star Prism during Starstruck (instant, high potency)
        if (TryStarPrism(context, target))
            return true;

        // Priority 2: Rainbow Drip with Rainbow Bright (instant)
        if (TryRainbowDrip(context, target, isMoving))
            return true;

        // Priority 3: Hammer Combo (all instant, use during burst)
        if (TryHammerCombo(context, target))
            return true;

        // Priority 4: Comet in Black (instant, high damage with Black Paint)
        if (TryCometInBlack(context, target, isMoving))
            return true;

        // Priority 5: Holy in White for movement (instant)
        if (TryHolyInWhite(context, target, isMoving))
            return true;

        // Priority 6: Subtractive Combo (when buff is active)
        if (TrySubtractiveCombo(context, target))
            return true;

        // Priority 7: Base Combo
        if (TryBaseCombo(context, target))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IIrisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region GCD Actions

    private bool TryPrepaintMotif(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Already casting
        if (context.IsCasting)
            return false;

        // Priority: Landscape > Creature > Weapon (for burst preparation)
        if (context.NeedsLandscapeMotif && level >= PCTActions.LandscapeMotif.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(PCTActions.StarrySkyMotif, player.GameObjectId))
            {
                context.Debug.PlannedAction = PCTActions.StarrySkyMotif.Name;
                context.Debug.DamageState = "Painting Starry Sky";
                return true;
            }
        }

        if (context.NeedsCreatureMotif && level >= PCTActions.CreatureMotif.MinLevel)
        {
            var motif = PCTActions.GetCreatureMotif(level, 0);
            if (context.ActionService.ExecuteGcd(motif, player.GameObjectId))
            {
                context.Debug.PlannedAction = motif.Name;
                context.Debug.DamageState = $"Painting {motif.Name}";
                return true;
            }
        }

        if (context.NeedsWeaponMotif && level >= PCTActions.WeaponMotif.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(PCTActions.HammerMotif, player.GameObjectId))
            {
                context.Debug.PlannedAction = PCTActions.HammerMotif.Name;
                context.Debug.DamageState = "Painting Hammer";
                return true;
            }
        }

        return false;
    }

    private bool TryStarPrism(IIrisContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.StarPrism.MinLevel)
            return false;

        // Requires Starstruck buff
        if (!context.HasStarstruck)
            return false;

        if (context.ActionService.ExecuteGcd(PCTActions.StarPrism, target.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.StarPrism.Name;
            context.Debug.DamageState = "Star Prism";
            return true;
        }

        return false;
    }

    private bool TryRainbowDrip(IIrisContext context, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.RainbowDrip.MinLevel)
            return false;

        // With Rainbow Bright, it's instant
        if (!context.HasRainbowBright)
        {
            // Without the buff, it has a long cast time
            // Only use if not moving and during burst
            if (isMoving)
                return false;

            // Only hardcast during burst window or if no other options
            if (!context.IsInBurstWindow)
                return false;
        }

        if (context.ActionService.ExecuteGcd(PCTActions.RainbowDrip, target.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.RainbowDrip.Name;
            context.Debug.DamageState = context.HasRainbowBright ? "Rainbow Drip (instant)" : "Rainbow Drip (hardcast)";
            return true;
        }

        return false;
    }

    private bool TryHammerCombo(IIrisContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.HammerStamp.MinLevel)
            return false;

        // Need Hammer Time buff to use hammer combo
        if (!context.HasHammerTime && !context.IsInHammerCombo)
            return false;

        // Get the next hammer action
        var hammerAction = PCTActions.GetHammerComboAction(context.HammerComboStep, level);
        if (hammerAction == null)
            return false;

        if (context.ActionService.ExecuteGcd(hammerAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = hammerAction.Name;
            context.Debug.DamageState = $"Hammer ({hammerAction.Name})";
            return true;
        }

        return false;
    }

    private bool TryCometInBlack(IIrisContext context, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.CometInBlack.MinLevel)
            return false;

        // Requires Black Paint
        if (!context.HasBlackPaint)
            return false;

        // Comet is instant, good for movement
        if (context.ActionService.ExecuteGcd(PCTActions.CometInBlack, target.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.CometInBlack.Name;
            context.Debug.DamageState = "Comet in Black";
            return true;
        }

        return false;
    }

    private bool TryHolyInWhite(IIrisContext context, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.HolyInWhite.MinLevel)
            return false;

        // Requires White Paint
        if (!context.HasWhitePaint)
            return false;

        // Holy is instant, use for movement or when paint is capping
        // Prioritize using for movement or if close to cap (4+ stacks)
        if (!isMoving && context.WhitePaint < 4)
        {
            // Don't waste paint if we have better options
            if (!context.IsInBurstWindow)
                return false;
        }

        if (context.ActionService.ExecuteGcd(PCTActions.HolyInWhite, target.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.HolyInWhite.Name;
            context.Debug.DamageState = $"Holy in White ({context.WhitePaint - 1} paint)";
            return true;
        }

        return false;
    }

    private bool TrySubtractiveCombo(IIrisContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.BlizzardInCyan.MinLevel)
            return false;

        // Need Subtractive Palette buff or Subtractive Spectrum
        if (!context.HasSubtractivePalette && !context.HasSubtractiveSpectrum)
            return false;

        // Get the appropriate combo action
        var comboAction = PCTActions.GetSubtractiveComboAction(context.BaseComboStep, context.ShouldUseAoe, level);

        if (context.ActionService.ExecuteGcd(comboAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = comboAction.Name;
            context.Debug.DamageState = $"Subtractive ({comboAction.Name})";
            return true;
        }

        return false;
    }

    private bool TryBaseCombo(IIrisContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the appropriate combo action based on step and AoE status
        var comboAction = PCTActions.GetBaseComboAction(context.BaseComboStep, context.ShouldUseAoe, level);

        if (context.ActionService.ExecuteGcd(comboAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = comboAction.Name;
            context.Debug.DamageState = $"Base Combo ({comboAction.Name})";
            return true;
        }

        return false;
    }

    #endregion
}
