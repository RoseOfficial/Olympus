using System;
using System.Collections.Generic;
using Moq;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;

namespace Olympus.Tests.Mocks;

/// <summary>
/// Factory methods for creating common mock objects used across tests.
/// </summary>
public static class MockBuilders
{
    /// <summary>
    /// Creates a mock ICombatEventService with configurable shadow HP behavior.
    /// </summary>
    /// <param name="getShadowHp">
    /// Optional function to control GetShadowHp behavior.
    /// Parameters: entityId, fallbackHp. Returns: shadow HP value.
    /// If null, returns fallbackHp (pass-through behavior).
    /// </param>
    public static Mock<ICombatEventService> CreateMockCombatEventService(
        Func<uint, uint, uint>? getShadowHp = null)
    {
        var mock = new Mock<ICombatEventService>();

        // Default behavior: return fallbackHp (pass-through)
        getShadowHp ??= (entityId, fallbackHp) => fallbackHp;

        mock.Setup(x => x.GetShadowHp(It.IsAny<uint>(), It.IsAny<uint>()))
            .Returns((uint entityId, uint fallbackHp) => getShadowHp(entityId, fallbackHp));

        return mock;
    }

    /// <summary>
    /// Creates a Configuration with default values suitable for testing.
    /// All healing spells are enabled by default.
    /// </summary>
    public static Configuration CreateDefaultConfiguration()
    {
        return new Configuration
        {
            // Master switches
            EnableHealing = true,
            EnableDamage = true,
            EnableDoT = true,

            // All healing spells enabled
            EnableCure = true,
            EnableCureII = true,
            EnableCureIII = true,
            EnableRegen = true,
            EnableMedica = true,
            EnableMedicaII = true,
            EnableMedicaIII = true,
            EnableAfflatusSolace = true,
            EnableAfflatusRapture = true,
            EnableAfflatusMisery = true,
            EnableTetragrammaton = true,
            EnableBenediction = true,
            EnableAssize = true,
            EnableAsylum = true,
            EnableDivineBenison = true,
            EnablePlenaryIndulgence = true,
            EnableTemperance = true,
            EnableAquaveil = true,
            EnableLiturgyOfTheBell = true,
            EnableDivineCaress = true,

            // Thresholds
            AoEHealMinTargets = 3,
            BenedictionEmergencyThreshold = 0.30f,
            DefensiveCooldownThreshold = 0.80f,

            // Esuna settings
            EnableEsuna = true,
            EsunaPriorityThreshold = 2,

            // Other settings
            Enabled = true,
        };
    }

    /// <summary>
    /// Creates a Configuration with all healing spells disabled.
    /// Useful for testing "no valid candidates" scenarios.
    /// </summary>
    public static Configuration CreateDisabledConfiguration()
    {
        return new Configuration
        {
            // Master switches - keep healing enabled but individual spells disabled
            EnableHealing = true,
            EnableDamage = false,
            EnableDoT = false,

            // All healing spells disabled
            EnableCure = false,
            EnableCureII = false,
            EnableCureIII = false,
            EnableRegen = false,
            EnableMedica = false,
            EnableMedicaII = false,
            EnableMedicaIII = false,
            EnableAfflatusSolace = false,
            EnableAfflatusRapture = false,
            EnableAfflatusMisery = false,
            EnableTetragrammaton = false,
            EnableBenediction = false,
            EnableAssize = false,
            EnableAsylum = false,
            EnableDivineBenison = false,
            EnablePlenaryIndulgence = false,
            EnableTemperance = false,
            EnableAquaveil = false,
            EnableLiturgyOfTheBell = false,
            EnableDivineCaress = false,

            // Esuna disabled
            EnableEsuna = false,

            // Other settings
            Enabled = true,
        };
    }

    /// <summary>
    /// Creates a mock IActionService with configurable behavior.
    /// </summary>
    /// <param name="isActionReady">Function to determine if action is ready. Default: always true.</param>
    /// <param name="canExecuteGcd">Whether GCD can be executed. Default: true.</param>
    /// <param name="canExecuteOgcd">Whether oGCD can be executed. Default: true.</param>
    public static Mock<IActionService> CreateMockActionService(
        Func<uint, bool>? isActionReady = null,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = true)
    {
        var mock = new Mock<IActionService>();

        // Default: all actions ready
        isActionReady ??= _ => true;

        mock.Setup(x => x.IsActionReady(It.IsAny<uint>()))
            .Returns((uint actionId) => isActionReady(actionId));

        mock.Setup(x => x.CanExecuteGcd).Returns(canExecuteGcd);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.CurrentGcdState).Returns(canExecuteGcd ? GcdState.Ready : GcdState.Rolling);
        mock.Setup(x => x.GcdRemaining).Returns(canExecuteGcd ? 0f : 1.5f);
        mock.Setup(x => x.AnimationLockRemaining).Returns(0f);
        mock.Setup(x => x.IsCasting).Returns(false);
        mock.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(0f);
        mock.Setup(x => x.GetAvailableWeaveSlots()).Returns(canExecuteOgcd ? 2 : 0);

        return mock;
    }

    /// <summary>
    /// Creates a mock IPlayerStatsService with configurable stats.
    /// </summary>
    /// <param name="mind">Mind stat value. Default: 3000.</param>
    /// <param name="determination">Determination stat value. Default: 2000.</param>
    /// <param name="weaponDamage">Weapon damage value. Default: 126.</param>
    public static Mock<IPlayerStatsService> CreateMockPlayerStatsService(
        int mind = 3000,
        int determination = 2000,
        int weaponDamage = 126)
    {
        var mock = new Mock<IPlayerStatsService>();

        mock.Setup(x => x.GetMind()).Returns(mind);
        mock.Setup(x => x.GetDetermination()).Returns(determination);
        mock.Setup(x => x.GetWeaponDamage(It.IsAny<int>())).Returns(weaponDamage);
        mock.Setup(x => x.GetHealingStats(It.IsAny<int>()))
            .Returns((mind, determination, weaponDamage));

        return mock;
    }

    /// <summary>
    /// Creates a mock IHpPredictionService with configurable behavior.
    /// </summary>
    /// <param name="getPredictedHp">
    /// Function to calculate predicted HP.
    /// Parameters: entityId, currentHp, maxHp. Returns: predicted HP.
    /// Default: returns currentHp (no pending heals).
    /// </param>
    public static Mock<IHpPredictionService> CreateMockHpPredictionService(
        Func<uint, uint, uint, uint>? getPredictedHp = null)
    {
        var mock = new Mock<IHpPredictionService>();

        // Default: return currentHp (no pending heals)
        getPredictedHp ??= (entityId, currentHp, maxHp) => currentHp;

        mock.Setup(x => x.GetPredictedHp(It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>()))
            .Returns((uint entityId, uint currentHp, uint maxHp) => getPredictedHp(entityId, currentHp, maxHp));

        mock.Setup(x => x.GetPredictedHpPercent(It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>()))
            .Returns((uint entityId, uint currentHp, uint maxHp) =>
            {
                if (maxHp == 0) return 0f;
                return (float)getPredictedHp(entityId, currentHp, maxHp) / maxHp;
            });

        mock.Setup(x => x.HasPendingHeals).Returns(false);
        mock.Setup(x => x.GetPendingHealAmount(It.IsAny<uint>())).Returns(0);
        mock.Setup(x => x.GetAllPendingHeals()).Returns(new Dictionary<uint, int>());

        return mock;
    }
}
