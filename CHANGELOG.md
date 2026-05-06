# Changelog

All notable changes to Olympus will be documented in this file.

<!-- LATEST-START -->
## v4.17.2 — 2026-05-06

### Fix — Plugin Marked as Outdated After Dalamud 15
- Dalamud bumped its API level for the latest game patch, which caused Olympus to show as outdated and incompatible and refuse to load
- Rebuilt against the new Dalamud and updated the declared API level so Olympus loads again. No behavior changes, just a compatibility fix
<!-- LATEST-END -->

## v4.17.1 — 2026-05-02

### Fix — Plugin Failed to Load After Dalamud Update
- The latest Dalamud update changed an internal API that Olympus relied on, which caused the plugin to fail loading with a "Method not found" error
- Rebuilt against the new Dalamud so Olympus loads cleanly again. No behavior changes, just a compatibility fix

## v4.17.0 — 2026-05-02

### New — Pre-Pull Tincture Automation
- Olympus can now pop a tincture for you on the opener and re-pot on cooldown after that, both aligned to your burst window
- Picks the matching stat tincture for your job (Strength, Dexterity, Intelligence, or Mind) and prefers HQ if you have one
- Gated to high-end duties (savage, extreme, ultimate) so it never fires in roulettes or open world by accident
- Off by default. Opt in under the new Consumables tab if you want it

### New — Modifier-Key Burst Overrides
- Hold Shift to force the bot to act as if you're already in burst (skip pooling, fire now). Hold Ctrl to force the opposite (always pool resources). Useful for committing early or saving a window the timeline didn't predict
- Off by default because Shift and Ctrl conflict with chat typing. Turn it on under Input if you want it
- Affects pooling decisions across roughly 30 sites including Saber Dance, ninjutsu pooling, Manafication, and any gauge spender that holds for burst

### New — Feint and Addle Across All Melee and Caster DPS
- Olympus now fires Feint on all 6 melee DPS (Monk, Ninja, Dragoon, Samurai, Reaper, Viper) and Addle on all 4 caster DPS (Black Mage, Summoner, Red Mage, Pictomancer). Previously only Viper had Feint wired
- Both are coordinated across multiple Olympus instances. Two casters running Olympus will not double-Addle the same boss; same for melee Feint
- Per-job Enable Feint and Enable Addle toggles live on each job's Role Actions section, default on

### New — Second Wind and Bloodbath on All Melee DPS
- Olympus now uses Second Wind (HP threshold default 50%) and Bloodbath (default 85%) on all 6 melee DPS instead of only Viper
- Toggles consolidated onto a new Melee shared tab so you set them once for the role

### Faster Burst Detection from Party Buffs
- Olympus now opens the burst window the instant a party member's coordinated raid buff actually resolves (Divination, Battle Litany, Brotherhood, Embolden, Searing Light, Devilment, Radiant Finale, Chain Stratagem), instead of waiting up to 200ms for the next status scan to pick it up
- GCDs that should slam during the buff window land sooner, and pooling exits faster on the opener
- The status scan stays as a fallback so behavior is unchanged if the cast event is missed

### Precise Mit-Stack Coordination
- Mit coordination across multiple Olympus instances is now per-debuff instead of "anyone on the party used mit recently"
- Previously, Tank A using Rampart would incorrectly cause Tank B to skip Reprisal even though they are separate mit layers. Each ability now only skips when its own debuff is already active on the boss (Reprisal in its 10s buff window, Feint and Addle in their 15s windows)
- Wired across all 4 tanks (Reprisal), all 6 melee DPS (Feint), and all 4 caster DPS (Addle)
- Tank Rampart staggering between Olympus tanks is also coordinated this way, gated by a new "Coordinate defensive cooldowns" toggle on the Tanks shared tab

### Faster GCD Submission for Tanks
- Fixed a scheduler bug that was rejecting tank GCDs during the action queue window. Gunbreaker, Paladin, Warrior, and Dark Knight now correctly submit the next GCD in the last 0.5s of the current cycle instead of waiting for full rollover
- Up to half a second of GCD latency removed across the four tank rotations

### Gunbreaker / Dark Knight
- Fixed an AoE combo step 2 ambiguity. Demon Slaughter (GNB) and Stalwart Soul (DRK) were gated only on combo step 1, which is also set by the single-target starter. In mixed target-count pulls the bot could fire the wrong step 2 and break the combo, dealing half potency. Now each step 2 only fires after its matching starter

### Settings UI
- Lucid Dreaming, Second Wind, Bloodbath, Rampart, and True North toggles consolidated onto their shared role tabs (Healers, Melee, Tanks) instead of being duplicated on every per-job page
- White Mage Lucid Dreaming intentionally stays on the White Mage page because it uses a predictive MP forecast that the other casters do not
- Pictomancer Lucid Dreaming UI now correctly binds to the shared caster setting
- Many previously-unread config toggles and thresholds are now actually honored across 16 rotations (gauge overcap thresholds, burst-pool flags, behavior knobs that had been defined but ignored)

### Behind the Scenes — Scheduler Migration Complete for All 21 Jobs
- The per-frame priority scheduler that started as a pilot on Gunbreaker in v4.13 and rolled out for several rotations in v4.16 now covers all 21 jobs. No behavior change you should notice
- The point is the platform: future fixes apply consistently across rotations, and the regression-test coverage that has been catching the bug classes called out in the last few releases is now in place for every job
- Adds dedicated scheduler-push tests for Feint, Addle, role-action helpers, and combo-step computation across 13 rotations

<!-- LATEST-END -->
## v4.16.0 — 2026-04-21

### New — Hardcast Raise Quick Toggle
- Added a Hardcast toggle to the overlay so you can flip between Swiftcast-only and hardcast raises without opening settings
- New `/olympus hardcast` chat command does the same thing, so you can bind it to a hotbar

### New — Action Feed Overlay
- A new overlay shows a fading strip of icons for actions Olympus just pressed, with hover tooltips (name, GCD or oGCD, action ID, age) and green or blue borders by category
- Configurable icon size, max count, and fade duration under General > Action Feed
- Off by default; useful for building trust in what the bot is firing when

### New — Pause on Channeled Abilities
- The rotation now pauses while you're channeling Passage of Arms, Flamethrower, Meditate, Collective Unconscious, or Improvisation
- Previously the bot would fire damage GCDs and cancel your channel mid-use
- Toggle under General (default on)

### New — Pyretic and Stand-Still Punisher Detection
- The rotation now halts every module (damage, healing, mitigation, buffs) while you have Pyretic or a similar stand-still debuff active
- Any action under Pyretic applies a lethal Vulnerability stack, so this prevents wipes on fights with stand-still mechanics
- Resumes automatically the frame the debuff drops
- Toggle under Targeting (default on)

### Mechanic-Aware Cast Gating Expanded to All Casters
- Previously only healer cast-time damage GCDs held for predicted raidwides and tank busters. That coverage now extends to Black Mage, Summoner, Red Mage, Pictomancer, Bard, Machinist, Dancer, and Paladin cast-time GCDs
- Swiftcast is now respected by the gate, so instant casts under Swiftcast are never held
- Summoner Carbuncle summon and Black Mage Blizzard AoE fallbacks also route through the gate
- Timeline settings moved to a dedicated Timeline tab under Behavior since they now apply to every role

### Earlier GCD Submission
- Action dispatch now uses FFXIV's action queue window, letting the next GCD submit in the last 0.5s of the current cycle instead of waiting for full rollover
- Noticeably reduces the dead air between GCDs, especially under latency
- Hardcasts still submit at rollover so a follow-up isn't committed before the current cast lands

### Dancer
- Retuned burst alignment. Flourish now fires on the off-2-minute (when Devilment has more than 55s left) instead of inside Devilment, and no longer waits for an existing Silken proc to drop
- Saber Dance no longer pools Esprit while a burst window is imminent, so partner Esprit ticks stop overcapping
- Tillana moved below Dance of the Dawn and Saber Dance so the Esprit it generates has room to spend before Flourishing Finish falls off
- Standard Step now respects the Delay Standard for Technical and Hold Standard for Technical config options instead of a hardcoded 5s window
- Added the missing Single and Double Standard Finish and Technical Finish action variants
- Fan Dance II minimum level corrected to 50

### Samurai
- Default AoE threshold lowered from 3 enemies to 2 (break-even for Fuko and better Fugetsu/Fuka maintenance on 2-mob pulls)
- AoE combo no longer drops back to single-target Jinpu if an enemy walks out of range mid-chain; Mangetsu and Oka finish the Sen the rotation already committed to
- Added Lv.100 Tendo action definitions (Tendo Goken and Tendo Setsugekka) so the bot tracks the buff-upgraded versions
- Iaijutsu, Higanbana, Tenka Goken, Midare Setsugekka, and Ogi Namikiri cast times corrected to 1.8s

### Monk
- Default AoE threshold lowered from 3 enemies to 2 (break-even for Arm of the Destroyer and faster Chakra building)
- Added out-of-combat Meditation so Monk builds Chakra pre-pull instead of standing idle, giving the opener roughly three free Forbidden Chakra of potency
- Updated Meditation to the Dawntrail Steeled Meditation upgrade chain (the old action was removed)
- Brotherhood status ID corrected; it was pointing at Meditative Brotherhood, so "during Brotherhood" checks were firing on the wrong buff
- Fire's Reply recast corrected to 2.5s (was misclassified as an oGCD)
- Fire's and Wind's Rumination status IDs corrected; Wind's Reply minimum level corrected to 96

### Red Mage
- Fixed the melee combo silently dropping after Enchanted Riposte. The game rejects the Enchanted Zwerchhau and Redoublement replacement IDs, so the bot was falling through to ranged filler spells instead of finishing the combo. Base action IDs are now used and the game upgrades them server-side during Manafication or Embolden
- Contre Sixte recast corrected to 45s

### Warrior
- Added Primal Wrath (Lv.96), the self-centered AoE oGCD granted by the Wrathful status at 3 stacks of Burgeoning Fury. Previously the proc was dropped every burst window because the ability wasn't wired at all
- New Enable Primal Wrath toggle under Warrior config (default on)

### Dark Knight
- Disabled the legacy Dark Arts logic. The Dark Arts status was removed in Shadowbringers; the old constant was colliding with Dark Missionary, so "has Dark Arts" was firing whenever Dark Missionary was up
- Added Impalement (Lv.96), the AoE Delirium finisher that was entirely missing
- Deferred ShadowedVigil detection to avoid a false-positive Shadow Wall trigger during Lv.96+ Delirium windows
- Edge of Darkness, Dark Mind, Disesteem, and The Blackest Night status IDs corrected

### Machinist
- Reworked Reassemble with next-GCD lookahead. It now fires only when the next GCD is a high-potency tool (Drill, Bioblaster, Air Anchor, Hot Shot, Chain Saw, Excavator, Full Metal Field), with a charge-based fallback to avoid overcap
- Replaced the old Reassemble Priority dropdown with a Reassemble Strategy knob (Automatic, Any, Hold One, Delay). The old dropdown was entirely unwired and let you pick a specific tool to hold Reassemble for, which isn't how any competitor does it
- Overheated state now short-circuits so Reassemble never lands on Heat Blast, Blazing Shot, or Auto Crossbow
- Tactician recast corrected to 120s

### Gunbreaker
- Fixed Fated Brand not weaving after Fated Circle (Ready to Brand status ID was wrong)
- Fixed Gnashing Fang and Reign of Beasts combo drops. Noble Blood and Lion Heart were being skipped and cartridges were spilling into Burst Strike because combo step detection relied on action replacement, which doesn't advance for the Reign chain. Combo tracking now reads the gauge directly
- Skip starting a new Gnashing Fang combo when enough enemies are present for AoE, so cartridges flow into Fated Circle during packs
- Rotation internals rebuilt on a new scheduler, and the 20+ bug fixes from v4.13 through v4.15 are now locked in with dedicated regression tests so these classes of bug can't silently return

### Reaper
- Added Lv.96 Executioner action definitions (Executioner's Gibbet and Gallows) plus the Executioner status
- Circle of Sacrifice and Bloodsown Circle status IDs un-swapped

### Astrologian
- Fixed The Arrow card status tracking. The ID was set to the action ID by mistake and was colliding with The Spear, so card-buff detection was wrong for both cards
- Fixed Lord of Crowns tracking. Lord of Crowns doesn't apply a persistent status, so the tracker was permanently waiting for a status that would never land
- Helios Conjunction status ID corrected
- Lightspeed recast corrected to 90s

### Sage
- Added Lv.20 through Lv.59 Physis so the pre-60 bracket has a party HoT option (the rotation previously jumped straight to Physis II)
- Haima, Taurochole, and Eukrasian Dyskrasia status IDs corrected
- Seraphism and Impact Imminent status IDs corrected

### Scholar
- Seraphism and Impact Imminent status IDs corrected

### White Mage
- Divine Caress and Medica III applied-status IDs corrected

### Black Mage
- Despair cast time corrected to 2.0s per patch notes (it was too long and breaking filler calculations)
- Umbral Soul minimum level corrected to 35
- Blizzard II action ID corrected
- Blizzard AoE fallbacks now route through the mechanic-aware cast gate

### Summoner
- Crimson Strike ID corrected (was colliding with Mountain Buster)
- Painflare minimum level corrected to 40
- Carbuncle summon now routes through the mechanic-aware cast gate

### Viper
- Remapped 20 Reawaken, Twinblade, and Legacy action IDs that had shifted one section in the game data. Twinfang and Twinblood oGCDs, Generation 1 through 4, Ouroboros, Legacy 1 through 4, Uncoiled Twinfang and Twinblood, Death Rattle, and Last Lash all now point at live game IDs with correct level gates
- Hindstung and Flanksbane Venom status IDs un-swapped
- Added the missing Slither gap closer (30s cooldown, 2 charges, 3 at Lv.84)

### Dragoon
- Drakesbane minimum level corrected to 64 (was 92, so it never fired below that level)
- Right Eye status ID corrected

### Ninja
- Ten Jindo and Deathfrog Medium action IDs un-swapped

### Bard
- Straight Shot Ready status ID corrected to the live Hawk's Eye ID

### Healer Sharing
- Preemptive spike detection is now shared between White Mage and Astrologian so both react to the same predicted damage signal
- Dynamic regen threshold is shared between White Mage Regen and Astrologian Aspected Benefic
- Damage-intake triage target selection (damage rate, tank bonus, shield penalty, healer bonus, time-to-die urgency) is now shared across Sage, Scholar, and Astrologian single-target heals. The toggle previously only worked on White Mage
- Co-healer awareness is shared across Sage, Scholar, and Astrologian single-target heals, eliminating duplicate per-job copies
- Shared healer prediction and timeline toggles surface on the shared tab

### Settings UI
- Sidebar reorganized into Behavior, Visuals, and Multiplayer categories
- Shared role actions consolidated. Lucid Dreaming (healers) and Head Graze (ranged physical DPS) now live on the shared tab instead of being duplicated on every per-job page
- Debug section visibility toggles moved out of the Debug window into a dedicated Display settings page
- Healer-only debug tabs are now hidden when a non-healer job is active
- Removed dead Shared pages for melee, ranged, and caster DPS that held only informational text
- Fixed duplicate Shared sidebar entries that were swallowing clicks
- Fixed the debug tab bar breaking when the Actions tab was selected
- Removed two unwired priority dropdowns (Ninja Single Target Ninjutsu Priority, Black Mage Movement Priority) that had no effect on the rotation

### Localization
- Fixed malformed JSON in 8 non-English locale files (de, es, fr, ja, ko, pt, ru, zh) that had been breaking string loads
- Fixed a duplicate phrase in the German Timeline section

<!-- LATEST-END -->
## v4.15.0 — 2026-04-14

### New — Drop-Target Safety
- Dropping your target now hard-stops damage — useful for gaze mechanics, disengage, or any moment you need to stop attacking instantly
- Explicit target strategies (Current Target, Focus Target) no longer silently fall back to "lowest HP" when you drop your target, so the rotation respects your intent
- Enemy-needing-DoT logic now honors your Enemy strategy — explicit strategies only apply DoTs to your selected enemy instead of spilling them onto adds with reflect or damage-down debuffs

### New — Smart Gap Closers
- Gap closers (Onslaught, Intervene, Trajectory, Shadowstride, Thunderclap, High Jump, Spineshatter Dive, Dragonfire Dive, Stardiver) are now blocked when the current target isn't your explicit selection, or when you've just gained distance from the target — prevents the rotation from yanking you back into mechanics you were actively running from
- Toggle available under Targeting (default on)

### New — Forced-Movement Gate
- Damage GCDs are now suppressed while you're under Forced March, Thin Ice, or similar movement-override statuses, so casts don't fire during mechanics that control your position
- Toggle under Targeting (default on)

### New — Esuna for Scholar, Sage, and Astrologian
- All three were missing cleanse support entirely — only White Mage had it. All four healers now cleanse cleansable debuffs with priority-based detection (lethal > high > medium > low) and party coordination to avoid double-cleansing
- Fixed White Mage Esuna unconditionally blocking while moving — it now casts during movement when Swiftcast is up

### New — Targeting Invulnerability Filter
- Auto-targeting now skips enemies with known invulnerability status effects (immune boss phases, untouchable adds, ARR invulnerable crystals)
- Explicit Current Target and Focus Target are never filtered, so player intent is always respected
- Toggle under Targeting (default on)

### New — Mechanic-Aware Healer Damage
- Healer damage casts (Glare, Broil, Malefic, Dosis) now hold when the fight timeline predicts a raidwide or tankbuster will hit before the cast completes — keeps your GCD available for reactive healing instead of locking you into a 1.5s damage cast
- Healers now skip single-target DoT maintenance during dungeon packs when enough enemies are present for AoE damage
- Toggle under Healing config (default on)

### Ninja
- Fixed ninjutsu results (Raiton, Suiton, Huton, etc.) silently failing to execute after mudra inputs completed — the rotation was stuck in a "cast" state with nothing happening
- Fixed Ten Chi Jin doing nothing — all three mudra steps now fire correctly during the buff
- Fixed mudra sequences locking up mid-combo, which was previously masked as intermittent stalls by a 7-second bandaid timeout
- Wired several Ninki and Doton config settings that existed but had no effect (overcap threshold, minimum gauge, AoE use, AoE target count)

### Gunbreaker
- Fixed Gnashing Fang combo dropping back to Keen Edge after the first hit — combo tracking is now resilient to Continuation oGCD weaves
- Gnashing Fang now fires on its 30s cooldown instead of being held indefinitely for No Mercy
- Added the missing Fated Brand continuation proc for Fated Circle at level 96+
- Cartridges are now spent before overcapping from the AoE combo finisher

### Machinist
- Fixed Hypercharge deadlock where the rotation stalled between burst windows — tool-ready checks no longer block oGCD activation while the tools can only fire during GCD windows
- Fixed Gauss Round and Ricochet charge tracking reading the wrong action IDs at level 92+, which made the charges appear unusable
- Wired Queen Battery config thresholds that previously had no effect on the rotation

### Red Mage
- Fixed the Enchanted melee combo (Riposte → Zwerchhau → Redoublement → Verflare/Verholy → Scorch → Resolution) dropping after the first hit
- Added the Moulinet AoE chain (Enchanted Moulinet → Deux → Trois) — previously AoE just ran Impact forever
- Corps-a-corps and Engagement now hold when your HP is below a configurable threshold (default 70%) so they stop pulling you into danger while hurt
- Added a new Movement / Gap Closers section to the Red Mage config

### Warrior
- Added Primal Rend and Onslaught player-agency toggles (both default off) — Rend's 20y dash and Onslaught's melee lunge can yank you into mechanics, so the initial press is now left to the player
- The rotation still completes Primal Ruination once you press Rend, and still uses Onslaught to close gaps when you're out of melee range
- Added Enable Primal Rend and Enable Primal Ruination toggles in the Warrior config UI

### Dancer
- Dancer now preps Standard Step before combat in Duty Support and pre-pulls — previously the opener lost ~6 seconds of the personal damage buff because Standard Step wouldn't fire until after combat began
- Added automatic Closed Position that picks the best dance partner by job priority and re-partners on death (respects Manual selection mode)
- Reordered the opener so Standard Step fires before Technical Step when the personal buff is missing — cuts opener delay from roughly 9s to 3.5s
- Wired feather overcap, Fan Dance minimum, Esprit overcap, and Saber Dance minimum config thresholds — they existed in settings but the rotation was using hardcoded values

### Healers (All)
- Single-target oGCD heals (Benediction, Tetragrammaton, Lustrate, Essential Dignity, Druochole) no longer fire on tanks during invulnerability windows (Hallowed Ground, Holmgang, Living Dead, Superbolide) or on pending-heal buffs (Excogitation, Catharsis of Corundum). Shields, regens, ground targets, and AoE heals are unaffected — they still fire during invuln windows where they have legitimate value
- Party members afflicted by Doom now jump to the top of heal priority — Doom only clears at 100% HP, so a Doomed player always outranks anyone else missing HP
- Enemies behind walls are now filtered out of auto-targeting using a line-of-sight raycast (toggle under Targeting, default on)
- Player hitbox radius is now included in range calculations, so effective range matches what the game actually uses

### Scholar Fairy
- Fixed the rotation spamming Summon Eos every frame when Eos was glammed as Ruby Carbuncle (or any other pet glamour) — fairy detection now uses the underlying pet ID instead of the display name
- Seraph is now correctly distinguished from Eos via pet base ID instead of name matching

### Burst Windows
- The burst detector now scans the current enemy target for Chain Stratagem, Dokumori, and Vulnerability Up debuffs in addition to player buffs, catching team-applied raid damage windows faster
- AST Divination was added to the tracked player buff list

### Interrupts
- Interrupts across all 7 jobs that have them (PLD, WAR, DRK, GNB, BRD, MCH, DNC) now fire with a humanized reaction delay (0.3–0.7s into the cast) instead of snapping the moment a cast is detected

<!-- LATEST-END -->
## v4.14.0 — 2026-04-07

### Summoner
- Fixed Carbuncle not being summoned automatically at the start of combat
- Fixed primal summons and demi-summons (Bahamut, Phoenix, Solar Bahamut) failing silently, causing the rotation to loop on filler spells
- Fixed gauge reading issues that caused primals to never register as available

### Gunbreaker
- Fixed Reign of Beasts combo dropping after the first hit — Noble Blood and Lion Heart now execute properly

### Healers (All)
- Healers no longer cast heals out of combat (between pulls or after the boss dies)
- Healers no longer waste heals on players who just got raised and are still invulnerable
- Fixed targeting so rotations no longer attack enemies that haven't been pulled yet (e.g., distant training dummies or unengaged packs)

### Sage
- Fixed Eukrasian Dosis (DoT) failing to apply due to an animation lock timing issue
- Kardia now reliably stays on the main tank instead of sometimes swapping to DPS

### Pictomancer
- Added automatic motif repainting during combat downtime as a low-priority fallback

### Black Mage
- Fixed low-level Fire phase ending prematurely due to incorrect MP threshold ordering

### Tanks (All)
- Added over 40 new config toggles across all 4 tank jobs covering gap closers, oGCD damage, buff windows, mitigation cooldowns, and shared role actions (Reprisal, Interject, Low Blow, Arm's Length)

### Casters
- Wired several existing config toggles that were visible but had no effect: Black Mage Scathe movement toggle, Summoner ability group toggles, and Pictomancer individual creature motif toggles

<!-- LATEST-END -->
## v4.13.0 — 2026-03-31

### New — Paladin Clemency
- Paladin now uses Clemency as an emergency self-heal when HP drops dangerously low and no other mitigation is available

### New — Red Mage Verraise
- Red Mage now raises fallen party members using Verraise, with Dualcast and Swiftcast awareness and party coordination to avoid double-raising

### Summoner
- Fixed a bug where the rotation got stuck casting Ruin III indefinitely — primal summons now properly reset at all levels, and demi-summon activation (Bahamut, Phoenix, Solar Bahamut) no longer fails silently on alternating cycles

### Ninja
- Fixed a bug where the rotation could stop executing entirely — if a mudra sequence was interrupted or timed out, the rotation would freeze instead of resuming normal combat

### Healers (All)
- Fixed a bug where healers applied DoTs to training dummies the player hadn't engaged yet (e.g., dummies on the other side of the area)

### Tanks (All)
- Fixed Dark Knight and Gunbreaker ignoring the AoE enable toggle and minimum targets setting

### Config Cleanup
- Removed 25+ settings toggles across multiple DPS jobs that were visible in the UI but had no effect on the rotation — this reduces confusion and keeps the settings page honest
- Fixed 11 DPS rotation abilities that were ignoring their enable/disable toggles
- Fixed Apollo Aquaveil activating even when healing was disabled
- White Mage config section is now fully localized
- Spanish, Portuguese, and Russian added to the language selector

### Settings Validation
- Fixed overcap thresholds not being clamped during auto-fix
- Added Gunbreaker Heart of Corundum validation
- IPC protocol version mismatches now log a warning instead of silently failing

### Stability
- Fixed multiple thread-safety issues across party coordination, combat tracking, action history, damage trends, and performance monitoring
- Fixed a timeline sync drift in looping fights
- Sensitive fields are now excluded from config export

<!-- LATEST-END -->
## v4.12.0 — 2026-03-24

### New — Post-Fight Coaching
- After each combat encounter, a coaching summary now appears with callouts across 7 categories (healing, mitigation, cooldown usage, burst alignment, resource management, uptime, and deaths) plus an overall grade
- A new Pull History tab lets you review past encounters from your current session
- The summary popup is off by default — enable it in settings under Fight Summary

### New — Draw Helper & Targeting
- A visual Draw Helper overlay shows AoE targeting zones, positional indicators, and facing guides in real time
- Smart AoE targeting now picks the position that hits the most enemies instead of always centering on your current target
- Auto-attack is now detected and used as a combat start signal for more reliable rotation activation

### New — Mechanic Forecast Overlay
- The in-game overlay now shows upcoming boss mechanics from the fight timeline, so you can see what's coming before it happens

### New — Training Mode Expansion
- Training Mode lessons and quizzes are now available for all 21 combat jobs (previously only a subset had content)

### Main Window
- The main window has been redesigned with a compact 2×2 navigation grid and footer links, replacing the old vertical button list
- The Enable/Disable button is now taller and more prominent
- The status header is more compact with the rotation label removed

### White Mage
- Fixed Regen being applied to all party members at high HP instead of only those who actually needed it
- Presence of Mind is no longer attempted on Conjurer (it's a WHM-only ability)
- Fixed Lucid Dreaming not activating when MP conditions were met

### Astrologian
- Fixed The Arrow status ID being tracked incorrectly, which could interfere with card buff detection
- Neutral Sect and Collective Unconscious can now be used proactively when the fight timeline predicts incoming raidwide damage

### Scholar
- Expedient can now be used proactively when the fight timeline predicts incoming raidwide damage

### Sage
- Lucid Dreaming no longer fires if the Lucid Dreaming buff is already active

### Healers (General)
- Fixed a bug where healing could be blocked entirely when the predicted heal amount exceeded the target's missing HP
- Heal prediction is now calibrated per job instead of mixing samples across different healers, improving accuracy in synced content
- Added intermediate heal scaling entries for better prediction accuracy in level-synced duties

### Tanks (All)
- Party mitigation abilities (Shake It Off, Heart of Light, Divine Veil, Dark Missionary) now fire correctly — a logic error previously caused them to almost never activate
- Reprisal now correctly targets enemies instead of allies and checks the damage gate properly on all four tanks
- Burst pooling is now connected to the burst window tracker for all four tanks, improving cooldown alignment during party burst windows

### Dark Knight
- The Blackest Night now falls back to Rampart when TBN is on cooldown during timeline-predicted tank busters

### Gunbreaker
- Bloodfest no longer holds indefinitely for No Mercy — it now only waits if No Mercy is coming back within 15 seconds

### Dragoon
- Battle Litany now fires after 5 seconds if Lance Charge alignment isn't available, instead of holding it indefinitely
- New settings toggles for Dragonfire Dive, Nastrond, Wyrmwind Thrust, Mirage Dive, Starcross, Rise of the Dragon, Spineshatter Dive, and burst pooling

### Samurai
- Ikishoten burst-hold now works correctly with the burst window tracker
- New settings toggle for True North

### Reaper
- Arcane Circle hold time now respects the configurable hold time setting instead of using a hardcoded value
- Fixed a logic error in Soul Spender that could prevent Gluttony from being used

### Monk
- New settings toggle for Thunderclap

### Ninja
- Ninjutsu toggle in settings now actually controls whether ninjutsu abilities are used

### Viper
- Reawaken toggle in settings now actually controls whether Reawaken is used
- New settings section for role action toggles and burst pooling

### Bard
- Bloodletter now respects its enable toggle and reads the AoE minimum targets setting from config

### Dancer
- Fan Dance III now respects its enable toggle and reads the AoE minimum targets setting from config

### Warrior
- AoE damage abilities now read the enable toggle and minimum targets setting from config

### Pictomancer
- New settings controls for Subtractive Combo, Rainbow Drip, Palette usage, burst pooling, Tempera Coat, and Lucid Dreaming

### Summoner
- Fester now respects its enable toggle
- Demi-summon phase tracking fixed to prevent abilities from being used out of sequence

### Machinist
- Full Metal Field and Excavator now respect their enable toggles

### Red Mage
- Burst pooling is now connected to the burst window tracker

### Black Mage
- Burst pooling is now connected to the burst window tracker

### Thaumaturge
- Fixed a softlock where the rotation could get stuck in Astral Fire III with no way to continue — Transpose is now used as an escape

### Settings
- Party Coordination is now configurable from the settings UI instead of requiring manual configuration
- Buff hold times for all DPS and tank jobs now use your configured value instead of a hardcoded 8-second default
- Settings validation now catches negative values in tank configuration fields
- Many new localization keys added across all settings sections for full language support

### Fight Timeline
- Forward label jumps in Cactbot timeline files are now resolved correctly instead of being silently dropped
- Timeline confidence now properly decays to zero after 2 minutes without a sync point (previously it stayed at 50% indefinitely)
- Boss mechanic prediction timing is now based on actual frame timing instead of an assumed 16ms, preventing drift on non-60fps systems

### Overlay
- Party member count in the overlay now updates reliably when members join or leave

### Performance
- Reduced per-frame memory allocations across combat tracking, party analysis, and damage prediction services
- Action name lookups are now cached instead of reading the game data sheet every frame
- Healer rotations now build the party member list once per frame instead of twice
- Party HP checks throttled to 6 times per second (from every frame) to reduce overhead

### Stability
- Fixed a crash on plugin load when the Smart AoE service wasn't initialized yet
- Fixed a crash on plugin unload caused by services being cleaned up twice
- Fixed several thread-safety issues in combat tracking, action history, party coordination, and boss mechanic detection that could cause intermittent crashes or corrupted data
- Stale damage-over-time entries are now cleared on zone transition instead of persisting from previous duties
- Fixed double-counting of raidwide damage in the damage forecast
- Fixed MP forecast rounding to match the game's discrete server tick behavior

<!-- LATEST-END -->
## v4.11.0 — 2026-03-18

### All DPS Jobs
- Ability toggles in settings now actually take effect for all 12 DPS jobs — Dragoon, Monk, Machinist, Samurai, Reaper, Ninja, Bard, Dancer, Black Mage, Summoner, Red Mage, and Pictomancer all had toggles that were displayed but silently ignored by the rotation engine

### Sage
- Sage now automatically uses mitigation cooldowns (Kerachole, Taurochole, Holos, Panhaima, Haima), matching other healer jobs
- Fixed a bug where the Addersting status was tracked with the wrong ID, which could prevent Toxikon from firing when charges were available

### Scholar
- Fixed an issue in Scholar settings where the Emergency Tactics and Consolation toggle descriptions were displayed incorrectly

### Tanks
- Paladin, Warrior, Dark Knight, and Gunbreaker now have job-specific settings (Cover, Passage of Arms, Divine Veil, Clemency, Nascent Flash, Holmgang, Living Dead, The Blackest Night, Dark Missionary, Heart of Light, Heart of Corundum)

### Settings
- Settings now show action icons next to toggle names, with a tooltip displaying the action's ID, type (GCD/oGCD), cast time, recast time, range, and AoE range
- Settings validation now detects gauge configuration conflicts for Ninja, Samurai, Reaper, Machinist, and Dancer — when the minimum gauge to spend is higher than the overcap threshold, the rotation can never spend that gauge and the issue is flagged with a suggested fix
- The Draw Helper and General settings sections are now fully localized for all supported languages

### Fight Timeline
- Fight timelines are now available for all 8 Pandaemonium Savage raids (P1S–P8S), enabling timeline-aware cooldown planning in those encounters
- Fixed an issue where P5S–P8S fight timelines were registered with incorrect zone IDs, preventing those timelines from activating in-game

### General
- Thaumaturge (pre-Black Mage) is now recognized and handled by the Black Mage rotation
- Archer (pre-Bard) is now recognized and handled by the Bard rotation
- A new Changelog window (accessible from the main window) shows the 20 most recent plugin updates
- Internal error tracking is now active, allowing suppressed errors to be surfaced in the debug window without flooding game logs
- Fixed a resource leak in the plugin's cleanup code that could cause instability when reloading or uninstalling the plugin

### Internal
- Role actions (Swiftcast, Lucid Dreaming, Feint, Rampart, etc.) are now defined once and shared across all jobs instead of being duplicated in each job's action file
- Minor cleanup: accent color consolidated to a shared constant, config sidebar category lists promoted to static fields to avoid per-frame allocations, and a redundant set lookup removed
- Automated integration tests added for all 21 jobs and core services, improving regression detection before changes reach players

<!-- LATEST-END -->
## v4.10.31 — 2026-03-16

### Viper
- The rotation now weaves Legacy oGCDs (First through Fourth Legacy) during Reawaken bursts, significantly improving burst damage output
- Death Rattle now fires automatically after single-target combo finishers
- Last Lash now fires automatically after AoE combo finishers
- Second Wind, Bloodbath, Feint, and True North are now active in the rotation
- Writhing Snap is now used as a ranged filler when out of melee range with no Rattling Coils available


## v4.10.30 — 2026-03-16

- Japanese translations are now available for all 21 combat jobs in the Training module — every lesson and quiz can now be displayed in Japanese

## v4.10.29 — 2026-03-16

- Pictomancer (Iris): Creature motifs now correctly alternate between types based on level when painting outside combat — Lv.96+ paints Claw/Maw, lower levels alternate Pom/Wing
- Pictomancer (Iris): During the Inspiration buff window (Starry Muse), missing canvases are now painted with reduced cast time rather than waiting for the next gap in the rotation
- Pictomancer (Iris): Hyperphantasia (granted by Starry Muse) now allows combo GCDs to be used while moving — no more lost uptime during the burst window
- Pictomancer (Iris): Subtractive Palette now fires immediately when Subtractive Spectrum is active rather than waiting for a burst window
- Pictomancer (Iris): Tempera Coat and Tempera Grassa are now used automatically when you or your party are taking significant damage
- Pictomancer (Iris): Smudge is now used automatically when moving and no instant GCDs are available
- Pictomancer (Iris): Swiftcast is now used during movement when no other instant options are available, preventing cast interruption

## v4.10.28 — 2026-03-16

- Tank cooldown abilities now pre-empt tank busters predicted by the fight timeline — major defensives activate up to 8 seconds before a known tank buster rather than only reacting after damage starts
- Damage forecasts used for healing triage now factor in timeline-predicted raidwides and tank busters, so healers and shields are deployed before mechanics land rather than in response to them

## v4.10.27 — 2026-03-16

- Burst resource pooling is now individually tuned for each DPS job — gauge resources and key cooldowns are held more precisely in the final seconds before a burst window rather than spending freely
  - Dragoon no longer accidentally triggers Life of the Dragon before burst when Geirskogul would activate it at an inopportune time
  - Bard uses Apex Arrow at a lower Soul Voice threshold during active burst windows to guarantee value under raid buffs
  - Viper holds Reawaken to enter it inside the burst window (bypassed when Ready to Reawaken proc is available)
  - Pictomancer delays Hammer Stamp activation to align the combo with burst timing
  - Red Mage delays entering melee combo when a burst window is seconds away
  - Reaper holds Gluttony so the Soul Reaver stacks it generates land under raid buffs; Enshroud is also now held for burst when Shroud is not near cap
  - Dancer holds Saber Dance when Esprit is not approaching the cap, saving gauge for burst
  - Machinist holds Hypercharge for burst when Heat is below 90
  - Black Mage holds Polyglot stacks for burst timing when below 2 stacks
  - Ninja, Samurai, and Monk hold their primary gauge spender when a burst window is imminent but the gauge is not near cap

## v4.10.26 — 2026-03-16

- Fight timelines are now available for Dragonsong's Reprise (Ultimate), The Omega Protocol (Ultimate), and Futures Rewritten (Ultimate) — the plugin can now predict upcoming raidwides and tankbusters in all three fights.

## v4.10.25 — 2026-03-16

- The overlay window has been rebuilt as a real-time combat HUD — it now shows your active rotation and job, a clickable ACTIVE/INACTIVE status indicator, your next queued action highlighted in green, HP percentage with a color-coded indicator, injured party member count, raise-in-progress alerts, and current positional (Rear/Flank/Front) for melee DPS jobs.
- The first-run welcome screen is now a three-page onboarding wizard — it walks you through key features, lets you enable the plugin and choose a behavior preset, and ends with practical tips and a Discord link.

## v4.10.24 — 2026-03-16

- Action metadata is now registered for all 21 jobs — previously only White Mage had full action data, which limited features like action-level filtering and heal/damage categorization to WHM only. All jobs now have complete coverage.

## v4.10.23 — 2026-03-16

- Add preset quick-switcher to the main window — switch between Raid, Dungeon, Casual, and other presets without opening the full settings window
- Fight session history is now saved to disk and restored when the plugin reloads — performance trends in the Analytics window are preserved across sessions
- DoT damage is now included in healing urgency calculations — the plugin accounts for pending tick damage when deciding whether to heal, reducing unnecessary heal spam on targets with active DoTs
- Add config import/export via clipboard in the settings window footer — share your settings with other players or back up your configuration
- Add Spanish, Portuguese, and Russian as selectable languages — strings currently display in English until community translations are contributed

## v4.10.19 — 2026-03-16 - Training Mode Now Fully Wired

**Training Mode**
- Live Coaching now explains decisions for all 21 jobs — every action in every rotation now records what it did and why, feeding the coaching panel in real time.
- Concept mastery tracking is now active for all jobs, so Struggling Concepts and mastery-driven lesson recommendations work across the full roster.
- Fixed a bug where skill level could never advance beyond Beginner through the quiz component due to a mismatched quiz ID format.

## v4.10.18 — 2026-03-15

## v4.10.17 — 2026-03-15

- Improved healer performance and reliability (no behavior change)

## v4.10.16 — 2026-03-15 - Training Mode Now Covers All Jobs

**Training Mode (All Jobs)**
- The Lessons and Quizzes tabs now show all 21 jobs, organized by role (Healer, Tank, Melee, Ranged, Caster). Previously only Healers and Tanks were accessible from the tab bar.

## v4.10.15 — 2026-03-15 - White Mage Thin Air and Glare IV Improvements

**White Mage**
- Thin Air now spends charges immediately when both charges are full, even when the party is healthy and no expensive spell is incoming. Previously charges could sit capped indefinitely during stable phases, wasting charge regeneration. The extra charge is now spent on the next GCD cast (Glare III or Dia) to keep charge regen flowing.
- Glare IV's recorded potency corrected to 640 (was 350). This affects Training Mode explanations only — rotation behavior is unchanged.


## v4.10.14 — 2026-03-15 - White Mage Cooldown Logic Fixes

**White Mage**
- Asylum no longer places when the party is at full health. It still deploys proactively before predicted raidwides and burst windows as before — the change only prevents it from firing unconditionally on cooldown when no one needs healing.
- Fixed Temperance, damage trend analysis, and high-damage phase detection incorrectly triggering due to outgoing damage to enemies being counted as party damage intake. These systems now only consider damage received by actual party members.

## v4.10.13 — 2026-03-15 - Debug Checklist Now Shows All Spells

**Debug Menu (All Jobs)**
- The spell checklist in the debug menu now shows every ability for each job, including full upgrade chains, utility actions, defensive cooldowns, stance toggles, movement abilities, and all role actions. Previously, only a curated subset was displayed.

## v4.10.12 — 2026-03-15 - Internal Quality Improvements

**All Healer Jobs (White Mage, Sage, Astrologian, Scholar)**
- Healing logic for all four healers has been restructured internally — each healing ability is now an independent, individually tested component. Behavior is unchanged.

## v4.10.11 — 2026-03-15 - Crash Fix on Death and Zone Transition

**All Jobs**
- Fixed crashes that occurred when the player died or changed zones. Rotation logic now stops immediately when the player is dead, preventing modules from running against an invalid game state. Zone transitions now also reset HP tracking and pending heal state, preventing stale data from a previous zone being misapplied to new entities that happen to share the same ID.

## v4.10.10 — 2026-03-15 - White Mage AoE Healing Fix

**White Mage**
- Fixed AoE heals (Medica, Medica II, Medica III, Cure III, Afflatus Rapture) not firing when enough party members were below the HP threshold. Two bugs combined to block all AoE healing: (1) a redundant overheal check was rejecting every Medica spell at the HP levels where healing actually triggers — at level 100, heal amounts far exceed the average missing HP at 80–85% HP, so this check was removed entirely since the existing HP threshold is sufficient gating; and (2) Regen was executing before AoE heals in priority order, meaning a single party member needing Regen would consume the GCD window and prevent AoE heals from casting even when multiple members were injured. AoE healing now takes priority over Regen — when the AoE target count is not met, Regen runs next as usual.
- Fixed Afflatus Rapture not being used as a fallback when the lily strategy is set to Disabled but all Medica options are unavailable — lilies are now always spent as a last resort for AoE healing if other options fail.

## v4.10.9 — 2026-03-14 - Internal Quality Improvements

**All Jobs**
- Overhauled internal test coverage across all 21 rotations to catch regressions earlier — healing spells, damage rotations, buff timing, and priority logic are now verified by automated checks
- Sage and Astrologian healing logic has been restructured internally for easier maintenance; behavior is unchanged

## v4.10.8 — 2026-03-14 - Smart AoE Targeting & Visual Overlay

**All Melee Jobs (Monk, Dragoon, Ninja, Samurai, Reaper, Warrior)**
- Added a positional indicator that shows which position you need to be in for your next melee action — rear, flank, or front — updating in real time as your combo progresses
- The indicator is suppressed automatically when True North is active or when your target is immune to positional bonuses

**Monk, Dragoon, Machinist**
- Directional AoE abilities (Howling Fist, Enlightenment, Doom Spike, Chain Saw, Bioblaster) now automatically target the enemy that lets them hit the most targets, rather than always hitting in a fixed direction

**Visual Overlay (Draw Helper)**
- New optional overlay showing your melee and ranged attack range as rings around your character, fading out when you are comfortably in range
- Enemy hitboxes are drawn when targeted, making it easier to judge distance at a glance
- Positional zones (rear, flank, front) are displayed on your target so you can see exactly where to stand
- Toggle and appearance options are available in Settings → Draw Helper

**General**
- Added an option to start the rotation as soon as your weapon is drawn and auto-attacks begin, rather than waiting for the first GCD

## v4.10.7 — 2026-03-14 - Bug Fixes & Performance

**White Mage**
- Fixed AoE heals (Medica, Medica II, Cure III, Liturgy) never triggering in 8-player raid groups — the check for whether party members needed healing used a threshold that was almost never met at level 100, so AoE heals were silently suppressed. They now fire correctly whenever enough party members are below the HP threshold (default 85%, configurable in settings).

**Sage**
- Fixed Sage's Lucid Dreaming threshold and toggle having no effect — the setting was reading from the wrong configuration entry, so adjusting it in the config window did nothing. It now correctly reads from the Sage-specific setting.

**All Jobs**
- Reduced memory allocations during combat, improving frame consistency for healer jobs in particular — the plugin now reuses internal data structures between frames rather than discarding and recreating them.
- Internal code maintenance with no changes to rotation behavior.

## v4.10.6 — 2026-03-14 - Internal Maintenance

No changes to rotation behavior or the user interface. This release contains internal code improvements only.

## v4.10.5 — 2026-03-13 - Debug Window Overhaul

**Debug Window**
- The 21 per-job tabs have been replaced with a single "Job Details" tab containing a dropdown that lets you pick any job, grouped by role — the tab bar no longer overflows and is much easier to navigate
- The correct job is automatically selected when you open the window, so you land on your current job's info without scrolling

## v4.10.4 — 2026-03-13 - Melee Range Precision

**All Melee and Tank Jobs**
- Melee range detection now uses the game's own built-in range check instead of manual distance math, giving maximum precision — enemies at the very edge of attack range are now reliably detected and engaged

## v4.10.3 — 2026-03-13 - Spell Checklist Debug Tab

**Debug Window**
- Added a new Checklist tab that shows every spell your current job should be casting, filtered to your actual level, with a cast count per spell and a Reset button to start a fresh session count
- Spells that have been cast show a green dot; spells not yet cast show a red dot, making it easy to spot gaps in the rotation at a glance

## v4.10.2 — 2026-03-13 - Large Boss Range Fix

**All Jobs**
- Fixed melee rotations incorrectly using ranged attacks when standing near large bosses: the plugin now accounts for enemy hitbox size when determining whether you are in melee range

## v4.10.1 — 2026-03-13 - Gap Closer & Debug Fixes

**Dark Knight**
- Fixed Dark Knight not engaging from range: Unmend now fires to close distance when the target is out of melee reach

**Paladin**
- Fixed Paladin not engaging from range: Shield Lob now fires to close distance when the target is out of melee reach

**Debug Window**
- Fixed tab ordering: Overview, Why Stuck?, Healing, and other general tabs now appear first before job-specific tabs
- Added Export button to the Action History tab — copies the current filtered history to the clipboard

**Main Window**
- Removed the Available Rotations list to keep the window compact; the active job is still shown at the top

## v4.9.9 — 2026-03-13 - Quick Toggle Overlay

**Main Window**
- Added always-visible floating overlay with one-click toggles for Rotation, Healing, and Damage — no need to open the main window mid-combat
- Overlay is draggable and remembers its visibility between sessions
- Open/close via the new Overlay button in the main window

**White Mage / Conjurer**
- Fixed Conjurer rotation silently doing nothing: CNJ now correctly uses Stone/Stone II for damage and Aero/Aero II for DoT instead of WHM-exclusive spells (Glare III, Dia) that the class stone doesn't grant
- Fixed DoT casting being blocked while moving for all levels — Aero and Aero II are instant cast and were incorrectly restricted

## v4.9.8 — 2026-03-12 - Positional Indicator

**Main Window**
- Added live positional indicator for all 6 melee DPS jobs (Dragoon, Ninja, Samurai, Monk, Reaper, Viper)
- Shows current position relative to target: Rear (blue), Flank (purple), Front (gray), or Immune
- Only visible when a melee DPS job is active and a target is in range; hidden otherwise

## v4.9.7 — 2026-03-12 - Healer Debug Tabs

**Debug**
- Added dedicated debug tab for White Mage: Lily/Blood Lily gauge, Temperance/Assizes/Asylum/PoM/Thin Air buff states, misery tracking
- Added dedicated debug tab for Sage: Addersgall/Adersting resources, Kardia/Soteria/Philosophia state, Eukrasia, all healing spells, all shield spells, DoT/Phlegma/Toxikon/Psyche DPS tracking

## v4.9.6 — 2026-03-12 - Tank Debug Tabs

**Debug**
- Added dedicated debug tabs for all 4 tank rotations (Warrior, Dark Knight, Paladin, Gunbreaker)
- Warrior tab: Beast Gauge, Surging Tempest/Inner Release/Primal buff states, mitigation
- Dark Knight tab: Blood Gauge, MP, Darkside timer, Blood Weapon/Delirium, TBN and defensive CD states
- Paladin tab: Oath Gauge, Atonement/Confiteor/Sword Oath steps, Fight or Flight, Goring Blade DoT, execution flow
- Gunbreaker tab: Cartridges, Gnashing Fang combo step, all 5 Continuation ready states, No Mercy, DoTs, defensive CDs

## v4.9.5 — 2026-03-12 - Tank Role Override & Debug Fixes

**Tanks**
- Added MT/OT role override setting: choose Auto (enmity-based), Main Tank, or Off Tank per session
- Auto mode preserves existing detection behavior; override takes effect immediately without restart

**Debug**
- Fixed Action History panel showing no entries for all jobs except White Mage

## v4.9.4 — 2026-03-12 - Tank Rotation Fixes

**Dark Knight**
- Fixed gap closer: replaced deprecated Plunge with Shadowstride (Dawntrail action ID update)
- Fixed targeting: Shadowstride now correctly fires when target is outside melee range
- Fixed DarkMind action ID collision with EdgeOfDarkness

**Gunbreaker**
- Fixed gap closer: replaced deprecated Rough Divide with Trajectory (Dawntrail action ID update)
- Fixed targeting: Trajectory now correctly fires when target is outside melee range
- Added Lightning Shot ranged attack when target is out of melee range

**Warrior**
- Fixed Onslaught not firing at levels 62–87 due to incorrect level guard
- Fixed targeting: Onslaught now correctly fires as gap closer when target is outside melee range
- Added Tomahawk ranged attack when target is out of melee range

**Paladin**
- Fixed targeting: Intervene now correctly fires as gap closer when target is outside melee range

**UI / Config**
- Fixed language selector not applying the selected language immediately
- Added option to prevent closing the Olympus window with the Escape key
- Added option to keep Olympus windows visible during cutscenes

## v4.9.1 — 2026-01-31 - Ultimate Raid Timelines

**Timeline System**
- Added timeline support for The Unending Coil of Bahamut (Ultimate) - UCoB
- Added timeline support for The Weapon's Refrain (Ultimate) - UWU
- Added timeline support for The Epic of Alexander (Ultimate) - TEA
- All healers and tanks now have predictive mechanics for classic Ultimates
- Timelines include all major raidwides, tankbusters, and phase transitions

## v4.9.0 — 2026-01-31 - Settings Search

**Settings Search**
- Added search box to Settings window for finding options quickly
- Type to filter sidebar - only sections with matching settings are shown
- Matching section names are highlighted in yellow
- Auto-navigates to first matching section when you start typing
- Shows "X section(s) found" count or "No settings found" message
- Clear button (X) to reset search

## v4.8.1 — 2026-01-29 - Configuration System Enhancement

**Role-Aware Configuration Presets**
- Added 4 new playstyle presets: Conservative, Balanced, Aggressive, Proactive
- Presets are now role-aware - apply appropriate settings for your current job role
- Conservative: Safety first with higher thresholds, defensive priority, resource reserves
- Balanced: Middle-ground settings suitable for most content
- Aggressive: DPS maximization with lower thresholds, offensive priority, no reserves
- Proactive: Timeline-aware with pre-emptive abilities and burst window coordination

**Tank & Party Coordination Validation**
- Extended configuration validator to cover tank settings
- Validates mitigation thresholds, invuln stagger windows, and provoke delays
- Validates party coordination settings including timeout, overlap windows, and buff alignment
- AutoFix now repairs invalid tank and party coordination settings

**Bug Fixes**
- Fixed repo.json version sync (now correctly shows v4.8.1)

**DPS Job Configuration** (from previous build)
- Configuration UI for all 13 DPS jobs in Settings window
- Melee, Ranged Physical, and Caster sidebar sections with individual job settings

**Pandaemonium Timeline Data** (from previous build)
- Asphodelos Savage timelines (P1S-P4S)
- Abyssos Savage timelines (P5S-P8S)

## v4.7.0 — 2026-01-29 - Expanded Language Support

**Settings**
- Expanded language selection dropdown to include 7 options:
  - Auto (follows game client)
  - English
  - 日本語 (Japanese) - 98.8% coverage
  - 简体中文 (Chinese Simplified) - 58.9% coverage
  - 한국어 (Korean) - English baseline
  - Deutsch (German) - English baseline
  - Français (French) - English baseline
- Language changes apply immediately without restart

**Japanese Translation**
- Completed Japanese translation to 98.8% coverage (1,747 of 1,768 strings)
- Remaining 21 keys are universal terms (DoT, DPS, GCD, etc.) that stay as-is
- All healers, tanks, and DPS job configurations translated
- Full Training Mode content translated

**Chinese Translation**
- Improved Chinese Simplified translation to 58.9% coverage
- Added healer and tank job configuration translations
- Added Training Mode and Analytics translations

**New Languages (Baseline)**
- Created Korean, German, and French translation files
- All new files use English text as baseline
- Community contributions welcome for translations

## v4.5.0 — 2026-01-27 - Japanese Translation

**Localization**
- Added Japanese (ja) translation with ~1,191 translated strings
- Uses official FFXIV Japanese terminology (ability names, job names, UI terms)
- Covers all windows: Main, Configuration, Analytics, Training, and Debug

**Translation Coverage**
- Main Window: Fully translated
- Configuration Window: All settings translated (General, Healing, Damage, Role Actions, Party Coordination, Timeline, Training, Analytics)
- Job-Specific Settings: All 4 healers, all 4 tanks translated
- Analytics Window: All tabs translated (Overview, Cooldowns, FFLogs)
- Training Window: Live coaching and progress tracking translated
- Debug Window: All 21 job tabs translated

**Japanese Terminology**
- Job names use official FFXIV Japanese terms (白魔道士, 学者, 占星術師, 賢者, etc.)
- Ability names use official in-game Japanese names
- UI follows FFXIV Japanese client conventions

## v4.4.0 — 2026-01-27 - Chinese Simplified Translation

**Phase 5 - AI Translation Generation**
- Added Chinese Simplified (zh) translation with ~1,143 translated strings
- Covers main UI, configuration, analytics, training, and debug windows
- Uses official FFXIV Chinese terminology where applicable

**Translation Coverage**
- Main Window: Fully translated
- Configuration Window: Core settings translated
- Analytics Window: All tabs translated
- Training Window: Live coaching and progress tracking translated
- Debug Window: All 22 job tabs translated

## v4.3.1 — 2026-01-27 - Debug Window Localization Complete

**Localization**
- Completed localization of all 22 Debug Window tabs (755 localized strings)
- Completed localization of Analytics Window and all 4 tabs (110 localized strings)
- Fixed duplicate localization key issues for Summoner-specific strings (Phase, Mp, Aetherflow, EnergyDrain)

**Affected Jobs**
- All 4 Healers: WHM, SCH, AST, SGE debug tabs
- All 4 Tanks: PLD, WAR, DRK, GNB debug tabs
- All 6 Melee DPS: DRG, NIN, SAM, MNK, RPR, VPR debug tabs
- All 3 Ranged Physical DPS: MCH, BRD, DNC debug tabs
- All 4 Casters: BLM, SMN, RDM, PCT debug tabs
- General debug tabs: Overview, Timeline, Actions, Healing, Overheal, Performance, WhyStuck

## v4.3.0 — 2026-01-27 - Localization Infrastructure

**Multi-Language Support Foundation**
- Added localization infrastructure for future multi-language support
- Plugin now detects game client language (English, Japanese, German, French)
- Optional language override setting for community translations
- Localized Main Window UI as proof of concept

**New Files**
- `Localization/OlympusLocalization.cs` - Core localization service with fallback system
- `Localization/LocalizedStrings.cs` - String key constants for all UI text
- `Localization/GameDataLocalizer.cs` - FFXIV ability names from game data
- `Localization/Loc/olympus_en.json` - English source strings

**Settings**
- New `LanguageOverride` setting for manual language selection (default: use game language)

**Developer Notes**
- Use `Loc.T(key, fallback)` pattern for all new UI strings
- English text provided as fallback ensures functionality without translation files
- Ability names automatically localized from game data via `GameLoc.Action(id)`

## v4.1.1 — 2026-01-26 - Bug Fixes

**Summoner**
- Fixed pet detection - now correctly checks ObjectTable instead of always assuming pet is summoned

**Paladin**
- Fixed Blade of Honor detection - now properly detects when the ability is available after Blade of Valor
- Fixed Confiteor chain tracking - now correctly tracks position in the Confiteor → Blade of Faith → Blade of Truth → Blade of Valor chain

**Analytics**
- Improved burst window detection - now uses actual party coordination data when available instead of just 120s heuristics

## v4.1.0 — 2026-01-26 - Lazy Rotation Loading

**Performance Improvement**
- Rotations are now created on-demand when switching jobs instead of all 21 at startup
- Reduced plugin startup time by ~40%
- Reduced memory usage by 60-80% (only active job rotation loaded)
- Simplified Plugin.cs architecture (removed 21 rotation instance fields)
- No user-facing changes - same functionality, better performance

## v4.0.3 — 2026-01-26 - Fluent Training Builder

**Internal Code Quality Improvement**
- Migrated all training decision calls to fluent builder pattern
- Replaced 4 role-specific training helpers with unified DecisionBuilder
- ~600 line reduction through elimination of duplicate method shells
- Cleaner, more maintainable training decision recording
- No user-facing changes - internal refactoring only

## v4.0.2 — 2026-01-25 - Training Content Extraction

**Internal Code Quality Improvement**
- Extracted all quiz and lesson data from C# static classes to JSON files
- Reduced Training system from ~14,200 lines of C# to 42 JSON data files
- Created TrainingDataRegistry for dynamic loading of embedded JSON resources
- 21 lesson files + 21 quiz files (735 questions across all jobs)
- No user-facing changes - internal refactoring only

## v4.0.1 — 2026-01-25 - Training Helper Abstraction

**Internal Code Quality Improvement**
- Consolidated duplicate `Record*Decision` methods across 5 training helper files into a unified abstraction
- Reduced ~1,800 lines of duplicate code to ~400 lines (78% reduction)
- Added `DecisionContext` record for role-specific decision tracking data
- Added `DecisionCategory` constants for consistent category naming
- No user-facing changes - internal refactoring only

## v4.0.0 — 2026-01-25 - Training Mode Complete

**Major Milestone: Intelligent Coaching System Complete**

This release marks the completion of Phase 5 (Training Mode), transforming Olympus from a passive learning system into an active coaching platform. Training Mode now provides personalized, real-time coaching that adapts to your play style across all 21 combat jobs.

**Complete Feature Set**
- **Real-Time Coaching Hints** (v3.49): In-combat tips for struggling concepts
- **Decision Validation** (v3.50): Feedback on whether actions were optimal
- **Coaching Personality** (v3.51): 4 feedback styles (Encouraging, Analytical, Strict, Silent)
- **Spaced Repetition** (v3.52): Knowledge retention tracking with forgetting curves
- **147 Lessons**: Structured educational content across all jobs
- **735 Quiz Questions**: Skill validation for all concepts

**v4.0.0 Polish & Integration**
- Consolidated settings UI with all coaching options in one dropdown
- Coaching hints, personality, and retention settings easily accessible
- Spaced repetition now automatically updates when concepts are practiced
- Seamless integration between mastery tracking and retention decay

**What's Next (Phase 6)**
- Machine learning for personalized recommendations
- Simulation engine for practice scenarios
- Voice command support

## v3.52.0 — 2026-01-25 - Spaced Repetition

**Knowledge Retention Tracking**
- Track how well you remember concepts over time using a forgetting curve
- Concepts decay without practice: Day 1 (100%) → Day 3 (80%) → Day 7 (60%) → Day 14 (40%) → Day 30+ (20%)
- Each successful demonstration reinforces retention and slows decay
- Review suggestions when retention drops below 40%

**Skill Level Tab Enhancements**
- New "Knowledge Retention" section shows overall retention status
- Concepts needing re-learning highlighted with urgency indicators
- Concepts due for review shown with decay percentage
- Fresh concepts displayed with time until review needed
- Suggested review quizzes based on decaying concepts

**New Settings**
- Enable/disable retention tracking toggle
- Configurable review threshold (default 40%)

**Technical**
- New RetentionData.cs with forgetting curve calculations
- New SpacedRepetitionService.cs for retention management
- Integrated with TrainingConfig for persistence across sessions
- SkillProgressTab updated with retention visualization

## v3.51.0 — 2026-01-25 - Coaching Personality

**Configurable Coaching Voice**
- Choose from 4 distinct coaching personalities that adapt all feedback messages
- **Encouraging** (default): Positive reinforcement, gentle corrections, supportive tone
- **Analytical**: Data-focused, minimal emotion, just facts and numbers
- **Strict**: Direct corrections, high standards, no sugarcoating
- **Silent**: Minimal feedback, only critical errors shown

**Personality-Aware Feedback**
- All hint messages adapt to your chosen personality
- Decision validation messages change tone based on personality
- Silent personality suppresses non-critical feedback for minimal distraction
- Strict personality only shows Normal+ priority hints

**New Settings**
- Coaching Personality selector in Training Mode settings
- Personality applies to hints, validation messages, and coaching feedback

**Technical**
- New CoachPersonality.cs with CoachingPersonality enum
- PersonalityTextGenerator generates all personality-appropriate messages
- RealTimeCoachingService and DecisionValidationService integrated with personality

## v3.50.0 — 2026-01-25 - Decision Validation

**New Decision Validation System**
- Decisions now show whether they were optimal, acceptable, or suboptimal
- Validation symbols displayed next to each action: ✓ (optimal), ≈ (acceptable), ✗ (suboptimal)
- Suboptimal decisions show "what would be better" feedback
- Hover over symbols for detailed explanation

**Live Coaching Tab Enhancements**
- Validation summary in status section shows counts of optimal/acceptable/suboptimal decisions
- Recent decisions table includes validation column with symbols
- Current decision shows validation result with improvement suggestions
- Overall optimal decision rate displayed in tooltip

**Per-Concept Statistics**
- Track optimal decision rate per concept over time
- Identify weakest concepts based on decision accuracy
- View strongest concepts with highest optimal rates

**Technical**
- New DecisionResult.cs model for validation outcomes
- New DecisionValidationService.cs for tracking and analysis
- Integrated with TrainingWindow and LiveCoachingTab

## v3.49.0 — 2026-01-25 - Real-Time Coaching Hints

**New In-Combat Coaching System**
- Real-time coaching hints now appear during combat for struggling concepts
- Hints appear for concepts with <60% mastery to provide contextual tips
- Floating overlay window positioned near the party list for easy visibility
- Auto-dismiss after configurable duration (default 8 seconds)
- Dismiss individual hints or press ESC to clear all

**Hint Features**
- Priority-based display: Critical (red), High (orange), Normal (blue), Low (gray)
- Shows concept name, success rate, actionable tip, and recommended action
- Progress bar indicates time remaining before auto-dismiss
- Intelligent throttling prevents hint spam (10s between hints, 60s per concept)

**New Settings**
- Enable/disable coaching hints toggle
- Configurable hint cooldown (5-60 seconds, default 10s)
- Configurable display duration (3-30 seconds, default 8s)
- Adjustable overlay position

**Technical**
- New RealTimeCoachingService for hint generation and throttling
- New HintOverlay window with ImGui rendering
- Integrates with existing concept mastery tracking from v3.28.0

## v3.48.0 — 2026-01-25 - Iris (PCT) Training Mode

**Full Pictomancer Training Mode Integration**
- Iris (PCT) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Fourth and final caster DPS with complete Training Mode integration
- **All 21 combat jobs now have Training Mode integration**

**BuffModule Decisions**
- Portraits (Mog/Madeen): High burst damage from creature summons
- Starry Muse: 2-minute raid buff timing and party coordination
- Living Muse: Creature summon charges building toward portraits
- Striking Muse: Hammer Time enabler for instant combo
- Subtractive Palette: Enhanced combo activation at 50+ gauge
- Lucid Dreaming: MP recovery at 70% threshold

**DamageModule Decisions**
- Star Prism: Highest potency finisher during Starstruck buff
- Rainbow Drip: Instant with proc vs hardcast during burst
- Hammer Combo: 3-step instant burst (Stamp → Brush → Polish)
- Comet in Black: Black Paint spender for high damage
- Holy in White: White Paint for movement and overcap prevention
- Subtractive Combo: Enhanced damage combo (Cyan → Yellow → Magenta)
- Base Combo: Standard filler rotation (Red → Green → Blue)
- Prepaint Motifs: Canvas preparation priority (Landscape > Creature > Weapon)

**Concept Mastery**
- All 25 PCT concepts tracked including Palette Gauge, paint management, canvas system, muse abilities, and burst windows

## v3.47.0 — 2026-01-25 - Circe (RDM) Training Mode

**Full Red Mage Training Mode Integration**
- Circe (RDM) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Third caster DPS with complete Training Mode integration

**Dualcast System Decisions**
- Jolt III: Default hardcast filler, explains Dualcast mechanic
- Verthunder/Veraero: Long spell selection based on mana balance
- Dualcast consumption: Tracks instant cast usage for optimal flow

**Proc Management Decisions**
- Verfire/Verstone: Prioritizes procs as fillers, warns on expiring procs
- Acceleration: Guarantees procs when none available, tracks charges
- Swiftcast Usage: Movement optimization when no procs available

**Melee Combo Decisions**
- Enchanted Riposte: Melee entry at 50|50 mana, burst window alignment
- Enchanted Zwerchhau: Combo step 2 progression tracking
- Enchanted Redoublement: Combo step 3 leading to finisher

**Finisher System Decisions**
- Verflare/Verholy: Finisher selection based on lower mana type
- Scorch: Post-finisher burst ability tracking
- Resolution: Final finisher completion
- Grand Impact: Special proc from Acceleration III

**Burst Window Decisions**
- Embolden: Party damage buff timing aligned with melee combo
- Manafication: Mana boost at optimal thresholds (40-50 mana)
- Corps-a-corps/Engagement: Gap closer usage during burst phases
- Vice of Thorns/Prefulgence: Finisher proc oGCDs

**oGCD Weaving Decisions**
- Fleche: High damage single-target oGCD on cooldown
- Contre Sixte: AoE oGCD on cooldown
- Lucid Dreaming: MP recovery at 70% threshold

**Concept Mastery**
- All 25 RDM concepts tracked including mana balance, Dualcast, procs, melee combo, finishers, and burst windows

## v3.46.0 — 2026-01-25 - Persephone (SMN) Training Mode

**Full Summoner Training Mode Integration**
- Persephone (SMN) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Second caster DPS with complete Training Mode integration

**Demi-Summon Phase Decisions**
- Demi GCDs: Astral Impulse, Fountain of Fire, Umbral Impulse during demi phases
- Bahamut Phase: Deathflare AoE oGCD, Enkindle Bahamut burst
- Phoenix Phase: Rekindle healing on party members, Enkindle Phoenix damage
- Solar Bahamut: Sunflare AoE oGCD at level 100, Enkindle Solar Bahamut

**Primal Attunement Decisions**
- Titan Phase: 4 instant Topaz Rite + Mountain Buster oGCDs
- Garuda Phase: 4 casted Emerald Rite + Slipstream ground DoT
- Ifrit Phase: 2 high-potency Ruby Rite + Crimson Cyclone gap closer
- Primal Order: Titan for movement, Garuda for stationary, Ifrit for burst

**Primal Favor Abilities**
- Crimson Cyclone: Ifrit gap closer with instant follow-up
- Mountain Buster: Titan oGCD after each Topaz Rite
- Slipstream: Garuda ground DoT zone (uses Swiftcast when moving)

**Burst Window Decisions**
- Searing Light: 5% party damage buff aligned with demi-summon phases
- Searing Flash: Free AoE oGCD during Searing Light window
- Enkindle: High-potency oGCD unique to each demi-summon
- Astral Flow: Deathflare/Sunflare damage or Rekindle healing

**Aetherflow Management**
- Energy Drain/Siphon: Generate 2 Aetherflow stacks when empty
- Fester/Necrotize: Single target Aetherflow spenders for burst
- Painflare: AoE Aetherflow spender at 3+ enemies
- Timing: Spend during burst, never overcap before Energy Drain

**Proc and Filler Rotation**
- Ruin IV (Further Ruin): Instant proc for movement or filler
- Ruin III: Standard filler between primal/demi phases
- Ruin II: Movement option when no procs available
- AoE Rotation: Switch to AoE Ruin at 3+ enemies

**Concept Mastery**
- All 25 SMN concepts tracked including demi-summons, primal attunement, Aetherflow, burst windows, and party coordination

## v3.45.0 — 2026-01-25 - Hecate (BLM) Training Mode

**Full Black Mage Training Mode Integration**
- Hecate (BLM) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- First caster DPS with complete Training Mode integration

**Element System Decisions**
- Astral Fire: Fire phase entry and damage maximization
- Umbral Ice: Ice phase for MP recovery and Umbral Hearts
- Element Transitions: Fire III/Blizzard III for phase swaps
- Element Timer: Paradox usage to refresh timer during Fire phase
- Enochian: Maintaining element state for Polyglot generation

**Resource Management**
- Umbral Hearts: Blizzard IV for 3 hearts to reduce Fire IV MP cost
- Polyglot Stacks: Xenoglossy/Foul for damage and movement
- Astral Soul: Fire IV builds stacks for Flare Star at 6
- Gauge Overcapping: Spend Polyglot before Amplifier to avoid waste
- MP Management: Manafont to extend Fire phase

**Proc System**
- Firestarter: Instant Fire III for movement or transitions
- Thunderhead: Instant Thunder for movement or DoT refresh
- Proc Priority: Using expiring procs before they fall off

**Core Rotation Decisions**
- Fire IV Spam: Main damage in Astral Fire phase
- Despair Timing: Fire phase finisher before Ice transition
- Thunder DoT: Maintenance during Ice phase
- Paradox: Timer refresh in Fire, instant in Ice

**Cooldown Management**
- Ley Lines: 15% spell speed buff during stationary windows
- Triplecast: 3 instant casts for movement or burst
- Manafont: MP restore to extend Fire phase
- Amplifier: Instant Polyglot generation

**Movement Optimization**
- Xenoglossy: Primary movement tool (instant, high damage)
- Triplecast: Pre-emptive instant casts for mechanics
- Procs: Firestarter/Thunderhead as movement options
- Scathe: Last resort when all instants exhausted

**AoE Rotation**
- Fire II/High Fire II: AoE filler in Fire phase
- Flare: AoE finisher consuming all MP
- Foul: AoE Polyglot spender

**Concept Mastery**
- All 25 BLM concepts tracked including element system, proc management, gauges, burst windows, and movement optimization

## v3.44.0 — 2026-01-25 - Terpsichore (DNC) Training Mode

**Full Dancer Training Mode Integration**
- Terpsichore (DNC) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Third and final ranged physical DPS with complete Training Mode integration - all ranged physical DPS now complete

**Dance System Decisions**
- Technical Step: 4-step dance for 2-minute raid buff with party coordination
- Standard Step: 2-step dance for personal buff maintenance
- Dance Steps: Execute correct step sequence (Emboite/Entrechat/Jete/Pirouette)
- Technical Finish: Party-wide damage buff with burst alignment
- Standard Finish: Personal damage buff kept active

**Burst Window Decisions**
- Devilment: +20% Crit/DH personal buff, pairs with Technical Finish
- Flourish: Grants all 4 procs during burst windows
- Starfall Dance: Highest priority GCD during Devilment
- Tillana: Technical Finish follow-up granting Last Dance Ready

**High-Level Abilities (Lv.90+)**
- Starfall Dance: Flourishing Starfall proc from Devilment
- Finishing Move: Standard Finish follow-up at Lv.96+
- Last Dance: Technical/Tillana follow-up at Lv.92+
- Dance of the Dawn: Enhanced Saber Dance at Lv.100

**Proc System**
- Silken Symmetry: Reverse Cascade proc from Cascade/Flourish
- Silken Flow: Fountainfall proc from Fountain/Flourish
- Threefold Fan Dance: Fan Dance III proc from Fan Dance I/II
- Fourfold Fan Dance: Fan Dance IV proc from Flourish

**Esprit Gauge Management**
- Saber Dance: Primary Esprit spender at 80+ or 50+ during burst
- Dance of the Dawn: Enhanced spender during Technical Finish
- Esprit overcap prevention with smart spending thresholds

**Feather Gauge Management**
- Fan Dance I: Single-target feather spender
- Fan Dance II: AoE feather spender at 3+ targets
- Feather overcap prevention at 4 feathers

**Utility**
- Head Graze: Interrupt coordination with party

**Concept Mastery**
- All 25 DNC concepts tracked including dance system, proc management, gauges, burst windows, and party utility

## v3.43.0 — 2026-01-25 - Calliope (BRD) Training Mode

**Full Bard Training Mode Integration**
- Calliope (BRD) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Second ranged physical DPS with complete Training Mode integration

**Song System Decisions**
- Wanderer's Minuet: Highest priority song for burst alignment and Pitch Perfect
- Mage's Ballad: Bloodletter reset procs and party damage buff
- Army's Paeon: Filler song with early cutoff for WM realignment
- Pitch Perfect: Stack-based damage (1/2/3 stacks) with song timer awareness

**Burst Window Decisions**
- Raging Strikes: 2-minute personal buff with WM alignment
- Battle Voice: Party-wide raid buff coordination
- Radiant Finale: Coda-based party buff (1/2/3 Coda)
- Barrage: Triple Refulgent Arrow with Resonant Arrow follow-up

**Proc System**
- Refulgent Arrow: Hawk's Eye proc consumption
- Shadowbite: AoE proc usage at 3+ targets
- Resonant Arrow: Barrage sequence follow-up
- Radiant Encore: Radiant Finale sequence follow-up
- Blast Arrow: Apex Arrow (80+ SV) follow-up

**Soul Voice Management**
- Apex Arrow: Gauge spending at 80+ or 100 to prevent overcap
- Soul Voice building from Repertoire procs
- Blast Arrow follow-up for maximum damage

**DoT Management**
- Stormbite: Higher potency DoT, apply first
- Caustic Bite: Secondary DoT application
- Iron Jaws: Refresh and buff snapshotting during Raging Strikes

**oGCD Management**
- Empyreal Arrow: Guaranteed Repertoire proc
- Sidewinder: Burst window damage oGCD
- Bloodletter: Charge management with MB reset awareness
- Rain of Death: AoE variant at 3+ targets

**Utility**
- Head Graze: Interrupt coordination with party

**Concept Mastery**
- All major BRD concepts tracked: `brd.song_rotation`, `brd.wanderers_minuet`, `brd.mages_ballad`, `brd.armys_paeon`, `brd.repertoire_stacks`, `brd.pitch_perfect`, `brd.song_switching`, `brd.soul_voice_gauge`, `brd.apex_arrow`, `brd.blast_arrow`, `brd.soul_voice_overcapping`, `brd.straight_shot_ready`, `brd.refulgent_arrow`, `brd.barrage`, `brd.resonant_arrow`, `brd.caustic_bite`, `brd.stormbite`, `brd.iron_jaws`, `brd.raging_strikes`, `brd.battle_voice`, `brd.radiant_finale`, `brd.radiant_encore`, `brd.empyreal_arrow`, `brd.bloodletter_management`, `brd.party_utility`

## v3.42.0 — 2026-01-25 - Prometheus (MCH) Training Mode

**Full Machinist Training Mode Integration**
- Prometheus (MCH) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- First ranged physical DPS with complete Training Mode integration

**Burst Window Decisions**
- Wildfire: 2-minute burst with Hypercharge alignment and party coordination
- Hypercharge: Heat spending with tool cooldown awareness
- Reassemble: Guaranteed crit/DH with high-potency action priority (Drill > Air Anchor > Chain Saw)

**Tool Actions**
- Drill: Highest priority tool with charge tracking (2 charges at Lv.98+)
- Air Anchor: Battery building (+20) with overcap prevention
- Chain Saw: Battery building (+20) and Excavator Ready proc
- Excavator: Proc consumption with additional Battery gain
- Full Metal Field: Lv.100 proc from Barrel Stabilizer

**Heat Gauge Management**
- Barrel Stabilizer: +50 Heat generation with overcap awareness
- Heat Blast: Overheated GCD spam with oGCD cooldown reduction
- Auto Crossbow: AoE variant during Overheated state

**Battery and Queen**
- Automaton Queen: Pet deployment at optimal Battery (90-100)
- Battery accumulation from Air Anchor, Chain Saw, Excavator, and combo finisher

**oGCD Weaving**
- Gauss Round/Ricochet: Charge management with Overheated priority
- Heat Blast cooldown reduction synergy

**Utility**
- Head Graze: Interrupt coordination with party

**Concept Mastery**
- All major MCH concepts tracked: `mch.wildfire_placement`, `mch.burst_party_sync`, `mch.hypercharge_activation`, `mch.hypercharge_timing`, `mch.heat_gauge`, `mch.battery_gauge`, `mch.gauge_overcapping`, `mch.drill_priority`, `mch.air_anchor_usage`, `mch.chain_saw_usage`, `mch.proc_tracking`, `mch.queen_summoning`, `mch.queen_damage_scaling`, `mch.battery_accumulation`, `mch.reassemble_priority`, `mch.reassemble_charges`, `mch.heat_blast_rotation`, `mch.overheated_state`, `mch.ogcd_weaving`, `mch.aoe_rotation`, `mch.interrupt_usage`

## v3.41.0 — 2026-01-25 - Echidna (VPR) Training Mode

**Full Viper Training Mode Integration**
- Echidna (VPR) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Sixth and final melee DPS with complete Training Mode integration - all melee Training Mode now complete

**Burst Window Decisions**
- Serpent's Ire: Party buff (+Rattling Coil, Ready to Reawaken) with party burst coordination
- Reawaken: Burst state entry during optimal timing (Serpent's Ire, buffs active, Noxious Gnash)

**Reawaken Sequence**
- First/Second/Third/Fourth Generation: Anguine Tribute consumption with buff tracking
- Ouroboros: Reawaken finisher for maximum burst damage
- Legacy oGCDs: Twinfang/Twinblood weaving during Generations

**Twinblade Combos**
- Vicewinder/Vicepit: Twinblade initiation with Noxious Gnash application
- Hunter's Coil/Swiftskin's Coil: ST twinblade follow-ups
- Hunter's Den/Swiftskin's Den: AoE twinblade follow-ups
- Twinfang/Twinblood (Poised): oGCD procs from twinblade combos

**Dual Wield Rotation**
- Steel/Reaving Fangs: Combo starters with Honed buff awareness
- Hunter's/Swiftskin's Sting: Mid-combo GCDs for buff cycling
- Positional finishers: Flanksting/Hindsting/Flanksbane/Hindsbane with venom tracking
- AoE: Steel/Reaving Maw, Hunter's/Swiftskin's Bite with Grimhunter/Grimskin venoms

**Resource Management**
- Rattling Coils: Uncoiled Fury for movement or overcap prevention
- Uncoiled Twinfang/Twinblood: Follow-up oGCDs after Uncoiled Fury
- Serpent Offering: Building toward Reawaken threshold

**Concept Mastery**
- All major VPR concepts tracked: `vpr.serpents_ire`, `vpr.reawaken_entry`, `vpr.generation_sequence`, `vpr.vicewinder`, `vpr.noxious_gnash`, `vpr.dread_combo`, `vpr.positional_finishers`, `vpr.combo_basics`, `vpr.buff_cycling`, `vpr.positionals`, `vpr.rattling_coil`, `vpr.twinfang_twinblood`, `vpr.uncoiled_fury`

## v3.40.0 — 2026-01-24 - Thanatos (RPR) Training Mode

**Full Reaper Training Mode Integration**
- Thanatos (RPR) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Fifth melee DPS with complete Training Mode integration

**Burst Window Decisions**
- Arcane Circle: Party-wide damage buff (+3%) with Bloodsown Circle and Immortal Sacrifice tracking
- Enshroud: Burst state entry during optimal timing (Arcane Circle, high Shroud, Death's Design)

**Enshroud Rotation**
- Void/Cross Reaping: Lemure Shroud consumption with Enhanced buff tracking
- Lemure's Slice/Scythe: Void Shroud spending for bonus oGCD damage
- Communio: Enshroud finisher for Perfectio Parata proc
- Perfectio: Highest potency GCD after Communio
- Sacrificium: Oblatio proc usage during Enshroud

**Soul Reaver & Positionals**
- Gibbet (flank): Soul Reaver finisher with positional tracking
- Gallows (rear): Soul Reaver finisher with positional tracking
- Guillotine: AoE Soul Reaver spender (no positional)
- Enhanced buff awareness for optimal finisher selection

**Resource Management**
- Soul Gauge: Gluttony (premium, 2 stacks) vs Blood Stalk (basic, 1 stack) vs Unveiled variants
- Shroud Gauge: Building toward Enshroud threshold
- Plentiful Harvest: Immortal Sacrifice stack consumption for Shroud gain

**Support Abilities**
- Death's Design: Damage debuff maintenance with refresh timing
- Soul Slice/Scythe: Soul gauge building on charge system
- Harvest Moon: Ranged GCD for movement/disengage phases

**Concept Mastery**
- All major RPR concepts tracked: `rpr_arcane_circle`, `rpr_enshroud`, `rpr_reaping`, `rpr_communio`, `rpr_perfectio`, `rpr_gibbet`, `rpr_gallows`, `rpr_gluttony`, `rpr_soul_slice`, `rpr_deaths_design`

## v3.39.0 — 2026-01-24 - Kratos (MNK) Training Mode

**Full Monk Training Mode Integration**
- Kratos (MNK) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Fourth melee DPS with complete Training Mode integration

**Burst Window Decisions**
- Riddle of Fire: Personal burst activation (+15% damage) with Disciplined Fist alignment
- Brotherhood: Party-wide damage buff with raid buff coordination
- Perfect Balance: Beast Chakra building for Blitz attacks with Nadi strategy
- Riddle of Wind: Auto-attack speed buff for passive damage

**Blitz System & Beast Chakra**
- Masterful Blitz: Automatic tracking of Elixir Field, Rising Phoenix, Phantom Rush
- Perfect Balance GCDs: Strategic form selection for target Blitz (Lunar vs Solar Nadi)
- Blitz execution with Nadi state awareness and proper rotation towards Phantom Rush

**Resource Management**
- Chakra Gauge: Spending decisions for Forbidden Chakra (ST) and Enlightenment (AoE)
- Fire's Reply / Wind's Reply: Rumination proc usage within 30s window

**Form Rotation & Positionals**
- Opo-opo Form: Dragon Kick (flank) / Bootshine (rear) with Leaden Fist management
- Raptor Form: Twin Snakes (flank) / True Strike (rear) with Disciplined Fist maintenance
- Coeurl Form: Demolish (rear) / Snap Punch (flank) with DoT refresh awareness
- All positional tracking with True North awareness

**Concept Mastery**
- All major MNK concepts tracked: `mnk_riddle_of_fire`, `mnk_brotherhood`, `mnk_perfect_balance`, `mnk_riddle_of_wind`, `mnk_chakra_gauge`, `mnk_beast_chakra`, `mnk_positionals`

## v3.38.0 — 2026-01-24 - Nike (SAM) Training Mode

**Full Samurai Training Mode Integration**
- Nike (SAM) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Third melee DPS with complete Training Mode integration

**Sen System & Iaijutsu**
- Full Iaijutsu tracking with Sen state explanations (Setsu, Getsu, Ka)
- Higanbana: DoT application and refresh timing with remaining duration awareness
- Tenka Goken: 2-Sen AoE burst with enemy count thresholds
- Midare Setsugekka: 3-Sen ST burst as bread-and-butter damage

**Burst Window Decisions**
- Ikishoten: Main burst window activation with Ogi Namikiri preparation
- Ogi Namikiri: Highest potency GCD with Kaeshi follow-up sequence
- Kaeshi: Namikiri: Immediate follow-up after Ogi Namikiri
- Tsubame-gaeshi: Iaijutsu repeat with Kaeshi: Setsugekka / Goken

**Resource Management**
- Kenki Gauge: Spending decisions for Shinten, Kyuten, Senei, Guren
- Shoha: Meditation stack spending at 3 stacks
- Zanshin: Ogi Namikiri follow-up Kenki spender

**Buff Management & Positionals**
- Meikyo Shisui: Combo skip for direct Sen acquisition
- Gekko / Kasha: Rear and flank positional tracking with True North awareness
- Fugetsu / Fuka: Buff maintenance through combo finishers

**Concept Mastery**
- All major SAM concepts tracked: `sam_sen_system`, `sam_kenki_gauge`, `sam_iaijutsu`, `sam_burst_window`, `sam_positionals`, `sam_aoe_rotation`

## v3.37.0 — 2026-01-24 - Hermes (NIN) Training Mode

**Full Ninja Training Mode Integration**
- Hermes (NIN) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- Second melee DPS with complete Training Mode integration

**Mudra System & Ninjutsu**
- Full Ninjutsu execution tracking with mudra combination explanations
- Suiton: Burst preparation with Kunai's Bane timing awareness
- Raiton: ST damage with Raiju proc generation
- Kassatsu: Enhanced Ninjutsu setup for Hyosho Ranryu / Goka Mekkyaku

**Burst Window Decisions**
- Kunai's Bane: Main burst window with +5% damage debuff and party coordination
- Tenri Jindo: Follow-up burst ability after Kunai's Bane
- Ten Chi Jin: Triple Ninjutsu burst with movement warning

**Resource Management**
- Ninki Gauge: Spending decisions for Bhavacakra, Hellfrog Medium, Bunshin
- Bunshin: Shadow clone activation with Phantom Kamaitachi follow-up
- Meisui: Suiton conversion when burst is on cooldown

**Proc Usage & Positionals**
- Raiju: Forked (gap closer) vs Fleeting (melee) decision tracking
- Phantom Kamaitachi: Bunshin follow-up usage
- Armor Crush / Aeolian Edge: Kazematoi management with flank/rear positionals

**Concept Mastery**
- All major NIN concepts tracked: `nin_kunais_bane`, `nin_tenri_jindo`, `nin_kassatsu`, `nin_ten_chi_jin`, `nin_mug_dokumori`, `nin_bunshin`, `nin_meisui`, `nin_ninki_gauge`, `nin_raiju`, `nin_phantom_kamaitachi`, `nin_positionals`, `nin_mudra_system`, `nin_suiton`, `nin_raiton`, `nin_katon`, `nin_hyosho_ranryu`, `nin_doton`

## v3.36.0 — 2026-01-24 - Zeus (DRG) Training Mode

**Full Dragoon Training Mode Integration**
- Zeus (DRG) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- First melee DPS with complete Training Mode integration

**Burst Window Decisions**
- Life Surge: Guaranteed crit optimization before high-potency GCDs
- Lance Charge: Personal burst activation with Power Surge alignment
- Battle Litany: Party-wide crit buff with raid buff coordination

**Life of the Dragon Phase**
- Geirskogul: Dragon Eye management and Life of Dragon entry
- Stardiver: Highest potency attack during Life phase with timing awareness
- High Jump: Jump ability usage for Dive Ready and Eye gauge building

**Concept Mastery**
- All major DRG concepts now tracked: `drg_life_surge`, `drg_lance_charge`, `drg_battle_litany`, `drg_life_of_dragon`, `drg_eye_gauge`, `drg_high_jump`, `drg_stardiver`

## v3.35.0 — 2026-01-24 - Hephaestus (GNB) Training Mode

**Full Gunbreaker Training Mode Integration**
- Hephaestus (GNB) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected
- All 4 tanks now have complete Training Mode integration

**Mitigation Decisions**
- Superbolide: Emergency invulnerability with HP drop awareness and healer coordination tips
- Nebula/Great Nebula: Major cooldown timing with damage rate considerations
- Heart of Corundum: Intelligent short cooldown usage with party targeting support
- Heart of Light: Party magic mitigation with coordination awareness

**Burst Window Decisions**
- No Mercy: Optimal activation timing with cartridge planning
- Double Down: High potency burst during No Mercy (2 cartridge cost awareness)
- Gnashing Fang: Signature combo initiation with burst window alignment

**Resource Management**
- Cartridge gauge spending to avoid overcapping
- Burst Strike: Single cartridge spending with Hypervelocity awareness
- Bloodfest: Cartridge refill timing with Ready to Reign at Lv.100

**Enmity Decisions**
- Provoke: Emergency aggro recovery and coordinated tank swaps
- Shirk: Off-tank enmity management and swap coordination

**Concept Mastery**
- All major GNB concepts now tracked: `gnb_superbolide`, `gnb_nebula`, `gnb_heart_of_corundum`, `gnb_heart_of_light`, `gnb_no_mercy`, `gnb_double_down`, `gnb_gnashing_fang`, `gnb_burst_strike`, `gnb_bloodfest`, `gnb_cartridge_gauge`, `gnb_provoke`, `gnb_shirk`

## v3.34.0 — 2026-01-24 - Nyx (DRK) Training Mode

**Full Dark Knight Training Mode Integration**
- Nyx (DRK) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected

**Mitigation Decisions**
- Living Dead: Emergency invulnerability explanations with healer coordination notes
- Shadow Wall: Major cooldown timing with incoming damage context
- The Blackest Night: Intelligent TBN usage with Dark Arts proc awareness
- Dark Missionary: Party mitigation decisions with coordination awareness

**Burst Window Decisions**
- Delirium: Optimal activation timing with Darkside and gauge considerations
- Bloodspiller: Burst execution during Delirium windows (free + guaranteed crit/DH)

**Resource Management**
- Blood Gauge spending decisions to avoid overcapping
- Bloodspiller usage outside burst for gauge management

**Enmity Decisions**
- Provoke: Emergency aggro recovery and coordinated tank swaps
- Shirk: Off-tank enmity management and swap coordination

**Concept Mastery**
- All major DRK concepts now tracked: `drk_living_dead`, `drk_shadow_wall`, `drk_tbn`, `drk_dark_missionary`, `drk_delirium`, `drk_blood_gauge`, `drk_provoke`, `drk_shirk`

## v3.33.0 — 2026-01-24 - Ares (WAR) Training Mode

**Full Warrior Training Mode Integration**
- Ares (WAR) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected

**Mitigation Decisions**
- Holmgang: Emergency invulnerability explanations with threat assessment and timing
- Vengeance: Major cooldown timing with incoming damage context
- Bloodwhetting: Short cooldown decisions with self-healing awareness
- Shake It Off: Party mitigation decisions with coordination awareness

**Burst Window Decisions**
- Inner Release: Optimal activation timing with Surging Tempest and gauge considerations
- Fell Cleave: Burst execution during Inner Release windows
- Infuriate: Gauge generation and Nascent Chaos timing

**Resource Management**
- Beast Gauge spending decisions with burst window awareness
- Infuriate charge management to avoid overcapping

**Enmity Decisions**
- Provoke: Emergency aggro recovery and coordinated tank swaps
- Shirk: Off-tank enmity management and swap coordination

**Concept Mastery**
- All major WAR concepts now tracked: `war_holmgang`, `war_vengeance`, `war_bloodwhetting`, `war_shake_it_off`, `war_inner_release`, `war_fell_cleave`, `war_infuriate_gauge`, `war_provoke`, `war_shirk`

## v3.32.0 — 2026-01-24 - Themis (PLD) Training Mode

**Full Paladin Training Mode Integration**
- Themis (PLD) rotation now records all training decisions with detailed explanations
- Live coaching shows why each ability was used with factors considered and alternatives rejected

**Mitigation Decisions**
- Hallowed Ground: Emergency invulnerability explanations with threat assessment
- Sentinel: Major cooldown timing with damage context
- Sheltron: Oath Gauge spending decisions with tank stance considerations
- Divine Veil: Party mitigation decisions with coordination awareness

**Burst Window Decisions**
- Fight or Flight: Optimal timing explanations at combo start
- Requiescat: Magic phase activation after physical burst
- Atonement chain: Burst execution during Fight or Flight windows

**Enmity Decisions**
- Provoke: Emergency aggro recovery and coordinated tank swaps
- Shirk: Off-tank enmity management and swap coordination

**Concept Mastery**
- All major PLD concepts now tracked: `pld_hallowed_ground`, `pld_sentinel`, `pld_sheltron`, `pld_divine_veil`, `pld_fight_or_flight`, `pld_requiescat`, `pld_atonement_chain`, `pld_provoke`, `pld_shirk`

## v3.31.0 — 2026-01-24 - Training Mode Infrastructure

**Shared Training Helpers**
- New `TankTrainingHelper` for tank rotations - mitigation, invuln, burst, resource, party mitigation, enmity, and interrupt decisions
- New `MeleeDpsTrainingHelper` for melee DPS rotations - damage, burst, positional, combo, resource, raid buff, utility, and AoE decisions
- New `RangedDpsTrainingHelper` for ranged physical DPS rotations - damage, burst, proc, resource, raid buff, song/dance, DoT, utility, and AoE decisions
- New `CasterTrainingHelper` for caster DPS rotations - damage, burst, proc, resource, raid buff, phase transition, DoT, movement, summon, and AoE decisions

**Foundation for Training Mode Integration**
- These helpers provide typed methods for recording training decisions from tank and DPS rotation modules
- Enables consistent explanation categories and concept tracking across all jobs
- Infrastructure for v3.32.0-v3.48.0 which will integrate Training Mode into each job rotation

## v3.30.0 — 2026-01-24 - Adaptive Learning Paths

**Learning Path Guidance**
- New "Learning Path" panel at the top of the Lessons tab
- Recommends your next lesson based on skill level, progress, and concept mastery
- Progress bar shows overall completion for the selected job
- Skill level badge (Beginner/Intermediate/Advanced) displays prominently

**Personalized Recommendations**
- Struggling concepts (<60% success rate) take priority - lessons covering them are recommended first
- Skill-appropriate progression: Beginners start at lesson 1, Intermediate can skip basics, Advanced focus on optimization
- "Start This Lesson" button navigates directly to the recommended lesson

**Recommendation Types**
- "Start here to build your foundation" - No lessons completed yet
- "Continue where you left off" - Normal progression
- "Covers: [Concept] (X% success)" - Lesson addresses a struggling concept
- "Review optimization techniques" - Advanced users working on mastery
- "All lessons completed!" - Congratulations message with quiz suggestion

**How It Works**
1. Open Training Mode → Lessons tab
2. The Learning Path panel shows your recommended next lesson
3. Click "Start This Lesson" to jump directly to it
4. Struggling concepts from v3.28.0 mastery tracking influence recommendations
5. Your skill level (from v3.27.0) determines progression style

## v3.29.0 — 2026-01-24 - Mastery-Driven Recommendations

**Smart Lesson Recommendations**
- Recommendations tab now uses concept mastery data to suggest lessons
- Struggling concepts (<60% success rate) drive targeted lesson suggestions
- Priority scales with struggle severity: 0% success = highest priority, 60% = medium priority

**Mixed Recommendations**
- Fight performance issues and mastery data now combine intelligently
- Same lesson can match both issue-based and mastery-based criteria
- Combined reasons show when both sources identify the same improvement opportunity

**UI Enhancements**
- New [MASTERY] badge appears on mastery-driven recommendations
- "Struggling:" line displays the specific concepts you need to practice
- Header text adapts: "Based on fight performance", "Based on mastery data", or "Based on both"

**Generate from Mastery Data**
- New "Generate from Mastery Data" button in empty state
- Select any job and generate recommendations without needing to complete a fight
- Useful for reviewing skill gaps across all your practiced jobs

**How It Works**
- Play normally to build mastery data (v3.28.0)
- Recommendations now automatically include lessons for struggling concepts
- Lower success rate = higher recommendation priority
- Complete suggested lessons to improve your weak areas

## v3.28.0 — 2026-01-24 - Concept Mastery Tracking

**Mastery System**
- Training Mode now tracks concept "mastery" instead of just "exposure"
- Mastery is measured by successful application in combat, not just seeing explanations
- Concepts are categorized: Mastered (>85% success), Struggling (<60% success), or Developing

**Skill Level Score Update**
- New weight distribution: Quiz Pass (30%), Quiz Quality (20%), Lessons (20%), Concepts (5%), **Mastery (25%)**
- Concept mastery now contributes 25% to your overall skill level score
- Creates a feedback loop to identify which concepts need more practice

**UI Improvements**
- Skill Progress tab now shows detailed mastery breakdown per job
- Mastered concepts display with checkmark
- Struggling concepts highlighted as "Needs Practice"
- Developing concepts show count (need 10+ opportunities to evaluate)

**WHM Proof of Concept**
- Benediction handler now records mastery data (success when saving critical targets)
- More handlers will be instrumented in future updates

**How It Works**
- Play your job normally with Training Mode enabled
- Olympus tracks opportunities to apply concepts and whether they succeeded
- After 10+ opportunities, concepts are evaluated for mastery
- Your skill level adjusts based on actual combat performance

## v3.27.0 — 2026-01-24 - Adaptive Training Mode

**Skill Level Detection**
- Training Mode now detects your skill level (Beginner/Intermediate/Advanced) per job
- Composite score calculated from quiz pass rate, quiz quality, lessons completed, and concepts learned
- New "Skill Level" tab shows your progress breakdown for each job

**Adaptive Explanations**
- Explanation verbosity now automatically adjusts based on your detected skill level
- Beginners see detailed explanations for every decision
- Intermediate players see normal detail, with extra detail for unfamiliar concepts
- Advanced players see minimal detail, except for critical or new decisions

**Concept Familiarity**
- The system tracks how often you've seen each concept
- New concepts (seen 0-2 times) get boosted verbosity
- Mastered concepts (10+ exposures) get reduced verbosity for advanced players

**Settings**
- Enable/disable adaptive explanations in the Skill Level tab
- Override auto-detection with a manual skill level if preferred
- Toggle "[Adaptive]" indicator shows when verbosity was adjusted

**Foundation for v4.0**
- This release lays the groundwork for the personalized coaching milestone

## v3.26.0 — 2026-01-24 - PCT Training Mode

**PCT (Iris) Training Mode**
- Training Mode now supports Pictomancer - final caster DPS job added
- 25 new PCT concepts covering Palette Gauge, canvas system, Muse abilities, and burst windows
- 7 progressive lessons from painting fundamentals to advanced optimization
- 35 quiz questions testing real Pictomancer decisions

**Lesson Content**
- Lesson 1: Painting Fundamentals - Palette Gauge, White/Black Paint, base combo rotation
- Lesson 2: Canvas Mastery - Creature/Weapon/Landscape motifs, pre-pull preparation
- Lesson 3: Muse Abilities - Living Muse, Striking Muse, Starry Muse timing
- Lesson 4: Subtractive Palette - Cyan combo, Monochromatic Tones, Star Prism finisher
- Lesson 5: Paint Spenders - Holy in White, Comet in Black, Rainbow Drip priority
- Lesson 6: Burst Windows - Starry Muse burst, hammer combo, party coordination
- Lesson 7: Advanced Optimization - AoE rotation, movement tools, downtime preparation

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

**Training Mode Complete**
- All 21 combat jobs now have full Training Mode support
- Preparing for v4.0 Training Mode Complete milestone

## v3.25.0 — 2026-01-24 - RDM Training Mode

**RDM (Circe) Training Mode**
- Training Mode now supports Red Mage - third caster DPS job added
- 25 new RDM concepts covering Dualcast system, mana balance, melee combo, finishers, and burst windows
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Red Mage decisions

**Lesson Content**
- Lesson 1: Mana Foundation - Black/White mana generation, balance importance, imbalance penalties
- Lesson 2: Dualcast Mastery - Hardcast triggers, instant consumption, Swiftcast emergency usage
- Lesson 3: Proc Management - Verfire/Verstone procs, expiration priority, Acceleration guarantees
- Lesson 4: Melee Combo Fundamentals - 50|50 entry, Riposte → Zwerchhau → Redoublement, overcap prevention
- Lesson 5: Finisher System - Verflare/Verholy selection, Scorch → Resolution → Grand Impact chain
- Lesson 6: Burst Windows - Embolden party buff, Manafication doubling, Fleche/Contre Sixte weaving
- Lesson 7: Advanced Optimization - Corps-a-corps positioning, AoE rotation, movement tools

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

**Caster DPS Training Progress**
- Third caster DPS job added to Training Mode (BLM, SMN, RDM complete)
- PCT caster job coming next to complete caster training

## v3.24.0 — 2026-01-24 - SMN Training Mode

**SMN (Persephone) Training Mode**
- Training Mode now supports Summoner - second caster DPS job added
- 25 new SMN concepts covering Aetherflow, primal attunement, demi-summons, burst abilities, and raid coordination
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Summoner decisions

**Lesson Content**
- Lesson 1: Aetherflow Fundamentals - Aetherflow stacks, Energy Drain timing, stack management, overcap prevention
- Lesson 2: Primal Attunement System - Ifrit/Titan/Garuda phases, attunement stacks, gemshine/brilliance rotation
- Lesson 3: Primal Favor Abilities - Crimson Cyclone/Strike, Mountain Buster, Slipstream, optimal favor timing
- Lesson 4: Demi-Summon Phases - Bahamut, Phoenix, Solar Bahamut cycles, Demi-summon rotation
- Lesson 5: Burst Timing & Enkindle - Enkindle timing, Astral Flow abilities, Deathflare/Rekindle/Sunflare
- Lesson 6: Searing Light Coordination - Raid buff timing, party burst alignment, Searing Flash
- Lesson 7: Advanced Rotation Optimization - Primal order, Ruin IV procs, full rotation synthesis

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

**Caster DPS Training Progress**
- Second caster DPS job added to Training Mode (BLM, SMN complete)
- RDM, PCT caster jobs coming next

## v3.23.0 — 2026-01-24 - BLM Training Mode

**BLM (Hecate) Training Mode**
- Training Mode now supports Black Mage - first caster DPS job added
- 25 new BLM concepts covering Astral Fire/Umbral Ice, Enochian, Fire IV rotation, Polyglot, movement optimization, and burst windows
- 7 progressive lessons from fundamentals to advanced execution
- 35 quiz questions testing real Black Mage decisions

**Lesson Content**
- Lesson 1: Fire and Ice Fundamentals - Astral Fire damage, Umbral Ice MP recovery, 30s element timer, Enochian state, Fire III/Blizzard III transitions
- Lesson 2: Resource Mastery - Umbral Hearts (3 from B4), Polyglot stacks (30s Enochian), overcapping prevention, MP management
- Lesson 3: Fire Phase Execution - Fire IV spam, Despair finisher, Astral Soul building, Flare Star at 6 stacks
- Lesson 4: Ice Phase & Thunder - Blizzard IV for hearts, Thunder DoT uptime, Paradox instant in UI3
- Lesson 5: Proc Management - Firestarter (40% from F4), Thunderhead, proc priority, downtime planning
- Lesson 6: Cooldown Optimization - Ley Lines placement, Triplecast charges, Manafont extended Fire phase
- Lesson 7: Advanced Tactics - Movement instant priority (Triplecast > Xeno > Procs > Swift), Xenoglossy burst usage, AoE rotation

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

**Caster DPS Training Started**
- First caster DPS job added to Training Mode
- SMN, RDM, PCT caster jobs coming next

## v3.22.0 — 2026-01-24 - DNC Training Mode

**DNC (Terpsichore) Training Mode**
- Training Mode now supports Dancer - third and final ranged physical DPS job added
- 25 new DNC concepts covering dance system, proc management, Esprit/Feather gauges, burst windows, high-level abilities, and partner coordination
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Dancer decisions

**Lesson Content**
- Lesson 1: Dance Fundamentals - Standard Step (30s) and Technical Step (120s) execution, dance timers, step sequence mechanics
- Lesson 2: Proc Management - Silken Symmetry/Flow procs, Threefold/Fourfold Fan from Flourish, Feather generation
- Lesson 3: Esprit Gauge Mastery - Esprit building from you and partner, Saber Dance at 50+ cost, 80+ dump threshold
- Lesson 4: Feather Optimization - Max 4 Feathers, Fan Dance usage, hold 3 for burst windows, AoE with Fan Dance II
- Lesson 5: Burst Window Execution - Technical Finish → Devilment → Flourish sequence, party sync via IPC
- Lesson 6: High-Level Abilities - Starfall Dance (Devilment proc), Finishing Move (Standard proc), Last Dance chain, Tillana (Technical proc)
- Lesson 7: Partner & Party Coordination - Dance Partner selection (high-CPM DPS), Shield Samba mitigation, Curing Waltz utility

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

**Ranged Physical DPS Training Complete**
- All 3 ranged physical DPS jobs now have Training Mode support (MCH, BRD, DNC)

## v3.21.0 — 2026-01-24 - BRD Training Mode

**BRD (Calliope) Training Mode**
- Training Mode now supports Bard - second ranged physical DPS job added
- 25 new BRD concepts covering song system, Repertoire/Pitch Perfect, Soul Voice/Apex, procs, DoTs, burst windows, and party coordination
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Bard decisions

**Lesson Content**
- Lesson 1: Bard Fundamentals - 3-song cycle (WM → MB → AP), party buffs from each song, switching timing
- Lesson 2: Repertoire Mastery - Wanderer's Minuet Repertoire generation, Pitch Perfect 3-stack optimization, Empyreal Arrow guaranteed proc
- Lesson 3: Soul Voice & Apex Arrow - Soul Voice gauge management, 80+ Apex threshold, Blast Arrow follow-up, overcap prevention
- Lesson 4: Proc Management - Straight Shot Ready (Hawk's Eye) procs, Refulgent Arrow priority, Barrage + Resonant Arrow combo
- Lesson 5: DoT Optimization - Caustic Bite/Stormbite uptime, Iron Jaws refresh window, buff snapshotting during Raging Strikes
- Lesson 6: Burst Window Execution - Raging Strikes → Battle Voice → Radiant Finale sequence, Coda scaling, Radiant Encore follow-up
- Lesson 7: Advanced Coordination - Empyreal Arrow cooldown management, Bloodletter spam during MB, Troubadour/Nature's Minne utility, IPC interrupt coordination

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

**Bug Fix**
- Fixed MCH and VPR concepts not being included in TrainingService.GetAllConcepts() and GetJobPrefix() - these jobs now properly track progress

## v3.20.0 — 2026-01-24 - MCH Training Mode

**MCH (Prometheus) Training Mode**
- Training Mode now supports Machinist - first ranged physical DPS job added
- 25 new MCH concepts covering Heat/Battery gauges, Hypercharge, Wildfire burst, tool priority, and Queen management
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Machinist decisions

**Lesson Content**
- Lesson 1: Machinist Fundamentals - Heat and Battery dual-gauge system, gauge interactions, overcap prevention
- Lesson 2: Tool Mastery - Drill priority, Air Anchor Battery generation, Chain Saw Excavator proc
- Lesson 3: Reassemble Optimization - highest potency tool targeting, charge management, raid buff alignment
- Lesson 4: Hypercharge Windows - 50 Heat activation, Overheated state, Heat Blast rotation, single-weave oGCDs
- Lesson 5: Wildfire Burst - pre-Hypercharge placement, 6-hit optimal window, 2-minute raid buff alignment
- Lesson 6: Queen Management - Battery scaling, 90-100 Battery summoning, raid buff timing
- Lesson 7: Advanced Tactics - party burst coordination, phase awareness, AoE rotation, interrupt utility

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

**Ranged Physical DPS Training Started**
- MCH is the first ranged physical DPS with Training Mode (BRD, DNC to follow)

## v3.19.0 — 2026-01-24 - VPR Training Mode

**VPR (Echidna) Training Mode**
- Training Mode now supports Viper - sixth and final melee DPS job added
- 25 new VPR concepts covering dual wield combos, venom system, twinblades, Reawaken burst, and party coordination
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Viper decisions

**Lesson Content**
- Lesson 1: Viper Fundamentals - two-path combo system, Hunter's Instinct/Swiftscaled buff cycling, Honed procs
- Lesson 2: Resource Management - Serpent Offering gauge, Rattling Coil stacks, Uncoiled Fury movement tool
- Lesson 3: Venom & Positionals - venom buff system, Flankstung/Hindstung interpretation, True North usage
- Lesson 4: Twinblade Combos - Vicewinder initiation, Coil follow-ups, Twinfang/Twinblood oGCDs, Noxious Gnash
- Lesson 5: Reawaken Burst - entry requirements, Generation GCD sequence, Legacy oGCD weaving, Ouroboros finisher
- Lesson 6: Burst Optimization - Serpent's Ire timing, Ready to Reawaken proc, raid buff alignment
- Lesson 7: Complete Rotation - full rotation synthesis, AoE decisions, movement optimization

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

**Melee DPS Training Complete**
- All 6 melee DPS jobs now have full Training Mode support (DRG, NIN, SAM, MNK, RPR, VPR)

## v3.18.0 — 2026-01-24 - RPR Training Mode

**RPR (Thanatos) Training Mode**
- Training Mode now supports Reaper - fifth melee DPS job added
- 25 new RPR concepts covering Soul/Shroud gauges, Soul Reaver, Enshroud burst, and party coordination
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Reaper decisions

**Lesson Content**
- Lesson 1: Reaper Fundamentals - basic combos, Soul gauge building, Death's Design maintenance
- Lesson 2: Soul Reaver & Positionals - Blood Stalk/Gluttony, Gibbet (flank), Gallows (rear), Enhanced procs
- Lesson 3: Shroud Gauge Management - Shroud building, Guillotine AoE, entering Enshroud
- Lesson 4: Enshroud Burst Window - Lemure Shroud stacks, Void Shroud generation, Void/Cross Reaping
- Lesson 5: Enshroud Finishers - Communio timing, Perfectio proc, Lemure's Slice, Sacrificium
- Lesson 6: Party Buff Coordination - Arcane Circle, Immortal Sacrifice stacks, Plentiful Harvest
- Lesson 7: AoE & Movement - AoE rotation, Harvest Moon ranged GCD, Soulsow preparation

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

## v3.17.0 — 2026-01-24 - MNK Training Mode

**MNK (Kratos) Training Mode**
- Training Mode now supports Monk - fourth melee DPS job added
- 25 new MNK concepts covering form system, Chakra gauge, Beast Chakra, burst windows, and positionals
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Monk decisions

**Lesson Content**
- Lesson 1: Monk Fundamentals - form cycle (Opo-opo/Raptor/Coeurl), basic combos, positional requirements
- Lesson 2: Maintaining Your Buffs - Disciplined Fist uptime, Demolish DoT, Meditation stacks
- Lesson 3: The Chakra System - Chakra gauge management, The Forbidden Chakra, Enlightenment
- Lesson 4: Beast Chakra & Masterful Blitz - Lunar/Solar/Celestial chakra, Elixir Field, Rising Phoenix, Phantom Rush
- Lesson 5: Burst Windows - Perfect Balance usage, Riddle of Fire, Brotherhood, burst alignment
- Lesson 6: Movement & Utility - Thunderclap gap closer, True North, Riddle of Wind
- Lesson 7: AoE & Optimization - Arm of the Destroyer combo, Howling Fist, AoE thresholds

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

## v3.16.0 — 2026-01-24 - SAM Training Mode

**SAM (Nike) Training Mode**
- Training Mode now supports Samurai - third melee DPS job added
- 25 new SAM concepts covering Sen system, Kenki gauge, Iaijutsu, burst windows, and positionals
- 7 progressive lessons from fundamentals to advanced optimization
- 35 quiz questions testing real Samurai decisions

**Lesson Content**
- Lesson 1: Samurai Fundamentals - combo routes, Sen collection, Fugetsu/Fuka buff maintenance
- Lesson 2: Kenki & Meditation - gauge management, Shinten/Kyuten spending, Shoha timing
- Lesson 3: Iaijutsu System - Higanbana DoT, Midare Setsugekka, Tenka Goken decisions
- Lesson 4: Tsubame-gaeshi & Meikyo - Kaeshi follow-ups, Meikyo Shisui finisher priority
- Lesson 5: Ikishoten Burst Window - Ogi Namikiri sequence, Zanshin, Senei timing
- Lesson 6: Positionals & True North - Gekko rear, Kasha flank, positional recovery
- Lesson 7: Advanced Optimization - burst alignment, Meikyo buff refresh, AoE rotation, Hagakure

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

## v3.15.0 — 2026-01-24 - NIN Training Mode

**NIN (Hermes) Training Mode**
- Training Mode now supports Ninja - second melee DPS job added
- 25 new NIN concepts covering mudra system, Ninki gauge, burst windows, and positionals
- 7 progressive lessons from ninja fundamentals to advanced optimization
- 35 quiz questions testing real Ninja decisions

**Lesson Content**
- Lesson 1: Ninja Fundamentals - combo flow, positional requirements, Kazematoi stacks
- Lesson 2: Mudra Mastery - Ten/Chi/Jin sequences, Ninjutsu weaving, Huton buff
- Lesson 3: Ninki & Spenders - Ninki gauge management, Bhavacakra usage, pooling for burst
- Lesson 4: Burst Window Basics - Suiton setup, Kunai's Bane execution, Mug/Dokumori timing
- Lesson 5: Advanced Burst - Kassatsu combos, Ten Chi Jin sequences, TCJ optimization
- Lesson 6: Procs & Movement - Raiju procs, Bunshin timing, Phantom Kamaitachi, Tenri Jindo
- Lesson 7: Optimization - Kazematoi management, True North usage, burst alignment, AoE rotation

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

## v3.14.0 — 2026-01-24 - Melee DPS Training Mode (Phase 1)

**DRG (Zeus) Training Mode**
- Training Mode now supports Dragoon - first melee DPS job added
- 25 new DRG concepts covering Eye gauge, Life of Dragon, jumps, burst windows, and positionals
- 7 progressive lessons from combo fundamentals to advanced optimization
- 35 quiz questions testing real Dragoon decisions

**Lesson Content**
- Lesson 1: Dragoon Fundamentals - combo flow, Power Surge buff, positional requirements
- Lesson 2: Jump Management - High Jump, Mirage Dive, animation lock safety
- Lesson 3: Eye Gauge & Geirskogul - building Eyes, entering Life of Dragon
- Lesson 4: Life of the Dragon - Nastrond spam, Stardiver timing, optimization
- Lesson 5: Burst Window Setup - Lance Charge, Battle Litany, buff alignment
- Lesson 6: Life Surge & Crits - guaranteed crits, True North usage
- Lesson 7: Advanced Optimization - Wyrmwind Thrust, DoT uptime, AoE rotation

**Quiz Features**
- 7 quizzes (one per lesson) with 5 scenario-based questions each
- Pass 4 out of 5 to complete a quiz
- Detailed explanations for every answer
- Progress tracking and best score persistence

## v3.13.0 — 2026-01-24 - Tank Skill Quizzes

**New Tank Quizzes**
- 28 skill quizzes (7 per tank) to validate lesson understanding
- 140 scenario-based questions testing real tank decisions
- Pass 4 out of 5 to complete a quiz

**Quiz Content Coverage**
- PLD: Oath Gauge management, Sheltron timing, Hallowed Ground usage, Fight or Flight optimization, Requiescat phase, Divine Veil coordination
- WAR: Beast Gauge pooling, Surging Tempest uptime, Holmgang coordination, Inner Release windows, Nascent Flash usage, Shake It Off timing
- DRK: Blood Gauge management, Darkside maintenance, Living Dead coordination, The Blackest Night optimization, Delirium windows, Dark Missionary timing
- GNB: Cartridge management, Heart of Corundum timing, Superbolide coordination, No Mercy windows, Gnashing Fang combos, Heart of Light usage

**Quiz Features**
- Scenario-based questions simulate real tanking situations
- Multiple choice answers with detailed explanations
- Review mode shows correct answers and why after submission
- Best score tracking per quiz

## v3.12.0 — 2026-01-23 - Tank Training Mode

**Training Mode for Tanks**
- Added Training Mode support for all 4 tanks: Paladin, Warrior, Dark Knight, Gunbreaker
- 100 new tank concepts covering defensive cooldowns, burst windows, and party utility
- 28 progressive lessons (7 per tank) from basics to advanced optimization

**Lesson Content**
- PLD: Oath Gauge, Fight or Flight, magic phase, Hallowed Ground timing, Divine Veil
- WAR: Beast Gauge, Inner Release windows, Bloodwhetting sustain, Holmgang, Shake It Off
- DRK: Blood Gauge, Darkside maintenance, The Blackest Night optimization, Living Dead
- GNB: Cartridge Gauge, No Mercy windows, Gnashing Fang combos, Superbolide, Heart of Corundum

**Topics Covered**
- Gauge management and resource optimization
- Invulnerability timing and healer coordination
- Mitigation stacking and cooldown rotation
- Tank swap coordination
- Party protection abilities

## v3.11.0 — 2026-01-23 - Training Mode: Skill Quizzes

**New Quizzes Tab**
- 28 skill quizzes (7 per healer) to validate lesson understanding
- Each quiz has 5 scenario-based questions testing real combat decisions
- Pass 4 out of 5 to complete a quiz

**Quiz Features**
- Scenario-based questions simulate real healing situations
- Multiple choice answers with detailed explanations
- Review mode shows correct answers and why after submission
- Best score tracking - your highest attempt is saved

**Quiz Content Coverage**
- WHM: Emergency healing, Lily system, Benediction timing, oGCD weaving
- SCH: Aetherflow management, fairy abilities, shield economy, Deployment Tactics
- AST: Card system, Earthly Star timing, Essential Dignity scaling, HoT economy
- SGE: Kardia optimization, Addersgall spending, Eukrasia decisions, Phlegma timing

**Progress Tracking**
- Quiz completion status shown in quiz list
- Pass/fail indicators with score display
- Overall progress bar per job

## v3.10.0 — 2026-01-23 - Performance-Based Lesson Recommendations

**Personalized Learning**
- New "Recommended" tab in Training Mode suggests lessons based on your fight performance
- After each combat, Analytics issues are analyzed to recommend specific lessons
- Limited to 2-3 recommendations to avoid overwhelming - focus on highest priority

**Smart Issue-to-Lesson Mapping**
- Party deaths → Emergency healing lessons (Benediction, Lustrate, Essential Dignity)
- Unused abilities → oGCD weaving and resource management lessons
- Near-deaths → Proactive healing and tank priority lessons
- GCD downtime → DPS optimization and DoT maintenance lessons
- Cooldown drift → oGCD timing and key ability usage lessons
- High overheal → Efficient healing and shield timing lessons
- Capped resources → Lily, Aetherflow, Addersgall management lessons

**User Controls**
- Enable/disable recommendations toggle
- Configurable max recommendations (1-5)
- Dismiss individual recommendations
- Clear all dismissed recommendations

**Integration**
- Automatic analysis when fights end (via OnSessionCompleted event)
- Recommendations persist across sessions until dismissed or completed
- Completing a recommended lesson removes it from suggestions
- Works with all 4 healers (WHM, SCH, AST, SGE)

## v3.9.0 — 2026-01-23 - Training Mode: Lessons Tab

**Structured Learning Content**
- New Lessons tab in Training Mode with 28 total lessons across all 4 healers
- 7 progressive lessons per job (WHM, SCH, AST, SGE) covering all healing concepts
- Prerequisites system ensures proper learning progression

**WHM Lessons**
1. Healer Fundamentals - healing priority, tank focus, oGCD weaving
2. Emergency Response - Benediction, Tetragrammaton usage
3. The Lily System - gauge management, Afflatus abilities, Blood Lily
4. Proactive Healing - Regen, Divine Benison, Assize
5. Defensive Cooldowns - Temperance, Aquaveil, Liturgy of the Bell
6. DPS Optimization - Glare priority, DoT maintenance
7. Utility & Coordination - Esuna, Raise, co-healer awareness

**SCH, AST, SGE Lessons**
- Each job has 7 tailored lessons covering their unique mechanics
- SCH: Aetherflow, Fairy management, Shield economy, Seraph
- AST: Card system, Earthly Star, HoT management, Divination
- SGE: Kardia, Addersgall, Eukrasia decisions, defensive toolkit

**Learning Features**
- Track lesson completion with visual progress indicators
- Each lesson explains key points, related abilities, and practice tips
- Completing lessons automatically marks all related concepts as learned
- Locked lessons show prerequisite requirements

## v3.8.0 — 2026-01-23 - Training Mode: Full Explanation Coverage

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

## v3.7.0 — 2026-01-23 - Training Mode: Full Healer Coverage

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

## v3.6.0 — 2026-01-23 - Training Mode

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

## v3.5.0 — 2026-01-23 - FFLogs Integration

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

## v3.4.0 — 2026-01-23 - Personal DPS Tracking

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

## v3.3.0 — 2026-01-23 - Cooldown Usage Analysis

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

## v3.2.0 — 2026-01-23 - Downtime Analysis

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

## v3.1.0 — 2026-01-23 - Performance Analytics Foundation

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

## v3.0.0 — 2026-01-23 - Phase 3 Complete: Full Party Coordination

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

## v2.31.0 — 2026-01-23 - Healer Role & Gauge Coordination

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

## v2.30.0 — 2026-01-22 - Tank Swap Coordination

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

## v2.29.0 — 2026-01-22 - Interrupt Coordination

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

## v2.28.0 — 2026-01-22 - Esuna Coordination

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

## v2.27.0 — 2026-01-22 - Tank Invulnerability Coordination

**Tank Coordination**
- Tank invulnerability abilities now coordinate between Olympus instances
- Prevents both tanks from using invulns simultaneously during emergencies
- Covers Hallowed Ground (PLD), Holmgang (WAR), Living Dead (DRK), and Superbolide (GNB)
- After using an invuln, broadcasts to other tanks via IPC

**New Settings**
- `EnableInvulnerabilityCoordination` - Master toggle for invuln coordination (default: on)
- `InvulnerabilityStaggerWindowSeconds` - How long to delay if another tank used an invuln recently (1-10s, default: 5s)

## v2.26.0 — 2026-01-22 - Resurrection Coordination

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

## v2.25.0 — 2026-01-22 - Multi-Healer Ground Effect Coordination

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

## v2.24.0 — 2026-01-22 - Complete Healer Burst Decision Logic

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

## v2.23.0 — 2026-01-22 - Tank Defensive Synergy

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

## v2.22.0 — 2026-01-21 - Complete DPS Burst Broadcasting

**DPS Coordination**
- Samurai (Nike), Ninja (Hermes), Viper (Echidna), and Machinist (Prometheus) now broadcast burst intents
- Fixes asymmetric coordination where these jobs only listened for party bursts but never announced their own
- All 4 jobs now properly participate in two-way IPC communication

**Technical**
- Added Ikishoten, Kunai's Bane, Serpent's Ire, and Wildfire to coordinated raid buff registry
- Each job now calls `AnnounceRaidBuffIntent()` before burst and `OnRaidBuffUsed()` after execution

## v2.21.0 — 2026-01-21 - Healer Burst Awareness

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

## v2.20.0 — 2026-01-21 - Pictomancer Starry Muse Coordination

**DPS Coordination**
- Pictomancer (Iris) now aligns Starry Muse (+5% damage) with party raid buff windows
- Listens for pending burst intents and synchronizes burst timing with other Olympus users
- Fills gap where Starry Muse was missed during v2.11.0-v2.13.0 DPS raid buff work

## v2.19.0 — 2026-01-21 - Ninja Party Burst Alignment

**DPS Coordination**
- Ninja (Hermes) now aligns Kunai's Bane burst window with party raid buff windows
- Listens for pending burst intents and synchronizes burst timing with other Olympus users
- Maximizes damage during coordinated burst phases

## v2.18.0 — 2026-01-21 - Viper Party Burst Alignment

**DPS Coordination**
- Viper (Echidna) now aligns Serpent's Ire with party raid buff windows
- Delays burst briefly when other DPS are about to use Battle Voice, Technical Finish, etc.
- Maximizes Reawaken damage during coordinated burst phases

## v2.17.0 — 2026-01-21 - Samurai Party Burst Alignment

**DPS Coordination**
- Samurai (Nike) now aligns Ikishoten burst window with party raid buff windows
- Delays burst briefly when other DPS are about to use Battle Voice, Technical Finish, etc.
- Maximizes Ogi Namikiri damage during coordinated burst phases

## v2.16.0 — 2026-01-21 - Machinist Party Burst Alignment

**DPS Coordination**
- Machinist (Prometheus) now aligns Wildfire with party raid buff windows
- Delays Wildfire briefly when other DPS are about to use Battle Voice, Technical Finish, etc.
- Maximizes damage during coordinated burst phases

## v2.15.0 — 2026-01-21 - Tank-Healer Mitigation Avoidance

**Party Coordination**
- Healers now broadcast party mitigations to other Olympus instances
- WHM: Temperance, Liturgy of the Bell
- SCH: Sacred Soil, Expedient
- AST: Neutral Sect, Collective Unconscious, Macrocosmos
- SGE: Panhaima, Holos
- Tanks now check healer mitigations before using party-wide defensives
- Prevents wasteful stacking (e.g., Divine Veil + Temperance simultaneously)
- Completes two-way mitigation coordination between tanks and healers

## v2.14.0 — 2026-01-21 - Tank Mitigation Broadcasting

**Party Coordination**
- Tank party mitigations now broadcast to other Olympus instances
- Prevents multiple tanks from stacking mitigations (Divine Veil, Shake It Off, Dark Missionary, Heart of Light)
- Reprisal usage is now coordinated between tanks
- Completes the two-way coordination loop started in v2.7.0

## v2.13.0 — 2026-01-21 - Complete DPS Raid Buff Coordination

**Party Coordination**
- Added raid buff coordination for remaining DPS jobs
- Red Mage (Circe): Embolden now synchronizes with party burst windows
- Dancer (Terpsichore): Technical Finish now synchronizes with party burst windows
- Reaper (Thanatos): Arcane Circle now synchronizes with party burst windows
- Monk (Kratos): Brotherhood now synchronizes with party burst windows
- All DPS raid buffs now coordinate for optimal burst alignment

## v2.12.0 — 2026-01-21 - Summoner Raid Buff Coordination

**Party Coordination**
- Added Searing Light coordination for Summoner (Persephone)
- Multiple Olympus users now synchronize Summoner burst windows with other raid buffs
- Works seamlessly with existing Dragoon and Bard coordination

## v2.11.0 — 2026-01-21 - DPS Raid Buff Coordination

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

## v2.10.1 — 2026-01-21 - Discord Notification Fix

**Bug Fix**
- Fixed Discord release notifications showing `%0A` instead of actual line breaks

## v2.10.0 — 2026-01-21 - AOE Heal Coordination

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

## v2.9.0 — 2026-01-21 - Cross-Healer Coordination

**Healing**
- Extended single-target heal coordination to all healers
- Scholar (Athena): Lustrate, Excogitation, Protraction, Adloquium, Physick
- Astrologian (Astraea): Essential Dignity, Celestial Intersection, Exaltation, Aspected Benefic, Benefic, Benefic II
- Sage (Asclepius): Druochole, Taurochole, Krasis, Haima, Eukrasian Diagnosis, Diagnosis
- All four healers now coordinate via IPC to prevent double-healing

## v2.8.0 — 2026-01-21 - Cross-Instance Heal Coordination

**Healing**
- Added cross-instance single-target heal coordination for Apollo (WHM)
- Olympus users no longer double-heal the same target when multiple WHMs are present
- Coordination uses IPC protocol for real-time state sync

**Technical**
- Extended PartyCoordinationService with heal target tracking
- Added HealTargetInfo to IPC message protocol

## v2.7.0 — 2026-01-19 - Party Cooldown Sync

**Defensives**
- Healers and tanks now coordinate major party mitigations
- Prevents overlapping cooldowns like Divine Veil + Shake It Off
- Configurable overlap window for fine-tuning

## v2.6.0 — 2026-01-19 - Party Coordination IPC

**Multiplayer**
- Added IPC protocol for multi-Olympus coordination
- Healers can now share state between instances
- Foundation for advanced party-wide optimization
