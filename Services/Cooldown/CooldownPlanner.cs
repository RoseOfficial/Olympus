using System;
using Olympus.Services.Prediction;

namespace Olympus.Services.Cooldown;

/// <summary>
/// Implements cooldown planning decisions based on party state,
/// damage trends, and configured thresholds. Centralizes defensive
/// cooldown logic for consistent decision-making across modules.
/// </summary>
public sealed class CooldownPlanner : ICooldownPlanner
{
    private readonly IDamageIntakeService _damageIntakeService;
    private readonly IDamageTrendService _damageTrendService;
    private readonly Configuration _configuration;

    // Cached state (updated each frame via Update())
    private float _avgPartyHpPercent = 1f;
    private float _lowestHpPercent = 1f;
    private int _criticalCount;
    private int _injuredCount;
    private float _partyDamageRate;
    private DamageTrend _damageTrend = DamageTrend.Stable;
    private bool _spikeImminent;

    // Constants for thresholds
    private const float CriticalHpThreshold = 0.30f;
    private const float EmergencyPartyThreshold = 0.40f;
    private const float HighUrgencyThreshold = 0.50f;
    private const float MediumUrgencyThreshold = 0.70f;

    public CooldownPlanner(
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        Configuration configuration)
    {
        _damageIntakeService = damageIntakeService;
        _damageTrendService = damageTrendService;
        _configuration = configuration;
    }

    /// <summary>
    /// Updates the planner state with current party metrics.
    /// Should be called once per frame before making cooldown decisions.
    /// </summary>
    /// <param name="avgPartyHpPercent">Average party HP percentage.</param>
    /// <param name="lowestHpPercent">Lowest party member HP percentage.</param>
    /// <param name="injuredCount">Number of injured party members.</param>
    /// <param name="criticalCount">Number of critically low party members.</param>
    public void Update(float avgPartyHpPercent, float lowestHpPercent, int injuredCount, int criticalCount)
    {
        _avgPartyHpPercent = avgPartyHpPercent;
        _lowestHpPercent = lowestHpPercent;
        _injuredCount = injuredCount;
        _criticalCount = criticalCount;
        _partyDamageRate = _damageIntakeService.GetPartyDamageRate(5f);
        _damageTrend = _damageTrendService.GetPartyDamageTrend(5f);
        _spikeImminent = _damageTrendService.IsDamageSpikeImminent(
            _configuration.Healing.SpikePatternConfidenceThreshold);
    }

    /// <inheritdoc />
    public bool ShouldUseMajorDefensive()
    {
        var config = _configuration.Defensive;

        // Emergency: multiple critical or very low average HP
        if (_criticalCount >= 2 || _avgPartyHpPercent < EmergencyPartyThreshold)
            return true;

        // High damage spike detected or imminent
        if (_spikeImminent)
            return true;

        // Party taking heavy damage and HP dropping
        if (_partyDamageRate >= config.DamageSpikeTriggerRate &&
            _damageTrend >= DamageTrend.Increasing)
            return true;

        // Standard threshold check with dynamic adjustment
        var effectiveThreshold = config.UseDynamicDefensiveThresholds &&
                                 _partyDamageRate >= config.DamageSpikeTriggerRate * 0.5f
            ? config.DefensiveCooldownThreshold + 0.10f
            : config.DefensiveCooldownThreshold;

        return _avgPartyHpPercent < effectiveThreshold && _injuredCount >= 3;
    }

    /// <inheritdoc />
    public bool ShouldUseMinorDefensive()
    {
        // Minor defensives (Divine Benison, Aquaveil) on tank
        // Return general recommendation - actual target selection is done by the module
        return _damageTrend >= DamageTrend.Increasing ||
               _partyDamageRate >= _configuration.Defensive.ProactiveBenisonDamageRate;
    }

    /// <inheritdoc />
    public bool ShouldConserveResources()
    {
        // Conserve when spike is expected but hasn't hit yet
        // Also conserve when party HP is still healthy but damage is building
        return _spikeImminent && _avgPartyHpPercent > 0.70f;
    }

    /// <inheritdoc />
    public bool IsInEmergencyMode()
    {
        // Emergency when multiple party members critically low
        // or when lowest party member is near death
        return _criticalCount >= 2 ||
               _lowestHpPercent < CriticalHpThreshold ||
               (_avgPartyHpPercent < EmergencyPartyThreshold && _damageTrend == DamageTrend.Spiking);
    }

    /// <inheritdoc />
    public CooldownPriority GetCooldownPriority(string cooldownType)
    {
        return cooldownType.ToLowerInvariant() switch
        {
            "temperance" => GetTemperancePriority(),
            "divinebenison" => GetDivineBenisonPriority(),
            "aquaveil" => GetAquaveilPriority(),
            "plenaryindulgence" => GetPlenaryIndulgencePriority(),
            "liturgyofthebell" => GetLiturgyPriority(),
            "assize" => GetAssizePriority(),
            "asylum" => GetAsylumPriority(),
            _ => CooldownPriority.Medium
        };
    }

    /// <inheritdoc />
    public bool IsDamageSpikeExpected()
    {
        return _spikeImminent || _damageTrend == DamageTrend.Spiking;
    }

    /// <inheritdoc />
    public float GetHealingUrgency()
    {
        // Calculate urgency from 0.0 (no urgency) to 1.0 (critical)
        var hpUrgency = 1f - _avgPartyHpPercent;
        var damageUrgency = Math.Min(_partyDamageRate / 3000f, 1f);
        var trendMultiplier = _damageTrend switch
        {
            DamageTrend.Spiking => 1.5f,
            DamageTrend.Increasing => 1.2f,
            DamageTrend.Stable => 1.0f,
            _ => 0.8f
        };

        var baseUrgency = Math.Max(hpUrgency, damageUrgency * 0.7f);
        return Math.Min(baseUrgency * trendMultiplier, 1f);
    }

    // === Priority calculation helpers ===

    private CooldownPriority GetTemperancePriority()
    {
        if (IsInEmergencyMode()) return CooldownPriority.Emergency;
        if (_spikeImminent && _avgPartyHpPercent < 0.80f) return CooldownPriority.High;
        if (_injuredCount >= 3 && _damageTrend >= DamageTrend.Increasing) return CooldownPriority.High;
        if (_avgPartyHpPercent < _configuration.Defensive.DefensiveCooldownThreshold) return CooldownPriority.Medium;
        return CooldownPriority.Low;
    }

    private CooldownPriority GetDivineBenisonPriority()
    {
        if (_partyDamageRate >= _configuration.Defensive.ProactiveBenisonDamageRate) return CooldownPriority.High;
        if (_spikeImminent) return CooldownPriority.High;
        return CooldownPriority.Medium;
    }

    private CooldownPriority GetAquaveilPriority()
    {
        if (_partyDamageRate >= _configuration.Defensive.ProactiveAquaveilDamageRate) return CooldownPriority.High;
        if (_spikeImminent) return CooldownPriority.High;
        return CooldownPriority.Medium;
    }

    private CooldownPriority GetPlenaryIndulgencePriority()
    {
        if (_injuredCount >= 4) return CooldownPriority.High;
        if (_injuredCount >= 3 && _damageTrend >= DamageTrend.Increasing) return CooldownPriority.Medium;
        return CooldownPriority.Low;
    }

    private CooldownPriority GetLiturgyPriority()
    {
        if (_spikeImminent && _avgPartyHpPercent < 0.70f) return CooldownPriority.High;
        if (_injuredCount >= 2 && _damageTrend == DamageTrend.Spiking) return CooldownPriority.High;
        return CooldownPriority.Medium;
    }

    private CooldownPriority GetAssizePriority()
    {
        // Assize is both healing and damage - prioritize based on healing need
        if (_injuredCount >= _configuration.Healing.AssizeHealingMinTargets &&
            _avgPartyHpPercent < _configuration.Healing.AssizeHealingHpThreshold)
            return CooldownPriority.High;
        return CooldownPriority.Medium; // Use on cooldown for damage
    }

    private CooldownPriority GetAsylumPriority()
    {
        if (_injuredCount >= 3 && _damageTrend >= DamageTrend.Stable) return CooldownPriority.High;
        if (_avgPartyHpPercent < 0.80f) return CooldownPriority.Medium;
        return CooldownPriority.Low;
    }
}
