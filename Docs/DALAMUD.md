# Dalamud Plugin Development Reference

Guide for Dalamud plugin architecture and lifecycle.

## Plugin Lifecycle

### Initialization
```csharp
public sealed class Plugin : IDalamudPlugin
{
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IObjectTable objectTable,
        // ... other services injected automatically
    )
    {
        // Constructor called when plugin loads
        // Initialize services, subscribe to events
    }
}
```

### Disposal
```csharp
public void Dispose()
{
    // CRITICAL: Unsubscribe all events
    framework.Update -= OnFrameworkUpdate;

    // Dispose managed resources
    foreach (var disposable in _disposables)
        disposable.Dispose();

    // Save configuration
    pluginInterface.SavePluginConfig(configuration);
}
```

### Hot Reload Safety
If events aren't unsubscribed, the plugin will crash on reload:
```csharp
// MUST unsubscribe in Dispose()
pluginInterface.UiBuilder.Draw -= DrawUI;
pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUI;
```

## Dependency Injection

Dalamud automatically injects services into the plugin constructor.

### Common Services
| Service | Purpose |
|---------|---------|
| `IDalamudPluginInterface` | Plugin API, config, UI |
| `IFramework` | Game loop, timing |
| `IObjectTable` | Game objects, players |
| `IPartyList` | Party members |
| `IClientState` | Player state, login status |
| `IPluginLog` | Logging |
| `ICommandManager` | Chat commands |
| `IChatGui` | Chat output |
| `IDataManager` | Game data sheets |
| `ICondition` | Game conditions |
| `ITargetManager` | Current target |
| `IGameInteropProvider` | Hooks, memory |

### Usage Pattern
```csharp
public Plugin(
    IPluginLog log,
    IObjectTable objectTable,
    IPartyList partyList)
{
    this.log = log;
    this.objectTable = objectTable;
    this.partyList = partyList;
}
```

## Configuration

### Implementing IPluginConfiguration
```csharp
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Settings
    public bool Enabled { get; set; } = false;
    public float Threshold { get; set; } = 0.5f;
}
```

### Loading/Saving
```csharp
// Load (in constructor)
var config = pluginInterface.GetPluginConfig() as Configuration
    ?? new Configuration();

// Save
pluginInterface.SavePluginConfig(configuration);
```

### Version Migration
```csharp
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    // Called by serializer - migrate old configs
    public void OnDeserialized()
    {
        if (Version < 2)
        {
            // Migrate from v1 to v2
            Version = 2;
        }
    }
}
```

## Framework Events

### Update Loop
```csharp
framework.Update += OnFrameworkUpdate;

private void OnFrameworkUpdate(IFramework framework)
{
    // Called every frame (~60fps)
    // Keep execution time <1ms

    if (!clientState.IsLoggedIn)
        return;

    var player = objectTable.LocalPlayer;
    if (player == null)
        return;

    // Main logic here
}
```

### Event Timing
- `Update`: Every frame, main game loop
- `UiBuilder.Draw`: Every frame, for ImGui
- `Condition.ConditionChange`: When conditions change

## UI (ImGui)

### Drawing Windows
```csharp
pluginInterface.UiBuilder.Draw += DrawUI;

private void DrawUI()
{
    if (!clientState.IsLoggedIn)
        return;

    windowSystem.Draw();
}
```

### Window System
```csharp
private readonly WindowSystem windowSystem = new("PluginName");

public Plugin(...)
{
    var mainWindow = new MainWindow();
    windowSystem.AddWindow(mainWindow);
}
```

### Window Class
```csharp
public class MainWindow : Window
{
    public MainWindow() : base("Window Title")
    {
        Size = new Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        ImGui.Text("Hello World");
    }
}
```

## Commands

### Registering Commands
```csharp
commandManager.AddHandler("/mycommand", new CommandInfo(OnCommand)
{
    HelpMessage = "Description of command"
});

private void OnCommand(string command, string args)
{
    var arg = args.Trim().ToLowerInvariant();
    // Handle command
}
```

### Cleanup
```csharp
public void Dispose()
{
    commandManager.RemoveHandler("/mycommand");
}
```

## Logging

### Log Levels
```csharp
log.Verbose("Detailed debug info");  // Highest verbosity
log.Debug("Debug information");
log.Information("Normal operation");
log.Warning("Potential issue");
log.Error("Error occurred");
log.Fatal("Critical failure");
```

### Structured Logging
```csharp
// Good - structured with placeholders
log.Debug("Executed {ActionId} on {TargetId}", actionId, targetId);

// Bad - string concatenation
log.Debug("Executed " + actionId + " on " + targetId);
```

## Object Table

### Finding Objects
```csharp
// Local player
var player = objectTable.LocalPlayer;

// By ID
var obj = objectTable.SearchById(entityId);

// All objects
foreach (var obj in objectTable)
{
    if (obj.ObjectKind == ObjectKind.BattleNpc)
    {
        // Process NPC
    }
}
```

### Object Types
```csharp
// Check object type
if (obj is IPlayerCharacter pc)
{
    // Player character
}
else if (obj is IBattleNpc npc)
{
    // Battle NPC
}
```

## Party Access

### Party Members
```csharp
foreach (var member in partyList)
{
    var name = member.Name;
    var hp = member.CurrentHP;
    var maxHp = member.MaxHP;
    var entityId = member.EntityId;

    // Get full game object
    var character = member.GameObject;
}
```

### Solo Play
```csharp
// When not in party, partyList is empty
// Use objectTable.LocalPlayer directly
if (partyList.Length == 0)
{
    var player = objectTable.LocalPlayer;
    // Handle solo
}
```

## IPC (Inter-Plugin Communication)

### Providing Functions
```csharp
// Register callable function
pluginInterface.GetIpcProvider<bool>("MyPlugin.IsEnabled")
    .RegisterFunc(() => configuration.Enabled);

// Register action
pluginInterface.GetIpcProvider<bool, bool>("MyPlugin.SetEnabled")
    .RegisterAction((enabled) => configuration.Enabled = enabled);
```

### Calling Other Plugins
```csharp
var subscriber = pluginInterface.GetIpcSubscriber<bool>("OtherPlugin.IsEnabled");
var isEnabled = subscriber.InvokeFunc();
```

### Cleanup
```csharp
public void Dispose()
{
    // Unregister IPC
    pluginInterface.GetIpcProvider<bool>("MyPlugin.IsEnabled").UnregisterFunc();
}
```

## Best Practices

### Performance
1. Keep Update handler <1ms execution
2. Avoid allocations in hot path
3. Cache frequently accessed data per-frame
4. Use early returns for common cases

### Stability
1. Always null-check game data
2. Handle missing/invalid data gracefully
3. Log errors with context
4. Save config periodically

### User Experience
1. Provide clear error messages
2. Include configuration validation
3. Document all settings
4. Respect user preferences
