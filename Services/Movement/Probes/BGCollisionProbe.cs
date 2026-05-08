using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;

namespace Olympus.Services.Movement.Probes;

public sealed unsafe class BGCollisionProbe : IBGCollisionProbe
{
    private readonly IPluginLog log;

    public BGCollisionProbe(IPluginLog log)
    {
        this.log = log;
    }

    public bool IsPathBlocked(Vector3 origin, Vector3 destination)
    {
        try
        {
            var direction = destination - origin;
            var distance = direction.Length();
            if (distance < 0.01f)
                return false;
            direction /= distance;

            var eye = origin;
            eye.Y += 2f;
            var hit = BGCollisionModule.RaycastMaterialFilter(eye, direction, out _, distance);
            return hit;
        }
        catch (Exception ex)
        {
            log.Verbose($"BGCollisionProbe.IsPathBlocked failed: {ex.Message}. Treating as clear.");
            return false;
        }
    }
}
