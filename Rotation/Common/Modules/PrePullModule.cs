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
    /// </summary>
    public bool TryDispatch(uint jobId, IRotationContext context)
    {
        if (_pullIntent.Current == PullIntent.None) return false;
        foreach (var c in _candidates)
            if (c.TryDispatch(jobId, context)) return true;
        return false;
    }
}
