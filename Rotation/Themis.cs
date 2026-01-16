using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Helpers;
using Olympus.Rotation.ThemisCore.Modules;
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
/// Paladin rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Themis, the Greek goddess of divine law and order.
/// </summary>
[Rotation("Themis", JobRegistry.Paladin, JobRegistry.Gladiator, Role = RotationRole.Tank)]
public sealed class Themis : BaseTankRotation<IThemisContext, IThemisModule>
{
    /// <inheritdoc />
    public override string Name => "Themis";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Paladin, JobRegistry.Gladiator];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IThemisModule> Modules => _modules;

    /// <summary>
    /// Gets the Themis-specific debug state. Used for Paladin-specific debug display.
    /// </summary>
    public ThemisDebugState ThemisDebug => _themisDebugState;

    // Persistent debug state
    private readonly ThemisDebugState _themisDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly ThemisStatusHelper _statusHelper;
    private readonly ThemisPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IThemisModule> _modules;

    public Themis(
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
        _statusHelper = new ThemisStatusHelper();
        _partyHelper = new ThemisPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IThemisModule>
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
        return SafeGameAccess.GetPldOathGauge(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // No combo active
        if (comboAction == 0 || comboTimer <= 0)
            return 1;

        // Check for single-target combo
        if (comboAction == PLDActions.FastBlade.ActionId)
            return 2; // Ready for Riot Blade

        if (comboAction == PLDActions.RiotBlade.ActionId)
            return 3; // Ready for Royal Authority / Rage of Halone

        // Check for AoE combo
        if (comboAction == PLDActions.TotalEclipse.ActionId)
            return 2; // Ready for Prominence

        // Unknown combo action, restart
        return 1;
    }

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Tanks don't use Lucid Dreaming, so MP forecast is minimal
        // Paladin uses MP for magic phase, but doesn't have regeneration buffs
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IThemisContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new ThemisContext(
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
            debugState: _themisDebugState,
            oathGauge: GaugeValue,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IThemisContext context)
    {
        // Map execution flow debug info (critical for troubleshooting)
        // Format: "GCD:Ready Rem:0.00s Combat:True CanGCD:True Target:Striking Dummy"
        _debugState.PlanningState = $"GCD:{_themisDebugState.GcdState} Rem:{_themisDebugState.GcdRemaining:F2}s Combat:{_themisDebugState.InCombat} CanGCD:{_themisDebugState.CanExecuteGcd} Tgt:{_themisDebugState.CurrentTarget}";

        // Map tank debug state to common debug state fields
        _debugState.PlannedAction = _themisDebugState.PlannedAction;
        _debugState.DpsState = _themisDebugState.DamageState;
        _debugState.DefensiveState = _themisDebugState.MitigationState;

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
