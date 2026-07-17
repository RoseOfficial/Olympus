using Olympus.Rotation.Common;
using Olympus.Services.Consumables;

namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Pre-pull candidate that dispatches a combat tincture during the opener.
/// Delegates the gate evaluation and dispatch to <see cref="ITinctureDispatcher"/>.
/// </summary>
public sealed class TinctureCandidate : IPrePullCandidate
{
    private readonly ITinctureDispatcher _dispatcher;

    public TinctureCandidate(ITinctureDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    /// Tincture opts into the countdown path so it fires at countdown &lt;= 2s without
    /// requiring PullIntent to have transitioned (player may not yet have cast or queued).
    public bool CanFireDuringCountdown => true;

    public bool TryDispatch(uint jobId, IRotationContext context)
    {
        return _dispatcher.TryDispatch(jobId, context.InCombat, prePullPhase: true);
    }
}
