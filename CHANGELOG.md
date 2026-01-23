# Changelog

All notable changes to Olympus will be documented in this file.

<!-- LATEST-START -->
## v3.8.0 - Training Mode: Full Explanation Coverage

**Complete Healer Explanations**
- Every healing decision now provides real-time explanations in Training Mode
- All 4 healers (WHM, SCH, AST, SGE) are fully instrumented with 60+ decision points

**Scholar (SCH) Explanations**
- Lustrate, Excogitation, Indomitability, Sacred Soil, and all Aetherflow abilities
- Whispering Dawn, Fey Blessing, Seraph, Consolation, and Dissipation
- Expedient, Deployment Tactics, Emergency Tactics
- Chain Stratagem and Recitation timing
- Resurrection with Swiftcast coordination

**Astrologian (AST) Explanations**
- Essential Dignity, Celestial Intersection, Celestial Opposition, Exaltation
- Earthly Star placement/detonation, Horoscope, Macrocosmos
- Card system: Draw timing, Play targeting by role, Divination, Minor Arcana, Astrodyne
- Neutral Sect, Sun Sign, Collective Unconscious
- Lightspeed and its use as Swiftcast alternative for raises

**Sage (SGE) Explanations**
- All Addersgall abilities: Druochole, Taurochole, Ixochole, Kerachole
- Physis II, Holos, Haima, Panhaima, Pepsis
- Rhizomata, Krasis, Zoe usage and timing
- Kardia management: placement, Soteria, Philosophia, smart swapping
- Eukrasian Diagnosis/Prognosis for shielding
- Pneuma timing for damage + healing
- MP management with Lucid Dreaming

**Learning Experience**
- Each explanation includes: factors considered, alternatives evaluated, and learning tips
- Priority levels (Critical/High/Normal/Low) help focus on important decisions
- Job-specific tips explain FFXIV healer mechanics and best practices
<!-- LATEST-END -->

## v3.7.0 - Training Mode: Full Healer Coverage

**Multi-Healer Training Support**
- Training Mode now supports all 4 healers: WHM, SCH, AST, and SGE
- Each healer has job-specific concepts and learning progress tracking
- Combined progress view shows mastery across all healer jobs

**Job-Specific Concepts**
- Scholar (SCH): 28 concepts covering Aetherflow, Fairy, shields, and Chain Stratagem timing
- Astrologian (AST): 28 concepts covering cards, Earthly Star, Divination, and burst alignment
- Sage (SGE): 30 concepts covering Kardia, Addersgall, Eukrasia decisions, and shield economy
- White Mage (WHM): Existing 27 concepts unchanged

**Infrastructure**
- TrainingService now tracks concepts across all healer jobs
- Each healer context now has access to TrainingService for decision explanations
- Progress tracking automatically detects job from concept ID prefix

## v3.6.0 - Training Mode

**Training Mode Foundation**
- New Training Mode transforms Olympus from a rotation assistant into an intelligent coach
- Real-time decision explanations during combat help you understand optimal play
- Learn WHY abilities are chosen, not just watch them be used

**Live Coaching Tab**
- Real-time explanation feed showing every rotation decision as it happens
- Current action highlighted with detailed reasoning and decision factors
- "Alternatives Considered" section explains what other options were evaluated
- Learning tips for each scenario help build muscle memory

**Progress Tracking**
- Track which healing concepts you've learned (25+ WHM concepts)
- Concepts marked as "learned" persist across sessions
- Identify concepts that need more attention (seen 10+ times but not learned)
- Visual progress bar shows overall mastery

**How To Use**
1. Open the Training window from the main Olympus panel
2. Enable Training Mode using the checkbox
3. Enter combat - explanations appear as abilities are used
4. Mark concepts as "learned" in the Progress tab as you understand them

**Technical Details**
- Minimal performance impact when disabled
- Explanations captured per-action with timestamp, category, and priority
- Configurable verbosity (Minimal, Normal, Detailed)
- Priority filter to focus on important decisions only

## v3.5.0 - FFLogs Integration

**FFLogs API Integration**
- Compare your performance against FFLogs community parses
- View zone rankings and percentile data directly in the Analytics window
- New FFLogs tab added to the Analytics window

**Character Lookup**
- Bind your character by name, server, and region
- Cached character ID for faster subsequent lookups
- Easy setup wizard with link to FFLogs API client creation

**Rankings Display**
- All Stars points and rank for current savage tier
- Per-encounter best and median percentiles
- Total kills per encounter
- Trend indicators showing improvement over time

**Performance Comparison**
- Compare local DPS to your FFLogs best parse
- Estimated percentile based on current rankings
- Improvement tips based on GCD uptime and cooldown efficiency gaps

**Configuration**
- OAuth credentials stored securely in plugin config
- Configurable cache expiry (15-240 minutes)
- Auto-refresh with rate limit awareness

**How To Set Up**
1. Go to https://www.fflogs.com/api/clients/ and create an API client
2. Enter your Client ID and Secret in the FFLogs tab
3. Bind your character (name + server + region)
4. View your rankings!

## v3.4.0 - Personal DPS Tracking

**DPS Metrics**
- Analytics now displays real personal DPS (damage per second) during combat
- Total damage dealt is tracked and displayed in real-time
- DPS calculated from actual damage events, not estimates

**How It Works**
- Hooks into the same combat event system used for healing tracking
- Captures all damage dealt by the local player (direct damage, DoTs, AOE)
- Displays live in the Analytics window Realtime tab

**Fight Summary**
- Post-fight DPS included in combat metrics
- Total damage dealt shown alongside healing and other stats

## v3.3.0 - Cooldown Usage Analysis

**Detailed Cooldown Tracking**
- Analytics now shows per-ability cooldown efficiency with visual bars
- Tracks when and in what combat phase (Opener/Burst/Sustained) abilities were used
- Detects missed opportunities where cooldowns sat available but unused

**Enhanced Analysis**
- Each tracked cooldown shows uses vs optimal uses with efficiency percentage
- Average drift displayed (how late abilities were used on average)
- Phase breakdown shows opener, burst, and sustained usage counts
- Missed opportunity windows highlighted with duration

**Actionable Feedback**
- Primary issue detection: Drift, Missed, Gaps, or Good
- Contextual tips based on detected issues
- Perfect usage gets "Excellent" rating with congratulatory message

**Settings**
- New `TrackCooldownDetails` option (enabled by default)
- New section visibility toggle for Cooldown Analysis

## v3.2.0 - Downtime Analysis

**Downtime Breakdown**
- Analytics now shows why GCD uptime was lost, not just the percentage
- Categorizes downtime into: Movement, Mechanics, Death, and Unexplained
- Unexplained downtime highlights the "bad" gaps players should minimize

**Visual Analysis**
- Progress bars show relative contribution of each downtime category
- Tooltips explain what each category means
- Color-coded by severity (neutral for movement, red for unexplained)

**Actionable Feedback**
- Tips appear when unexplained downtime exceeds 5 seconds
- Movement-heavy fights get slidecast suggestions
- Helps players identify specific areas for improvement

**Settings**
- New `TrackDowntimeBreakdown` option (enabled by default)
- New section visibility toggle for Downtime Analysis

## v3.1.0 - Performance Analytics Foundation

**New Analytics System**
- Added performance analytics with real-time combat metrics tracking
- New Analytics window accessible from main window
- Tracks GCD uptime, deaths, near-deaths, and healing efficiency

**Real-time Metrics**
- Live combat duration and GCD uptime display
- Near-death detection when party members drop below configurable HP threshold (default 15%)
- Death tracking per combat encounter
- Overheal percentage from CombatEventService integration

**Fight Analysis**
- Post-fight performance scoring (0-100 scale with letter grades)
- GCD uptime, cooldown efficiency, healing efficiency, and survival scores
- Automated issue detection with severity levels
- Actionable suggestions for improvement

**Session History**
- Records last 50 fight sessions (configurable)
- Trend analysis showing improving/declining performance
- Session comparison with duration, score, and GCD uptime
- Clear history option for fresh start

**Configuration**
- Enable/disable tracking toggle
- Configurable near-death HP threshold (5-30%)
- Minimum combat duration to record (5-60 seconds)
- Section visibility toggles for all tabs

## v3.0.0 - Phase 3 Complete: Full Party Coordination

**Milestone Achievement**
- Phase 3 complete! Olympus instances now fully coordinate across all party members

**Coordination Features (v2.6.0 - v3.0.0)**
- Healers coordinate single-target and AoE heals to prevent overlap
- Tanks coordinate party mitigations (Divine Veil, Shake It Off, etc.)
- Healers broadcast party mitigations (Temperance, Expedient, etc.)
- Ground healing zones coordinate to prevent stacking
- Tank swaps coordinate via Provoke/Shirk handshake
- Interrupts coordinate between tanks and ranged DPS
- Healers broadcast gauge state for smarter resource decisions
- Primary/secondary healer roles auto-determined
- Resurrection targets coordinate to prevent double-raises
- Esuna targets coordinate to prevent wasted cleanses
- DPS burst windows align across party

## v2.31.0 - Healer Role & Gauge Coordination

**Multi-Healer Optimization**
- All four healers (WHM, SCH, AST, SGE) now share gauge state with other Olympus instances
- Healers declare primary/secondary roles based on job priority (WHM > AST > SCH > SGE)
- Secondary healers use lower healing thresholds (default 50%) to defer healing to the primary

**Gauge Broadcasting**
- WHM: Lily count and Blood Lily progress
- SCH: Aetherflow stacks and Fairy Gauge
- AST: Seal count and current card state
- SGE: Addersgall and Addersting stacks

**Role-Aware Healing**
- Each healer context now provides `IsPrimaryHealer` and `GetRoleAdjustedThreshold()` helpers
- Primary healer maintains normal healing thresholds and takes the lead
- Secondary healer defers healing unless HP drops below their lower threshold
- Enables more DPS uptime for secondary healers while maintaining party safety

**Existing Settings Used**
- `EnableHealerGaugeSharing` - Master toggle for gauge broadcasting (default: on)
- `EnableHealerRoleCoordination` - Master toggle for role system (default: on)
- `PreferredHealerRole` - Override auto-detection (Auto/Primary/Secondary)
- `SecondaryHealAssistThreshold` - HP% threshold for secondary healer (30-80%, default: 50%)

## v2.30.0 - Tank Swap Coordination

**Tank Coordination**
- Tanks now coordinate Provoke and Shirk between Olympus instances via IPC
- Prevents redundant actions when both tanks try to swap simultaneously
- Enables synchronized tank swap sequences for smooth aggro transitions

**How It Works**
- When a tank needs to swap (losing aggro), Olympus requests coordination from the co-tank
- The co-tank confirms by preparing to Shirk (or vice versa for Provoke)
- Both tanks execute their swap actions in sync
- Falls back to solo action after timeout if co-tank doesn't respond (1.5s default)

**New Settings**
- `EnableTankSwapCoordination` - Master toggle for tank swap coordination (default: on)
- `TankSwapReservationExpiryMs` - How long swap reservations remain valid (3000-10000ms, default: 5000ms)
- `TankSwapConfirmationTimeoutSeconds` - Timeout before acting solo (0.5-3.0s, default: 1.5s)

## v2.29.0 - Interrupt Coordination

**Party Coordination**
- Tanks and ranged physical DPS now coordinate interrupt abilities between Olympus instances
- Prevents multiple players from interrupting the same enemy cast
- Tank interrupts: Interject (Lv.18), Low Blow (Lv.12)
- Ranged physical DPS interrupt: Head Graze (Lv.24)

**How It Works**
- When a player is about to interrupt, Olympus checks if another instance is already interrupting that target
- The first player to interrupt reserves the target via IPC
- Other players will skip the interrupt to avoid wasting cooldowns
- Reservations expire based on remaining cast time (with 500ms buffer)

**New Settings**
- `EnableInterruptCoordination` - Master toggle for interrupt coordination (default: on)
- `InterruptReservationExpiryMs` - How long interrupt reservations remain valid (1000-5000ms, default: 3000ms)

## v2.28.0 - Esuna Coordination

**Healing**
- Healers now coordinate Esuna usage between Olympus instances
- Prevents multiple healers from cleansing the same debuff on the same target
- Currently integrated with Apollo (WHM) - other healers will follow

**How It Works**
- When a healer is about to cast Esuna, Olympus checks if another instance is already cleansing that target
- The first healer to cast reserves the target via IPC
- Other healers will skip that target and look for other party members with cleansable debuffs
- Reservations expire quickly (2 seconds) since Esuna is instant cast

**New Settings**
- `EnableCleanseCoordination` - Master toggle for cleanse coordination (default: on)
- `CleanseReservationExpiryMs` - How long cleanse reservations remain valid (1000-5000ms, default: 2000ms)

## v2.27.0 - Tank Invulnerability Coordination

**Tank Coordination**
- Tank invulnerability abilities now coordinate between Olympus instances
- Prevents both tanks from using invulns simultaneously during emergencies
- Covers Hallowed Ground (PLD), Holmgang (WAR), Living Dead (DRK), and Superbolide (GNB)
- After using an invuln, broadcasts to other tanks via IPC

**New Settings**
- `EnableInvulnerabilityCoordination` - Master toggle for invuln coordination (default: on)
- `InvulnerabilityStaggerWindowSeconds` - How long to delay if another tank used an invuln recently (1-10s, default: 5s)

## v2.26.0 - Resurrection Coordination

**Healing**
- All healers now coordinate resurrections between Olympus instances
- Prevents multiple healers from raising the same dead party member
- WHM (Raise), SCH (Resurrection), AST (Ascend), and SGE (Egeiro) all participate

**How It Works**
- When a healer is about to cast Raise, Olympus checks if another instance is already raising that target
- The first healer to start casting reserves the target via IPC
- Other healers will skip that target and look for other dead party members (if any)
- Swiftcast raises take priority over hardcast raises

**New Settings**
- `EnableRaiseCoordination` - Master toggle for resurrection coordination (default: on)
- `RaiseReservationExpiryMs` - How long raise reservations remain valid (5000-15000ms, default: 10000ms)

**Technical**
- New IPC message type: RaiseIntent
- New protocol classes: RaiseIntentMessage, RaiseReservation
- BaseResurrectionModule now integrates with party coordination service

## v2.25.0 - Multi-Healer Ground Effect Coordination

**Healing**
- Ground-targeted healing zones now coordinate between Olympus healers
- Prevents inefficient overlap when multiple healers place abilities in the same area
- WHM Asylum, SCH Sacred Soil, AST Earthly Star, and SGE Kerachole all participate

**How It Works**
- When you're about to place a ground effect, Olympus checks if a remote healer already has one active nearby
- If overlap is detected (configurable threshold), your healer skips placement to avoid waste
- After placing a ground effect, Olympus broadcasts its position to other instances

**New Settings**
- `EnableGroundEffectCoordination` - Master toggle for ground effect coordination (default: on)
- `GroundEffectOverlapThreshold` - How much overlap (0-1) before skipping, 0.5 = 50% overlap (default: 0.5)
- `EnableHealerGaugeSharing` - Infrastructure for future gauge-aware decisions (default: on)
- `EnableHealerRoleCoordination` - Infrastructure for primary/secondary healer roles (default: on)

**Technical**
- New IPC message types: GaugeState, RoleDeclaration, GroundEffectPlaced
- New data registry: CoordinatedGroundEffects.cs with radius/duration for each ability
- Foundation laid for gauge sharing and role declaration in future updates

## v2.24.0 - Complete Healer Burst Decision Logic

**Healing**
- Scholar and Astrologian now deploy abilities proactively before DPS burst windows
- SCH (Athena): Sacred Soil, Whispering Dawn, and Fey Blessing consider burst timing
- AST (Astraea): Earthly Star placement now considers burst timing with longer maturation window

**How It Works**
- When `PreferShieldsBeforeBurst` is enabled:
  - Sacred Soil, Whispering Dawn, Fey Blessing deploy 3-8 seconds before burst (same as Asylum/Kerachole)
  - Earthly Star places 8-12 seconds before burst (longer window for Giant Dominance maturation)
- These abilities now also check for imminent raidwides (previously only HP threshold)
- Emergency HP thresholds still override proactive logic

**Technical**
- Added raidwide awareness to Whispering Dawn, Fey Blessing, and Earthly Star placement
- TimelineHelper.IsRaidwideImminent now accepts optional custom window parameter
- Completes the healer burst awareness feature from v2.21.0

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
