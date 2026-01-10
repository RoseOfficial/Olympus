using System;

namespace Olympus.Config;

/// <summary>
/// Sage-specific configuration options.
/// Controls Kardia, Addersgall, healing, shields, and damage behavior.
/// Named after Asclepius, the god of medicine.
/// </summary>
public sealed class SageConfig
{
    #region Kardia Settings

    /// <summary>
    /// Whether to automatically place Kardia on a party member.
    /// </summary>
    public bool AutoKardia { get; set; } = true;

    /// <summary>
    /// Whether to allow swapping Kardia target during combat.
    /// </summary>
    public bool KardiaSwapEnabled { get; set; } = true;

    /// <summary>
    /// HP threshold below which to consider swapping Kardia target.
    /// Only swaps if current Kardia target is above this threshold.
    /// </summary>
    private float _kardiaSwapThreshold = 0.60f;
    public float KardiaSwapThreshold
    {
        get => _kardiaSwapThreshold;
        set => _kardiaSwapThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Whether to use Soteria automatically.
    /// </summary>
    public bool EnableSoteria { get; set; } = true;

    /// <summary>
    /// HP threshold of Kardia target to trigger Soteria.
    /// </summary>
    private float _soteriaThreshold = 0.65f;
    public float SoteriaThreshold
    {
        get => _soteriaThreshold;
        set => _soteriaThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Addersgall Settings

    /// <summary>
    /// Number of Addersgall stacks to reserve for emergency healing.
    /// </summary>
    private int _addersgallReserve = 1;
    public int AddersgallReserve
    {
        get => _addersgallReserve;
        set => _addersgallReserve = Math.Clamp(value, 0, 3);
    }

    /// <summary>
    /// Whether to prevent Addersgall from capping (timer stops at 3 stacks).
    /// Will spend stacks proactively when timer is about to grant a new stack.
    /// </summary>
    public bool PreventAddersgallCap { get; set; } = true;

    /// <summary>
    /// Seconds before Addersgall timer grants new stack to start spending.
    /// Only relevant when at 3 stacks.
    /// </summary>
    private float _addersgallCapPreventWindow = 3f;
    public float AddersgallCapPreventWindow
    {
        get => _addersgallCapPreventWindow;
        set => _addersgallCapPreventWindow = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Whether to use Rhizomata to generate Addersgall stacks.
    /// </summary>
    public bool EnableRhizomata { get; set; } = true;

    /// <summary>
    /// Minimum free Addersgall slots before using Rhizomata.
    /// </summary>
    private int _rhizomataMinFreeSlots = 2;
    public int RhizomataMinFreeSlots
    {
        get => _rhizomataMinFreeSlots;
        set => _rhizomataMinFreeSlots = Math.Clamp(value, 1, 3);
    }

    #endregion

    #region Healing Toggles

    /// <summary>
    /// Whether to use Diagnosis (basic GCD heal).
    /// Generally avoided in favor of oGCD heals.
    /// </summary>
    public bool EnableDiagnosis { get; set; } = true;

    /// <summary>
    /// Whether to use Eukrasian Diagnosis (shield).
    /// </summary>
    public bool EnableEukrasianDiagnosis { get; set; } = true;

    /// <summary>
    /// Whether to use Prognosis (basic AoE heal).
    /// </summary>
    public bool EnablePrognosis { get; set; } = true;

    /// <summary>
    /// Whether to use Eukrasian Prognosis (AoE shield).
    /// </summary>
    public bool EnableEukrasianPrognosis { get; set; } = true;

    /// <summary>
    /// Whether to use Druochole (oGCD single-target heal).
    /// </summary>
    public bool EnableDruochole { get; set; } = true;

    /// <summary>
    /// Whether to use Taurochole (oGCD heal + mitigation).
    /// </summary>
    public bool EnableTaurochole { get; set; } = true;

    /// <summary>
    /// Whether to use Ixochole (oGCD AoE heal).
    /// </summary>
    public bool EnableIxochole { get; set; } = true;

    /// <summary>
    /// Whether to use Kerachole (oGCD AoE HoT + mitigation).
    /// </summary>
    public bool EnableKerachole { get; set; } = true;

    /// <summary>
    /// Whether to use Physis II (free AoE HoT).
    /// </summary>
    public bool EnablePhysisII { get; set; } = true;

    /// <summary>
    /// Whether to use Holos (free AoE heal + shield + mitigation).
    /// </summary>
    public bool EnableHolos { get; set; } = true;

    /// <summary>
    /// Whether to use Pepsis to convert shields to healing.
    /// </summary>
    public bool EnablePepsis { get; set; } = true;

    /// <summary>
    /// Whether to use Pneuma (damage + party heal).
    /// </summary>
    public bool EnablePneuma { get; set; } = true;

    #endregion

    #region Healing Thresholds

    /// <summary>
    /// HP threshold to trigger Diagnosis.
    /// </summary>
    private float _diagnosisThreshold = 0.40f;
    public float DiagnosisThreshold
    {
        get => _diagnosisThreshold;
        set => _diagnosisThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Eukrasian Diagnosis (shield).
    /// </summary>
    private float _eukrasianDiagnosisThreshold = 0.75f;
    public float EukrasianDiagnosisThreshold
    {
        get => _eukrasianDiagnosisThreshold;
        set => _eukrasianDiagnosisThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Druochole.
    /// </summary>
    private float _druocholeThreshold = 0.55f;
    public float DruocholeThreshold
    {
        get => _druocholeThreshold;
        set => _druocholeThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Taurochole.
    /// </summary>
    private float _taurocholeThreshold = 0.55f;
    public float TaurocholeThreshold
    {
        get => _taurocholeThreshold;
        set => _taurocholeThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Average party HP threshold to trigger Kerachole.
    /// </summary>
    private float _keracholeThreshold = 0.80f;
    public float KeracholeThreshold
    {
        get => _keracholeThreshold;
        set => _keracholeThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Average party HP threshold to trigger Ixochole.
    /// </summary>
    private float _ixocholeThreshold = 0.65f;
    public float IxocholeThreshold
    {
        get => _ixocholeThreshold;
        set => _ixocholeThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Average party HP threshold to trigger Physis II.
    /// </summary>
    private float _physisIIThreshold = 0.80f;
    public float PhysisIIThreshold
    {
        get => _physisIIThreshold;
        set => _physisIIThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Average party HP threshold to trigger Holos.
    /// </summary>
    private float _holosThreshold = 0.60f;
    public float HolosThreshold
    {
        get => _holosThreshold;
        set => _holosThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Average party HP threshold to trigger Pneuma.
    /// </summary>
    private float _pneumaThreshold = 0.65f;
    public float PneumaThreshold
    {
        get => _pneumaThreshold;
        set => _pneumaThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Pepsis (converts shields to healing).
    /// </summary>
    private float _pepsisThreshold = 0.50f;
    public float PepsisThreshold
    {
        get => _pepsisThreshold;
        set => _pepsisThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum party members below AoE threshold to trigger AoE heals.
    /// </summary>
    private int _aoeHealMinTargets = 3;
    public int AoEHealMinTargets
    {
        get => _aoeHealMinTargets;
        set => _aoeHealMinTargets = Math.Clamp(value, 1, 8);
    }

    /// <summary>
    /// HP threshold for AoE healing (Prognosis, Ixochole, etc.).
    /// </summary>
    private float _aoeHealThreshold = 0.70f;
    public float AoEHealThreshold
    {
        get => _aoeHealThreshold;
        set => _aoeHealThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Shield Settings

    /// <summary>
    /// Whether to use Haima (multi-hit single target shield).
    /// </summary>
    public bool EnableHaima { get; set; } = true;

    /// <summary>
    /// Whether to use Panhaima (multi-hit party shield).
    /// </summary>
    public bool EnablePanhaima { get; set; } = true;

    /// <summary>
    /// HP threshold to use Haima on tank.
    /// </summary>
    private float _haimaThreshold = 0.80f;
    public float HaimaThreshold
    {
        get => _haimaThreshold;
        set => _haimaThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Average party HP threshold to use Panhaima.
    /// </summary>
    private float _panhaimaThreshold = 0.85f;
    public float PanhaimaThreshold
    {
        get => _panhaimaThreshold;
        set => _panhaimaThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Whether to avoid overwriting existing shields with Eukrasian heals.
    /// </summary>
    public bool AvoidOverwritingShields { get; set; } = true;

    #endregion

    #region Buff Settings

    /// <summary>
    /// Whether to use Zoe to boost GCD heals.
    /// </summary>
    public bool EnableZoe { get; set; } = true;

    /// <summary>
    /// Strategy for Zoe usage.
    /// </summary>
    public ZoeUsageStrategy ZoeStrategy { get; set; } = ZoeUsageStrategy.WithPneuma;

    /// <summary>
    /// Whether to use Krasis to boost healing received.
    /// </summary>
    public bool EnableKrasis { get; set; } = true;

    /// <summary>
    /// HP threshold to use Krasis on a target.
    /// </summary>
    private float _krasisThreshold = 0.65f;
    public float KrasisThreshold
    {
        get => _krasisThreshold;
        set => _krasisThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Whether to use Philosophia (party-wide Kardia).
    /// </summary>
    public bool EnablePhilosophia { get; set; } = true;

    /// <summary>
    /// Average party HP threshold to trigger Philosophia.
    /// </summary>
    private float _philosophiaThreshold = 0.75f;
    public float PhilosophiaThreshold
    {
        get => _philosophiaThreshold;
        set => _philosophiaThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Damage Settings

    /// <summary>
    /// Whether to use single-target damage spells (Dosis).
    /// </summary>
    public bool EnableSingleTargetDamage { get; set; } = true;

    /// <summary>
    /// Whether to use AoE damage (Dyskrasia).
    /// </summary>
    public bool EnableAoEDamage { get; set; } = true;

    /// <summary>
    /// Whether to maintain DoT on target (Eukrasian Dosis).
    /// </summary>
    public bool EnableDot { get; set; } = true;

    /// <summary>
    /// Seconds remaining on DoT before refreshing.
    /// </summary>
    private float _dotRefreshThreshold = 3f;
    public float DotRefreshThreshold
    {
        get => _dotRefreshThreshold;
        set => _dotRefreshThreshold = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Whether to use Phlegma (instant damage with charges).
    /// </summary>
    public bool EnablePhlegma { get; set; } = true;

    /// <summary>
    /// Whether to use Toxikon when Addersting is available.
    /// </summary>
    public bool EnableToxikon { get; set; } = true;

    /// <summary>
    /// Whether to use Psyche (oGCD damage).
    /// </summary>
    public bool EnablePsyche { get; set; } = true;

    /// <summary>
    /// Minimum enemies for Dyskrasia AoE damage.
    /// </summary>
    private int _aoeDamageMinTargets = 3;
    public int AoEDamageMinTargets
    {
        get => _aoeDamageMinTargets;
        set => _aoeDamageMinTargets = Math.Clamp(value, 1, 10);
    }

    #endregion

    #region Lucid Dreaming Settings

    /// <summary>
    /// Whether to use Lucid Dreaming automatically.
    /// </summary>
    public bool EnableLucidDreaming { get; set; } = true;

    /// <summary>
    /// MP percentage threshold to trigger Lucid Dreaming.
    /// </summary>
    private float _lucidDreamingThreshold = 0.70f;
    public float LucidDreamingThreshold
    {
        get => _lucidDreamingThreshold;
        set => _lucidDreamingThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion
}

/// <summary>
/// Strategy for using Zoe (+50% next GCD heal).
/// </summary>
public enum ZoeUsageStrategy
{
    /// <summary>
    /// Use Zoe with Pneuma for maximum value.
    /// </summary>
    WithPneuma,

    /// <summary>
    /// Use Zoe with E.Prognosis for AoE shield.
    /// </summary>
    WithEukrasianPrognosis,

    /// <summary>
    /// Use Zoe immediately when healing needed.
    /// </summary>
    OnDemand,

    /// <summary>
    /// Manual control only.
    /// </summary>
    Manual
}
