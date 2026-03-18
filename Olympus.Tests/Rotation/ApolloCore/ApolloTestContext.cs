using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ApolloCore;

/// <summary>
/// Factory for creating ApolloContext instances with mocked dependencies.
/// </summary>
public static class ApolloTestContext
{
    /// <summary>
    /// Creates an ApolloContext with fully mocked dependencies.
    /// </summary>
    public static ApolloContext Create(
        Configuration? config = null,
        IPlayerCharacter? player = null,
        Mock<IPartyHelper>? partyHelper = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        Mock<IDebuffDetectionService>? debuffDetectionService = null,
        Mock<IHealingSpellSelector>? healingSpellSelector = null,
        DebugState? debugState = null,
        byte level = 90,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        bool inCombat = false,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false)
    {
        config ??= CreateDefaultWhiteMageConfiguration();

        var playerChar = player ?? MockBuilders.CreateMockPlayerCharacter(
            level: level,
            currentHp: currentHp,
            maxHp: maxHp,
            currentMp: currentMp).Object;

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        partyHelper ??= MockBuilders.CreateMockPartyHelper();
        debuffDetectionService ??= MockBuilders.CreateMockDebuffDetectionService();
        targetingService ??= MockBuilders.CreateMockTargetingService();

        var combatEventService = MockBuilders.CreateMockCombatEventService();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var damageTrendService = MockBuilders.CreateMockDamageTrendService();
        var frameCache = MockBuilders.CreateMockFrameScopedCache();
        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();
        var mpForecastService = MockBuilders.CreateMockMpForecastService();
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var cooldownPlanner = MockBuilders.CreateMockCooldownPlanner();

        var actionTracker = MockBuilders.CreateMockActionTracker(config);
        var statusHelper = new StatusHelper();

        healingSpellSelector ??= CreateDefaultHealingSpellSelector();

        return new ApolloContext(
            playerChar,
            inCombat,
            isMoving,
            canExecuteGcd,
            canExecuteOgcd,
            actionService.Object,
            actionTracker,
            combatEventService.Object,
            damageIntakeService.Object,
            damageTrendService.Object,
            frameCache.Object,
            config,
            debuffDetectionService.Object,
            hpPredictionService.Object,
            mpForecastService.Object,
            objectTable.Object,
            partyList.Object,
            playerStatsService.Object,
            targetingService.Object,
            healingSpellSelector.Object,
            cooldownPlanner.Object,
            statusHelper,
            partyHelper.Object,
            coHealerDetectionService: null,
            bossMechanicDetector: null,
            shieldTrackingService: null,
            debugState: debugState);
    }

    /// <summary>
    /// Creates a default mock IHealingSpellSelector that returns no heals.
    /// </summary>
    public static Mock<IHealingSpellSelector> CreateDefaultHealingSpellSelector()
    {
        var mock = new Mock<IHealingSpellSelector>();
        mock.Setup(x => x.SelectBestSingleHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<IBattleChara>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<float>()))
            .Returns(((ActionDefinition?)null, 0));
        mock.Setup(x => x.SelectBestAoEHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<IBattleChara?>()))
            .Returns(((ActionDefinition?)null, 0, (IBattleChara?)null));
        return mock;
    }

    /// <summary>
    /// Creates a default Configuration with all White Mage settings enabled.
    /// </summary>
    public static Configuration CreateDefaultWhiteMageConfiguration()
    {
        var config = new Configuration
        {
            Enabled = true,
            EnableHealing = true,
            EnableDamage = true,
            EnableDoT = true,
        };

        // Healing
        config.Healing.EnableCure = true;
        config.Healing.EnableCureII = true;
        config.Healing.EnableCureIII = true;
        config.Healing.EnableRegen = true;
        config.Healing.EnableMedica = true;
        config.Healing.EnableMedicaII = true;
        config.Healing.EnableAfflatusSolace = true;
        config.Healing.EnableAfflatusRapture = true;
        config.Healing.EnableTetragrammaton = true;
        config.Healing.EnableBenediction = true;
        config.Healing.EnableAssize = true;
        config.Healing.EnableAsylum = true;
        config.Healing.BenedictionEmergencyThreshold = 0.30f;
        config.Healing.OgcdEmergencyThreshold = 0.50f;
        config.Healing.GcdEmergencyThreshold = 0.40f;
        config.Healing.AoEHealMinTargets = 3;

        // Buffs
        config.Buffs.EnableThinAir = true;
        config.Buffs.EnablePresenceOfMind = true;
        config.Buffs.EnableAetherialShift = true;

        // Defensive
        config.Defensive.EnableDivineBenison = true;
        config.Defensive.EnableAquaveil = true;
        config.Defensive.EnableTemperance = true;
        config.Defensive.EnableLiturgyOfTheBell = true;
        config.Defensive.EnablePlenaryIndulgence = true;
        config.Defensive.EnableDivineCaress = true;
        config.Defensive.DefensiveCooldownThreshold = 0.80f;

        // Damage
        config.Damage.EnableStone = true;
        config.Damage.EnableGlareIII = true;
        config.Damage.EnableHoly = true;
        config.Damage.AoEDamageMinTargets = 3;

        // DoT
        config.Dot.EnableDia = true;

        // Resurrection
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.10f;

        // Role actions
        config.RoleActions.EnableEsuna = true;
        config.RoleActions.EsunaPriorityThreshold = 2;
        config.RoleActions.EnableSurecast = true;

        // Buff config (Lucid Dreaming is in Buffs for WHM)
        config.Buffs.EnableLucidDreaming = true;

        return config;
    }
}
