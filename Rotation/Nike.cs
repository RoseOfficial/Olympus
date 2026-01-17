using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Helpers;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Positional;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation;

/// <summary>
/// Samurai rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Nike, the Greek goddess of victory.
/// </summary>
[Rotation("Nike", JobRegistry.Samurai, Role = RotationRole.MeleeDps)]
public sealed class Nike : BaseMeleeDpsRotation<INikeContext, INikeModule>, IDisposable
{
    /// <inheritdoc />
    public override string Name => "Nike";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Samurai];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<INikeModule> Modules => _modules;

    /// <summary>
    /// Gets the Nike-specific debug state. Used for Samurai-specific debug display.
    /// </summary>
    public NikeDebugState NikeDebug => _nikeDebugState;

    // Persistent debug state
    private readonly NikeDebugState _nikeDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly NikeStatusHelper _statusHelper;
    private readonly NikePartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<INikeModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Gauge values (read each frame)
    private int _kenki;
    private SAMActions.SenType _sen;
    private int _meditation;

    // Track last Iaijutsu for Kaeshi
    private SAMActions.IaijutsuType _lastIaijutsu = SAMActions.IaijutsuType.None;

    public Nike(
        IPluginLog log,
        ActionTracker actionTracker,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        Configuration configuration,
        IObjectTable objectTable,
        IPartyList partyList,
        ITargetingService targetingService,
        IHpPredictionService hpPredictionService,
        ActionService actionService,
        IPlayerStatsService playerStatsService,
        IDebuffDetectionService debuffDetectionService,
        IPositionalService positionalService,
        ITimelineService? timelineService = null,
        IErrorMetricsService? errorMetrics = null)
        : base(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            positionalService,
            errorMetrics)
    {
        _timelineService = timelineService;

        // Initialize helpers
        _statusHelper = new NikeStatusHelper();
        _partyHelper = new NikePartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<INikeModule>
        {
            new BuffModule(),    // Priority 20 - Buff management
            new DamageModule(),  // Priority 30 - DPS rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        // Subscribe to combat events to track Iaijutsu for Kaeshi
        combatEventService.OnAbilityUsed += OnAbilityUsed;
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _kenki = SafeGameAccess.GetSamKenki(ErrorMetrics);
        _sen = (SAMActions.SenType)SafeGameAccess.GetSamSen(ErrorMetrics);
        _meditation = SafeGameAccess.GetSamMeditation(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // Samurai combos:
        // ST: Hakaze/Gyofu -> Jinpu/Shifu -> Gekko/Kasha
        //     Hakaze/Gyofu -> Yukikaze
        // AoE: Fuko/Fuga -> Mangetsu/Oka
        if (comboTimer <= 0)
            return 0;

        return comboAction switch
        {
            // Single target combo starters
            7477 => 1, // Hakaze
            36963 => 1, // Gyofu

            // Single target combo step 2
            7478 => 2, // Jinpu
            7479 => 2, // Shifu

            // AoE combo starters
            7483 => 1, // Fuga
            25780 => 1, // Fuko

            _ => 0
        };
    }

    /// <summary>
    /// Updates MP forecast. Samurai don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Samurai don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override INikeContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new NikeContext(
            player: player,
            inCombat: inCombat,
            isMoving: isMoving,
            canExecuteGcd: ActionService.CanExecuteGcd,
            canExecuteOgcd: ActionService.CanExecuteOgcd,
            actionService: ActionService,
            actionTracker: ActionTracker,
            combatEventService: CombatEventService,
            damageIntakeService: DamageIntakeService,
            damageTrendService: DamageTrendService,
            frameCache: FrameCache,
            configuration: Configuration,
            debuffDetectionService: DebuffDetectionService,
            hpPredictionService: HpPredictionService,
            mpForecastService: MpForecastService,
            playerStatsService: PlayerStatsService,
            targetingService: TargetingService,
            objectTable: ObjectTable,
            partyList: PartyList,
            positionalService: PositionalService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            debugState: _nikeDebugState,
            kenki: _kenki,
            sen: _sen,
            meditation: _meditation,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            isAtRear: IsAtRear,
            isAtFlank: IsAtFlank,
            targetHasPositionalImmunity: TargetHasPositionalImmunity,
            lastIaijutsu: _lastIaijutsu,
            timelineService: _timelineService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(INikeContext context)
    {
        // Map Samurai debug state to common debug state fields
        _debugState.PlanningState = _nikeDebugState.PlanningState;
        _debugState.PlannedAction = _nikeDebugState.PlannedAction;
        _debugState.DpsState = _nikeDebugState.DamageState;

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion

    #region Iaijutsu Tracking

    private void OnAbilityUsed(uint sourceId, uint actionId)
    {
        // Only track our own actions
        var localPlayer = ObjectTable.LocalPlayer;
        if (localPlayer == null || sourceId != localPlayer.EntityId)
            return;

        // Track Iaijutsu for Kaeshi selection
        _lastIaijutsu = actionId switch
        {
            7489 => SAMActions.IaijutsuType.Higanbana,      // Higanbana
            7488 => SAMActions.IaijutsuType.TenkaGoken,     // Tenka Goken
            7487 => SAMActions.IaijutsuType.MidareSetsugekka, // Midare Setsugekka
            // Kaeshi actions reset the last Iaijutsu
            16484 or 16485 or 16486 => SAMActions.IaijutsuType.None,
            _ => _lastIaijutsu
        };
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Cleanup when the rotation is disposed.
    /// </summary>
    public void Dispose()
    {
        CombatEventService.OnAbilityUsed -= OnAbilityUsed;
    }

    #endregion
}
