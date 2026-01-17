# FFXIV Monk Reference

Job-specific reference for Monk (MNK) rotation development.

## Job Overview

Monk is a melee DPS with a unique form system and positional requirements. The job cycles through three forms, building Beast Chakra to unleash powerful Blitz attacks.

**Job IDs**: MNK = 20, PGL = 2

## Form System

### The Three Forms
Monk cycles through forms in a fixed sequence:

```
Opo-opo → Raptor → Coeurl → (back to Opo-opo)
```

Each form has specific GCDs:

| Form | Single Target | AoE | Positional |
|------|--------------|-----|------------|
| Opo-opo | Bootshine/Dragon Kick | Arm of the Destroyer | Rear (Boot) / Flank (DK) |
| Raptor | True Strike/Twin Snakes | Four-point Fury | Rear (TS) / Flank (Twin) |
| Coeurl | Snap Punch/Demolish | Rockbreaker | Flank (SP) / Rear (Demo) |

### Form Buffs
- Each form GCD grants the next form's buff
- Form buffs last 30 seconds
- Using the wrong form GCD loses DPS (no Beast Chakra granted)

### Formless Fist
- Granted by Form Shift or Perfect Balance
- Allows any form GCD to be used
- Still respects positional requirements

## Beast Chakra System

### Beast Chakra Types
```csharp
public enum BeastChakraType : byte
{
    None = 0,
    OpoOpo = 1,   // From Opo-opo form GCDs
    Raptor = 2,   // From Raptor form GCDs
    Coeurl = 3    // From Coeurl form GCDs
}
```

### Accumulation Rules
- Each form GCD grants 1 Beast Chakra of that form's type
- Max 3 Beast Chakra stored
- At 3 Beast Chakra, Masterful Blitz becomes available
- Beast Chakra are consumed when executing a Blitz

## Nadi System

### Nadi Types
```csharp
[Flags]
public enum NadiFlags : byte
{
    None = 0,
    Lunar = 1,  // From Elixir Field/Burst (all same chakra)
    Solar = 2   // From Flint Strike/Rising Phoenix (all different chakra)
}
```

### Nadi Generation
| Blitz Type | Chakra Pattern | Nadi Granted |
|------------|----------------|--------------|
| Elixir Field/Burst | 3 identical | Lunar |
| Flint Strike/Rising Phoenix | 3 different | Solar |
| Celestial Revolution | 2 same + 1 different | None |

### Phantom Rush
- Requires **both** Lunar and Solar Nadi
- Most powerful Blitz (1150 potency)
- Consumes both Nadi after use

## Masterful Blitz Selection

The Blitz action is determined by Beast Chakra pattern and Nadi state:

```
if (hasLunar && hasSolar && level >= 90):
    return PhantomRush
else if (all 3 chakra identical):
    return level >= 92 ? ElixirBurst : ElixirField
else if (all 3 chakra different):
    return level >= 86 ? RisingPhoenix : FlintStrike
else:
    return CelestialRevolution  // 2 same + 1 different
```

### Optimal Blitz Rotation
For Phantom Rush access:
1. Perfect Balance → 3x same form GCD → Lunar Blitz
2. Perfect Balance → 3x different form GCDs → Solar Blitz
3. With both Nadi → Phantom Rush

## Chakra (5-Chakra) System

Separate from Beast Chakra, this is a resource gauge:

| Max | Generation | Spenders |
|-----|------------|----------|
| 5 | Critical hits, Brotherhood, Meditation | The Forbidden Chakra (ST), Enlightenment (AoE) |

### Usage
- Spend at 5 Chakra (don't overcap)
- Use between GCDs (oGCD)
- Brotherhood generates Chakra from party members' weaponskills

## Positional Requirements

### Positional Bonus
- **Rear**: Behind the target
- **Flank**: To the side of the target
- Missing positional loses ~50-60 potency per GCD

### Actions by Position
| Position | Actions |
|----------|---------|
| Rear | Bootshine (crit bonus), True Strike, Demolish |
| Flank | Dragon Kick, Twin Snakes, Snap Punch |

### True North
- 10s buff (2 charges, 45s recharge)
- All positionals count as correct
- Use for unavoidable mispositioning

## Key Actions

### GCDs (Form Rotation)
| Action | ID | Level | Potency | Notes |
|--------|-----|-------|---------|-------|
| Bootshine | 53 | 1 | 210 | Rear: guaranteed crit |
| Dragon Kick | 74 | 50 | 320 | Flank: grants Leaden Fist |
| True Strike | 54 | 4 | 300 | Rear positional |
| Twin Snakes | 61 | 18 | 280 | Flank: grants Disciplined Fist |
| Snap Punch | 56 | 6 | 310 | Flank positional |
| Demolish | 66 | 30 | 130+DoT | Rear: 70p×6 DoT |

### Level 96+ Enhanced GCDs
| Action | ID | Potency | Notes |
|--------|-----|---------|-------|
| Leaping Opo | 36945 | 260 | No positional |
| Rising Raptor | 36946 | 340 | No positional |
| Pouncing Coeurl | 36947 | 370 | No positional |

### Masterful Blitz Actions
| Action | ID | Level | Potency | Chakra Pattern |
|--------|-----|-------|---------|----------------|
| Elixir Field | 3545 | 60 | 600 | 3 identical |
| Elixir Burst | 36948 | 92 | 800 | 3 identical (upgraded) |
| Flint Strike | 25882 | 60 | 600 | 3 different |
| Rising Phoenix | 25768 | 86 | 700 | 3 different (upgraded) |
| Phantom Rush | 25769 | 90 | 1150 | Both Nadi |

### Reply Actions (Level 100)
| Action | ID | Category | Trigger |
|--------|-----|----------|---------|
| Wind's Reply | 36949 | GCD | After Phantom Rush |
| Fire's Reply | 36950 | GCD | After Riddle of Fire ends |

### Buffs
| Action | ID | Level | CD | Effect |
|--------|-----|-------|-----|--------|
| Riddle of Fire | 7395 | 68 | 60s | +15% damage for 20s |
| Brotherhood | 7396 | 70 | 120s | +5% party damage, Chakra gen |
| Perfect Balance | 69 | 50 | 40s (2 charges) | Formless for 3 GCDs |
| Riddle of Wind | 25766 | 72 | 90s | Speed buff + 400p damage |

## Status Effect IDs

```csharp
public static class StatusIds
{
    // Form statuses
    public const uint OpoOpoForm = 107;
    public const uint RaptorForm = 108;
    public const uint CoeurlForm = 109;
    public const uint FormlessFist = 2513;

    // Damage buffs
    public const uint LeadenFist = 1861;
    public const uint DisciplinedFist = 3001;
    public const uint RiddleOfFire = 1181;
    public const uint Brotherhood = 1182;
    public const uint BrotherhoodMeditative = 2173;
    public const uint PerfectBalance = 110;
    public const uint RiddleOfWind = 2687;

    // Fury procs (Lv.96+)
    public const uint RaptorsFury = 3848;
    public const uint CoeurlsFury = 3849;
    public const uint OpooposFury = 3850;

    // Reply procs (Lv.100)
    public const uint FiresRumination = 3847;
    public const uint WindsRumination = 3846;

    // DoT
    public const uint Demolish = 246;

    // Defensive/Utility
    public const uint RiddleOfEarth = 1179;
    public const uint Mantra = 102;
    public const uint TrueNorth = 1250;
    public const uint Bloodbath = 84;
}
```

## Rotation Priority

### Single Target Combat Loop
```
1. Use Masterful Blitz if 3 Beast Chakra
2. Use Fire's Reply if proc active
3. Use Wind's Reply if proc active
4. Maintain Disciplined Fist buff (15s)
5. Maintain Demolish DoT (18s)
6. Use form GCDs in sequence (Opo → Raptor → Coeurl)
7. Spend 5 Chakra with The Forbidden Chakra
8. Use Perfect Balance in burst windows
```

### oGCD Weave Priority
```
1. Riddle of Fire (align with burst)
2. Brotherhood (align with party buffs)
3. Perfect Balance (during RoF)
4. Riddle of Wind
5. The Forbidden Chakra (5 Chakra)
```

## Common Scenarios

### Opener Sequence
1. Form Shift → Pre-pull
2. Riddle of Fire + Brotherhood
3. Perfect Balance → 3 different GCDs → Solar Blitz
4. Perfect Balance → 3 same GCDs → Lunar Blitz
5. Build to Phantom Rush

### Perfect Balance Windows
- Use during Riddle of Fire for burst
- Alternate between Lunar (3 same) and Solar (3 different)
- Build toward Phantom Rush as priority

### Positional Unavailable
1. Pop True North before form GCD
2. Use Lv.96+ enhanced GCDs (no positional)
3. Accept potency loss if neither available
