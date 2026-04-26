using System;

namespace Olympus.Services.Pull;

/// <summary>
/// Per-frame state machine. See <see cref="IPullIntentService"/>.
///
/// State transitions:
///   None     → Imminent  on cast-of-hostile, or 100ms-confirmed hostile queue
///   None     → Active    on direct combat-entry without prior intent
///   Imminent → Active    on combat-entry
///   Imminent → None      after 3s without combat or any new intent signal
///   Active   → None      after 2s in combat
/// </summary>
public sealed class PullIntentService : IPullIntentService
{
    private const double ImminentTimeoutSeconds = 3.0;
    private const double ActiveDurationSeconds = 2.0;
    private const double QueueConfirmationMs = 100.0;

    private PullIntent _current = PullIntent.None;
    private DateTime? _imminentSince;
    private DateTime? _activeSince;
    private DateTime? _queueSeenAt;
    private uint? _queueSeenId;
    private bool _hasExitedActivePhase;
    private bool _wasInCombat;

    public PullIntent Current => _current;

    public void Update(
        bool isPlayerCasting,
        bool isCastTargetHostile,
        uint? queuedActionId,
        bool isQueuedActionHostile,
        bool isInCombat,
        DateTime utcNow)
    {
        // Reset latch on fresh combat entry.
        if (isInCombat && !_wasInCombat)
            _hasExitedActivePhase = false;
        _wasInCombat = isInCombat;

        // Track sustained queue presence for 100ms confirmation.
        if (queuedActionId.HasValue && isQueuedActionHostile)
        {
            if (_queueSeenId != queuedActionId)
            {
                _queueSeenId = queuedActionId;
                _queueSeenAt = utcNow;
            }
        }
        else
        {
            _queueSeenAt = null;
            _queueSeenId = null;
        }

        // Compute trigger sources for this frame.
        var castTrigger = isPlayerCasting && isCastTargetHostile && !isInCombat;
        var queueTrigger = _queueSeenAt.HasValue
                           && (utcNow - _queueSeenAt.Value).TotalMilliseconds >= QueueConfirmationMs
                           && !isInCombat;
        var anyIntent = castTrigger || queueTrigger;

        switch (_current)
        {
            case PullIntent.None:
                if (isInCombat && !_hasExitedActivePhase)
                {
                    _current = PullIntent.Active;
                    _activeSince = utcNow;
                }
                else if (anyIntent)
                {
                    _current = PullIntent.Imminent;
                    _imminentSince = utcNow;
                }
                break;

            case PullIntent.Imminent:
                if (isInCombat)
                {
                    _current = PullIntent.Active;
                    _activeSince = utcNow;
                    _imminentSince = null;
                }
                else if ((utcNow - _imminentSince!.Value).TotalSeconds >= ImminentTimeoutSeconds
                         && !anyIntent)
                {
                    _current = PullIntent.None;
                    _imminentSince = null;
                }
                break;

            case PullIntent.Active:
                if ((utcNow - _activeSince!.Value).TotalSeconds >= ActiveDurationSeconds)
                {
                    _current = PullIntent.None;
                    _activeSince = null;
                    _hasExitedActivePhase = true;
                }
                break;
        }
    }
}
