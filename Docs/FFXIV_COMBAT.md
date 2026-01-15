# FFXIV Combat Mechanics

Reference document for FFXIV combat system mechanics relevant to rotation development.

## Global Cooldown (GCD)

The GCD is the primary pacing mechanism in FFXIV combat.

### Base GCD
- **Default**: 2.5 seconds
- **Affected by**: Spell Speed (casters/healers), Skill Speed (physical)
- **Minimum**: ~2.0s with high Speed stats

### GCD Recast Group
- **Group ID**: 57 (hardcoded by the game)
- **Check via**: `ActionManager->GetRecastGroupDetail(57)`
- All GCD actions share this recast timer

### Instant GCDs
Some GCDs are instant-cast but still trigger GCD:
- Lily heals (Afflatus Solace/Rapture)
- Procs (Freecure → free Cure II)
- Movement abilities with GCD cost

## Off-Global Cooldowns (oGCDs)

### Weave Windows
- **Single weave**: 0.7s after instant GCD
- **Double weave**: Up to 1.4s of oGCDs
- **Clipping**: Using oGCDs that delay the next GCD

### Animation Lock
- **Duration**: ~0.6-0.8s per oGCD
- **Check via**: `ActionManager->AnimationLock`
- Cannot queue actions during animation lock

### Best Practices
1. Use oGCDs after instant GCDs when possible
2. Avoid clipping the GCD with slow oGCDs
3. Plan oGCDs around movement requirements

## Snapshotting

FFXIV uses server-side snapshotting for damage/healing.

### How It Works
1. Action is executed (client-side)
2. Server receives action at timestamp T
3. Buffs/debuffs active at T determine effect
4. Damage/healing calculated using stats at T
5. Effect applies to targets in range at T

### Implications
- Buffs must be active when cast **completes**, not starts
- Moving out of AoE after snapshot still takes damage
- DoTs snapshot stats at application time

### Timing
- Cast bar completion = snapshot point
- Network latency affects apparent timing
- ~50-100ms buffer is generally safe

## Targeting

### Target Types
| Type | Description |
|------|-------------|
| Hostile | Attackable enemies |
| Friendly | Party members, NPCs |
| Ground | AoE placement targets |

### Target Validation
```csharp
// Check if target is valid for action
if (target.ObjectKind != ObjectKind.BattleNpc)
    return false;

if ((target.StatusFlags & 128) != 0)  // Hostile/untargetable
    return false;
```

### Range Checks
- **Melee**: 3y (yalms)
- **Standard ranged**: 25y
- **AoE radius**: Varies by spell (6y, 15y, 20y, etc.)

## Status Effects

### Application
- Buffs/debuffs apply after action resolves
- Duration countdown starts immediately
- Some effects have "application delay"

### Refresh Rules
- Most effects can be refreshed before expiry
- Some have minimum remaining time before refresh
- Overwriting with weaker effect is possible

### Status IDs
Status effects have unique IDs per effect:
- Regen: 158
- Medica II HoT: 150
- Dia DoT: 1871

Check `Data/ActionIds.cs` for common status IDs.

## Damage Calculation

### Base Formula
```
Damage = Potency × JobMod × MainStat × DetMod × CritMod × DHMod × BuffMod
```

### Healing Formula
```
Healing = Potency × JobMod × MND × DetMod × CritMod × BuffMod
```

### Potency
- Base effectiveness measure
- 100 potency = baseline
- Higher potency = more damage/healing

## Combat State

### In Combat Detection
```csharp
// Check player combat status
var inCombat = (player.StatusFlags & StatusFlags.InCombat) != 0;
```

### Combat Events
- Combat starts when any party member engages
- Combat ends ~5-10s after last enemy dies
- Some content has special combat flags

## MP Management

### MP Costs (Healers)
- GCD heals: 400-1000 MP
- Damage spells: 200-400 MP
- oGCDs: Usually free

### MP Recovery
- **Natural regen**: ~200 MP/tick (3s)
- **Lucid Dreaming**: +55 MP/tick for 21s
- **Combat state**: Regen rates vary

## Frame Timing

### Update Loop
- Plugin runs every frame (~60fps = 16ms budget)
- Combat decisions must complete in <1ms
- Avoid allocations in hot path

### Timing Considerations
```csharp
// Bad: Creates garbage every frame
var targets = party.Where(x => x.Hp < x.MaxHp).ToList();

// Good: Pre-allocated, reused
for (int i = 0; i < partyMembers.Length; i++)
{
    if (partyMembers[i].Hp < partyMembers[i].MaxHp)
        // process
}
```
