using System;
using System.Numerics;
using Olympus.Services.Movement;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class CameraInputTransformTests
{
    // Convention (mirrors BossMod MovementOverride): worldHeading = atan2(x, z);
    // rel = worldHeading - (cameraAzimuth + PI); sumForward = cos(rel), sumLeft = sin(rel).
    [Fact]
    public void WorldDirection_AlignedWithCameraForward_IsPureForward()
    {
        var azimuth = 0.7f;
        var forwardHeading = azimuth + MathF.PI;
        var dir = new Vector2(MathF.Sin(forwardHeading), MathF.Cos(forwardHeading));
        var (sumForward, sumLeft) = TrashAvoidanceService.WorldDirectionToCameraInput(dir, azimuth);
        Assert.Equal(1f, sumForward, 3);
        Assert.Equal(0f, sumLeft, 3);
    }

    [Fact]
    public void WorldDirection_OppositeCameraForward_IsPureBackward()
    {
        var azimuth = -1.2f;
        var dir = new Vector2(MathF.Sin(azimuth), MathF.Cos(azimuth)); // toward the camera
        var (sumForward, sumLeft) = TrashAvoidanceService.WorldDirectionToCameraInput(dir, azimuth);
        Assert.Equal(-1f, sumForward, 3);
        Assert.Equal(0f, sumLeft, 3);
    }

    [Fact]
    public void WorldDirection_90DegreesLeft_IsPureStrafe()
    {
        var azimuth = 0f;
        var leftHeading = azimuth + MathF.PI + MathF.PI / 2f;
        var dir = new Vector2(MathF.Sin(leftHeading), MathF.Cos(leftHeading));
        var (sumForward, sumLeft) = TrashAvoidanceService.WorldDirectionToCameraInput(dir, azimuth);
        Assert.Equal(0f, sumForward, 3);
        Assert.Equal(1f, sumLeft, 3);
    }
}
