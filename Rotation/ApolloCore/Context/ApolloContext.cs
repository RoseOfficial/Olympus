using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Cache;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.ApolloCore.Context;

/// <summary>
/// Context for Apollo (White Mage / Conjurer) rotation modules.
/// Extends BaseHealerContext with WHM-specific services and cached state.
/// </summary>
public sealed class ApolloContext : BaseHealerContext, IApolloContext
{
    // WHM-specific helpers
    public StatusHelper StatusHelper { get; }
    public IPartyHelper PartyHelper { get; }

    // Debug state (WHM-specific fields — see Rotation/Common/DebugState.cs)
    public DebugState Debug { get; }

    #region WHM Cached Status Checks

    private bool? _hasThinAir;
    private bool? _hasFreecure;
    private int? _lilyCount;
    private int? _bloodLilyCount;
    private int? _sacredSightStacks;

    public bool HasThinAir => _hasThinAir ??= StatusHelper.HasThinAir(Player);
    public bool HasFreecure => _hasFreecure ??= StatusHelper.HasFreecure(Player);
    public int LilyCount => _lilyCount ??= StatusHelper.GetLilyCount();
    public int BloodLilyCount => _bloodLilyCount ??= StatusHelper.GetBloodLilyCount();
    public int SacredSightStacks => _sacredSightStacks ??= StatusHelper.GetSacredSightStacks(Player);

    #endregion

    protected override bool CheckHasSwiftcast() => BaseStatusHelper.HasSwiftcast(Player);
    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics()
        => PartyHelper.CalculatePartyHealthMetrics(Player);
    protected override string GetJobName() => "Apollo";

    public ApolloContext(
        IPlayerCharacter player,
        bool inCombat,
        bool isMoving,
        bool canExecuteGcd,
        bool canExecuteOgcd,
        IActionService actionService,
        ActionTracker actionTracker,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        IFrameScopedCache frameCache,
        Configuration configuration,
        IDebuffDetectionService debuffDetectionService,
        IHpPredictionService hpPredictionService,
        IMpForecastService mpForecastService,
        IObjectTable objectTable,
        IPartyList partyList,
        IPlayerStatsService playerStatsService,
        ITargetingService targetingService,
        IHealingSpellSelector healingSpellSelector,
        ICooldownPlanner cooldownPlanner,
        StatusHelper statusHelper,
        IPartyHelper partyHelper,
        ICoHealerDetectionService? coHealerDetectionService = null,
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITimelineService? timelineService = null,
        ITrainingService? trainingService = null,
        DebugState? debugState = null,
        IPluginLog? log = null)
        : base(player, inCombat, isMoving, canExecuteGcd, canExecuteOgcd,
               actionService, actionTracker, combatEventService, damageIntakeService, damageTrendService,
               frameCache, configuration, debuffDetectionService, hpPredictionService, mpForecastService,
               objectTable, partyList, playerStatsService, targetingService,
               healingSpellSelector, cooldownPlanner,
               coHealerDetectionService, bossMechanicDetector, shieldTrackingService,
               partyAnalyzer: null,
               partyCoordinationService, timelineService, trainingService, log)
    {
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState ?? new DebugState();
    }
}

