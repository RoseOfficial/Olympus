# Changelog

All notable changes to Olympus are documented in this file.

## [1.0.0] - 2026-01-03

### Added
- Comprehensive README documentation
- Unit test suite with 58 tests covering HealingCalculator and DebuffDetectionService
- CHANGELOG for version tracking
- Reset to Defaults button in settings

### Changed
- Updated plugin description to reflect WHM-only scope
- Clarified roadmap for future job modules
- First stable release

---

## [0.8.8] - 2026-01-03

### Fixed
- Divine Caress not casting after Temperance expires

## [0.8.7] - 2026-01-03

### Fixed
- Temperance casting issues

### Added
- Real-time party HP monitoring for defensive cooldowns

## [0.8.6] - 2026-01-02

### Changed
- Prioritize Glare IV AoE over Holy III when Sacred Sight active

## [0.8.5] - 2026-01-02

### Fixed
- Glare IV incorrectly blocking all damage casts at level 92+

## [0.8.4] - 2026-01-01

### Added
- "Why Stuck?" debug tab for rotation diagnostics

## [0.8.3] - 2026-01-01

### Fixed
- Healing oGCDs ignoring master Enable Healing toggle

## [0.8.2] - 2025-12-31

### Fixed
- Healing oGCDs not casting (Benediction, Tetragrammaton, Divine Benison)

## [0.8.0] - 2025-12-30

### Added
- Esuna with priority-based debuff cleansing
- Surecast role action support
- Rescue role action support (manual mode only)
- Configurable Esuna priority threshold

## [0.7.0] - 2025-12-29

### Added
- Complete WHM defensive oGCDs:
  - Divine Benison (shield)
  - Pleniary Indulgence (heal buff)
  - Temperance (party mitigation)
  - Aquaveil (damage reduction)
  - Liturgy of the Bell (delayed heal)
  - Divine Caress (post-Temperance heal)
- Blood Lily system with Afflatus Misery
- Defensive cooldown threshold configuration

## [0.6.3] - 2025-12-28

### Added
- Thin Air MP-saving oGCD with smart timing

## [0.6.1] - 2025-12-28

### Added
- Asylum ground-targeted AoE HoT ability

## [0.6.0] - 2025-12-27

### Added
- Comprehensive spell status display with all WHM spells

## [0.5.25] - 2025-12-27

### Added
- Holy AoE damage spells with enemy count detection

## [0.5.24] - 2025-12-26

### Added
- Aetherial Shift gap closer ability

## [0.5.21] - 2025-12-25

### Added
- Cure III targeted AoE heal with stacking detection

## [0.5.20] - 2025-12-25

### Added
- Regen HoT with tank priority

### Changed
- Removed HP threshold system in favor of overheal prevention

## [0.5.19] - 2025-12-24

### Added
- Presence of Mind oGCD buff

## [0.5.18] - 2025-12-24

### Fixed
- Windows now hide on login/character select screen

## [0.5.17] - 2025-12-23

### Added
- Overheal prevention system
- Freecure proc support

## [0.5.13] - 2025-12-22

### Added
- Spell selection debug information

### Changed
- Moved Assize to DPS oGCD category

## [0.5.10] - 2025-12-21

### Added
- Cure II with smart heal selection

## [0.5.9] - 2025-12-20

### Added
- Single-weave enforcement to prevent double oGCD per cycle

## [0.5.7] - 2025-12-19

### Added
- Swiftcast + Raise automatic resurrection

## [0.5.6] - 2025-12-18

### Changed
- Replaced threshold-based healing with overheal prevention system

## [0.5.5] - 2025-12-17

### Added
- Auto-calibration system for accurate heal predictions

## [0.5.1] - 2025-12-16

### Fixed
- GCD uptime now persists after combat ends

## [0.5.0] - 2025-12-15

### Changed
- Redesigned debug menu with tabbed interface

## [0.4.0] - 2025-12-14

### Changed
- Simplified codebase with party member helpers
- Reduced code duplication

## [0.3.9] - 2025-12-13

### Added
- RSR-style reactive execution with HP prediction

## [0.2.0] - 2025-12-10

### Added
- Medica AoE healing support

## [0.1.0] - 2025-12-08

### Added
- Initial release
- Stone/Glare damage spell progression
- Level-sync awareness
- Basic targeting system
- Main window with enable/disable toggle
- Settings window
- Debug window
