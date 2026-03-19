using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Bard (Calliope) configuration options.
/// Controls song rotation, DoT management, and proc handling.
/// </summary>
public sealed class BardConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Refulgent Arrow procs.
    /// </summary>
    public bool EnableRefulgentArrow { get; set; } = true;

    /// <summary>
    /// Whether to use Apex Arrow.
    /// </summary>
    public bool EnableApexArrow { get; set; } = true;

    /// <summary>
    /// Whether to use Blast Arrow.
    /// </summary>
    public bool EnableBlastArrow { get; set; } = true;

    /// <summary>
    /// Whether to use Bloodletter/Rain of Death.
    /// </summary>
    public bool EnableBloodletter { get; set; } = true;

    /// <summary>
    /// Whether to use Empyreal Arrow.
    /// </summary>
    public bool EnableEmpyrealArrow { get; set; } = true;

    /// <summary>
    /// Whether to use Sidewinder.
    /// </summary>
    public bool EnableSidewinder { get; set; } = true;

    /// <summary>
    /// Whether to use Pitch Perfect during Wanderer's Minuet.
    /// </summary>
    public bool EnablePitchPerfect { get; set; } = true;

    #endregion

    #region DoT Toggles

    /// <summary>
    /// Whether to maintain Caustic Bite.
    /// </summary>
    public bool EnableCausticBite { get; set; } = true;

    /// <summary>
    /// Whether to maintain Stormbite.
    /// </summary>
    public bool EnableStormbite { get; set; } = true;

    /// <summary>
    /// Whether to use Iron Jaws for DoT refresh.
    /// </summary>
    public bool EnableIronJaws { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Raging Strikes.
    /// </summary>
    public bool EnableRagingStrikes { get; set; } = true;

    /// <summary>
    /// Whether to use Battle Voice (party buff).
    /// </summary>
    public bool EnableBattleVoice { get; set; } = true;

    /// <summary>
    /// Whether to use Radiant Finale (party buff).
    /// </summary>
    public bool EnableRadiantFinale { get; set; } = true;

    /// <summary>
    /// Whether to use Barrage.
    /// </summary>
    public bool EnableBarrage { get; set; } = true;

    /// <summary>
    /// Whether to rotate through songs automatically.
    /// </summary>
    public bool EnableSongRotation { get; set; } = true;

    /// <summary>
    /// Whether to use Resonant Arrow.
    /// </summary>
    public bool EnableResonantArrow { get; set; } = true;

    /// <summary>
    /// Whether to use Radiant Encore.
    /// </summary>
    public bool EnableRadiantEncore { get; set; } = true;

    #endregion

    #region Song Settings

    /// <summary>
    /// Preferred song rotation order.
    /// </summary>
    public SongRotation SongRotation { get; set; } = SongRotation.WanderersMagesBallad;

    /// <summary>
    /// Minimum Repertoire stacks for Pitch Perfect.
    /// </summary>
    private int _pitchPerfectMinStacks = 3;
    public int PitchPerfectMinStacks
    {
        get => _pitchPerfectMinStacks;
        set => _pitchPerfectMinStacks = Math.Clamp(value, 1, 3);
    }

    /// <summary>
    /// Use Pitch Perfect at 2 stacks if song is ending soon.
    /// </summary>
    public bool UsePitchPerfectEarly { get; set; } = true;

    /// <summary>
    /// Seconds remaining on song to use Pitch Perfect early.
    /// </summary>
    private float _pitchPerfectEarlyThreshold = 3.0f;
    public float PitchPerfectEarlyThreshold
    {
        get => _pitchPerfectEarlyThreshold;
        set => _pitchPerfectEarlyThreshold = Math.Clamp(value, 0f, 10f);
    }

    #endregion

    #region Soul Voice Settings

    /// <summary>
    /// Minimum Soul Voice gauge for Apex Arrow.
    /// </summary>
    private int _apexArrowMinGauge = 80;
    public int ApexArrowMinGauge
    {
        get => _apexArrowMinGauge;
        set => _apexArrowMinGauge = Math.Clamp(value, 20, 100);
    }

    /// <summary>
    /// Use Apex Arrow during burst windows regardless of gauge.
    /// </summary>
    public bool UseApexDuringBurst { get; set; } = true;

    #endregion

    #region DoT Settings

    /// <summary>
    /// Seconds remaining on DoTs before refreshing with Iron Jaws.
    /// </summary>
    private float _dotRefreshThreshold = 6.0f;
    public float DotRefreshThreshold
    {
        get => _dotRefreshThreshold;
        set => _dotRefreshThreshold = Math.Clamp(value, 0f, 15f);
    }

    /// <summary>
    /// Minimum target HP percentage to apply DoTs.
    /// </summary>
    private float _dotMinTargetHp = 0.10f;
    public float DotMinTargetHp
    {
        get => _dotMinTargetHp;
        set => _dotMinTargetHp = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Spread DoTs to multiple targets.
    /// </summary>
    public bool SpreadDots { get; set; } = true;

    /// <summary>
    /// Maximum targets to spread DoTs to.
    /// </summary>
    private int _dotSpreadMaxTargets = 3;
    public int DotSpreadMaxTargets
    {
        get => _dotSpreadMaxTargets;
        set => _dotSpreadMaxTargets = Math.Clamp(value, 1, 8);
    }

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Pool Soul Voice gauge for raid buff burst windows.
    /// When enabled, uses Apex Arrow at 50+ during burst (vs 80+ normally).
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

    /// <summary>
    /// Maximum seconds to hold buffs waiting for party burst.
    /// </summary>
    private float _buffHoldTime = 3.0f;
    public float BuffHoldTime
    {
        get => _buffHoldTime;
        set => _buffHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Minimum Coda for Radiant Finale.
    /// </summary>
    private int _radiantFinaleMinCoda = 3;
    public int RadiantFinaleMinCoda
    {
        get => _radiantFinaleMinCoda;
        set => _radiantFinaleMinCoda = Math.Clamp(value, 1, 3);
    }

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

    #region Utility Settings

    /// <summary>
    /// Whether to use Head Graze for interrupts.
    /// </summary>
    public bool EnableHeadGraze { get; set; } = true;

    /// <summary>
    /// Whether to use Nature's Minne for party support.
    /// </summary>
    public bool EnableNaturesMinne { get; set; } = true;

    /// <summary>
    /// Whether to use Warden's Paean for cleansing.
    /// </summary>
    public bool EnableWardensPaean { get; set; } = true;

    #endregion
}

/// <summary>
/// Song rotation order preference.
/// </summary>
public enum SongRotation
{
    /// <summary>
    /// Wanderer's Minuet → Mage's Ballad → Army's Paeon
    /// Standard rotation for maximum DPS.
    /// </summary>
    WanderersMagesBallad,

    /// <summary>
    /// Army's Paeon → Wanderer's Minuet → Mage's Ballad
    /// Alternative rotation for specific fight timings.
    /// </summary>
    ArmysWanderersMages
}
