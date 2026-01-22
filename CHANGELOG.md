# Changelog

All notable changes to Olympus will be documented in this file.

<!-- LATEST-START -->
## v2.16.0 - Machinist Party Burst Alignment

**DPS Coordination**
- Machinist (Prometheus) now aligns Wildfire with party raid buff windows
- Delays Wildfire briefly when other DPS are about to use Battle Voice, Technical Finish, etc.
- Maximizes damage during coordinated burst phases
<!-- LATEST-END -->

## v2.15.0 - Tank-Healer Mitigation Avoidance

**Party Coordination**
- Healers now broadcast party mitigations to other Olympus instances
- WHM: Temperance, Liturgy of the Bell
- SCH: Sacred Soil, Expedient
- AST: Neutral Sect, Collective Unconscious, Macrocosmos
- SGE: Panhaima, Holos
- Tanks now check healer mitigations before using party-wide defensives
- Prevents wasteful stacking (e.g., Divine Veil + Temperance simultaneously)
- Completes two-way mitigation coordination between tanks and healers

## v2.14.0 - Tank Mitigation Broadcasting

**Party Coordination**
- Tank party mitigations now broadcast to other Olympus instances
- Prevents multiple tanks from stacking mitigations (Divine Veil, Shake It Off, Dark Missionary, Heart of Light)
- Reprisal usage is now coordinated between tanks
- Completes the two-way coordination loop started in v2.7.0

## v2.13.0 - Complete DPS Raid Buff Coordination

**Party Coordination**
- Added raid buff coordination for remaining DPS jobs
- Red Mage (Circe): Embolden now synchronizes with party burst windows
- Dancer (Terpsichore): Technical Finish now synchronizes with party burst windows
- Reaper (Thanatos): Arcane Circle now synchronizes with party burst windows
- Monk (Kratos): Brotherhood now synchronizes with party burst windows
- All DPS raid buffs now coordinate for optimal burst alignment

## v2.12.0 - Summoner Raid Buff Coordination

**Party Coordination**
- Added Searing Light coordination for Summoner (Persephone)
- Multiple Olympus users now synchronize Summoner burst windows with other raid buffs
- Works seamlessly with existing Dragoon and Bard coordination

## v2.11.0 - DPS Raid Buff Coordination

**DPS Coordination**
- Added cross-instance raid buff synchronization for DPS jobs
- Dragoon (Zeus): Battle Litany now synchronizes with other Olympus DPS
- Bard (Calliope): Battle Voice and Radiant Finale now synchronize with party burst

**How It Works**
- DPS jobs announce their intent before using raid buffs
- Other Olympus instances align their burst windows when a party member is about to use buffs
- Handles desync gracefully (e.g., after death) - uses buffs independently until realigned

**Settings**
- New option: `EnableRaidBuffCoordination` (enabled by default)
- New option: `RaidBuffAlignmentWindowSeconds` (1-10 seconds, default 3s)
- New option: `MaxBuffDesyncSeconds` (10-60 seconds, default 30s)
- New option: `LogRaidBuffCoordination` (debug logging)

## v2.10.1 - Discord Notification Fix

**Bug Fix**
- Fixed Discord release notifications showing `%0A` instead of actual line breaks

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
