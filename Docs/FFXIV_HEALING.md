# FFXIV Healing Systems

Reference document for healer decision-making and healing mechanics.

## Healing Priority Framework

### Triage Order
1. **Prevent deaths**: Target below lethal threshold
2. **Emergency healing**: Tank/critical HP targets
3. **Maintain HoTs**: Keep Regen/HoTs rolling
4. **Spot healing**: Address missing HP efficiently
5. **DPS**: When healing is not needed

### HP Thresholds
| Level | % HP | Action |
|-------|------|--------|
| Critical | <30% | Benediction, emergency oGCDs |
| Emergency | 30-50% | Priority oGCD healing |
| Urgent | 50-70% | GCD healing if needed |
| Maintenance | 70-90% | HoT refresh, spot healing |
| Healthy | >90% | DPS priority |

## oGCD vs GCD Healing

### oGCD Advantages
- No GCD cost (preserves DPS uptime)
- Usually instant (no cast time)
- Can be used during movement

### When to Use oGCDs
1. During weave windows after instant GCDs
2. For emergency healing (faster response)
3. When MP conservation is needed
4. When movement prevents casting

### GCD Healing Scenarios
- oGCDs on cooldown
- Sustained heavy damage
- Multiple targets need healing
- AoE healing required

## Healing Decision Tree

```
Is anyone dying? (HP < 30%)
├─ Yes → Emergency heal (Benediction/Tetragrammaton)
└─ No → Is there an emergency? (HP < 50%)
    ├─ Yes → oGCD heal if available
    └─ No → Are HoTs needed?
        ├─ Yes → Apply/refresh HoTs
        └─ No → Is HP below threshold?
            ├─ Yes → Efficient GCD heal
            └─ No → DPS
```

## Overheal Prevention

### Why Overheal Matters
- Wasted MP on excess healing
- GCD spent not doing damage
- Inefficient resource usage

### Prevention Strategies
1. **Right-size heals**: Use appropriate potency
2. **HP prediction**: Account for incoming heals/damage
3. **Tolerance thresholds**: Allow small overheal for safety

### Tolerance Settings
- Single-target: 2-5% overheal acceptable
- AoE heals: 10-20% overheal acceptable (multiple targets)
- Emergency: Overheal doesn't matter

## AoE Healing

### Trigger Conditions
1. Multiple targets need healing (3+ typically)
2. Targets are in range of AoE
3. oGCD AoE is available
4. Damage pattern indicates raidwide

### AoE Efficiency
```
Efficiency = (Targets Hit × Potency) / MP Cost

Example:
Medica II (700p heal + HoT) on 6 targets:
Efficiency = (6 × 700) / 1000 MP = 4.2 healing per MP

Cure II (800p) on 1 target:
Efficiency = (1 × 800) / 1000 MP = 0.8 healing per MP
```

## Healing Prediction

### HP Projection
```
ProjectedHP = CurrentHP + IncomingHeals - PredictedDamage

IncomingHeals = HoT ticks + Pending cast heals
PredictedDamage = DamageRate × LookaheadTime
```

### Factors to Consider
1. **HoT ticks**: Regen, Medica II, etc.
2. **Shield absorption**: Divine Benison, Galvanize
3. **Incoming damage**: Boss mechanics, DoTs
4. **Co-healer activity**: Avoid double-healing

## Shield vs Pure Healing

### Shield Healers (SCH, SGE, AST Nocturnal)
- Shields absorb damage before HP
- Must be applied BEFORE damage
- Don't stack (stronger wins)

### Pure Healers (WHM, AST Diurnal)
- Heal after damage taken
- HoTs provide sustained recovery
- Can react to damage

### Mixed Strategy
- Pre-shield for predictable damage
- Pure heal for sustained/unpredictable
- Coordinate with co-healer

## Damage Intake Analysis

### Metrics
- **DPS (Damage Per Second)**: Average damage rate
- **Spike Detection**: Sudden large HP drops
- **Trend Analysis**: Rising/falling damage patterns

### Usage
```csharp
var damageRate = damageIntakeService.GetDamageRate(targetId, 3f);

if (damageRate > 500)  // High sustained damage
    // Use stronger heals, consider oGCDs
```

## Co-Healer Coordination

### Detection
- Monitor party healing events
- Track co-healer casting
- Identify healing style

### Coordination Rules
1. Let co-healer handle their targets
2. Reduce threshold if co-healer is active
3. Emergency override if target is dying

## MP Management

### Conservation Strategies
1. Prefer free heals (Lilies, oGCDs)
2. Use Lucid Dreaming proactively
3. Downgrade spells when safe (Cure vs Cure II)

### Emergency MP
- Keep ~3000 MP reserve for emergencies
- Prioritize Raise MP cost (~2400 MP)
- Use Thin Air for expensive casts

## Healing Calibration

### Purpose
Adapt healing calculations to actual in-game performance.

### Factors
- Player stats (Mind, Determination, Crit)
- Party composition
- Content difficulty

### Calibration Process
1. Track actual healing amounts
2. Compare to predicted amounts
3. Adjust calibration factor
4. Store for future use
