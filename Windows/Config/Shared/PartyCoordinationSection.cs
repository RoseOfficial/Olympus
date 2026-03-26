using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Renders the Party Coordination settings section.
/// </summary>
public sealed class PartyCoordinationSection
{
    private readonly Configuration config;
    private readonly Action save;

    public PartyCoordinationSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.9f, 1.0f), Loc.T(LocalizedStrings.PartyCoordination.SectionTitle, "Party Coordination Settings"));
        ImGui.Separator();

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.PartyCoordination.EnablePartyCoordination, "Enable Party Coordination"),
            () => config.PartyCoordination.EnablePartyCoordination,
            v => config.PartyCoordination.EnablePartyCoordination = v,
            Loc.T(LocalizedStrings.PartyCoordination.EnablePartyCoordinationDesc,
                "Coordinate heals and cooldowns with other Olympus users in your party. Changes take effect on next plugin reload."),
            save);

        if (!config.PartyCoordination.EnablePartyCoordination)
            return;

        DrawCoordinationSection();
        DrawConnectionSection();
    }

    private void DrawCoordinationSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.PartyCoordination.CoordinationSection, "Coordination"), "PartyCoordCoord"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.PartyCoordination.EnableCooldownCoordination, "Enable Cooldown Coordination"),
                () => config.PartyCoordination.EnableCooldownCoordination,
                v => config.PartyCoordination.EnableCooldownCoordination = v,
                Loc.T(LocalizedStrings.PartyCoordination.EnableCooldownCoordinationDesc,
                    "Prevent stacking defensive cooldowns with other Olympus users"),
                save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.PartyCoordination.BroadcastMajorCooldowns, "Broadcast Major Cooldowns"),
                () => config.PartyCoordination.BroadcastMajorCooldowns,
                v => config.PartyCoordination.BroadcastMajorCooldowns = v,
                Loc.T(LocalizedStrings.PartyCoordination.BroadcastMajorCooldownsDesc,
                    "Announce when you use major cooldowns so other instances can coordinate"),
                save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.PartyCoordination.EnableAoEHealCoordination, "Enable AoE Heal Coordination"),
                () => config.PartyCoordination.EnableAoEHealCoordination,
                v => config.PartyCoordination.EnableAoEHealCoordination = v,
                Loc.T(LocalizedStrings.PartyCoordination.EnableAoEHealCoordinationDesc,
                    "Prevent multiple healers casting party-wide heals simultaneously"),
                save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawConnectionSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.PartyCoordination.ConnectionSection, "Connection (Advanced)"), "PartyCoordConn", false))
        {
            ConfigUIHelpers.BeginIndent();

            config.PartyCoordination.HeartbeatIntervalMs = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.PartyCoordination.HeartbeatInterval, "Heartbeat Interval (ms)"),
                config.PartyCoordination.HeartbeatIntervalMs, 500, 5000,
                Loc.T(LocalizedStrings.PartyCoordination.HeartbeatIntervalDesc, "How often to broadcast presence (lower = faster, more overhead)"),
                save, v => config.PartyCoordination.HeartbeatIntervalMs = v);

            config.PartyCoordination.InstanceTimeoutMs = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.PartyCoordination.InstanceTimeout, "Instance Timeout (ms)"),
                config.PartyCoordination.InstanceTimeoutMs, 2000, 15000,
                Loc.T(LocalizedStrings.PartyCoordination.InstanceTimeoutDesc, "Time before an instance is considered disconnected"),
                save, v => config.PartyCoordination.InstanceTimeoutMs = v);

            config.PartyCoordination.HealReservationExpiryMs = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.PartyCoordination.HealReservationExpiry, "Heal Reservation Expiry (ms)"),
                config.PartyCoordination.HealReservationExpiryMs, 1000, 5000,
                Loc.T(LocalizedStrings.PartyCoordination.HealReservationExpiryDesc, "How long heal reservations stay valid"),
                save, v => config.PartyCoordination.HealReservationExpiryMs = v);

            ConfigUIHelpers.EndIndent();
        }
    }
}
