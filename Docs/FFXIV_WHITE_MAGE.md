# FFXIV White Mage Reference

Job-specific reference for White Mage (WHM) rotation development.

## Job Overview

White Mage is a pure healer with strong reactive healing and the Lily system for resource-free GCD heals.

**Job IDs**: WHM = 24, CNJ = 6

## Lily System

### Lily Gauge
- **Max Lilies**: 3
- **Generation**: 1 Lily every 20 seconds (in combat)
- **Usage**: Afflatus Solace (ST) or Afflatus Rapture (AoE)

### Blood Lily
- **Generation**: 1 Blood Lily per Lily heal used
- **Max**: 3 Blood Lilies
- **Usage**: Afflatus Misery (1240p AoE damage)

### Lily Strategy
| Strategy | Description |
|----------|-------------|
| Aggressive | Use lilies freely for any healing |
| Balanced | Prefer lilies, build toward Misery |
| Conservative | Save lilies for emergencies |

### Lily Cap Prevention
- At 3/3 Lilies, new lilies are wasted
- Use a lily heal on anyone with any damage
- ~60 seconds of generation lost per wasted lily

## GCD Heals

### Single Target
| Spell | Potency | Cast | MP | Notes |
|-------|---------|------|-----|-------|
| Cure | 500 | 1.5s | 400 | Freecure proc (15%) |
| Cure II | 800 | 2.0s | 1000 | Main ST heal |
| Regen | 250 + 250×5 | Instant | 400 | 18s HoT |
| Afflatus Solace | 800 | Instant | 0 | Lily cost |

### AoE
| Spell | Potency | Cast | MP | Notes |
|-------|---------|------|-----|-------|
| Medica | 400 | 2.0s | 900 | Basic AoE |
| Medica II | 250 + 150×5 | 2.0s | 1000 | AoE HoT |
| Medica III | 600 | 2.0s | 1000 | Upgraded Medica |
| Cure III | 600 | 2.0s | 1500 | Stack heal (6y) |
| Afflatus Rapture | 400 | Instant | 0 | Lily cost |

## oGCD Heals

### Single Target
| Ability | Potency | CD | Notes |
|---------|---------|-----|-------|
| Tetragrammaton | 700 | 60s | Primary oGCD |
| Benediction | Full HP | 180s | Emergency heal |
| Divine Benison | Shield 500 | 30s | 15s shield |

### AoE
| Ability | Potency | CD | Notes |
|---------|---------|-----|-------|
| Assize | 400 heal + 400 dmg | 40s | Dual purpose |
| Asylum | 100×5 + 10% heal buff | 90s | Ground AoE HoT |
| Plenary Indulgence | +200 to Medica/Cure III | 60s | Confession stacks |
| Liturgy of the Bell | 400×5 | 180s | Reactive healing |

### Mitigation
| Ability | Effect | CD | Notes |
|---------|--------|-----|-------|
| Temperance | 10% heal up, 10% mit | 120s | Party-wide |
| Aquaveil | 15% mit, removes 1 hit | 60s | Single target |
| Divine Caress | HoT after Temperance | - | Temperance upgrade |

## Damage Abilities

### GCD Damage
| Spell | Potency | Cast | MP | Notes |
|-------|---------|------|-----|-------|
| Glare III | 310 | 1.5s | 400 | Main filler |
| Glare IV | 640 | Instant | 400 | Proc from Dia |
| Holy III | 150 | 2.5s | 400 | AoE + 4s stun |
| Afflatus Misery | 1240 | Instant | 0 | Blood Lily spender |

### DoT
| Spell | Potency | Duration | Notes |
|-------|---------|----------|-------|
| Dia | 75 + 75×10 | 30s | Refresh at <3s |

## Buffs

### Self Buffs
| Ability | Effect | CD | Notes |
|---------|--------|-----|-------|
| Presence of Mind | 20% spell speed | 120s | DPS window |
| Thin Air | Next spell free | 60s | MP conservation |
| Swiftcast | Instant next cast | 60s | Role action |

### Utility
| Ability | Effect | CD | Notes |
|---------|--------|-----|-------|
| Lucid Dreaming | MP regen | 60s | Use at ~70% MP |
| Surecast | Knockback immunity | 120s | Role action |
| Rescue | Pull ally | 120s | Role action |
| Esuna | Cleanse debuff | - | Role action |

## Rotation Priority

### Combat Loop
```
1. Check for deaths → Raise if needed
2. Emergency heal → Benediction/Tetra if dying
3. Esuna → Cleanse dangerous debuffs
4. oGCD heals → Use in weave windows
5. HoT maintenance → Keep Regen rolling
6. GCD heals → If HP thresholds breached
7. DoT → Refresh Dia if <3s
8. Damage → Glare III filler
```

### Weave Windows
- After Dia (instant) → Double weave
- After Afflatus heals (instant) → Double weave
- After Glare III (1.5s cast) → Single weave

## Status Effect IDs

```csharp
// WHM-specific statuses
public const uint Status_Regen = 158;
public const uint Status_MedicaII = 150;
public const uint Status_MedicaIII = 3986;
public const uint Status_Dia = 1871;
public const uint Status_DivineBenison = 1218;
public const uint Status_Aquaveil = 2708;
public const uint Status_Temperance = 1872;
public const uint Status_PresenceOfMind = 157;
public const uint Status_ThinAir = 1217;
public const uint Status_Freecure = 155;
```

## Common Scenarios

### Tank Buster
1. Pre-shield with Divine Benison
2. Have Tetra ready for after-hit
3. Benediction if HP drops critical
4. Regen for recovery

### Raidwide Damage
1. Pre-Medica II for HoT
2. Assize during damage (heals + damages)
3. Afflatus Rapture for instant AoE
4. Cure III if stacked

### MP Emergency
1. Use Thin Air on expensive cast
2. Prioritize Lily heals (free)
3. Lucid Dreaming immediately
4. Downgrade to Cure if desperate
