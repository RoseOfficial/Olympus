using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Summoner (Persephone) configuration options.
/// Controls primal attunement, demi-summons, and burst timing.
/// </summary>
public sealed class SummonerConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use single-target rotation.
    /// </summary>
    public bool EnableSingleTargetRotation { get; set; } = true;

    /// <summary>
    /// Whether to use AoE rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Ruin III/Tri-disaster.
    /// </summary>
    public bool EnableRuin { get; set; } = true;

    /// <summary>
    /// Whether to use Astral Impulse/Astral Flare (Bahamut GCDs).
    /// </summary>
    public bool EnableAstralAbilities { get; set; } = true;

    /// <summary>
    /// Whether to use Fountain of Fire/Brand of Purgatory (Phoenix GCDs).
    /// </summary>
    public bool EnableFountainAbilities { get; set; } = true;

    /// <summary>
    /// Whether to use Ruin IV procs.
    /// </summary>
    public bool EnableRuinIV { get; set; } = true;

    #endregion

    #region Primal Toggles

    /// <summary>
    /// Whether to summon Ifrit-Egi.
    /// </summary>
    public bool EnableIfrit { get; set; } = true;

    /// <summary>
    /// Whether to summon Titan-Egi.
    /// </summary>
    public bool EnableTitan { get; set; } = true;

    /// <summary>
    /// Whether to summon Garuda-Egi.
    /// </summary>
    public bool EnableGaruda { get; set; } = true;

    /// <summary>
    /// Whether to use primal abilities (Gemshine/Precious Brilliance).
    /// </summary>
    public bool EnablePrimalAbilities { get; set; } = true;

    #endregion

    #region Demi-Summon Toggles

    /// <summary>
    /// Whether to summon Bahamut.
    /// </summary>
    public bool EnableBahamut { get; set; } = true;

    /// <summary>
    /// Whether to summon Phoenix.
    /// </summary>
    public bool EnablePhoenix { get; set; } = true;

    /// <summary>
    /// Whether to summon Solar Bahamut.
    /// </summary>
    public bool EnableSolarBahamut { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Searing Light (party buff).
    /// </summary>
    public bool EnableSearingLight { get; set; } = true;

    /// <summary>
    /// Whether to use Enkindle abilities.
    /// </summary>
    public bool EnableEnkindle { get; set; } = true;

    /// <summary>
    /// Whether to use Astral Flow.
    /// </summary>
    public bool EnableAstralFlow { get; set; } = true;

    #endregion

    #region Aetherflow Settings

    /// <summary>
    /// Whether to use Energy Drain/Energy Siphon.
    /// </summary>
    public bool EnableEnergyDrain { get; set; } = true;

    /// <summary>
    /// Whether to use Fester/Painflare.
    /// </summary>
    public bool EnableFester { get; set; } = true;

    /// <summary>
    /// Aetherflow stacks to reserve for emergency.
    /// </summary>
    private int _aetherflowReserve = 0;
    public int AetherflowReserve
    {
        get => _aetherflowReserve;
        set => _aetherflowReserve = Math.Clamp(value, 0, 2);
    }

    #endregion

    #region Primal Order Settings

    /// <summary>
    /// Preferred primal summon order.
    /// </summary>
    public PrimalOrder PrimalSummonOrder { get; set; } = PrimalOrder.TitanIfritGaruda;

    /// <summary>
    /// Whether to prioritize Ifrit during movement-heavy phases.
    /// </summary>
    public bool AdaptOrderForMovement { get; set; } = true;

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Align Searing Light with party burst windows.
    /// </summary>
    public bool AlignSearingLightWithParty { get; set; } = true;

    /// <summary>
    /// Maximum seconds to hold Searing Light waiting for party buffs.
    /// </summary>
    private float _searingLightHoldTime = 3.0f;
    public float SearingLightHoldTime
    {
        get => _searingLightHoldTime;
        set => _searingLightHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Summon Bahamut/Phoenix during burst windows.
    /// </summary>
    public bool UseDemiDuringBurst { get; set; } = true;

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

    #region MP Management

    /// <summary>
    /// Whether to use Lucid Dreaming.
    /// </summary>
    public bool EnableLucidDreaming { get; set; } = true;

    /// <summary>
    /// MP percentage threshold for Lucid Dreaming.
    /// </summary>
    private float _lucidDreamingThreshold = 0.70f;
    public float LucidDreamingThreshold
    {
        get => _lucidDreamingThreshold;
        set => _lucidDreamingThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Utility Settings

    /// <summary>
    /// Whether to use Resurrection.
    /// </summary>
    public bool EnableResurrection { get; set; } = true;

    /// <summary>
    /// Whether to use Swiftcast for Resurrection.
    /// </summary>
    public bool UseSwiftcastForResurrection { get; set; } = true;

    #endregion
}

/// <summary>
/// Primal summon order preference.
/// </summary>
public enum PrimalOrder
{
    /// <summary>
    /// Titan → Ifrit → Garuda (standard optimization).
    /// </summary>
    TitanIfritGaruda,

    /// <summary>
    /// Garuda → Titan → Ifrit (movement-friendly opener).
    /// </summary>
    GarudaTitanIfrit,

    /// <summary>
    /// Ifrit → Garuda → Titan (burst-focused).
    /// </summary>
    IfritGarudaTitan
}
