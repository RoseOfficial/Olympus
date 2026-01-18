using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.IrisCore.Helpers;

/// <summary>
/// Helper class for checking Pictomancer-specific status effects.
/// </summary>
public sealed class IrisStatusHelper
{
    #region Role Buffs

    /// <summary>
    /// Checks if the player has Swiftcast active.
    /// </summary>
    public bool HasSwiftcast(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.Swiftcast);
    }

    /// <summary>
    /// Gets the remaining duration of Swiftcast.
    /// </summary>
    public float GetSwiftcastRemaining(IGameObject player)
    {
        return GetStatusDuration(player, PCTActions.StatusIds.Swiftcast);
    }

    /// <summary>
    /// Checks if the player has Lucid Dreaming active.
    /// </summary>
    public bool HasLucidDreaming(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.LucidDreaming);
    }

    #endregion

    #region Pictomancer Core Buffs

    /// <summary>
    /// Checks if the player has Subtractive Palette buff active.
    /// </summary>
    public bool HasSubtractivePalette(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.SubtractivePalette);
    }

    /// <summary>
    /// Gets the remaining duration of Subtractive Palette.
    /// </summary>
    public float GetSubtractivePaletteRemaining(IGameObject player)
    {
        return GetStatusDuration(player, PCTActions.StatusIds.SubtractivePalette);
    }

    /// <summary>
    /// Checks if the player has Monochrome Tones active (Black Paint mode).
    /// </summary>
    public bool HasMonochromeTones(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.MonochromeTones);
    }

    /// <summary>
    /// Checks if the player has Starry Muse buff active.
    /// </summary>
    public bool HasStarryMuse(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.StarryMuse);
    }

    /// <summary>
    /// Gets the remaining duration of Starry Muse buff.
    /// </summary>
    public float GetStarryMuseRemaining(IGameObject player)
    {
        return GetStatusDuration(player, PCTActions.StatusIds.StarryMuse);
    }

    /// <summary>
    /// Checks if the player has Starstruck active (Star Prism ready).
    /// </summary>
    public bool HasStarstruck(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.Starstruck);
    }

    /// <summary>
    /// Gets the remaining duration of Starstruck.
    /// </summary>
    public float GetStarstruckRemaining(IGameObject player)
    {
        return GetStatusDuration(player, PCTActions.StatusIds.Starstruck);
    }

    /// <summary>
    /// Checks if the player has Hyperphantasia active.
    /// </summary>
    public bool HasHyperphantasia(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.Hyperphantasia);
    }

    /// <summary>
    /// Gets the remaining duration of Hyperphantasia.
    /// </summary>
    public float GetHyperphantasiaRemaining(IGameObject player)
    {
        return GetStatusDuration(player, PCTActions.StatusIds.Hyperphantasia);
    }

    /// <summary>
    /// Gets the stack count of Hyperphantasia.
    /// </summary>
    public int GetHyperphantasiaStacks(IGameObject player)
    {
        return GetStatusStacks(player, PCTActions.StatusIds.Hyperphantasia);
    }

    /// <summary>
    /// Checks if the player has Inspiration active (reduced motif cast time).
    /// </summary>
    public bool HasInspiration(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.Inspiration);
    }

    /// <summary>
    /// Checks if the player has Subtractive Spectrum active.
    /// </summary>
    public bool HasSubtractiveSpectrum(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.SubtractiveSpectrum);
    }

    /// <summary>
    /// Checks if the player has Rainbow Bright active (instant Rainbow Drip).
    /// </summary>
    public bool HasRainbowBright(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.RainbowBright);
    }

    /// <summary>
    /// Gets the remaining duration of Rainbow Bright.
    /// </summary>
    public float GetRainbowBrightRemaining(IGameObject player)
    {
        return GetStatusDuration(player, PCTActions.StatusIds.RainbowBright);
    }

    /// <summary>
    /// Checks if the player has Hammer Time active.
    /// </summary>
    public bool HasHammerTime(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.HammerTime);
    }

    /// <summary>
    /// Gets the remaining duration of Hammer Time.
    /// </summary>
    public float GetHammerTimeRemaining(IGameObject player)
    {
        return GetStatusDuration(player, PCTActions.StatusIds.HammerTime);
    }

    /// <summary>
    /// Gets the stack count of Hammer Time (number of hammer hits remaining).
    /// </summary>
    public int GetHammerTimeStacks(IGameObject player)
    {
        return GetStatusStacks(player, PCTActions.StatusIds.HammerTime);
    }

    /// <summary>
    /// Checks if the player has Aetherhues active.
    /// </summary>
    public bool HasAetherhues(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.Aetherhues);
    }

    /// <summary>
    /// Gets the stack count of Aetherhues.
    /// </summary>
    public int GetAetherhuesStacks(IGameObject player)
    {
        return GetStatusStacks(player, PCTActions.StatusIds.Aetherhues);
    }

    #endregion

    #region Mitigation

    /// <summary>
    /// Checks if the player has Tempera Coat active.
    /// </summary>
    public bool HasTemperaCoat(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.TemperaCoat);
    }

    /// <summary>
    /// Checks if the player has Tempera Grassa active.
    /// </summary>
    public bool HasTemperaGrassa(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.TemperaGrassa);
    }

    /// <summary>
    /// Checks if the player has Smudge movement buff active.
    /// </summary>
    public bool HasSmudge(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.Smudge);
    }

    /// <summary>
    /// Checks if the player has Surecast active.
    /// </summary>
    public bool HasSurecast(IGameObject player)
    {
        return HasStatus(player, PCTActions.StatusIds.Surecast);
    }

    #endregion

    #region Base Status Helpers

    /// <summary>
    /// Checks if a game object has a specific status effect.
    /// </summary>
    private bool HasStatus(IGameObject gameObject, uint statusId)
    {
        if (gameObject is not IBattleChara battleChara)
            return false;

        foreach (var status in battleChara.StatusList)
        {
            if (status.StatusId == statusId)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the remaining duration of a status effect.
    /// </summary>
    private float GetStatusDuration(IGameObject gameObject, uint statusId)
    {
        if (gameObject is not IBattleChara battleChara)
            return 0f;

        foreach (var status in battleChara.StatusList)
        {
            if (status.StatusId == statusId)
                return status.RemainingTime;
        }

        return 0f;
    }

    /// <summary>
    /// Gets the stack count of a status effect.
    /// </summary>
    private int GetStatusStacks(IGameObject gameObject, uint statusId)
    {
        if (gameObject is not IBattleChara battleChara)
            return 0;

        foreach (var status in battleChara.StatusList)
        {
            if (status.StatusId == statusId)
                return status.Param;
        }

        return 0;
    }

    #endregion
}
