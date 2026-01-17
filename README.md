# Olympus

An intelligent rotation assistant for FFXIV that handles healing, damage, mitigation, and resource management with full level-sync awareness.

## Supported Jobs

| Role | Jobs | Status |
|------|------|--------|
| **Healers** | White Mage, Scholar, Astrologian, Sage | ✅ Complete |
| **Tanks** | Paladin, Warrior, Dark Knight, Gunbreaker | ✅ Complete |
| **Melee DPS** | Monk, Dragoon, Ninja | 🔄 3/6 |
| **Ranged Physical** | Bard, Machinist, Dancer | ⏳ Coming Soon |
| **Casters** | Black Mage, Summoner, Red Mage, Pictomancer | ⏳ Coming Soon |

## Features

### Healers
- HP prediction to prevent overhealing
- AoE damage detection with configurable thresholds
- Lily/Aetherflow/Seal/Addersgall resource management
- Priority-based debuff cleansing (Esuna)
- Automatic resurrection with Swiftcast

### Tanks
- Intelligent mitigation rotation
- Cooldown stacking prevention
- Invulnerability timing (Hallowed Ground, Holmgang, Living Dead, Superbolide)
- Self-healing optimization (Equilibrium, Abyssal Drain, Aurora)
- Threat generation and AoE management

### Melee DPS
- Positional optimization (rear/flank awareness)
- Burst window management
- Combo tracking and resource management
- oGCD weaving optimization

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
3. Enter combat on any supported job
4. Click **Settings** to customize behavior

## Commands

| Command | Description |
|---------|-------------|
| `/olympus` | Open the main status window |
| `/olympus toggle` | Enable/disable the rotation |
| `/olympus debug` | Open the debug window |

## Job Modules

Each rotation is named after a Greek deity matching the job's theme:

| Role | Job | Module |
|------|-----|--------|
| Healer | White Mage | Apollo |
| Healer | Scholar | Athena |
| Healer | Astrologian | Astraea |
| Healer | Sage | Asclepius |
| Tank | Paladin | Themis |
| Tank | Warrior | Ares |
| Tank | Dark Knight | Nyx |
| Tank | Gunbreaker | Hephaestus |
| Melee | Monk | Kratos |
| Melee | Dragoon | Zeus |
| Melee | Ninja | Hermes |

## Roadmap

- ✅ All Healers (4/4)
- ✅ All Tanks (4/4)
- 🔄 Melee DPS (3/6) - Samurai, Reaper, Viper next
- ⏳ Ranged Physical (0/3)
- ⏳ Casters (0/4)

## Contributing

Issues and pull requests welcome at [GitHub](https://github.com/RoseOfficial/Olympus).

## License

This project is provided as-is for personal use with FFXIV.
