using System.Numerics;
using Olympus.Models;
using Olympus.Services.Action;

namespace Olympus.Services.Debug;

/// <summary>
/// Centralized color definitions for debug UI.
/// </summary>
public static class DebugColors
{
    // Status colors
    public static readonly Vector4 Success = new(0.0f, 1.0f, 0.0f, 1.0f);
    public static readonly Vector4 Failure = new(1.0f, 0.3f, 0.3f, 1.0f);
    public static readonly Vector4 Skip = new(1.0f, 1.0f, 0.0f, 1.0f);
    public static readonly Vector4 Dim = new(0.6f, 0.6f, 0.6f, 1.0f);
    public static readonly Vector4 Warning = new(1.0f, 0.8f, 0.0f, 1.0f);
    public static readonly Vector4 Heal = new(0.4f, 1.0f, 0.4f, 1.0f);

    // FFLogs percentile colors
    public static readonly Vector4 FFLogsGold = new(0.90f, 0.80f, 0.50f, 1.0f);    // 100%
    public static readonly Vector4 FFLogsPink = new(0.89f, 0.41f, 0.66f, 1.0f);    // 99%
    public static readonly Vector4 FFLogsOrange = new(1.00f, 0.50f, 0.00f, 1.0f);  // 95-98%
    public static readonly Vector4 FFLogsPurple = new(0.64f, 0.21f, 0.93f, 1.0f);  // 75-94%
    public static readonly Vector4 FFLogsBlue = new(0.00f, 0.44f, 1.00f, 1.0f);    // 50-74%
    public static readonly Vector4 FFLogsGreen = new(0.12f, 1.00f, 0.00f, 1.0f);   // 25-49%
    public static readonly Vector4 FFLogsGrey = new(0.40f, 0.40f, 0.40f, 1.0f);    // 0-24%

    /// <summary>
    /// Get FFLogs-style color based on percentage (0-100).
    /// </summary>
    public static Vector4 GetFFLogsColor(float percentage) => percentage switch
    {
        100f => FFLogsGold,
        >= 99f => FFLogsPink,
        >= 95f => FFLogsOrange,
        >= 75f => FFLogsPurple,
        >= 50f => FFLogsBlue,
        >= 25f => FFLogsGreen,
        _ => FFLogsGrey
    };

    /// <summary>
    /// Get color for an action result.
    /// </summary>
    public static Vector4 GetResultColor(ActionResult result) => result switch
    {
        ActionResult.Success => Success,
        ActionResult.NoTarget or ActionResult.ActionNotReady or ActionResult.OnCooldown => Skip,
        _ => Failure
    };

    /// <summary>
    /// Get icon character for an action result.
    /// </summary>
    public static string GetResultIcon(ActionResult result) => result switch
    {
        ActionResult.Success => "+",
        ActionResult.NoTarget => "?",
        ActionResult.ActionNotReady or ActionResult.OnCooldown => "~",
        _ => "X"
    };

    /// <summary>
    /// Get color for planning state.
    /// </summary>
    public static Vector4 GetPlanningStateColor(string state) => state switch
    {
        "DPS" => Success,
        "Single Heal" or "AoE Heal" => Heal,
        "Not in combat" => Dim,
        _ => Warning
    };

    /// <summary>
    /// Get color for AoE healing status.
    /// </summary>
    public static Vector4 GetAoEStatusColor(string status)
    {
        if (status.StartsWith("Casting"))
            return Success;
        if (status.StartsWith("Injured"))
            return Warning;
        return Dim;
    }

    /// <summary>
    /// Get color for GCD state.
    /// </summary>
    public static Vector4 GetGcdStateColor(GcdState state) => state switch
    {
        GcdState.Ready => Success,
        _ => Dim
    };
}
