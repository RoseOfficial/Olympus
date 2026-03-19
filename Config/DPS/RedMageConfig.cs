using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Red Mage (Circe) configuration options.
/// Controls Dualcast system, mana balance, and melee combo timing.
/// </summary>
public sealed class RedMageConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Verstone/Verfire procs.
    /// </summary>
    public bool EnableProcs { get; set; } = true;

    /// <summary>
    /// Whether to use melee combo (Riposte → Zwerchhau → Redoublement).
    /// </summary>
    public bool EnableMeleeCombo { get; set; } = true;

    /// <summary>
    /// Whether to use finisher combo (Verholy/Verflare → Scorch → Resolution).
    /// </summary>
    public bool EnableFinisherCombo { get; set; } = true;

    /// <summary>
    /// Whether to use Grand Impact procs.
    /// </summary>
    public bool EnableGrandImpact { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Embolden (party buff).
    /// </summary>
    public bool EnableEmbolden { get; set; } = true;

    /// <summary>
    /// Whether to use Manafication.
    /// </summary>
    public bool EnableManafication { get; set; } = true;

    /// <summary>
    /// Whether to use Acceleration.
    /// </summary>
    public bool EnableAcceleration { get; set; } = true;

    /// <summary>
    /// Whether to use Swiftcast.
    /// </summary>
    public bool EnableSwiftcast { get; set; } = true;

    #endregion

    #region oGCD Toggles

    /// <summary>
    /// Whether to use Fleche.
    /// </summary>
    public bool EnableFleche { get; set; } = true;

    /// <summary>
    /// Whether to use Contre Sixte.
    /// </summary>
    public bool EnableContreSixte { get; set; } = true;

    /// <summary>
    /// Whether to use Corps-a-corps.
    /// </summary>
    public bool EnableCorpsACorps { get; set; } = true;

    /// <summary>
    /// Whether to use Engagement/Displacement.
    /// </summary>
    public bool EnableEngagement { get; set; } = true;

    /// <summary>
    /// Prefer Engagement over Displacement (safer).
    /// </summary>
    public bool PreferEngagementOverDisplacement { get; set; } = true;

    /// <summary>
    /// Whether to use Vice of Thorns (follow-up after Embolden).
    /// </summary>
    public bool EnableViceOfThorns { get; set; } = true;

    /// <summary>
    /// Whether to use Prefulgence (follow-up after Manafication).
    /// </summary>
    public bool EnablePrefulgence { get; set; } = true;

    #endregion

    #region Mana Balance Settings

    /// <summary>
    /// Minimum mana to enter melee combo.
    /// </summary>
    private int _meleeComboMinMana = 50;
    public int MeleeComboMinMana
    {
        get => _meleeComboMinMana;
        set => _meleeComboMinMana = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Maximum mana imbalance before prioritizing the lower color.
    /// </summary>
    private int _manaImbalanceThreshold = 30;
    public int ManaImbalanceThreshold
    {
        get => _manaImbalanceThreshold;
        set => _manaImbalanceThreshold = Math.Clamp(value, 10, 50);
    }

    /// <summary>
    /// Whether to strictly balance mana (prioritize lower).
    /// </summary>
    public bool StrictManaBalance { get; set; } = true;

    #endregion

    #region Melee Combo Settings

    /// <summary>
    /// Use melee combo during burst windows.
    /// </summary>
    public bool UseMeleeDuringBurst { get; set; } = true;

    /// <summary>
    /// Hold melee combo for Embolden if close.
    /// </summary>
    public bool HoldMeleeForEmbolden { get; set; } = true;

    /// <summary>
    /// Maximum seconds to hold melee combo waiting for Embolden.
    /// </summary>
    private float _meleeHoldForEmbolden = 5.0f;
    public float MeleeHoldForEmbolden
    {
        get => _meleeHoldForEmbolden;
        set => _meleeHoldForEmbolden = Math.Clamp(value, 0f, 15f);
    }

    /// <summary>
    /// Verholy vs Verflare preference.
    /// </summary>
    public FinisherPreference FinisherPreference { get; set; } = FinisherPreference.BalanceBased;

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Hold melee combo entry for raid buff burst windows.
    /// When enabled, delays melee combo when burst is imminent within 8s.
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

    /// <summary>
    /// Maximum seconds to hold Embolden waiting for party buffs.
    /// </summary>
    private float _emboldenHoldTime = 3.0f;
    public float EmboldenHoldTime
    {
        get => _emboldenHoldTime;
        set => _emboldenHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Use Manafication with melee combo.
    /// </summary>
    public bool UseManaficationWithMelee { get; set; } = true;

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
    /// Whether to use Verraise.
    /// </summary>
    public bool EnableVerraise { get; set; } = true;

    /// <summary>
    /// Whether to use Swiftcast/Dualcast for Verraise.
    /// </summary>
    public bool UseDualcastForVerraise { get; set; } = true;

    /// <summary>
    /// Whether to use Magick Barrier for party mitigation.
    /// </summary>
    public bool EnableMagickBarrier { get; set; } = true;

    #endregion
}

/// <summary>
/// Verholy/Verflare finisher preference.
/// </summary>
public enum FinisherPreference
{
    /// <summary>
    /// Use finisher that balances mana (proc for lower).
    /// </summary>
    BalanceBased,

    /// <summary>
    /// Always prefer Verholy.
    /// </summary>
    PreferVerholy,

    /// <summary>
    /// Always prefer Verflare.
    /// </summary>
    PreferVerflare
}
