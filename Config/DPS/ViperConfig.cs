using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Viper (Echidna) configuration options.
/// Controls dual wield combos, venom system, and Reawaken burst.
/// </summary>
public sealed class ViperConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE combo rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Twinblade combos.
    /// </summary>
    public bool EnableTwinbladeCombo { get; set; } = true;

    /// <summary>
    /// Whether to use Reawaken burst sequence.
    /// </summary>
    public bool EnableReawaken { get; set; } = true;

    /// <summary>
    /// Whether to use Generation abilities (during Reawaken).
    /// </summary>
    public bool EnableGenerationAbilities { get; set; } = true;

    /// <summary>
    /// Whether to use Ouroboros finisher.
    /// </summary>
    public bool EnableOuroboros { get; set; } = true;

    /// <summary>
    /// Whether to use Uncoiled Fury.
    /// </summary>
    public bool EnableUncoiledFury { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Serpent's Ire.
    /// </summary>
    public bool EnableSerpentsIre { get; set; } = true;

    #endregion

    #region Venom Settings

    /// <summary>
    /// Whether to maintain venom effects.
    /// </summary>
    public bool MaintainVenoms { get; set; } = true;

    /// <summary>
    /// Prioritize Hindstung/Flankstung based on position.
    /// </summary>
    public bool OptimizeVenomPositionals { get; set; } = true;

    #endregion

    #region Rattling Coil Settings

    /// <summary>
    /// Minimum Rattling Coil stacks to use Uncoiled Fury.
    /// </summary>
    private int _rattlingCoilMinStacks = 1;
    public int RattlingCoilMinStacks
    {
        get => _rattlingCoilMinStacks;
        set => _rattlingCoilMinStacks = Math.Clamp(value, 1, 3);
    }

    /// <summary>
    /// Save Rattling Coil stacks for burst windows.
    /// </summary>
    public bool SaveRattlingCoilForBurst { get; set; } = true;

    #endregion

    #region Anguine Tribute Settings

    /// <summary>
    /// Minimum Anguine Tribute stacks for Reawaken.
    /// </summary>
    private int _anguineMinStacks = 5;
    public int AnguineMinStacks
    {
        get => _anguineMinStacks;
        set => _anguineMinStacks = Math.Clamp(value, 1, 5);
    }

    /// <summary>
    /// Save Anguine Tribute for burst windows.
    /// </summary>
    public bool SaveAnguineForBurst { get; set; } = true;

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Maximum seconds to hold Serpent's Ire waiting for party buffs.
    /// </summary>
    private float _serpentsIreHoldTime = 3.0f;
    public float SerpentsIreHoldTime
    {
        get => _serpentsIreHoldTime;
        set => _serpentsIreHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Enter Reawaken during burst windows.
    /// </summary>
    public bool UseReawakenDuringBurst { get; set; } = true;

    /// <summary>
    /// Pool gauge resources (Reawaken) for raid buff burst windows.
    /// When enabled, holds Serpent Offering spenders within 8s of an imminent burst.
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

    #endregion

    #region AoE Settings

    /// <summary>
    /// Minimum enemies for AoE rotation.
    /// </summary>
    private int _aoEMinTargets = 3;
    public int AoEMinTargets
    {
        get => _aoEMinTargets;
        set => _aoEMinTargets = Math.Clamp(value, 2, 8);
    }

    #endregion

    #region Positional Settings

    /// <summary>
    /// Whether to enforce positional requirements.
    /// </summary>
    public bool EnforcePositionals { get; set; } = false;

    /// <summary>
    /// Allow weaponskills even without True North when out of position.
    /// </summary>
    public bool AllowPositionalLoss { get; set; } = true;

    #endregion

    #region Role Action Settings

    /// <summary>
    /// Enable Feint for enemy damage reduction.
    /// </summary>
    public bool EnableFeint { get; set; } = true;

    #endregion
}
