using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Helpers;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;

namespace Olympus.Rotation;

/// <summary>
/// Warrior rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Ares, the Greek god of war and battle fury.
/// </summary>
[Rotation("Ares", JobRegistry.Warrior, JobRegistry.Marauder, Role = RotationRole.Tank)]
public sealed class Ares : BaseTankRotation<IAresContext, IAresModule>
{
    /// <inheritdoc />
    public override string Name => "Ares";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Warrior, JobRegistry.Marauder];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IAresModule> Modules => _modules;

    /// <summary>
    /// Gets the Ares-specific debug state. Used for Warrior-specific debug display.
    /// </summary>
    public AresDebugState AresDebug => _aresDebugState;

    // Persistent debug state
    private readonly AresDebugState _aresDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly AresStatusHelper _statusHelper;
    private readonly AresPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IAresModule> _modules;

    public Ares(
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
        IEnmityService enmityService,
        ITankCooldownService tankCooldownService,
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
            enmityService,
            tankCooldownService,
            errorMetrics)
    {
        // Initialize helpers
        _statusHelper = new AresStatusHelper();
        _partyHelper = new AresPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IAresModule>
        {
            new EnmityModule(),     // Priority 5 - Enmity management is critical
            new MitigationModule(), // Priority 10 - Stay alive
            new BuffModule(),       // Priority 20 - Buff management
            new DamageModule(),     // Priority 30 - DPS rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override int ReadGaugeValue()
    {
        return SafeGameAccess.GetWarBeastGauge(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // No combo active
        if (comboAction == 0 || comboTimer <= 0)
            return 0;

        // Check for single-target combo: Heavy Swing -> Maim -> Storm's Path/Eye
        if (comboAction == WARActions.HeavySwing.ActionId)
            return 1; // Ready for Maim

        if (comboAction == WARActions.Maim.ActionId)
            return 2; // Ready for Storm's Path/Eye

        // Check for AoE combo: Overpower -> Mythril Tempest
        if (comboAction == WARActions.Overpower.ActionId)
            return 1; // Ready for Mythril Tempest

        // Unknown combo action, restart
        return 0;
    }

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Warriors don't use MP for any abilities
        // No need to track MP
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IAresContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new AresContext(
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
            enmityService: EnmityService,
            tankCooldownService: TankCooldownService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            debugState: _aresDebugState,
            beastGauge: GaugeValue,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IAresContext context)
    {
        // Map tank debug state to common debug state fields
        _debugState.PlanningState = _aresDebugState.DamageState;
        _debugState.PlannedAction = _aresDebugState.PlannedAction;
        _debugState.DpsState = _aresDebugState.DamageState;
        _debugState.DefensiveState = _aresDebugState.MitigationState;

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
