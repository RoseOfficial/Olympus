using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Dragoon (Zeus) configuration options.
/// Controls Eye gauge management, jump timing, and burst windows.
/// </summary>
public sealed class DragoonConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE combo rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Jump/High Jump on cooldown.
    /// </summary>
    public bool EnableJumps { get; set; } = true;

    /// <summary>
    /// Whether to use Spineshatter Dive.
    /// </summary>
    public bool EnableSpineshatterDive { get; set; } = true;

    /// <summary>
    /// Whether to use Dragonfire Dive.
    /// </summary>
    public bool EnableDragonfireDive { get; set; } = true;

    /// <summary>
    /// Whether to use Stardiver during Life of the Dragon.
    /// </summary>
    public bool EnableStardiver { get; set; } = true;

    /// <summary>
    /// Whether to use Geirskogul to enter Life of the Dragon.
    /// </summary>
    public bool EnableGeirskogul { get; set; } = true;

    /// <summary>
    /// Whether to use Nastrond during Life of the Dragon.
    /// </summary>
    public bool EnableNastrond { get; set; } = true;

    /// <summary>
    /// Whether to use Wyrmwind Thrust when available.
    /// </summary>
    public bool EnableWyrmwindThrust { get; set; } = true;

    /// <summary>
    /// Whether to use Mirage Dive during Life Surge.
    /// </summary>
    public bool EnableMirageDive { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Lance Charge.
    /// </summary>
    public bool EnableLanceCharge { get; set; } = true;

    /// <summary>
    /// Whether to use Battle Litany (party buff).
    /// </summary>
    public bool EnableBattleLitany { get; set; } = true;

    /// <summary>
    /// Whether to use Life Surge for guaranteed crits.
    /// </summary>
    public bool EnableLifeSurge { get; set; } = true;

    #endregion

    #region Eye Gauge Settings

    /// <summary>
    /// Minimum Eye gauge stacks to use Geirskogul.
    /// 0 = use immediately, 2 = wait for full gauge.
    /// </summary>
    private int _geirskogulMinEyes = 0;
    public int GeirskogulMinEyes
    {
        get => _geirskogulMinEyes;
        set => _geirskogulMinEyes = Math.Clamp(value, 0, 2);
    }

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Maximum seconds to hold Battle Litany waiting for party buffs.
    /// </summary>
    private float _battleLitanyHoldTime = 3.0f;
    public float BattleLitanyHoldTime
    {
        get => _battleLitanyHoldTime;
        set => _battleLitanyHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Pool gauge resources (Geirskogul, Nastrond) for raid buff burst windows.
    /// When enabled, holds gauge spenders within 8s of an imminent burst.
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
    /// When enabled, will not use positional actions if not in correct position.
    /// </summary>
    public bool EnforcePositionals { get; set; } = false;

    /// <summary>
    /// Allow True Thrust/Raiden Thrust even without True North when out of position.
    /// </summary>
    public bool AllowPositionalLoss { get; set; } = true;

    #endregion
}
