using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace Olympus.Services.Targeting;

/// <summary>
/// Tracks player-to-target distance over a short lookback window to decide whether
/// gap closers are safe to fire. See <see cref="IGapCloserSafetyService"/> for the
/// design rationale.
/// </summary>
public sealed class GapCloserSafetyService : IGapCloserSafetyService
{
    private readonly Configuration _configuration;
    private readonly ITargetManager _targetManager;

    // Circular buffer of recent (timestamp_ms, distance) samples for the player's
    // current target. Small (8 entries) because we only need the oldest sample
    // within the lookback window. Reset whenever the tracked target changes.
    private readonly (long timestampMs, float distance)[] _samples = new (long, float)[8];
    private int _sampleCount;
    private int _sampleHead; // next write index
    private ulong _trackedTargetId;
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public string? LastBlockReason { get; private set; }

    public GapCloserSafetyService(Configuration configuration, ITargetManager targetManager)
    {
        _configuration = configuration;
        _targetManager = targetManager;
    }

    /// <inheritdoc />
    public void Update(IPlayerCharacter? player, IBattleChara? currentTarget)
    {
        if (player == null || currentTarget == null)
        {
            ResetSamples(0UL);
            return;
        }

        // Reset tracking when the target changes — old distance samples are meaningless.
        if (currentTarget.GameObjectId != _trackedTargetId)
        {
            ResetSamples(currentTarget.GameObjectId);
        }

        var distance = Vector2.Distance(
            new Vector2(player.Position.X, player.Position.Z),
            new Vector2(currentTarget.Position.X, currentTarget.Position.Z));

        var nowMs = _clock.ElapsedMilliseconds;

        _samples[_sampleHead] = (nowMs, distance);
        _sampleHead = (_sampleHead + 1) % _samples.Length;
        if (_sampleCount < _samples.Length)
            _sampleCount++;
    }

    /// <inheritdoc />
    public bool ShouldBlockGapCloser(IBattleChara target, IPlayerCharacter player)
    {
        // Master toggle off → never block.
        if (!_configuration.Targeting.SafeGapCloser)
        {
            LastBlockReason = null;
            return false;
        }

        // Belt & suspenders: if damage is paused for "no target" reasons, block too.
        if (_configuration.Targeting.PauseWhenNoTarget && _targetManager.Target == null)
        {
            LastBlockReason = "no target selected";
            return true;
        }

        // Rule 1: Only gap close onto the player's explicitly selected enemy.
        // Prevents Olympus from dragging the player to a strategy-picked fallback
        // (e.g., LowestHp add they never meant to engage).
        var userTarget = _targetManager.Target as IBattleNpc;
        if (userTarget == null || userTarget.GameObjectId != target.GameObjectId)
        {
            LastBlockReason = "target mismatch (only gap-close to your explicit target)";
            return true;
        }

        // Rule 2: If the player has been gaining distance from the target within the
        // lookback window, they are deliberately repositioning — don't yank them back.
        // Covers spread markers, stack markers, ground AoE, gaze mechanics that the
        // player is currently avoiding.
        if (IsPlayerMovingAway(target, player))
        {
            LastBlockReason = "moving away from target";
            return true;
        }

        LastBlockReason = null;
        return false;
    }

    private bool IsPlayerMovingAway(IBattleChara target, IPlayerCharacter player)
    {
        if (_sampleCount < 2)
            return false;

        var lookbackMs = _configuration.Targeting.GapCloserMovementLookbackMs;
        var awayThreshold = _configuration.Targeting.GapCloserMovementAwayThresholdY;
        var nowMs = _clock.ElapsedMilliseconds;

        // Find the oldest sample still inside the lookback window.
        float oldestDistance = -1f;
        long oldestAgeMs = 0;
        for (int i = 0; i < _sampleCount; i++)
        {
            var idx = (_sampleHead - 1 - i + _samples.Length) % _samples.Length;
            var sample = _samples[idx];
            var ageMs = nowMs - sample.timestampMs;
            if (ageMs <= lookbackMs)
            {
                oldestDistance = sample.distance;
                oldestAgeMs = ageMs;
            }
            else
            {
                break;
            }
        }

        if (oldestDistance < 0f || oldestAgeMs < 100)
            return false;

        var currentDistance = Vector2.Distance(
            new Vector2(player.Position.X, player.Position.Z),
            new Vector2(target.Position.X, target.Position.Z));

        return currentDistance - oldestDistance >= awayThreshold;
    }

    private void ResetSamples(ulong newTargetId)
    {
        _trackedTargetId = newTargetId;
        _sampleCount = 0;
        _sampleHead = 0;
    }
}
