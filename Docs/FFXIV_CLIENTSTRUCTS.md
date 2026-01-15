# FFXIVClientStructs Reference

Guide for working with game memory through FFXIVClientStructs.

## Overview

FFXIVClientStructs provides managed wrappers around FFXIV's native game structures. All access is `unsafe` and requires careful handling.

## Safety Rules

### Critical Rules
1. **Always null-check** pointers before dereferencing
2. **Never cache pointers** across frames (they can become invalid)
3. **Keep unsafe code contained** in Service classes
4. **Return managed types** from Service methods

### Code Patterns

**Good: Safe access in Service**
```csharp
public class ActionService
{
    public bool IsGcdReady()
    {
        unsafe
        {
            var actionManager = ActionManager.Instance();
            if (actionManager == null)
                return false;

            if (actionManager->AnimationLock > 0)
                return false;

            var gcdRecast = actionManager->GetRecastGroupDetail(57);
            return gcdRecast == null || gcdRecast->IsActive == 0;
        }
    }
}
```

**Bad: Cached pointer**
```csharp
// DON'T DO THIS - pointer may become invalid
private ActionManager* _cachedManager;

public void Init()
{
    _cachedManager = ActionManager.Instance();  // DANGEROUS
}
```

## ActionManager

Primary interface for action execution and cooldown checking.

### Getting Instance
```csharp
unsafe
{
    var actionManager = ActionManager.Instance();
    if (actionManager == null)
        return;  // Loading screen, etc.
}
```

### Checking GCD
```csharp
// Group 57 = GCD recast group (hardcoded by game)
var gcdRecast = actionManager->GetRecastGroupDetail(57);
if (gcdRecast != null && gcdRecast->IsActive != 0)
{
    // GCD is on cooldown
    var remaining = gcdRecast->Total - gcdRecast->Elapsed;
}
```

### Animation Lock
```csharp
// Animation lock prevents queueing actions
if (actionManager->AnimationLock > 0)
    return false;  // Can't use action yet
```

### Action Status
```csharp
var status = actionManager->GetActionStatus(ActionType.Action, actionId);
// 0 = usable
// Non-zero = error code (out of range, not learned, etc.)
```

### Using Actions
```csharp
// Execute action on target
var success = actionManager->UseAction(
    ActionType.Action,
    actionId,
    targetId
);
```

## Character Data

### Local Player
```csharp
var player = objectTable.LocalPlayer;
if (player == null)
    return;

// Safe managed properties
var hp = player.CurrentHp;
var maxHp = player.MaxHp;
var mp = player.CurrentMp;
var jobId = player.ClassJob.RowId;
```

### Status Effects
```csharp
// Check for a specific buff
foreach (var status in player.StatusList)
{
    if (status.StatusId == StatusIds.Regen)
    {
        var remaining = status.RemainingTime;
        var stacks = status.StackCount;
    }
}
```

### Combat State
```csharp
var inCombat = (player.StatusFlags & StatusFlags.InCombat) != 0;
```

## Party Data

### Party List
```csharp
foreach (var member in partyList)
{
    var hp = member.CurrentHP;
    var maxHp = member.MaxHP;
    var entityId = member.EntityId;
    var gameObjectId = member.GameObject?.GameObjectId ?? 0;
}
```

### Object Table Access
```csharp
// Get full character data from party member
var partyMember = partyList[0];
var character = objectTable.SearchById(partyMember.GameObjectId);
if (character is IPlayerCharacter pc)
{
    // Access full player character data
}
```

## Gauge Data

### WHM Gauge (Lily System)
```csharp
unsafe
{
    var gauge = JobGauges.Get<WHMGauge>();
    var lilies = gauge.Lily;           // 0-3
    var bloodLily = gauge.BloodLily;   // 0-3
    var lilyTimer = gauge.LilyTimer;   // ms until next lily
}
```

### Other Healer Gauges
```csharp
// SCH
var schGauge = JobGauges.Get<SCHGauge>();
var aetherflow = schGauge.Aetherflow;
var fairyGauge = schGauge.FairyGauge;

// AST
var astGauge = JobGauges.Get<ASTGauge>();
var card = astGauge.DrawnCard;

// SGE
var sgeGauge = JobGauges.Get<SGEGauge>();
var addersgall = sgeGauge.Addersgall;
var addersting = sgeGauge.Addersting;
```

## Game Framework

### Framework Update
```csharp
// Called every frame
framework.Update += OnFrameworkUpdate;

private void OnFrameworkUpdate(IFramework framework)
{
    // ~60fps = 16ms budget
    // Keep execution time <1ms
}
```

### Timing
```csharp
// Get elapsed time
var deltaTime = framework.UpdateDelta.TotalSeconds;
```

## Common Pitfalls

### Null Pointer Dereference
```csharp
// WRONG - may crash
var status = actionManager->GetActionStatus(...);

// RIGHT - check first
var actionManager = ActionManager.Instance();
if (actionManager == null)
    return;
var status = actionManager->GetActionStatus(...);
```

### Invalid Target
```csharp
// Game uses special invalid ID
public const uint InvalidTargetId = 0xE0000000;

if (targetId == InvalidTargetId)
    return false;  // No valid target
```

### Status List Iteration
```csharp
// StatusList can be modified during iteration
// Copy or use index-based iteration for safety

// Safe approach
for (int i = 0; i < player.StatusList.Length; i++)
{
    var status = player.StatusList[i];
    // process
}
```

## Performance Considerations

### Memory Access
- Pointer dereferencing is fast but requires null checks
- Minimize number of unsafe calls per frame
- Cache results in managed variables when safe

### Allocation Avoidance
```csharp
// BAD - allocates each frame
var targets = party.Where(x => x.Hp < x.MaxHp).ToList();

// GOOD - pre-allocated array
private readonly IPartyMember[] _healTargets = new IPartyMember[8];

private int FindHealTargets()
{
    int count = 0;
    foreach (var member in partyList)
    {
        if (member.CurrentHP < member.MaxHP)
            _healTargets[count++] = member;
    }
    return count;
}
```
