# Olympus

![Version](https://img.shields.io/github/v/release/RoseOfficial/Olympus?label=version)
![Lines of Code](https://aschey.tech/tokei/github/RoseOfficial/Olympus?category=code)
![Code Size](https://img.shields.io/github/languages/code-size/RoseOfficial/Olympus)
![Last Commit](https://img.shields.io/github/last-commit/RoseOfficial/Olympus)
![C#](https://img.shields.io/github/languages/top/RoseOfficial/Olympus)

An intelligent rotation assistant for FFXIV that handles healing, damage, mitigation, and resource management with full level-sync awareness.

## Supported Jobs

| Role | Jobs | Status |
|------|------|--------|
| **Healers** | White Mage, Scholar, Astrologian, Sage | ✅ Complete |
| **Tanks** | Paladin, Warrior, Dark Knight, Gunbreaker | ✅ Complete |
| **Melee DPS** | Monk, Dragoon, Ninja, Samurai, Reaper, Viper | ✅ Complete |
| **Ranged Physical** | Bard, Machinist, Dancer | ✅ Complete |
| **Casters** | Black Mage, Summoner, Red Mage, Pictomancer | ✅ Complete |

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

### Ranged Physical DPS
- Song/Dance rotation management
- Proc tracking and optimization
- Party buff coordination
- DoT maintenance and refresh timing

### Casters
- Cast optimization and movement planning
- Mana management and resource pooling
- Enochian/Astral/Umbral state tracking
- Proc usage and priority management

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
| Melee | Samurai | Nike |
| Melee | Reaper | Thanatos |
| Melee | Viper | Echidna |
| Ranged | Bard | Calliope |
| Ranged | Machinist | Prometheus |
| Ranged | Dancer | Terpsichore |
| Caster | Black Mage | Hecate |
| Caster | Summoner | Persephone |
| Caster | Red Mage | Circe |
| Caster | Pictomancer | Iris |

## Roadmap

- ✅ All Healers (4/4)
- ✅ All Tanks (4/4)
- ✅ All Melee DPS (6/6)
- ✅ All Ranged Physical (3/3)
- ✅ All Casters (4/4)

**All 21 combat jobs complete!**

## Contributing

Issues and pull requests welcome at [GitHub](https://github.com/RoseOfficial/Olympus).

## License

This project is provided as-is for personal use with FFXIV.
