using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Olympus.Services.Targeting;

/// <summary>
/// Production implementation: reads FFXIV's MarkingController singleton.
/// Mirrors RSR's MarkingHelper slot indices exactly (HeadMarker enum):
///   Attack1=0, Attack2=1, Attack3=2, Attack4=3, Attack5=4,
///   Bind1=5, Bind2=6, Bind3=7,
///   Stop1=8, Stop2=9,
///   Square=10, Circle=11, Cross=12, Triangle=13,
///   Attack6=14, Attack7=15, Attack8=16.
/// Fails open (returns empty arrays) when MarkingController is unavailable.
/// </summary>
public sealed unsafe class DalamudMarkerProbe : IMarkerProbe
{
    private static readonly int[] AttackSlots = [0, 1, 2, 3, 4, 14, 15, 16];
    private static readonly int[] StopSlots   = [8, 9];

    private static readonly ulong[] EmptyAttack = new ulong[8];
    private static readonly ulong[] EmptyStop   = new ulong[2];

    /// <inheritdoc />
    public ulong[] GetAttackMarkTargets()
    {
        try
        {
            var instance = MarkingController.Instance();
            if (instance == null || instance->Markers.Length == 0) return EmptyAttack;

            var result = new ulong[8];
            for (var i = 0; i < AttackSlots.Length; i++)
            {
                var raw = instance->Markers[AttackSlots[i]].ObjectId;
                result[i] = raw > 0 ? (ulong)raw : 0ul;
            }
            return result;
        }
        catch
        {
            return EmptyAttack;
        }
    }

    /// <inheritdoc />
    public ulong[] GetStopMarkTargets()
    {
        try
        {
            var instance = MarkingController.Instance();
            if (instance == null || instance->Markers.Length == 0) return EmptyStop;

            var result = new ulong[2];
            for (var i = 0; i < StopSlots.Length; i++)
            {
                var raw = instance->Markers[StopSlots[i]].ObjectId;
                result[i] = raw > 0 ? (ulong)raw : 0ul;
            }
            return result;
        }
        catch
        {
            return EmptyStop;
        }
    }
}
