using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Olympus.Data;
using Olympus.Services.Party;

namespace Olympus.Services;

/// <summary>
/// Detects raid buff burst windows by scanning the local player's status effects
/// and optionally consulting IPC state from PartyCoordinationService.
///
/// Burst window = any of the coordinated party raid buffs is active on the player.
/// These buffs (Battle Litany, Technical Finish, Brotherhood, etc.) all have ~120s CDs
/// and ~15–20s durations, creating a predictable ~100s cycle.
/// </summary>
public sealed class BurstWindowService : IBurstWindowService
{
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Raid buff status IDs that appear on the LOCAL PLAYER when burst is active.
    // These are party-wide buffs applied by DPS jobs to the entire party.
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

    public BurstWindowService(IPartyCoordinationService? partyCoordinationService = null)
    {
        _partyCoordinationService = partyCoordinationService;
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

    public void Update(IPlayerCharacter player)
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

        // Also check IPC burst state (multi-instance scenario)
        if (!_isInBurstWindow && _partyCoordinationService?.IsInBurstWindow() == true)
        {
            _isInBurstWindow = true;
            _secondsRemainingInBurst = _partyCoordinationService.GetBurstWindowRemaining();
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
