using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace Olympus.Services.Movement;

/// <summary>
/// Reads the active camera's azimuth for world-to-input direction conversion.
/// Test seam: production reads the native camera; tests inject a fixed value.
/// </summary>
public interface ICameraAzimuthProbe
{
    /// <summary>Camera azimuth in radians, or null when no camera is available.</summary>
    float? GetCameraAzimuthRadians();
}

public sealed unsafe class CameraAzimuthProbe : ICameraAzimuthProbe
{
    public float? GetCameraAzimuthRadians()
    {
        var manager = CameraManager.Instance();
        if (manager == null) return null;
        var camera = manager->GetActiveCamera();
        if (camera == null) return null;
        var render = camera->SceneCamera.RenderCamera;
        if (render == null) return null;
        var view = render->ViewMatrix;
        return System.MathF.Atan2(view.M13, view.M33);
    }
}
