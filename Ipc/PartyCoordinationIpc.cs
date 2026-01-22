using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Olympus.Services.Party;

namespace Olympus.Ipc;

/// <summary>
/// IPC endpoints for party coordination between multiple Olympus instances.
/// </summary>
/// <remarks>
/// Available IPC endpoints:
/// - Olympus.Party.Heartbeat: Periodic alive signal (string JSON)
/// - Olympus.Party.HealIntent: Broadcast heal reservation (string JSON)
/// - Olympus.Party.HealLanded: Notify heal completion (string JSON)
/// - Olympus.Party.CooldownUsed: Announce major cooldown usage (string JSON)
/// </remarks>
public sealed class PartyCoordinationIpc : IDisposable
{
    private readonly PartyCoordinationService _service;
    private readonly IPluginLog _log;

    // IPC providers (for sending)
    private readonly ICallGateProvider<string, object> _heartbeatProvider;
    private readonly ICallGateProvider<string, object> _healIntentProvider;
    private readonly ICallGateProvider<string, object> _healLandedProvider;
    private readonly ICallGateProvider<string, object> _cooldownUsedProvider;
    private readonly ICallGateProvider<string, object> _aoEHealIntentProvider;
    private readonly ICallGateProvider<string, object> _raidBuffIntentProvider;
    private readonly ICallGateProvider<string, object> _burstWindowStartProvider;
    private readonly ICallGateProvider<string, object> _gaugeStateProvider;
    private readonly ICallGateProvider<string, object> _roleDeclarationProvider;
    private readonly ICallGateProvider<string, object> _groundEffectPlacedProvider;
    private readonly ICallGateProvider<string, object> _raiseIntentProvider;
    private readonly ICallGateProvider<string, object> _cleanseIntentProvider;

    // IPC subscribers (for receiving)
    private readonly ICallGateSubscriber<string, object> _heartbeatSubscriber;
    private readonly ICallGateSubscriber<string, object> _healIntentSubscriber;
    private readonly ICallGateSubscriber<string, object> _healLandedSubscriber;
    private readonly ICallGateSubscriber<string, object> _cooldownUsedSubscriber;
    private readonly ICallGateSubscriber<string, object> _aoEHealIntentSubscriber;
    private readonly ICallGateSubscriber<string, object> _raidBuffIntentSubscriber;
    private readonly ICallGateSubscriber<string, object> _burstWindowStartSubscriber;
    private readonly ICallGateSubscriber<string, object> _gaugeStateSubscriber;
    private readonly ICallGateSubscriber<string, object> _roleDeclarationSubscriber;
    private readonly ICallGateSubscriber<string, object> _groundEffectPlacedSubscriber;
    private readonly ICallGateSubscriber<string, object> _raiseIntentSubscriber;
    private readonly ICallGateSubscriber<string, object> _cleanseIntentSubscriber;

    public PartyCoordinationIpc(
        IDalamudPluginInterface pluginInterface,
        PartyCoordinationService service,
        IPluginLog log)
    {
        _service = service;
        _log = log;

        // Register providers (we send on these)
        _heartbeatProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.Heartbeat");
        _healIntentProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.HealIntent");
        _healLandedProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.HealLanded");
        _cooldownUsedProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.CooldownUsed");
        _aoEHealIntentProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.AoEHealIntent");
        _raidBuffIntentProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.RaidBuffIntent");
        _burstWindowStartProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.BurstWindowStart");
        _gaugeStateProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.GaugeState");
        _roleDeclarationProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.RoleDeclaration");
        _groundEffectPlacedProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.GroundEffectPlaced");
        _raiseIntentProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.RaiseIntent");
        _cleanseIntentProvider = pluginInterface.GetIpcProvider<string, object>("Olympus.Party.CleanseIntent");

        // Register action handlers (for broadcast)
        _heartbeatProvider.RegisterAction(OnHeartbeatReceived);
        _healIntentProvider.RegisterAction(OnHealIntentReceived);
        _healLandedProvider.RegisterAction(OnHealLandedReceived);
        _cooldownUsedProvider.RegisterAction(OnCooldownUsedReceived);
        _aoEHealIntentProvider.RegisterAction(OnAoEHealIntentReceived);
        _raidBuffIntentProvider.RegisterAction(OnRaidBuffIntentReceived);
        _burstWindowStartProvider.RegisterAction(OnBurstWindowStartReceived);
        _gaugeStateProvider.RegisterAction(OnGaugeStateReceived);
        _roleDeclarationProvider.RegisterAction(OnRoleDeclarationReceived);
        _groundEffectPlacedProvider.RegisterAction(OnGroundEffectPlacedReceived);
        _raiseIntentProvider.RegisterAction(OnRaiseIntentReceived);
        _cleanseIntentProvider.RegisterAction(OnCleanseIntentReceived);

        // Subscribe to receive messages from other instances
        _heartbeatSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.Heartbeat");
        _healIntentSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.HealIntent");
        _healLandedSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.HealLanded");
        _cooldownUsedSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.CooldownUsed");
        _aoEHealIntentSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.AoEHealIntent");
        _raidBuffIntentSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.RaidBuffIntent");
        _burstWindowStartSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.BurstWindowStart");
        _gaugeStateSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.GaugeState");
        _roleDeclarationSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.RoleDeclaration");
        _groundEffectPlacedSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.GroundEffectPlaced");
        _raiseIntentSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.RaiseIntent");
        _cleanseIntentSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Olympus.Party.CleanseIntent");

        // Wire up service events to IPC broadcasts
        _service.OnHeartbeatReady += SendHeartbeat;
        _service.OnHealIntentReady += SendHealIntent;
        _service.OnHealLandedReady += SendHealLanded;
        _service.OnCooldownUsedReady += SendCooldownUsed;
        _service.OnAoEHealIntentReady += SendAoEHealIntent;
        _service.OnRaidBuffIntentReady += SendRaidBuffIntent;
        _service.OnBurstWindowStartReady += SendBurstWindowStart;
        _service.OnGaugeStateReady += SendGaugeState;
        _service.OnRoleDeclarationReady += SendRoleDeclaration;
        _service.OnGroundEffectPlacedReady += SendGroundEffectPlaced;
        _service.OnRaiseIntentReady += SendRaiseIntent;
        _service.OnCleanseIntentReady += SendCleanseIntent;

        _log.Info("Party coordination IPC initialized");
    }

    #region Send Methods

    private void SendHeartbeat(HeartbeatMessage message)
    {
        try
        {
            var json = message.ToJson();
            _heartbeatProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send heartbeat");
        }
    }

    private void SendHealIntent(HealIntentMessage message)
    {
        try
        {
            var json = message.ToJson();
            _healIntentProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send heal intent");
        }
    }

    private void SendHealLanded(HealLandedMessage message)
    {
        try
        {
            var json = message.ToJson();
            _healLandedProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send heal landed");
        }
    }

    private void SendCooldownUsed(CooldownUsedMessage message)
    {
        try
        {
            var json = message.ToJson();
            _cooldownUsedProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send cooldown used");
        }
    }

    private void SendAoEHealIntent(AoEHealIntentMessage message)
    {
        try
        {
            var json = message.ToJson();
            _aoEHealIntentProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send AoE heal intent");
        }
    }

    private void SendRaidBuffIntent(RaidBuffIntentMessage message)
    {
        try
        {
            var json = message.ToJson();
            _raidBuffIntentProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send raid buff intent");
        }
    }

    private void SendBurstWindowStart(BurstWindowStartMessage message)
    {
        try
        {
            var json = message.ToJson();
            _burstWindowStartProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send burst window start");
        }
    }

    private void SendGaugeState(GaugeStateMessage message)
    {
        try
        {
            var json = message.ToJson();
            _gaugeStateProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send gauge state");
        }
    }

    private void SendRoleDeclaration(RoleDeclarationMessage message)
    {
        try
        {
            var json = message.ToJson();
            _roleDeclarationProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send role declaration");
        }
    }

    private void SendGroundEffectPlaced(GroundEffectPlacedMessage message)
    {
        try
        {
            var json = message.ToJson();
            _groundEffectPlacedProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send ground effect placed");
        }
    }

    private void SendRaiseIntent(RaiseIntentMessage message)
    {
        try
        {
            var json = message.ToJson();
            _raiseIntentProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send raise intent");
        }
    }

    private void SendCleanseIntent(CleanseIntentMessage message)
    {
        try
        {
            var json = message.ToJson();
            _cleanseIntentProvider.SendMessage(json);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send cleanse intent");
        }
    }

    #endregion

    #region Receive Handlers

    private void OnHeartbeatReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as HeartbeatMessage;
            if (message != null)
            {
                _service.HandleRemoteHeartbeat(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process heartbeat");
        }
    }

    private void OnHealIntentReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as HealIntentMessage;
            if (message != null)
            {
                _service.HandleRemoteHealIntent(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process heal intent");
        }
    }

    private void OnHealLandedReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as HealLandedMessage;
            if (message != null)
            {
                _service.HandleRemoteHealLanded(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process heal landed");
        }
    }

    private void OnCooldownUsedReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as CooldownUsedMessage;
            if (message != null)
            {
                _service.HandleRemoteCooldownUsed(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process cooldown used");
        }
    }

    private void OnAoEHealIntentReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as AoEHealIntentMessage;
            if (message != null)
            {
                _service.HandleRemoteAoEHealIntent(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process AoE heal intent");
        }
    }

    private void OnRaidBuffIntentReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as RaidBuffIntentMessage;
            if (message != null)
            {
                _service.HandleRemoteRaidBuffIntent(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process raid buff intent");
        }
    }

    private void OnBurstWindowStartReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as BurstWindowStartMessage;
            if (message != null)
            {
                _service.HandleRemoteBurstWindowStart(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process burst window start");
        }
    }

    private void OnGaugeStateReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as GaugeStateMessage;
            if (message != null)
            {
                _service.HandleRemoteGaugeState(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process gauge state");
        }
    }

    private void OnRoleDeclarationReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as RoleDeclarationMessage;
            if (message != null)
            {
                _service.HandleRemoteRoleDeclaration(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process role declaration");
        }
    }

    private void OnGroundEffectPlacedReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as GroundEffectPlacedMessage;
            if (message != null)
            {
                _service.HandleRemoteGroundEffectPlaced(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process ground effect placed");
        }
    }

    private void OnRaiseIntentReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as RaiseIntentMessage;
            if (message != null)
            {
                _service.HandleRemoteRaiseIntent(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process raise intent");
        }
    }

    private void OnCleanseIntentReceived(string json)
    {
        try
        {
            var message = PartyMessage.FromJson(json) as CleanseIntentMessage;
            if (message != null)
            {
                _service.HandleRemoteCleanseIntent(message);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to process cleanse intent");
        }
    }

    #endregion

    public void Dispose()
    {
        // Unsubscribe from service events
        _service.OnHeartbeatReady -= SendHeartbeat;
        _service.OnHealIntentReady -= SendHealIntent;
        _service.OnHealLandedReady -= SendHealLanded;
        _service.OnCooldownUsedReady -= SendCooldownUsed;
        _service.OnAoEHealIntentReady -= SendAoEHealIntent;
        _service.OnRaidBuffIntentReady -= SendRaidBuffIntent;
        _service.OnBurstWindowStartReady -= SendBurstWindowStart;
        _service.OnGaugeStateReady -= SendGaugeState;
        _service.OnRoleDeclarationReady -= SendRoleDeclaration;
        _service.OnGroundEffectPlacedReady -= SendGroundEffectPlaced;
        _service.OnRaiseIntentReady -= SendRaiseIntent;
        _service.OnCleanseIntentReady -= SendCleanseIntent;

        // Unregister IPC handlers
        _heartbeatProvider.UnregisterAction();
        _healIntentProvider.UnregisterAction();
        _healLandedProvider.UnregisterAction();
        _cooldownUsedProvider.UnregisterAction();
        _aoEHealIntentProvider.UnregisterAction();
        _raidBuffIntentProvider.UnregisterAction();
        _burstWindowStartProvider.UnregisterAction();
        _gaugeStateProvider.UnregisterAction();
        _roleDeclarationProvider.UnregisterAction();
        _groundEffectPlacedProvider.UnregisterAction();
        _raiseIntentProvider.UnregisterAction();
        _cleanseIntentProvider.UnregisterAction();

        _log.Info("Party coordination IPC disposed");
    }
}
