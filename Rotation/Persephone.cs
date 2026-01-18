using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Rotation.PersephoneCore.Helpers;
using Olympus.Rotation.PersephoneCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation;

/// <summary>
/// Summoner rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Persephone, the Greek queen of the underworld who commands souls and summons.
/// </summary>
[Rotation("Persephone", JobRegistry.Summoner, Role = RotationRole.Caster)]
public sealed class Persephone : BaseCasterDpsRotation<IPersephoneContext, IPersephoneModule>
{
    /// <inheritdoc />
    public override string Name => "Persephone";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Summoner, JobRegistry.Arcanist];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IPersephoneModule> Modules => _modules;

    /// <summary>
    /// Gets the Persephone-specific debug state. Used for Summoner-specific debug display.
    /// </summary>
    public PersephoneDebugState PersephoneDebug => _persephoneDebugState;

    // Persistent debug state
    private readonly PersephoneDebugState _persephoneDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly PersephoneStatusHelper _statusHelper;
    private readonly PersephonePartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IPersephoneModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Gauge values (read each frame)
    private int _aetherflowStacks;
    private int _attunement;
    private int _attunementStacks;
    private float _attunementTimer;
    private float _summonTimer;
    private bool _ifritReady;
    private bool _titanReady;
    private bool _garudaReady;

    // Demi-summon tracking (determined by status effects and action usage)
    private bool _isBahamutActive;
    private bool _isPhoenixActive;
    private bool _isSolarBahamutActive;

    // Phase tracking for Enkindle/Astral Flow usage
    private bool _hasUsedEnkindleThisPhase;
    private bool _hasUsedAstralFlowThisPhase;
    private float _lastDemiSummonTimer;

    public Persephone(
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
            errorMetrics)
    {
        _timelineService = timelineService;

        // Initialize helpers
        _statusHelper = new PersephoneStatusHelper();
        _partyHelper = new PersephonePartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IPersephoneModule>
        {
            new BuffModule(),    // Priority 20 - oGCD management (Enkindle, Astral Flow, Aetherflow, Searing Light)
            new DamageModule(),  // Priority 30 - GCD rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _aetherflowStacks = SafeGameAccess.GetSmnAetherflowStacks(ErrorMetrics);
        _attunement = SafeGameAccess.GetSmnAttunement(ErrorMetrics);
        _attunementStacks = SafeGameAccess.GetSmnAttunementStacks(ErrorMetrics);
        _attunementTimer = SafeGameAccess.GetSmnAttunementTimer(ErrorMetrics);
        _summonTimer = SafeGameAccess.GetSmnSummonTimer(ErrorMetrics);
        _ifritReady = SafeGameAccess.GetSmnIfritReady(ErrorMetrics);
        _titanReady = SafeGameAccess.GetSmnTitanReady(ErrorMetrics);
        _garudaReady = SafeGameAccess.GetSmnGarudaReady(ErrorMetrics);

        // Track demi-summon phase changes
        if (_summonTimer > 0 && _lastDemiSummonTimer <= 0)
        {
            // New demi-summon phase started - reset tracking
            _hasUsedEnkindleThisPhase = false;
            _hasUsedAstralFlowThisPhase = false;
        }
        else if (_summonTimer <= 0 && _lastDemiSummonTimer > 0)
        {
            // Demi-summon phase ended
            _isBahamutActive = false;
            _isPhoenixActive = false;
            _isSolarBahamutActive = false;
        }

        _lastDemiSummonTimer = _summonTimer;

        // Determine which demi-summon is active
        // This is determined by checking action readiness and recent action usage
        // The game changes the demi-summon GCDs based on which summon is active
        if (_summonTimer > 0)
        {
            // Check which Enkindle/Astral Flow is available to determine summon type
            // Bahamut: Enkindle Bahamut + Deathflare
            // Phoenix: Enkindle Phoenix + Rekindle
            // Solar Bahamut: Enkindle Solar Bahamut + Sunflare

            // Use action readiness to determine which summon is active
            var bahamutEnkindleReady = ActionService.IsActionReady(SMNActions.EnkindleBahamut.ActionId);
            var phoenixEnkindleReady = ActionService.IsActionReady(SMNActions.EnkindlePhoenix.ActionId);
            var solarBahamutEnkindleReady = ActionService.IsActionReady(SMNActions.EnkindleSolarBahamut.ActionId);

            // Only one should be ready at a time during the appropriate phase
            if (solarBahamutEnkindleReady || ActionService.IsActionReady(SMNActions.Sunflare.ActionId))
            {
                _isSolarBahamutActive = true;
                _isBahamutActive = false;
                _isPhoenixActive = false;
            }
            else if (phoenixEnkindleReady || ActionService.IsActionReady(SMNActions.Rekindle.ActionId))
            {
                _isPhoenixActive = true;
                _isBahamutActive = false;
                _isSolarBahamutActive = false;
            }
            else if (bahamutEnkindleReady || ActionService.IsActionReady(SMNActions.Deathflare.ActionId))
            {
                _isBahamutActive = true;
                _isPhoenixActive = false;
                _isSolarBahamutActive = false;
            }
            else
            {
                // Fallback: if summon timer > 0 but no enkindle ready,
                // assume it's whatever was active before or Bahamut by default
                if (!_isBahamutActive && !_isPhoenixActive && !_isSolarBahamutActive)
                    _isBahamutActive = true;
            }
        }
    }

    /// <summary>
    /// Updates MP forecast. Summoners use Lucid Dreaming for MP management.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        var hasLucid = _statusHelper.HasLucidDreaming(player);
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: hasLucid);
    }

    /// <inheritdoc />
    protected override IPersephoneContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new PersephoneContext(
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
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            debugState: _persephoneDebugState,
            aetherflowStacks: _aetherflowStacks,
            attunement: _attunement,
            attunementStacks: _attunementStacks,
            attunementTimer: _attunementTimer,
            summonTimer: _summonTimer,
            ifritReady: _ifritReady,
            titanReady: _titanReady,
            garudaReady: _garudaReady,
            isBahamutActive: _isBahamutActive,
            isPhoenixActive: _isPhoenixActive,
            isSolarBahamutActive: _isSolarBahamutActive,
            hasUsedEnkindleThisPhase: _hasUsedEnkindleThisPhase,
            hasUsedAstralFlowThisPhase: _hasUsedAstralFlowThisPhase,
            timelineService: _timelineService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IPersephoneContext context)
    {
        // Map Summoner debug state to common debug state fields
        _debugState.PlanningState = _persephoneDebugState.PlanningState;
        _debugState.PlannedAction = _persephoneDebugState.PlannedAction;
        _debugState.DpsState = _persephoneDebugState.DamageState;
        // Note: BuffState is tracked in PersephoneDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
