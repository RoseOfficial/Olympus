using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Olympus.Services.Movement.Probes;

public sealed unsafe class ObjectInteractor : IObjectInteractor
{
    private readonly IObjectTable objectTable;
    private readonly IPluginLog log;

    public ObjectInteractor(IObjectTable objectTable, IPluginLog log)
    {
        this.objectTable = objectTable;
        this.log = log;
    }

    public void Interact(ulong gameObjectId)
    {
        try
        {
            var obj = objectTable.SearchById(gameObjectId);
            if (obj == null)
                return;

            var native = (GameObject*)obj.Address;
            if (native == null)
                return;

            TargetSystem.Instance()->InteractWithObject(native);
        }
        catch (Exception ex)
        {
            log.Verbose($"ObjectInteractor.Interact({gameObjectId}) failed: {ex.Message}");
        }
    }
}
