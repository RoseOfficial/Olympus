using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Renders the Consumables config section: master toggle for tincture
/// automation plus the empty-inventory warning toggle.
/// </summary>
public sealed class ConsumablesSection
{
    private readonly Configuration config;
    private readonly Action save;

    public ConsumablesSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f),
            Loc.T(LocalizedStrings.Consumables.ConsumablesHeader, "Consumables"));
        ImGui.Separator();

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Consumables.EnableAutoTincture, "Auto-use combat tinctures"),
            () => config.Consumables.EnableAutoTincture,
            v => config.Consumables.EnableAutoTincture = v,
            Loc.T(LocalizedStrings.Consumables.EnableAutoTinctureDesc,
                "When enabled, Olympus will use a combat tincture during opener and re-pot windows. Only fires in high-end content (savage, extreme, ultimate, criterion, chaotic alliance). Default off because pots cost real gil."),
            save);

        ImGui.Spacing();

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Consumables.WarnOnEmptyInventory, "Warn when inventory is empty"),
            () => config.Consumables.WarnOnEmptyInventory,
            v => config.Consumables.WarnOnEmptyInventory = v,
            Loc.T(LocalizedStrings.Consumables.WarnOnEmptyInventoryDesc,
                "Fires a one-shot chat warning per fight when auto-tincture is enabled but no matching tincture is in your inventory."),
            save);
    }
}
