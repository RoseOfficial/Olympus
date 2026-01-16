using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Helpers;
using Olympus.Rotation.HephaestusCore.Modules;
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
/// Gunbreaker rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Hephaestus, the Greek god of forge and weapons.
/// </summary>
[Rotation("Hephaestus", JobRegistry.Gunbreaker, Role = RotationRole.Tank)]
public sealed class Hephaestus : BaseTankRotation<IHephaestusContext, IHephaestusModule>
{
    /// <inheritdoc />
    public override string Name => "Hephaestus";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Gunbreaker];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IHephaestusModule> Modules => _modules;

    /// <summary>
    /// Gets the Hephaestus-specific debug state. Used for Gunbreaker-specific debug display.
    /// </summary>
    public HephaestusDebugState HephaestusDebug => _hephaestusDebugState;

    // Persistent debug state
    private readonly HephaestusDebugState _hephaestusDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly HephaestusStatusHelper _statusHelper;
    private readonly HephaestusPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IHephaestusModule> _modules;

    // Gnashing Fang combo step tracking
    private int _gnashingFangStep;

    public Hephaestus(
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
        _statusHelper = new HephaestusStatusHelper();
        _partyHelper = new HephaestusPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IHephaestusModule>
        {
            new EnmityModule(),     // Priority 5 - Enmity management is critical
            new MitigationModule(), // Priority 10 - Stay alive (Heart of Corundum intelligence)
            new BuffModule(),       // Priority 20 - Buff management (No Mercy, Bloodfest)
            new DamageModule(),     // Priority 30 - DPS rotation with Gnashing Fang combo
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override int ReadGaugeValue()
    {
        // Read Cartridge Gauge (0-3)
        return SafeGameAccess.GetGnbCartridges(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // No combo active
        if (comboAction == 0 || comboTimer <= 0)
            return 0;

        // Check for single-target combo: Keen Edge -> Brutal Shell -> Solid Barrel
        if (comboAction == GNBActions.KeenEdge.ActionId)
            return 1; // Ready for Brutal Shell

        if (comboAction == GNBActions.BrutalShell.ActionId)
            return 2; // Ready for Solid Barrel

        // Check for AoE combo: Demon Slice -> Demon Slaughter
        if (comboAction == GNBActions.DemonSlice.ActionId)
            return 1; // Ready for Demon Slaughter

        // Unknown combo action, restart
        return 0;
    }

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Gunbreakers don't use MP for their core rotation
        // Just track for completeness
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false); // Tanks don't have Lucid Dreaming
    }

    /// <inheritdoc />
    protected override IHephaestusContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        // Update Gnashing Fang step tracking based on Ready buffs
        UpdateGnashingFangStep(player);

        return new HephaestusContext(
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
            debugState: _hephaestusDebugState,
            cartridges: GaugeValue,
            gnashingFangStep: _gnashingFangStep,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            log: Log);
    }

    /// <summary>
    /// Updates the Gnashing Fang combo step based on Ready buffs.
    /// </summary>
    private void UpdateGnashingFangStep(IPlayerCharacter player)
    {
        // Check Ready buffs to determine where we are in the combo
        // After Gnashing Fang: ReadyToRip (step 1)
        // After Savage Claw: ReadyToTear (step 2)
        // After Wicked Talon: ReadyToGouge (step 3, combo complete)
        // After using Eye Gouge: combo is done (step 0)

        if (_statusHelper.HasReadyToRip(player))
        {
            _gnashingFangStep = 1; // After Gnashing Fang
        }
        else if (_statusHelper.HasReadyToTear(player))
        {
            _gnashingFangStep = 2; // After Savage Claw
        }
        else if (_statusHelper.HasReadyToGouge(player))
        {
            _gnashingFangStep = 3; // After Wicked Talon (combo complete, waiting for Eye Gouge)
        }
        else
        {
            _gnashingFangStep = 0; // Not in combo
        }
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IHephaestusContext context)
    {
        // Map tank debug state to common debug state fields
        _debugState.PlanningState = _hephaestusDebugState.DamageState;
        _debugState.PlannedAction = _hephaestusDebugState.PlannedAction;
        _debugState.DpsState = _hephaestusDebugState.DamageState;
        _debugState.DefensiveState = _hephaestusDebugState.MitigationState;

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
