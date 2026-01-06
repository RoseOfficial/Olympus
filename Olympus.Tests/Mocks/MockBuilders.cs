using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

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
        var config = new Configuration
        {
            // Master switches
            EnableHealing = true,
            EnableDamage = true,
            EnableDoT = true,

            // Other settings
            Enabled = true,
        };

        // All healing spells enabled
        config.Healing.EnableCure = true;
        config.Healing.EnableCureII = true;
        config.Healing.EnableCureIII = true;
        config.Healing.EnableRegen = true;
        config.Healing.EnableMedica = true;
        config.Healing.EnableMedicaII = true;
        config.Healing.EnableMedicaIII = true;
        config.Healing.EnableAfflatusSolace = true;
        config.Healing.EnableAfflatusRapture = true;
        config.Damage.EnableAfflatusMisery = true;
        config.Healing.EnableTetragrammaton = true;
        config.Healing.EnableBenediction = true;
        config.Healing.EnableAssize = true;
        config.Healing.EnableAsylum = true;
        config.Defensive.EnableDivineBenison = true;
        config.Defensive.EnablePlenaryIndulgence = true;
        config.Defensive.EnableTemperance = true;
        config.Defensive.EnableAquaveil = true;
        config.Defensive.EnableLiturgyOfTheBell = true;
        config.Defensive.EnableDivineCaress = true;

        // Thresholds
        config.Healing.AoEHealMinTargets = 3;
        config.Healing.BenedictionEmergencyThreshold = 0.30f;
        config.Defensive.DefensiveCooldownThreshold = 0.80f;

        // Esuna settings
        config.RoleActions.EnableEsuna = true;
        config.RoleActions.EsunaPriorityThreshold = 2;

        return config;
    }

    /// <summary>
    /// Creates a Configuration with all healing spells disabled.
    /// Useful for testing "no valid candidates" scenarios.
    /// </summary>
    public static Configuration CreateDisabledConfiguration()
    {
        var config = new Configuration
        {
            // Master switches - keep healing enabled but individual spells disabled
            EnableHealing = true,
            EnableDamage = false,
            EnableDoT = false,

            // Other settings
            Enabled = true,
        };

        // All healing spells disabled
        config.Healing.EnableCure = false;
        config.Healing.EnableCureII = false;
        config.Healing.EnableCureIII = false;
        config.Healing.EnableRegen = false;
        config.Healing.EnableMedica = false;
        config.Healing.EnableMedicaII = false;
        config.Healing.EnableMedicaIII = false;
        config.Healing.EnableAfflatusSolace = false;
        config.Healing.EnableAfflatusRapture = false;
        config.Damage.EnableAfflatusMisery = false;
        config.Healing.EnableTetragrammaton = false;
        config.Healing.EnableBenediction = false;
        config.Healing.EnableAssize = false;
        config.Healing.EnableAsylum = false;
        config.Defensive.EnableDivineBenison = false;
        config.Defensive.EnablePlenaryIndulgence = false;
        config.Defensive.EnableTemperance = false;
        config.Defensive.EnableAquaveil = false;
        config.Defensive.EnableLiturgyOfTheBell = false;
        config.Defensive.EnableDivineCaress = false;

        // Esuna disabled
        config.RoleActions.EnableEsuna = false;

        return config;
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
    /// Creates a mock IDamageIntakeService with configurable behavior.
    /// </summary>
    /// <param name="getRecentDamageIntake">
    /// Function to get recent damage intake. Default: returns 0.
    /// </param>
    /// <param name="getDamageRate">
    /// Function to get damage rate. Default: returns 0.
    /// </param>
    public static Mock<IDamageIntakeService> CreateMockDamageIntakeService(
        Func<uint, float, int>? getRecentDamageIntake = null,
        Func<uint, float, float>? getDamageRate = null)
    {
        var mock = new Mock<IDamageIntakeService>();

        // Default: no recent damage
        getRecentDamageIntake ??= (_, _) => 0;
        getDamageRate ??= (_, _) => 0f;

        mock.Setup(x => x.GetRecentDamageIntake(It.IsAny<uint>(), It.IsAny<float>()))
            .Returns((uint entityId, float windowSeconds) => getRecentDamageIntake(entityId, windowSeconds));

        mock.Setup(x => x.GetDamageRate(It.IsAny<uint>(), It.IsAny<float>()))
            .Returns((uint entityId, float windowSeconds) => getDamageRate(entityId, windowSeconds));

        mock.Setup(x => x.GetPartyDamageIntake(It.IsAny<float>())).Returns(0);
        mock.Setup(x => x.GetPartyDamageRate(It.IsAny<float>())).Returns(0f);

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

    /// <summary>
    /// Creates a mock IDebuffDetectionService with configurable behavior.
    /// </summary>
    /// <param name="findHighestPriorityDebuff">
    /// Function to return debuff info for a target.
    /// Default: returns no debuff (None priority).
    /// </param>
    public static Mock<IDebuffDetectionService> CreateMockDebuffDetectionService(
        Func<IBattleChara, (uint statusId, DebuffPriority priority, float remainingTime)>? findHighestPriorityDebuff = null)
    {
        var mock = new Mock<IDebuffDetectionService>();

        // Default: no debuffs found
        findHighestPriorityDebuff ??= _ => (0, DebuffPriority.None, 0f);

        mock.Setup(x => x.FindHighestPriorityDebuff(It.IsAny<IBattleChara>()))
            .Returns((IBattleChara target) => findHighestPriorityDebuff(target));

        mock.Setup(x => x.IsDispellable(It.IsAny<uint>())).Returns(false);
        mock.Setup(x => x.GetDebuffPriority(It.IsAny<uint>())).Returns(DebuffPriority.None);

        return mock;
    }

    /// <summary>
    /// Creates a mock ITargetingService with configurable behavior.
    /// </summary>
    /// <param name="findEnemy">Function to find enemy target. Default: returns null.</param>
    /// <param name="countEnemiesInRange">Number of enemies in range. Default: 0.</param>
    public static Mock<ITargetingService> CreateMockTargetingService(
        Func<EnemyTargetingStrategy, float, IPlayerCharacter, IBattleNpc?>? findEnemy = null,
        int countEnemiesInRange = 0)
    {
        var mock = new Mock<ITargetingService>();

        // Default: no enemies found
        findEnemy ??= (_, _, _) => null;

        mock.Setup(x => x.FindEnemy(It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((EnemyTargetingStrategy strategy, float maxRange, IPlayerCharacter player) =>
                findEnemy(strategy, maxRange, player));

        mock.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(countEnemiesInRange);

        mock.Setup(x => x.FindBestAoETarget(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(((IBattleNpc?)null, countEnemiesInRange));

        mock.Setup(x => x.FindEnemyNeedingDot(It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        return mock;
    }

    /// <summary>
    /// Creates a mock IPartyHelper with configurable behavior.
    /// </summary>
    /// <param name="partyMembers">List of party members to return. Default: empty list.</param>
    /// <param name="lowestHpMember">The lowest HP party member. Default: null.</param>
    /// <param name="deadMember">Dead party member needing raise. Default: null.</param>
    public static Mock<IPartyHelper> CreateMockPartyHelper(
        List<IBattleChara>? partyMembers = null,
        IBattleChara? lowestHpMember = null,
        IBattleChara? deadMember = null)
    {
        var mock = new Mock<IPartyHelper>();

        partyMembers ??= new List<IBattleChara>();

        mock.Setup(x => x.GetAllPartyMembers(It.IsAny<IPlayerCharacter>(), It.IsAny<bool>()))
            .Returns(partyMembers);

        mock.Setup(x => x.FindLowestHpPartyMember(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(lowestHpMember);

        mock.Setup(x => x.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember);

        mock.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        mock.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>()))
            .Returns(1.0f);

        mock.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((1.0f, 1.0f, 0));

        mock.Setup(x => x.CountPartyMembersNeedingAoEHeal(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((0, false, new List<(uint, string)>(), 0));

        mock.Setup(x => x.FindBestCureIIITarget(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((null, 0, new List<uint>()));

        mock.Setup(x => x.FindRegenTarget(It.IsAny<IPlayerCharacter>(), It.IsAny<float>(), It.IsAny<float>()))
            .Returns((IBattleChara?)null);

        mock.Setup(x => x.NeedsRegen(It.IsAny<IBattleChara>(), It.IsAny<float>(), It.IsAny<float>()))
            .Returns(false);

        return mock;
    }

    /// <summary>
    /// Creates a mock IPlayerCharacter with basic properties set.
    /// </summary>
    /// <param name="level">Player level. Default: 90.</param>
    /// <param name="currentHp">Current HP. Default: 50000.</param>
    /// <param name="maxHp">Max HP. Default: 50000.</param>
    /// <param name="currentMp">Current MP. Default: 10000.</param>
    /// <param name="position">Player position. Default: origin.</param>
    public static Mock<IPlayerCharacter> CreateMockPlayerCharacter(
        byte level = 90,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        Vector3? position = null)
    {
        var mock = new Mock<IPlayerCharacter>();

        mock.Setup(x => x.Level).Returns(level);
        mock.Setup(x => x.CurrentHp).Returns(currentHp);
        mock.Setup(x => x.MaxHp).Returns(maxHp);
        mock.Setup(x => x.CurrentMp).Returns(currentMp);
        mock.Setup(x => x.Position).Returns(position ?? Vector3.Zero);
        mock.Setup(x => x.IsDead).Returns(false);
        mock.Setup(x => x.IsCasting).Returns(false);
        mock.Setup(x => x.EntityId).Returns(1u);
        mock.Setup(x => x.GameObjectId).Returns(1ul);

        return mock;
    }

    /// <summary>
    /// Creates a mock IBattleChara (party member) with basic properties set.
    /// </summary>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="name">Character name.</param>
    /// <param name="currentHp">Current HP.</param>
    /// <param name="maxHp">Max HP.</param>
    /// <param name="isDead">Whether the character is dead.</param>
    /// <param name="position">Character position.</param>
    public static Mock<IBattleChara> CreateMockBattleChara(
        uint entityId = 2u,
        string name = "PartyMember",
        uint currentHp = 40000,
        uint maxHp = 50000,
        bool isDead = false,
        Vector3? position = null)
    {
        var mock = new Mock<IBattleChara>();

        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.CurrentHp).Returns(currentHp);
        mock.Setup(x => x.MaxHp).Returns(maxHp);
        mock.Setup(x => x.IsDead).Returns(isDead);
        mock.Setup(x => x.Position).Returns(position ?? Vector3.Zero);

        // Note: Name property uses SeString which has non-virtual TextValue.
        // For tests that need Name, they must use the real game objects or
        // accept that Name.TextValue will return empty/null.
        // Note: StatusList cannot be easily mocked - tests requiring status checks
        // should create their own mock setup for specific status IDs.

        return mock;
    }

    /// <summary>
    /// Creates a mock IObjectTable.
    /// </summary>
    public static Mock<IObjectTable> CreateMockObjectTable()
    {
        var mock = new Mock<IObjectTable>();
        mock.Setup(x => x.GetEnumerator()).Returns(new List<IGameObject>().GetEnumerator());
        return mock;
    }

    /// <summary>
    /// Creates a mock IPartyList.
    /// </summary>
    public static Mock<IPartyList> CreateMockPartyList(int length = 0)
    {
        var mock = new Mock<IPartyList>();
        mock.Setup(x => x.Length).Returns(length);
        mock.Setup(x => x.GetEnumerator()).Returns(new List<IPartyMember>().GetEnumerator());
        return mock;
    }

    /// <summary>
    /// Creates a mock IDataManager.
    /// </summary>
    public static Mock<IDataManager> CreateMockDataManager()
    {
        var mock = new Mock<IDataManager>();
        return mock;
    }

    /// <summary>
    /// Creates an ActionTracker with mocked dependencies.
    /// </summary>
    public static ActionTracker CreateMockActionTracker(Configuration? config = null)
    {
        config ??= CreateDefaultConfiguration();
        var dataManager = CreateMockDataManager();
        return new ActionTracker(dataManager.Object, config);
    }
}
