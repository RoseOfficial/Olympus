using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Olympus.Services.Pull;

/// <summary>
/// Production implementation of <see cref="ICountdownProbe"/>.
///
/// Reads the party countdown timer from the <c>AgentCountDownSettingDialog</c>
/// native struct via raw field offsets. Offsets established by RSR's
/// <c>Countdown</c> struct and cross-confirmed by BossMod's
/// <c>WorldStateGameSync.UpdateClient</c>:
///   0x28 = Timer (float, counts DOWN from the set duration)
///   0x38 = Active (byte, non-zero when a countdown is running)
///
/// The Dalamud-bundled FFXIVClientStructs does not expose <c>Timer</c> or
/// <c>Active</c> as documented C# properties on <c>AgentCountDownSettingDialog</c>,
/// so we cast the instance pointer to <c>byte*</c> and read directly.
///
/// Fails open to null on any exception or null instance pointer; if game-patch
/// struct changes invalidate these offsets the feature becomes inert (no crash).
/// </summary>
public sealed unsafe class DalamudCountdownProbe : ICountdownProbe
{
    // Offsets within AgentCountDownSettingDialog (confirmed RSR + BossMod production).
    private const int OffsetTimer  = 0x28; // float — current seconds remaining
    private const int OffsetActive = 0x38; // byte  — non-zero when countdown is running

    // The game caps /countdown at 30s. A value outside (0, 30] means the raw offsets
    // no longer point at the fields we think they do (game-patch struct drift), so we
    // report "no countdown" rather than feeding a garbage time to pre-pull consumers.
    // Without this, drift fails WEIRD (actions fire at random times out of combat)
    // instead of failing INERT.
    private const float MaxPlausibleCountdownSeconds = 30f;

    // Throttle: at most one log per mode per 5s to avoid per-frame spam on patch day.
    private const double LogThrottleSeconds = 5.0;
    private DateTime _lastExceptionLogTime = DateTime.MinValue;
    private DateTime _lastPlausibilityLogTime = DateTime.MinValue;

    private readonly IPluginLog? _log;

    public DalamudCountdownProbe(IPluginLog? log = null)
    {
        _log = log;
    }

    /// <inheritdoc />
    public float? GetCountdownRemaining()
    {
        try
        {
            var agent = AgentCountDownSettingDialog.Instance();
            if (agent == null) return null;

            var raw = (byte*)agent;

            var active = *(raw + OffsetActive);
            if (active == 0) return null;

            var timer = *(float*)(raw + OffsetTimer);

            // Rejects tiny residuals lingering after a countdown ends, NaN (all
            // comparisons false), and implausible values from offset drift.
            if (!(timer > 0f) || timer > MaxPlausibleCountdownSeconds)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastPlausibilityLogTime).TotalSeconds >= LogThrottleSeconds)
                {
                    _lastPlausibilityLogTime = now;
                    _log?.Debug(
                        "[CountdownProbe] countdown Active but timer out of plausible range ({0}); treating as no countdown (possible offset drift).",
                        timer);
                }
                return null;
            }

            return timer;
        }
        catch (Exception ex)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastExceptionLogTime).TotalSeconds >= LogThrottleSeconds)
            {
                _lastExceptionLogTime = now;
                _log?.Warning(
                    "[CountdownProbe] exception reading countdown agent (offsets may have drifted on a game patch): {0}",
                    ex.Message);
            }
            return null;
        }
    }
}
