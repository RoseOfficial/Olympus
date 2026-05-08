using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;

namespace Olympus.Services.Movement;

[StructLayout(LayoutKind.Explicit, Size = 0x140)]
internal unsafe struct MoveControllerSubMemberForMine
{
    [FieldOffset(0x94)] public byte Spinning;
}

public sealed unsafe class RMIWalkHookService : IRMIWalkHookService
{
    private const int UserInputDampenerMs = 200;
    private const string RMIWalkSig = "E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D";

    private readonly IPluginLog log;
    private DateTime lastUserInputAt = DateTime.MinValue;

    private delegate void RMIWalkDelegate(MoveControllerSubMemberForMine* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    private Hook<RMIWalkDelegate>? rmiWalkHook;

    public Vector3? DesiredInputVector { get; set; }
    public bool HookInstalled { get; private set; }

    public RMIWalkHookService(IGameInteropProvider interopProvider, IPluginLog log)
    {
        this.log = log;

        try
        {
            rmiWalkHook = interopProvider.HookFromSignature<RMIWalkDelegate>(RMIWalkSig, RMIWalkDetour);
            rmiWalkHook.Enable();
            HookInstalled = true;
            log.Info("[Movement] RMIWalk hook installed via signature.");
        }
        catch (Exception ex)
        {
            log.Warning($"[Movement] RMIWalk sigscan/hook install failed: {ex.Message}. AoE avoidance movement disabled for this game version.");
            HookInstalled = false;
        }
    }

    public void Dispose()
    {
        try
        {
            rmiWalkHook?.Disable();
            rmiWalkHook?.Dispose();
            rmiWalkHook = null;
        }
        catch (Exception ex)
        {
            log.Error($"[Movement] RMIWalk hook disposal failed: {ex.Message}");
        }
    }

    private void RMIWalkDetour(MoveControllerSubMemberForMine* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        // Always call original first so the game populates input fields with the player's actual input.
        rmiWalkHook!.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);

        var hasUserInput = (*sumLeft != 0f) || (*sumForward != 0f) || (*sumTurnLeft != 0f);
        if (hasUserInput)
        {
            lastUserInputAt = DateTime.UtcNow;
            return;
        }

        // Within 200ms of last user input, still yield (avoid tap-and-stop oscillation).
        if ((DateTime.UtcNow - lastUserInputAt).TotalMilliseconds < UserInputDampenerMs)
            return;

        var v = DesiredInputVector;
        if (v == null) return;

        *sumForward = v.Value.X;
        *sumLeft = v.Value.Y;
        *sumTurnLeft = v.Value.Z;
    }
}
