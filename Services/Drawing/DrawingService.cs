using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Pictomancy;

namespace Olympus.Services.Drawing;

/// <summary>
/// Pictomancy-based world-space drawing service.
/// Pattern follows Avarice's PictomancyRenderer with graceful fallback.
/// </summary>
public sealed class DrawingService : IDisposable
{
    private readonly DrawHelperConfig _config;
    private readonly IPluginLog _log;
    private PctDrawList? _drawList;
    private bool _initialized;

    public bool IsDrawing => _drawList != null;

    public DrawingService(IDalamudPluginInterface pluginInterface, DrawHelperConfig config, IPluginLog log)
    {
        _config = config;
        _log = log;

        try
        {
            PictoService.Initialize(pluginInterface);
            _initialized = true;
        }
        catch (Exception ex)
        {
            _log.Warning($"Pictomancy not available: {ex.Message}");
            _initialized = false;
        }
    }

    public void BeginFrame()
    {
        _drawList = null;
        if (!_initialized || !_config.UsePictomancy) return;

        try
        {
            var hints = new PctDrawHints(
                autoDraw: true,
                maxAlpha: (byte)(_config.PictomancyMaxAlpha * 255f),
                clipNativeUI: _config.PictomancyClipNativeUI);
            _drawList = PictoService.Draw(ImGui.GetWindowDrawList(), hints);
        }
        catch (Exception ex)
        {
            _log.Debug($"Pictomancy BeginFrame failed: {ex.Message}");
            _drawList = null;
        }
    }

    public void EndFrame()
    {
        if (_drawList == null) return;
        _drawList.Dispose();
        _drawList = null;
    }

    public void DrawCircle(Vector3 center, float radius, uint color, float thickness = 2f)
    {
        _drawList?.AddCircle(center, radius, color, thickness: thickness);
    }

    public void DrawCircleFilled(Vector3 center, float radius, uint color)
    {
        _drawList?.AddCircleFilled(center, radius, color);
    }

    /// <summary>
    /// Draw a fan/cone shape. Angles in radians, FFXIV convention (negated for Pictomancy).
    /// </summary>
    public void DrawFan(Vector3 center, float innerRadius, float outerRadius,
        float startRads, float endRads, uint color, float thickness = 2f)
    {
        if (_drawList == null) return;
        _drawList.AddFan(center, innerRadius, outerRadius,
            ToPictomancyAngle(startRads), ToPictomancyAngle(endRads),
            color, thickness: thickness);
    }

    public void DrawFanFilled(Vector3 center, float innerRadius, float outerRadius,
        float startRads, float endRads, uint color)
    {
        if (_drawList == null) return;
        _drawList.AddFanFilled(center, innerRadius, outerRadius,
            ToPictomancyAngle(startRads), ToPictomancyAngle(endRads), color);
    }

    public void DrawDot(Vector3 position, float size, uint color)
    {
        _drawList?.AddDot(position, size, color);
    }

    public void PathLineTo(Vector3 point)
    {
        _drawList?.PathLineTo(point);
    }

    public void PathStroke(uint color, float thickness = 2f, bool closed = false)
    {
        _drawList?.PathStroke(color, closed ? PctStrokeFlags.Closed : PctStrokeFlags.None, thickness);
    }

    /// <summary>
    /// Draw a rectangle in world space using path lines (outline) or fan approximation (filled).
    /// </summary>
    public void DrawRect(Vector3 origin, float heading, float halfWidth, float length, uint color, bool filled = false, float thickness = 2f)
    {
        if (_drawList == null) return;

        if (filled)
        {
            // Approximate fill using a fan that covers the rect area
            // The fan angle must be wide enough to cover halfWidth at the end of length
            var fanHalfAngle = MathF.Atan2(halfWidth, length) + 0.01f;
            var fanRadius = MathF.Sqrt(length * length + halfWidth * halfWidth);
            _drawList.AddFanFilled(origin, 0f, fanRadius,
                ToPictomancyAngle(heading - fanHalfAngle),
                ToPictomancyAngle(heading + fanHalfAngle), color);
        }
        else
        {
            // Outline: 4 path lines forming a closed rectangle
            var sinH = MathF.Sin(heading);
            var cosH = MathF.Cos(heading);
            var fwd = new Vector3(sinH * length, 0, cosH * length);
            var right = new Vector3(cosH * halfWidth, 0, -sinH * halfWidth);

            _drawList.PathLineTo(origin - right);
            _drawList.PathLineTo(origin + right);
            _drawList.PathLineTo(origin + right + fwd);
            _drawList.PathLineTo(origin - right + fwd);
            _drawList.PathStroke(color, PctStrokeFlags.Closed, thickness);
        }
    }

    /// <summary>
    /// Pictomancy uses opposite angle direction from FFXIV.
    /// </summary>
    private static float ToPictomancyAngle(float angle) => -angle;

    public void Dispose()
    {
        if (_initialized)
        {
            try { PictoService.Dispose(); }
            catch { /* ignore */ }
        }
    }
}
