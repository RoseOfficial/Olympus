using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Olympus.Config;

namespace Olympus.Services.Input;

public sealed class ModifierKeyService : IModifierKeyService
{
    private readonly IKeyState keyState;
    private readonly Configuration configuration;
    private bool burstOverride;
    private bool conservativeOverride;

    public ModifierKeyService(IKeyState keyState, Configuration configuration)
    {
        this.keyState = keyState;
        this.configuration = configuration;
    }

    public bool IsBurstOverride => burstOverride;

    public bool IsConservativeOverride => conservativeOverride;

    public void Update()
    {
        if (!configuration.Input.EnableModifierOverrides)
        {
            burstOverride = false;
            conservativeOverride = false;
            return;
        }

        var burst = IsKeyHeld(configuration.Input.BurstOverrideKey);
        var conservative = IsKeyHeld(configuration.Input.ConservativeOverrideKey);

        // Both keys held cancels each other out so the rotation falls back to
        // its normal behavior. Avoids an ambiguous "which intent wins" state.
        if (burst && conservative)
        {
            burstOverride = false;
            conservativeOverride = false;
            return;
        }

        burstOverride = burst;
        conservativeOverride = conservative;
    }

    private bool IsKeyHeld(ModifierKey key) => key switch
    {
        ModifierKey.Shift => keyState[VirtualKey.SHIFT],
        ModifierKey.Control => keyState[VirtualKey.CONTROL],
        ModifierKey.Alt => keyState[VirtualKey.MENU],
        _ => false,
    };
}
