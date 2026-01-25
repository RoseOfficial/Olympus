# Olympus

![Version](https://img.shields.io/github/v/release/RoseOfficial/Olympus?label=version)
![Lines of Code](https://aschey.tech/tokei/github/RoseOfficial/Olympus?category=code)
![Code Size](https://img.shields.io/github/languages/code-size/RoseOfficial/Olympus)
![Last Commit](https://img.shields.io/github/last-commit/RoseOfficial/Olympus)
![C#](https://img.shields.io/github/languages/top/RoseOfficial/Olympus)

An intelligent rotation assistant for FFXIV that goes beyond automation. Olympus provides **intelligent decision-making** through fight prediction, party coordination, performance analytics, and an integrated training system to help you master your job.

## What Makes Olympus Different

| Feature | Description |
|---------|-------------|
| **Fight Awareness** | Timeline integration predicts raidwides and tankbusters before they happen |
| **Party Coordination** | Multiple Olympus users coordinate heals, mitigations, and burst windows via IPC |
| **Training Mode** | Learn *why* abilities are chosen with real-time explanations and skill tracking |
| **Performance Analytics** | Track GCD uptime, cooldown efficiency, and compare against FFLogs |

## Supported Jobs (21/21)

| Role | Jobs | Status |
|------|------|--------|
| **Healers** | White Mage, Scholar, Astrologian, Sage | ✅ Complete |
| **Tanks** | Paladin, Warrior, Dark Knight, Gunbreaker | ✅ Complete |
| **Melee DPS** | Monk, Dragoon, Ninja, Samurai, Reaper, Viper | ✅ Complete |
| **Ranged Physical** | Bard, Machinist, Dancer | ✅ Complete |
| **Casters** | Black Mage, Summoner, Red Mage, Pictomancer | ✅ Complete |

## Core Features

### Intelligent Rotation
- **Level-sync awareness** - Abilities adjust to your current level
- **Resource management** - Lily, Aetherflow, Kenki, Heat, and all job gauges
- **oGCD weaving** - Optimal ability timing without clipping
- **Positional optimization** - Rear/flank awareness for melee DPS
- **Proc tracking** - Never waste a proc or let buffs fall off

### Fight Timeline Integration
- **Raidwide prediction** - Pre-shield and pre-heal before damage hits
- **Tankbuster awareness** - Mitigations timed for incoming hits
- **Phase tracking** - Adapts to fight phases and mechanics
- **Arcadion Savage support** - Full timeline data for current tier

### Party Coordination (IPC)
When multiple party members use Olympus, they coordinate automatically:

| Coordination Type | What It Does |
|-------------------|--------------|
| **Heal Coordination** | Prevents double-healing the same target |
| **AOE Heal Sync** | Staggers party heals to avoid overlap |
| **Mitigation Stacking** | Prevents wasting Divine Veil + Temperance together |
| **Raise Coordination** | Only one healer raises each dead player |
| **Burst Windows** | DPS align raid buffs for maximum damage |
| **Tank Swaps** | Coordinated Provoke/Shirk sequences |
| **Interrupt Priority** | One player per interruptible cast |

### Performance Analytics
- **Real-time metrics** - GCD uptime, deaths, near-deaths during combat
- **Post-fight scoring** - Letter grades (S/A/B/C/D) with breakdown
- **Downtime analysis** - Categorizes lost GCDs (movement, death, mechanics)
- **Cooldown tracking** - Drift detection and missed opportunity alerts
- **Session history** - Track improvement over multiple fights
- **FFLogs integration** - Compare your performance to community parses

### Training Mode
Transform from passenger to pilot with intelligent coaching:

| Feature | Description |
|---------|-------------|
| **Live Explanations** | See *why* each ability is chosen in real-time |
| **525+ Concepts** | Job-specific knowledge across all 21 jobs |
| **147 Lessons** | Progressive learning from basics to optimization |
| **147 Quizzes** | Validate understanding with scenario questions |
| **Skill Detection** | Auto-detects Beginner/Intermediate/Advanced level |
| **Concept Mastery** | Tracks successful application in combat |
| **Adaptive Detail** | Explanations adjust to your skill level |

## Installation

### Custom Repository (Recommended)
1. Open Dalamud Plugin Installer in-game
2. Go to **Settings** (gear icon) → **Experimental**
3. Under "Custom Plugin Repositories", add:
   ```
   https://raw.githubusercontent.com/RoseOfficial/Olympus/main/repo.json
   ```
4. Click **Save and Close**
5. Search for "Olympus" and install

Updates are delivered automatically.

### Manual Installation
1. Download `Olympus.zip` from [Releases](https://github.com/RoseOfficial/Olympus/releases)
2. Extract to `%APPDATA%\XIVLauncher\installedPlugins\Olympus\`
3. Reload plugins or restart the game

## Quick Start

1. `/olympus` - Open the main window
2. Click **Enable** to activate
3. Enter combat on any supported job
4. Open **Training** to learn as you play
5. Open **Analytics** to track performance

## Commands

| Command | Description |
|---------|-------------|
| `/olympus` | Open main window |
| `/olympus toggle` | Enable/disable rotation |
| `/olympus debug` | Open debug window |

## Job Modules

Each rotation is named after a Greek deity:

| Role | Job | Module | Role | Job | Module |
|------|-----|--------|------|-----|--------|
| Healer | White Mage | Apollo | Melee | Reaper | Thanatos |
| Healer | Scholar | Athena | Melee | Viper | Echidna |
| Healer | Astrologian | Astraea | Ranged | Bard | Calliope |
| Healer | Sage | Asclepius | Ranged | Machinist | Prometheus |
| Tank | Paladin | Themis | Ranged | Dancer | Terpsichore |
| Tank | Warrior | Ares | Caster | Black Mage | Hecate |
| Tank | Dark Knight | Nyx | Caster | Summoner | Persephone |
| Tank | Gunbreaker | Hephaestus | Caster | Red Mage | Circe |
| Melee | Monk | Kratos | Caster | Pictomancer | Iris |
| Melee | Dragoon | Zeus | | | |
| Melee | Ninja | Hermes | | | |
| Melee | Samurai | Nike | | | |

## Development Phases

| Phase | Status | Milestone |
|-------|--------|-----------|
| Phase 1 | ✅ Complete | All 21 combat jobs |
| Phase 2 | ✅ Complete | Fight timeline integration |
| Phase 3 | ✅ Complete | Full party coordination via IPC |
| Phase 4 | ✅ Complete | Performance analytics + FFLogs |
| Phase 5 | 🔄 In Progress | Training mode + personalized coaching |
| Phase 6 | 📋 Planned | ML integration + simulation engine |

## Contributing

Issues and pull requests welcome at [GitHub](https://github.com/RoseOfficial/Olympus).

## License

This project is provided as-is for personal use with FFXIV.
