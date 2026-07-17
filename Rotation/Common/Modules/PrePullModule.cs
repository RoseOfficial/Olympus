using System.Collections.Generic;
using Olympus.Rotation.Common;
using Olympus.Services.Pull;

namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Strict pre-pull dispatch slot. Holds a registered list of pre-pull candidates
/// and runs them in registration order when <c>IPullIntentService.Current != None</c>.
/// One instance per rotation, constructed by <c>BaseRotation</c>.
///
/// v1 ships with <see cref="TinctureCandidate"/> registered for the opener-pot case.
/// Per-job pre-pull weaves (DNC Standard Step migration, BRD Empyreal Arrow timing,
/// MCH Reassemble pre-Wildfire, AST card draw, future opener-table actions) plug in
/// here as additional candidates registered in concrete rotation constructors.
/// </summary>
public sealed class PrePullModule
{
    private readonly IPullIntentService _pullIntent;
    private readonly List<IPrePullCandidate> _candidates = new();

    public PrePullModule(IPullIntentService pullIntent)
    {
        _pullIntent = pullIntent;
    }

    public void Register(IPrePullCandidate candidate) => _candidates.Add(candidate);

    /// <summary>
    /// Attempts to dispatch the first ready pre-pull candidate. Returns true if any
    /// candidate fired. Caller should treat the frame as having spent its oGCD slot.
    ///
    /// Two paths:
    /// - PullIntent != None (cast- or queue-based signal): all registered candidates run.
    /// - Countdown &lt;= 2s AND PullIntent is still None: only candidates where
    ///   <see cref="IPrePullCandidate.CanFireDuringCountdown"/> is true are run.
    ///   Other candidates are skipped so the countdown does not broaden their gate.
    /// </summary>
    public bool TryDispatch(uint jobId, IRotationContext context)
    {
        var pullIntentActive = _pullIntent.Current != PullIntent.None;
        var countdownActive = _pullIntent.CountdownRemaining is <= 2.0f;

        if (!pullIntentActive && !countdownActive) return false;

        foreach (var c in _candidates)
        {
            // When firing via the countdown path (no PullIntent yet), only run
            // candidates that explicitly opt in — the gate must not broaden for others.
            if (!pullIntentActive && !c.CanFireDuringCountdown) continue;
            if (c.TryDispatch(jobId, context)) return true;
        }
        return false;
    }
}
