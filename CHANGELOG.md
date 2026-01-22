# Changelog

All notable changes to Olympus will be documented in this file.

<!-- LATEST-START -->
## v2.23.0 - Tank Defensive Synergy

**Tank Coordination**
- Tanks now coordinate personal defensive cooldowns (Rampart, Sentinel, Nebula, etc.)
- When two Olympus tanks are in the same party, they stagger major mitigations
- Prevents wasteful overlap where both tanks use defensives on the same hit
- Maximizes mitigation uptime across tankbuster sequences

**New Settings**
- `EnableDefensiveCoordination` - Enable tank-to-tank mitigation staggering (default: on)
- `DefensiveStaggerWindowSeconds` - How long to delay if remote tank used mitigation (1-10s, default: 3s)

**Coordinated Abilities**
- Rampart (all tanks)
- Sentinel / Guardian (PLD)
- Vengeance / Damnation (WAR)
- Bloodwhetting (WAR)
- Shadow Wall / Shadowed Vigil (DRK)
- Nebula / Great Nebula (GNB)
<!-- LATEST-END -->

## v2.22.0 - Complete DPS Burst Broadcasting

**DPS Coordination**
- Samurai (Nike), Ninja (Hermes), Viper (Echidna), and Machinist (Prometheus) now broadcast burst intents
- Fixes asymmetric coordination where these jobs only listened for party bursts but never announced their own
- All 4 jobs now properly participate in two-way IPC communication

**Technical**
- Added Ikishoten, Kunai's Bane, Serpent's Ire, and Wildfire to coordinated raid buff registry
- Each job now calls `AnnounceRaidBuffIntent()` before burst and `OnRaidBuffUsed()` after execution

## v2.21.0 - Healer Burst Awareness

**Healing**
- All healers now aware of DPS burst windows for optimized decision-making
- Healers can query party burst state (active, imminent, time remaining)
- WHM (Apollo): Temperance and Liturgy of the Bell consider burst timing
- SCH (Athena): Expedient considers burst timing
- AST (Astraea): Neutral Sect and Collective Unconscious consider burst timing
- SGE (Asclepius): Kerachole, Holos, and Panhaima consider burst timing

**New Settings**
- `EnableHealerBurstAwareness` - Master toggle for burst-aware healer decisions (default: on)
- `BurstImminentWindowSeconds` - How many seconds before burst to consider "imminent" (2-10s, default: 5s)
- `PreferShieldsBeforeBurst` - Deploy HoTs/shields proactively before burst windows (default: off)
- `DelayMitigationsDuringBurst` - Delay major mitigations during active bursts unless emergency (default: off)

**How It Works**
- DPS modules already broadcast raid buff intents via IPC
- Healers now consume this information to optimize timing
- When `PreferShieldsBeforeBurst` is enabled, Asylum and Kerachole deploy 3-8 seconds before burst
- When `DelayMitigationsDuringBurst` is enabled, Temperance/Expedient/etc. wait for burst to end (unless HP is critical)

## v2.20.0 - Pictomancer Starry Muse Coordination

**DPS Coordination**
- Pictomancer (Iris) now aligns Starry Muse (+5% damage) with party raid buff windows
- Listens for pending burst intents and synchronizes burst timing with other Olympus users
- Fills gap where Starry Muse was missed during v2.11.0-v2.13.0 DPS raid buff work

## v2.19.0 - Ninja Party Burst Alignment

**DPS Coordination**
- Ninja (Hermes) now aligns Kunai's Bane burst window with party raid buff windows
- Listens for pending burst intents and synchronizes burst timing with other Olympus users
- Maximizes damage during coordinated burst phases

## v2.18.0 - Viper Party Burst Alignment

**DPS Coordination**
- Viper (Echidna) now aligns Serpent's Ire with party raid buff windows
- Delays burst briefly when other DPS are about to use Battle Voice, Technical Finish, etc.
- Maximizes Reawaken damage during coordinated burst phases

## v2.17.0 - Samurai Party Burst Alignment

**DPS Coordination**
- Samurai (Nike) now aligns Ikishoten burst window with party raid buff windows
- Delays burst briefly when other DPS are about to use Battle Voice, Technical Finish, etc.
- Maximizes Ogi Namikiri damage during coordinated burst phases

## v2.16.0 - Machinist Party Burst Alignment

**DPS Coordination**
- Machinist (Prometheus) now aligns Wildfire with party raid buff windows
- Delays Wildfire briefly when other DPS are about to use Battle Voice, Technical Finish, etc.
- Maximizes damage during coordinated burst phases

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
