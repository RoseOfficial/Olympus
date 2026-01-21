# Changelog

All notable changes to Olympus will be documented in this file.

<!-- LATEST-START -->
## v2.10.0 - AOE Heal Coordination

**Healing**
- Added cross-instance party-wide (AOE) heal coordination for all healers
- Multiple Olympus healers no longer cast AOE heals simultaneously
- WHM: Medica, Cure III, Afflatus Rapture
- SCH: Succor, Indomitability
- AST: Helios, Aspected Helios, Helios Conjunction, Celestial Opposition
- SGE: Prognosis, Ixochole, Kerachole, Eukrasian Prognosis

**Settings**
- New option: `EnableAoEHealCoordination` (enabled by default)
- New option: `AoEHealReservationExpiryMs` (configurable 1500-5000ms, default 2500ms)
<!-- LATEST-END -->

## v2.9.0 - Cross-Healer Coordination

**Healing**
- Extended single-target heal coordination to all healers
- Scholar (Athena): Lustrate, Excogitation, Protraction, Adloquium, Physick
- Astrologian (Astraea): Essential Dignity, Celestial Intersection, Exaltation, Aspected Benefic, Benefic, Benefic II
- Sage (Asclepius): Druochole, Taurochole, Krasis, Haima, Eukrasian Diagnosis, Diagnosis
- All four healers now coordinate via IPC to prevent double-healing

## v2.8.0 - Cross-Instance Heal Coordination

**Healing**
- Added cross-instance single-target heal coordination for Apollo (WHM)
- Olympus users no longer double-heal the same target when multiple WHMs are present
- Coordination uses IPC protocol for real-time state sync

**Technical**
- Extended PartyCoordinationService with heal target tracking
- Added HealTargetInfo to IPC message protocol

## v2.7.0 - Party Cooldown Sync

**Defensives**
- Healers and tanks now coordinate major party mitigations
- Prevents overlapping cooldowns like Divine Veil + Shake It Off
- Configurable overlap window for fine-tuning

## v2.6.0 - Party Coordination IPC

**Multiplayer**
- Added IPC protocol for multi-Olympus coordination
- Healers can now share state between instances
- Foundation for advanced party-wide optimization
