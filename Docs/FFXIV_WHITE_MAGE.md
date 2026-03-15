# FFXIV White Mage Reference

Job-specific reference for White Mage (WHM) rotation development.

## Job Overview

White Mage is a pure healer with strong reactive healing, the Lily system for resource-free GCD heals, and significant personal DPS throughput. It brings no party buffs but contributes heavily through efficient MP-free healing and high-potency DoT/Glare uptime.

**Job IDs**: WHM = 24, CNJ = 6

## Lily System

### Lily Gauge
- **Max Lilies**: 3
- **Generation**: 1 Lily every 20 seconds (in combat only)
- **Usage**: Afflatus Solace (ST) or Afflatus Rapture (AoE) — costs 1 Lily
- **MP Cost**: 0 (Lily heals are free)

### Blood Lily
- **Generation**: 1 Blood Lily per Lily heal used (not per lily consumed — one Lily heal = one Blood Lily increment)
- **Max**: 3 Blood Lilies = Afflatus Misery ready
- **Usage**: Afflatus Misery (1240p AoE damage, 5y radius)

### Lily Strategy
| Strategy | Description |
|----------|-------------|
| Aggressive | Use lilies freely for any healing need |
| Balanced | Prefer lilies, build toward Misery |
| Conservative | Save lilies for emergencies |

### Lily Cap Prevention
- At 3/3 Lilies, new lilies are wasted (60 seconds of generation lost per cap)
- Use a lily heal on anyone with any damage to avoid waste
- In extreme cases, use Afflatus Rapture on a lightly-injured party member

### Blood Lily Cycle
The lily cycle is central to WHM DPS:
1. Use Afflatus Solace/Rapture when healing is needed (free heals)
2. After 3 lily heals, use Afflatus Misery (1240p AoE instant)
3. Misery is always a gain — the DPS "cost" of lily heals is entirely refunded by Misery
4. Lily heals are therefore functionally free from a DPS perspective

## GCD Heals

### Single Target
| Spell | Potency | Cast | MP | Notes |
|-------|---------|------|-----|-------|
| Cure | 500 | 1.5s | 400 | 15% chance of Freecure proc (free Cure II) |
| Cure II | 800 | 2.0s | 1000 | Primary ST heal |
| Regen | 250×6 (1500p total) | Instant | 400 | 18s HoT — superior to Cure II if target survives ~6s |
| Afflatus Solace | 800 | Instant | 0 | Lily cost, builds Blood Lily |

### AoE
| Spell | Potency | Cast | MP | Notes |
|-------|---------|------|-----|-------|
| Medica | 400 | 2.0s | 1000 | Basic AoE, use when Medica III regen already active |
| Medica II | 200 + 150×5 (950p) | 2.0s | 1300 | AoE + 15s HoT — obsoleted by Medica III at lvl 96 |
| Medica III | 300 + 150×5 (1050p) | 2.0s | 1300 | **Replaces Medica II at level 96** — strictly better in all cases |
| Cure III | 600 | 2.0s | 1500 | Stack-targeted AoE (10y radius around target) |
| Afflatus Rapture | 400 | Instant | 0 | AoE lily heal, builds Blood Lily |

**Medica III vs Medica II**: Medica III is always preferred at level 96+. It has higher direct potency and equal or higher total healing per cast. Medica (basic) is only used when Medica III's regen is already active on all party members and immediate upfront healing is needed.

## oGCD Heals

### Single Target
| Ability | Potency | CD | Notes |
|---------|---------|-----|-------|
| Tetragrammaton | 700 | 60s | Primary oGCD heal |
| Benediction | Full HP | 180s | Emergency heal — restores 100% of target's max HP |
| Divine Benison | 500 shield | 30s | 15s shield, 2 charges |

### AoE
| Ability | Potency | CD | Notes |
|---------|---------|-----|-------|
| Assize | 400 heal + 400 dmg + 500 MP | 40s | Dual damage/heal; never delay — use on cooldown |
| Asylum | 100×8 (800p) + 10% heal buff | 90s | Ground AoE HoT, 10y radius, 24s duration |
| Plenary Indulgence | +200 to next Medica/CureIII/Rapture | 60s | Weave BEFORE the AoE heal GCD |
| Liturgy of the Bell | 400 per stack (5 stacks) | 180s | Reactive: heals party when **WHM takes damage** |

### Mitigation
| Ability | Effect | CD | Notes |
|---------|--------|-----|-------|
| Temperance | **20% GCD heal potency** + 10% party mit | 120s | Use before raidwides; healing buff applies to GCDs only |
| Divine Caress | 400 shield + 100×5 HoT | — | Only usable under Divine Grace (from Temperance); up to 30s window |
| Aquaveil | 15% mit, 8s duration | 60s | Best used before tankbusters |

**Temperance note**: The 20% healing buff applies only to GCD heals (Medica, Cure II, etc.) — NOT oGCD heals (Tetragrammaton, Benediction). This is important for timing: GCD heals under Temperance are significantly more efficient.

**Divine Caress note**: The 30-second Divine Grace window allows staggering Divine Caress to cover a second damage event, rather than using it immediately on the first oGCD window after Temperance.

**Liturgy of the Bell note**: The bell triggers when the **WHM** (not party members) takes damage. In raidwides that hit the whole party, the WHM takes damage and the bell fires, healing all party members within 20y of the bell's placed location (not the WHM's current location).

## Damage Abilities

### GCD Damage
| Spell | Potency | Cast | MP | Notes |
|-------|---------|------|-----|-------|
| Glare III | 310 | 1.5s | 400 | Main filler; single weave only |
| Glare IV | 640 | Instant | 400 | Sacred Sight proc from Presence of Mind; double weave window |
| Holy III | 150 | 1.5s | 400 | AoE + stun; skip if Sacred Sight active (Glare IV better) |
| Afflatus Misery | 1240 | Instant | 0 | Blood Lily spender; always use when available |

**Glare IV / Sacred Sight**: Presence of Mind at level 92+ grants 3 stacks of Sacred Sight (30s window). Each Glare IV cast consumes one stack. Use all 3 within the 30s window. Glare IV is instant cast — full double-weave window available.

### DoT
| Spell | Potency | Duration | Notes |
|-------|---------|----------|-------|
| Dia | 65 (upfront) + DoT ticks | 30s | Instant cast — always a double-weave window; refresh at <3s remaining |

**Dia note**: Dia's damage is snapshotted at application. Applying/refreshing during raid buff windows captures the buff on all remaining ticks. Dia's instant cast provides a free double-weave window every 30 seconds.

## Buffs

### Self Buffs
| Ability | Effect | CD | Notes |
|---------|--------|-----|-------|
| Presence of Mind | 20% spell speed (recast reduction), grants 3× Sacred Sight | 120s | Align with raid buff windows (every 2m) |
| Thin Air | Next GCD spell costs 0 MP | 60s/charge | **2 charges** — never let both cap |
| Swiftcast | Instant next cast | 60s | Priority: Raise > movement tool > emergency heal |

### Utility
| Ability | Effect | CD | Notes |
|---------|--------|-----|-------|
| Lucid Dreaming | 3,850 MP over 21s | 60s | Use at ~70-80% MP, never delay |
| Assize | +500 MP (plus heal/damage) | 40s | Primary MP sustain tool — use on cooldown |
| Surecast | Knockback immunity | 120s | Role action |
| Rescue | Pull ally | 120s | Role action |
| Esuna | Cleanse dispellable debuff | — | Role action |

## Rotation Priority

### Combat Loop
```
1. Check for deaths → Raise if needed (Swiftcast + Thin Air → Raise)
2. Emergency heal → Benediction if dying
3. Esuna → Cleanse dangerous debuffs
4. oGCD heals → Tetragrammaton, Divine Benison, Divine Caress
5. Temperance/Liturgy/Assize → Defensive cooldowns
6. HoT maintenance → Keep Regen rolling on tank
7. GCD heals → If HP thresholds breached
8. DoT → Refresh Dia if <3s remaining
9. Sacred Sight → Use Glare IV stacks immediately
10. Blood Lily → Use Afflatus Misery when at 3/3
11. Damage → Glare III filler
```

### Weaving Windows
- **After Dia (instant)** → Double weave (2 oGCDs)
- **After Glare IV (instant under Sacred Sight)** → Double weave (2 oGCDs)
- **After Afflatus heals (instant)** → Double weave (2 oGCDs)
- **After Glare III (1.5s cast)** → Single weave only (1 oGCD safely)

**Critical**: Stack 2-oGCD combinations (Assize+PresenceOfMind, Assize+Lucid) under instant-cast GCDs (Dia, Glare IV, Afflatus), not under hardcast Glare III.

### Presence of Mind Alignment
- 2-minute cooldown, aligns with party raid buff windows
- At level 92+, using PoM grants 3× Sacred Sight (instant Glare IV stacks)
- Stack PoM with Assize (both ~2m CDs) when possible
- Each Glare IV gives a double-weave window — use to deploy other cooldowns

## Advanced Techniques

### Thin Air Management (2 charges)
1. **Raise** — highest MP savings (2400 MP); always use Thin Air before Swiftcast+Raise
2. **AoE heal incoming** — saves 1000-1500 MP on Medica/CureIII
3. **Single heal** — saves 1000 MP on Cure II
4. **At max charges (both capped)** — spend on ANY GCD cast (including Glare III or Dia) immediately; losing charge regen is worse than "wasting" Thin Air on a 400 MP spell

### MP Management Priority
1. **Assize on cooldown** — +500 MP every 40s; never delay for MP reasons
2. **Lucid Dreaming** — use at 70-80% MP; the regen starts immediately, don't wait
3. **Afflatus heals** — 0 MP cost; every lily heal instead of Cure II saves 1000 MP
4. **Thin Air** — priority order: Raise > AoE heal > ST heal > at-max-cap spending
5. **Medica vs Cure II** — Afflatus Solace (0 MP) is always better than Cure II (1000 MP) when healing is needed

### Swiftcast Priority
1. **Raise** (always, unless no one is dead and zero expected deaths)
2. **Movement tool** if no raise expected in next 60s
3. **Emergency heal** (rare — instant Cure II if Swiftcast + Thin Air are used together)
4. Never use Swiftcast on an Afflatus spell — Afflatus spells are already instant

### Plenary Indulgence Timing
- Weave Plenary Indulgence **immediately before** the AoE heal GCD
- The Confession buff adds 200p to each heal from: Medica, Medica III (upfront only, not regen ticks), Cure III, Afflatus Rapture
- With correct timing: weave Plenary → cast Medica III → Confession fires on the 300p direct hit
- Incorrect timing: Plenary expires before heal lands = no bonus

### Liturgy of the Bell Deployment
- Deploy before multi-hit raidwides (each hit on the WHM triggers one stack)
- Single-hit raidwides waste all remaining stacks (only one fires)
- Bell heals from **placed location**, not WHM position — place on the party stack
- At 20s expiry, remaining stacks trigger for 200p each (weaker than the 400p reactive heal)
- Only one Bell can be active at a time

### Blood Lily → Misery Timing
- Afflatus Misery is always a DPS gain — use immediately when ready
- The DPS cost of 3 Afflatus heals is refunded entirely by Misery's 1240p
- Advanced (optimized): hold Misery for 2-minute party buff window if within ~10s of the window
- Never hold Misery if the fight will end before the buff window arrives

### Regen vs Cure II
- Regen (18s HoT, 250p/tick = 1500p total, instant, 400 MP) is massively more efficient than Cure II (800p, 2s cast, 1000 MP) when the target is not in immediate danger
- Use Regen on the tank proactively; fall back to Cure II only when immediate large healing is needed

### Movement
- **Instant GCDs for free movement**: Dia refresh, Afflatus Solace/Rapture, Glare IV (Sacred Sight), Afflatus Misery
- **Slidecasting**: Glare III's 1.5s cast within a 2.5s recast leaves ~1s of movement after the cast completes
- **Priority**: Dia refresh (if available) → Afflatus heal (if needed) → Glare IV (if stacks) → Slidecast Glare III

## Status Effect IDs

```csharp
// DoT statuses
public const uint Status_Aero = 143;
public const uint Status_AeroII = 144;
public const uint Status_Dia = 1871;

// Medica regen statuses
public const uint Status_MedicaII = 150;
public const uint Status_MedicaIII = 3986;

// Raise
public const uint Status_Raise = 148;

// Damage buffs
public const uint Status_SacredSight = 3879;   // From Presence of Mind (lvl 92+), 3 stacks

// Role/utility buffs
public const uint Status_Swiftcast = 167;
public const uint Status_ThinAir = 1217;
public const uint Status_Freecure = 155;
public const uint Status_Surecast = 160;
public const uint Status_LucidDreaming = 1204;

// Defensive cooldowns / HoT
public const uint Status_DivineBenison = 1218;
public const uint Status_Aquaveil = 2708;
public const uint Status_Temperance = 1872;
public const uint Status_DivineGrace = 3881;   // Enables Divine Caress (30s after Temperance)
public const uint Status_PlenaryIndulgence = 1219;  // Confession debuff on targets
public const uint Status_Regen = 158;
```

## Common Scenarios

### Tank Buster
1. Pre-shield: Divine Benison (30s CD, 2 charges — apply proactively)
2. Pre-mitigation: Aquaveil if tank buster is in <8s
3. After hit: Tetragrammaton for immediate 700p
4. Recovery: Regen if HP is low but stable
5. Emergency: Benediction if tank drops to critical HP

### Raidwide Damage
1. Pre-mitigation: Temperance (20% GCD heal buff + 10% party mit) — weave 1-2 GCDs before
2. Pre-HoT: Asylum deployed 5-8s before raidwide for HoT ticks
3. Pre-shield: Plenary Indulgence woven just before the AoE heal GCD
4. Post-raidwide: Medica III → Afflatus Rapture → Assize
5. Recovery: Divine Caress if Temperance is active (stagger for second hit if applicable)

### Raise Sequence
1. Apply Thin Air (saves 2400 MP)
2. Swiftcast (instant raise)
3. Target dead player → Raise
4. Or hardcast Raise if both Swiftcast and Thin Air are on cooldown

### MP Emergency
1. Assize on cooldown (mandatory +500 MP every 40s)
2. Lucid Dreaming immediately
3. Thin Air on every expensive GCD
4. Switch to Afflatus heals exclusively (0 MP)
5. Skip Glare III if MP critical (it costs 400 MP per cast)
6. Last resort: downgrade to Cure (400 MP) if out of Cure II budget
