using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.IrisCore.Helpers;

/// <summary>
/// Helper class for checking Pictomancer-specific status effects.
/// </summary>
public sealed class IrisStatusHelper : BaseStatusHelper
{
    #region Role Buffs

    /// <summary>
    /// Gets the remaining duration of Swiftcast.
    /// </summary>
    public float GetSwiftcastRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, PCTActions.StatusIds.Swiftcast);
    }

    #endregion

    #region Pictomancer Core Buffs

    /// <summary>
    /// Checks if the player has Subtractive Palette buff active.
    /// </summary>
    public bool HasSubtractivePalette(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.SubtractivePalette);
    }

    /// <summary>
    /// Gets the remaining duration of Subtractive Palette.
    /// </summary>
    public float GetSubtractivePaletteRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, PCTActions.StatusIds.SubtractivePalette);
    }

    /// <summary>
    /// Checks if the player has Monochrome Tones active (Black Paint mode).
    /// </summary>
    public bool HasMonochromeTones(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.MonochromeTones);
    }

    /// <summary>
    /// Checks if the player has Starry Muse buff active.
    /// </summary>
    public bool HasStarryMuse(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.StarryMuse);
    }

    /// <summary>
    /// Gets the remaining duration of Starry Muse buff.
    /// </summary>
    public float GetStarryMuseRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, PCTActions.StatusIds.StarryMuse);
    }

    /// <summary>
    /// Checks if the player has Starstruck active (Star Prism ready).
    /// </summary>
    public bool HasStarstruck(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.Starstruck);
    }

    /// <summary>
    /// Gets the remaining duration of Starstruck.
    /// </summary>
    public float GetStarstruckRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, PCTActions.StatusIds.Starstruck);
    }

    /// <summary>
    /// Checks if the player has Hyperphantasia active.
    /// </summary>
    public bool HasHyperphantasia(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.Hyperphantasia);
    }

    /// <summary>
    /// Gets the remaining duration of Hyperphantasia.
    /// </summary>
    public float GetHyperphantasiaRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, PCTActions.StatusIds.Hyperphantasia);
    }

    /// <summary>
    /// Gets the stack count of Hyperphantasia.
    /// </summary>
    public int GetHyperphantasiaStacks(IBattleChara player)
    {
        return GetStatusStacks(player, PCTActions.StatusIds.Hyperphantasia);
    }

    /// <summary>
    /// Checks if the player has Inspiration active (reduced motif cast time).
    /// </summary>
    public bool HasInspiration(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.Inspiration);
    }

    /// <summary>
    /// Checks if the player has Subtractive Spectrum active.
    /// </summary>
    public bool HasSubtractiveSpectrum(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.SubtractiveSpectrum);
    }

    /// <summary>
    /// Checks if the player has Rainbow Bright active (instant Rainbow Drip).
    /// </summary>
    public bool HasRainbowBright(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.RainbowBright);
    }

    /// <summary>
    /// Gets the remaining duration of Rainbow Bright.
    /// </summary>
    public float GetRainbowBrightRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, PCTActions.StatusIds.RainbowBright);
    }

    /// <summary>
    /// Checks if the player has Hammer Time active.
    /// </summary>
    public bool HasHammerTime(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.HammerTime);
    }

    /// <summary>
    /// Gets the remaining duration of Hammer Time.
    /// </summary>
    public float GetHammerTimeRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, PCTActions.StatusIds.HammerTime);
    }

    /// <summary>
    /// Gets the stack count of Hammer Time (number of hammer hits remaining).
    /// </summary>
    public int GetHammerTimeStacks(IBattleChara player)
    {
        return GetStatusStacks(player, PCTActions.StatusIds.HammerTime);
    }

    /// <summary>
    /// Checks if the player has Aetherhues active.
    /// </summary>
    public bool HasAetherhues(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.Aetherhues);
    }

    /// <summary>
    /// Gets the stack count of Aetherhues.
    /// </summary>
    public int GetAetherhuesStacks(IBattleChara player)
    {
        return GetStatusStacks(player, PCTActions.StatusIds.Aetherhues);
    }

    #endregion

    #region Mitigation

    /// <summary>
    /// Checks if the player has Tempera Coat active.
    /// </summary>
    public bool HasTemperaCoat(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.TemperaCoat);
    }

    /// <summary>
    /// Checks if the player has Tempera Grassa active.
    /// </summary>
    public bool HasTemperaGrassa(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.TemperaGrassa);
    }

    /// <summary>
    /// Checks if the player has Smudge movement buff active.
    /// </summary>
    public bool HasSmudge(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.Smudge);
    }

    /// <summary>
    /// Checks if the player has Surecast active.
    /// </summary>
    public bool HasSurecast(IBattleChara player)
    {
        return HasStatus(player, PCTActions.StatusIds.Surecast);
    }

    #endregion

}
