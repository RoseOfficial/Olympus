using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Services.Party;

namespace Olympus.Services;

/// <summary>
/// Detects raid buff burst windows by scanning the local player's status effects,
/// subscribing to party cast events for low-latency detection, and optionally
/// consulting IPC state from PartyCoordinationService.
///
/// Burst window = any of the coordinated party raid buffs is active on the player.
/// These buffs (Battle Litany, Technical Finish, Brotherhood, etc.) all have ~120s CDs
/// and ~15–20s durations, creating a predictable ~100s cycle.
///
/// Cast-event subscription removes the per-frame status-scan latency: when a party
/// member's raid buff resolves, we mark the burst window immediately rather than
/// waiting for the local status list to populate (typically 50-200ms later for
/// non-self casts due to network propagation + Dalamud refresh). The status scan
/// stays as a fallback to catch any missed events and to detect buff falloff.
/// </summary>
public sealed class BurstWindowService : IBurstWindowService, IDisposable
{
    private readonly IPartyCoordinationService? _partyCoordinationService;
    private readonly ICombatEventService? _combatEventService;
    private readonly IPartyList? _partyList;
    private readonly IObjectTable? _objectTable;
    private readonly Action<uint, uint>? _onAbilityUsedHandler;

    // Raid buff status IDs that appear on the LOCAL PLAYER when burst is active.
    // These are party-wide buffs applied by DPS/support jobs to the entire party.
    private static readonly HashSet<uint> RaidBuffStatusIds = new()
    {
        DRGActions.StatusIds.BattleLitany,      // 786  – +10% crit rate
        BRDActions.StatusIds.BattleVoice,        // 141  – +20% DH rate
        BRDActions.StatusIds.RadiantFinale,      // 2964 – +2-6% damage
        SMNActions.StatusIds.SearingLight,       // 2703 – +5% damage
        RDMActions.StatusIds.EmboldenParty,      // 1297 – +5% damage (party version)
        DNCActions.StatusIds.TechnicalFinish,   // 1822 – +5% damage
        RPRActions.StatusIds.ArcaneCircle,       // 2599 – +3% damage
        MNKActions.StatusIds.Brotherhood,        // 1182 – +5% damage
        PCTActions.StatusIds.StarryMuse,         // 3685 – +5% damage
        ASTActions.DivinationStatusId,           // 1878 – +6% damage
    };

    // Raid debuff status IDs that appear on the ENEMY TARGET during burst.
    // These are vulnerability/crit debuffs applied by support jobs that extend
    // the effective burst window on a specific target.
    private static readonly HashSet<uint> RaidDebuffStatusIds = new()
    {
        SCHActions.ChainStratagemStatusId,       // 1221 – +10% crit rate on target
        NINActions.StatusIds.Dokumori,           // 3849 – +5% damage taken
        NINActions.StatusIds.VulnerabilityUp,    // 638  – +5% damage taken (Trick Attack)
    };

    // Burst cycle approximation for timer-based prediction when IPC is unavailable.
    // Most raid buffs: 120s CD, ~20s duration → gap between windows ≈ 100s.
    private const float BurstCycleGapSeconds = 100f;

    // Cached per-frame state
    private bool _isInBurstWindow;
    private float _secondsRemainingInBurst;

    // Tracks when the most recent burst window ended for timer-based prediction
    private DateTime? _lastBurstWindowEnd;

    // Burst window history tracking
    private readonly List<(DateTime Start, DateTime End)> _burstWindowHistory = new();
    private DateTime _currentWindowStart;
    private bool _wasInBurst;

    public BurstWindowService(
        IPartyCoordinationService? partyCoordinationService = null,
        ICombatEventService? combatEventService = null,
        IPartyList? partyList = null,
        IObjectTable? objectTable = null)
    {
        _partyCoordinationService = partyCoordinationService;
        _combatEventService = combatEventService;
        _partyList = partyList;
        _objectTable = objectTable;

        if (_combatEventService != null)
        {
            _onAbilityUsedHandler = OnAbilityUsed;
            _combatEventService.OnAbilityUsed += _onAbilityUsedHandler;
        }
    }

    public void Dispose()
    {
        if (_combatEventService != null && _onAbilityUsedHandler != null)
            _combatEventService.OnAbilityUsed -= _onAbilityUsedHandler;
    }

    /// <summary>
    /// Cast-event handler. Fires when any nearby actor's action effect resolves.
    /// Filters to coordinated raid buffs cast by the local player or a party member,
    /// then opens the burst window with the buff's known duration.
    /// </summary>
    private void OnAbilityUsed(uint casterEntityId, uint actionId)
    {
        if (!CoordinatedRaidBuffs.IsCoordinatedRaidBuff(actionId))
            return;

        if (!IsCasterInParty(casterEntityId))
            return;

        var duration = CoordinatedRaidBuffs.GetBuffDuration(actionId);

        if (!_isInBurstWindow)
        {
            _isInBurstWindow = true;
            _currentWindowStart = DateTime.UtcNow;
            _wasInBurst = true;
        }
        if (duration > _secondsRemainingInBurst)
            _secondsRemainingInBurst = duration;
    }

    private bool IsCasterInParty(uint casterEntityId)
    {
        // Self-cast always counts: the local player's own raid buff applies to them.
        if (_objectTable?.LocalPlayer?.EntityId == casterEntityId)
            return true;

        // Party member cast: their raid buff applies to the local player.
        if (_partyList == null)
            return false;
        foreach (var member in _partyList)
        {
            if (member?.GameObject?.EntityId == casterEntityId)
                return true;
        }
        return false;
    }

    #region IBurstWindowService

    public bool IsInBurstWindow => _isInBurstWindow;

    public float SecondsRemainingInBurst => _secondsRemainingInBurst;

    public IReadOnlyList<(DateTime Start, DateTime End)> BurstWindowHistory => _burstWindowHistory;

    public bool IsBurstImminent(float thresholdSeconds = 5f)
    {
        if (_isInBurstWindow)
            return false;

        // IPC: another Olympus instance announced incoming raid buff
        if (_partyCoordinationService?.HasPendingRaidBuffIntent(thresholdSeconds) == true)
            return true;

        // IPC: general burst state from party coordination
        var ipcState = _partyCoordinationService?.GetBurstWindowState();
        if (ipcState is { IsImminent: true, SecondsUntilBurst: >= 0 } state &&
            state.SecondsUntilBurst <= thresholdSeconds)
            return true;

        // Timer-based fallback: use the ~100s burst cycle
        var until = TimerBasedSecondsUntilBurst;
        return until >= 0f && until <= thresholdSeconds;
    }

    public float SecondsUntilNextBurst
    {
        get
        {
            if (_isInBurstWindow)
                return 0f;

            // IPC estimate
            if (_partyCoordinationService != null)
            {
                var ipcSeconds = _partyCoordinationService.GetSecondsUntilBurst();
                if (ipcSeconds >= 0f)
                    return ipcSeconds;
            }

            return TimerBasedSecondsUntilBurst;
        }
    }

    public void Update(IPlayerCharacter player, IBattleChara? currentTarget = null)
    {
        // Scan player status list for active party raid buffs
        _isInBurstWindow = false;
        _secondsRemainingInBurst = 0f;

        foreach (var status in player.StatusList)
        {
            if (!RaidBuffStatusIds.Contains(status.StatusId))
                continue;

            _isInBurstWindow = true;
            if (status.RemainingTime > _secondsRemainingInBurst)
                _secondsRemainingInBurst = status.RemainingTime;
        }

        // Scan current target for raid debuffs (Chain Stratagem, Dokumori, VulnerabilityUp)
        if (currentTarget?.StatusList != null)
        {
            foreach (var status in currentTarget.StatusList)
            {
                if (!RaidDebuffStatusIds.Contains(status.StatusId))
                    continue;

                _isInBurstWindow = true;
                if (status.RemainingTime > _secondsRemainingInBurst)
                    _secondsRemainingInBurst = status.RemainingTime;
            }
        }

        // Also check IPC burst state (multi-instance scenario)
        if (!_isInBurstWindow && _partyCoordinationService?.IsInBurstWindow() == true)
        {
            _isInBurstWindow = true;
            _secondsRemainingInBurst = _partyCoordinationService?.GetBurstWindowRemaining() ?? 0f;
        }

        // Record when the burst window ends for timer-based cycle prediction
        if (_wasInBurst && !_isInBurstWindow)
            _lastBurstWindowEnd = DateTime.UtcNow;

        // Track burst window history transitions
        if (_isInBurstWindow && !_wasInBurst)
            _currentWindowStart = DateTime.UtcNow;
        else if (!_isInBurstWindow && _wasInBurst)
            _burstWindowHistory.Add((_currentWindowStart, DateTime.UtcNow));
        _wasInBurst = _isInBurstWindow;
    }

    public void ResetHistory()
    {
        _burstWindowHistory.Clear();
        _wasInBurst = false;
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Estimates seconds until the next burst using the ~100s cycle gap from the last known end.
    /// Returns -1 if no timing data is available.
    /// </summary>
    private float TimerBasedSecondsUntilBurst
    {
        get
        {
            if (!_lastBurstWindowEnd.HasValue)
                return -1f;

            var elapsed = (float)(DateTime.UtcNow - _lastBurstWindowEnd.Value).TotalSeconds;
            var remaining = BurstCycleGapSeconds - elapsed;
            return remaining >= 0f ? remaining : -1f;
        }
    }

    #endregion
}
