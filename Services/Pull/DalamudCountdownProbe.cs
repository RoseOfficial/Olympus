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
            if (!(timer > 0f) || timer > MaxPlausibleCountdownSeconds) return null;

            return timer;
        }
        catch
        {
            return null;
        }
    }
}
