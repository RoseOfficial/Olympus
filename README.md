# Olympus - Apollo (White Mage)

An intelligent White Mage rotation assistant for FFXIV that handles healing, damage, defensive cooldowns, and resource management with full level-sync awareness.

## Features

### Intelligent Healing
- **Automatic single-target healing** with HP prediction to prevent overhealing
- **AoE healing** with party damage detection (configurable threshold)
- **Lily heal prioritization** (Afflatus Solace/Rapture) for optimal resource usage
- **Regen management** with tank priority and refresh prevention
- **Cure III stacking detection** for optimal positioning-based heals

### Damage Rotation
- **Full spell progression** from Stone I to Glare IV with level-sync awareness
- **DoT management** (Aero/Dia) with automatic refresh
- **AoE damage** (Holy/Holy III) with configurable enemy count threshold
- **Sacred Sight proc handling** for instant Glare IV

### Defensive Cooldowns
- **Divine Benison** shield on tank
- **Tetragrammaton** instant heal weaving
- **Benediction** emergency heal (configurable HP threshold)
- **Temperance** party mitigation with Divine Caress follow-up
- **Asylum** ground-targeted HoT
- **Aquaveil** and **Liturgy of the Bell** support

### Resource Management
- **Blood Lily system** with Afflatus Misery at 3 lilies
- **Thin Air** MP conservation
- **Lucid Dreaming** automatic MP recovery
- **Presence of Mind** buff usage

### Role Actions
- **Esuna** with priority-based debuff cleansing (Doom > Vulnerability > Paralysis)
- **Swiftcast + Raise** automatic resurrection
- **Surecast** and **Rescue** support (manual mode recommended)

## Installation

### Requirements
- FFXIV with Dalamud installed
- XIVLauncher with addon hooks

### Custom Repository (Recommended)
1. Open the Dalamud Plugin Installer in-game
2. Go to **Settings** (gear icon) → **Experimental**
3. Under "Custom Plugin Repositories", add:
   ```
   https://raw.githubusercontent.com/RoseOfficial/Olympus/main/repo.json
   ```
4. Click **Save and Close**
5. Search for "Olympus" in the plugin installer and install

Updates will be delivered automatically through the plugin installer.

### Manual Installation
1. Download `Olympus.zip` from the [Releases](https://github.com/RoseOfficial/Olympus/releases) page
2. Extract to `%APPDATA%\XIVLauncher\installedPlugins\Olympus\`
3. Reload plugins in Dalamud settings or restart the game

## Quick Start

1. Open the main window with `/olympus`
2. Click **Enable** to activate the rotation
3. Enter combat and watch Apollo handle your rotation
4. Click **Settings** to customize behavior

## Commands

| Command | Description |
|---------|-------------|
| `/olympus` | Open the main status window |
| `/olympus toggle` | Enable/disable the rotation |
| `/olympus debug` | Open the debug window |

## Configuration Guide

### Targeting Strategies

| Strategy | Description |
|----------|-------------|
| **Lowest HP** | Target the enemy with lowest HP percentage (default) |
| **Highest HP** | Target the enemy with highest HP percentage |
| **Nearest** | Target the closest enemy |
| **Tank Assist** | Target what the tank is targeting |
| **Current Target** | Use your current target if valid |
| **Focus Target** | Use your focus target if valid |

**Tank Assist Fallback**: When enabled, falls back to Lowest HP if tank has no target.

### Healing Settings

#### Master Toggles
- **Enable Healing** - Master switch for all healing
- **Enable Damage** - Master switch for damage spells
- **Enable DoT** - Master switch for Aero/Dia

#### Thresholds
| Setting | Default | Description |
|---------|---------|-------------|
| **Benediction Emergency** | 30% | Use Benediction only below this HP |
| **Defensive Cooldown** | 80% | Use defensives when party avg below this |
| **AoE Heal Min Targets** | 3 | Minimum injured to trigger AoE heal |

#### Individual Spell Toggles
Each healing spell can be individually enabled/disabled:
- **Single Target**: Cure, Cure II
- **AoE**: Medica, Medica II, Medica III, Cure III
- **Lily Heals**: Afflatus Solace, Afflatus Rapture
- **HoT**: Regen
- **oGCD**: Tetragrammaton, Benediction, Assize, Asylum

### Damage Settings

| Setting | Default | Description |
|---------|---------|-------------|
| **AoE Damage Min Targets** | 3 | Minimum enemies for Holy |

Each damage spell can be individually toggled:
- **Single Target**: Stone (I-IV), Glare (I, III, IV)
- **AoE**: Holy, Holy III
- **Blood Lily**: Afflatus Misery

### Resurrection Settings

| Setting | Default | Description |
|---------|---------|-------------|
| **Enable Raise** | On | Automatic resurrection |
| **Allow Hardcast Raise** | Off | 8s cast when Swiftcast unavailable |
| **Min MP Threshold** | 25% | MP required before raising |

### Role Action Settings

#### Esuna
| Setting | Default | Description |
|---------|---------|-------------|
| **Priority Threshold** | 2 | 0=Lethal only, 1=High+, 2=Medium+, 3=All |

Priority levels:
- **0 (Lethal)**: Doom, Throttle - cleanse immediately
- **1 (High)**: Vulnerability Up, Damage Down
- **2 (Medium)**: Paralysis, Silence, Pacification, Sleep
- **3 (Low)**: Bind, Heavy, Blind

#### Surecast & Rescue
Both default to manual mode (0) for safety. Automatic usage not recommended.

### Defensive Cooldown Settings

| Setting | Default | Description |
|---------|---------|-------------|
| **Defensive Threshold** | 80% | Use when party avg HP below this |
| **Use with AoE Heals** | On | Synergy with Plenary Indulgence |

Individual toggles for: Divine Benison, Pleniary Indulgence, Temperance, Aquaveil, Liturgy of the Bell, Divine Caress

## Level Sync Support

Apollo automatically selects the appropriate spell based on your current synced level:

| Level Range | Single Target | AoE Damage | DoT |
|-------------|---------------|------------|-----|
| 1-3 | Stone | - | - |
| 4-17 | Stone | - | Aero |
| 18-45 | Stone II | - | Aero |
| 46-53 | Stone III | Holy | Aero II |
| 54-63 | Stone III | Holy | Aero II |
| 64-71 | Stone IV | Holy | Aero II |
| 72-81 | Glare | Holy | Dia |
| 82-91 | Glare III | Holy III | Dia |
| 92-100 | Glare IV | Holy III | Dia |

## Debug Window

Access with `/olympus debug`. Contains multiple tabs:

- **Overview**: GCD planning state, quick stats
- **Healing**: HP predictions, AoE healing info, recent heals
- **Actions**: GCD details, spell usage tracking
- **Performance**: Statistics, downtime metrics
- **Why Stuck?**: Rotation diagnostics for troubleshooting

## Known Limitations

1. **Trust Dungeon Support**: Works but tank detection uses targeting heuristics
2. **Manual Targeting**: Some content may require manual target selection
3. **Movement**: Instant casts prioritized while moving, but positioning is player responsibility
4. **Rescue**: Automatic mode disabled due to high risk of killing party members
5. **Ground Targeting**: Asylum places on tank location, not optimal positioning

## Troubleshooting

### Rotation Not Working
1. Check if Olympus is **Enabled** in the main window
2. Verify you're on White Mage or Conjurer
3. Check the **Why Stuck?** tab in the debug window
4. Ensure you have a valid target

### Not Healing
1. Verify **Enable Healing** is on in settings
2. Check individual spell toggles
3. Confirm party members are in range (30y for most heals)

### Not Doing Damage
1. Verify **Enable Damage** is on in settings
2. Check enemy targeting strategy
3. Ensure enemy is in range and attackable

### MP Running Out
1. Check if Lucid Dreaming is being used (should auto-trigger at 70% MP)
2. Reduce hardcast raises
3. Enable Thin Air

## Roadmap

Future healer modules planned:
- **Athena** (Scholar)
- **Astraea** (Astrologian)
- **Asclepius** (Sage)

## Contributing

Issues and pull requests welcome at [GitHub](https://github.com/RoseOfficial/Olympus).

## License

This project is provided as-is for personal use with FFXIV.
