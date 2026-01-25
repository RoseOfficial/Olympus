# Changelog

All notable changes to Olympus will be documented in this file.

<!-- LATEST-START -->
## v3.39.0 - Kratos (MNK) Training Mode

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
<!-- LATEST-END -->

## v3.38.0 - Nike (SAM) Training Mode

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

## v3.37.0 - Hermes (NIN) Training Mode

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

## v3.36.0 - Zeus (DRG) Training Mode

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

## v3.35.0 - Hephaestus (GNB) Training Mode

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

## v3.34.0 - Nyx (DRK) Training Mode

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

## v3.33.0 - Ares (WAR) Training Mode

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

## v3.32.0 - Themis (PLD) Training Mode

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

## v3.31.0 - Training Mode Infrastructure

**Shared Training Helpers**
- New `TankTrainingHelper` for tank rotations - mitigation, invuln, burst, resource, party mitigation, enmity, and interrupt decisions
- New `MeleeDpsTrainingHelper` for melee DPS rotations - damage, burst, positional, combo, resource, raid buff, utility, and AoE decisions
- New `RangedDpsTrainingHelper` for ranged physical DPS rotations - damage, burst, proc, resource, raid buff, song/dance, DoT, utility, and AoE decisions
- New `CasterTrainingHelper` for caster DPS rotations - damage, burst, proc, resource, raid buff, phase transition, DoT, movement, summon, and AoE decisions

**Foundation for Training Mode Integration**
- These helpers provide typed methods for recording training decisions from tank and DPS rotation modules
- Enables consistent explanation categories and concept tracking across all jobs
- Infrastructure for v3.32.0-v3.48.0 which will integrate Training Mode into each job rotation

## v3.30.0 - Adaptive Learning Paths

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

## v3.29.0 - Mastery-Driven Recommendations

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

## v3.28.0 - Concept Mastery Tracking

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

## v3.27.0 - Adaptive Training Mode

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

## v3.26.0 - PCT Training Mode

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

## v3.25.0 - RDM Training Mode

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

## v3.24.0 - SMN Training Mode

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

## v3.23.0 - BLM Training Mode

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

## v3.22.0 - DNC Training Mode

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

## v3.21.0 - BRD Training Mode

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

## v3.20.0 - MCH Training Mode

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

## v3.19.0 - VPR Training Mode

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

## v3.18.0 - RPR Training Mode

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

## v3.17.0 - MNK Training Mode

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

## v3.16.0 - SAM Training Mode

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

## v3.15.0 - NIN Training Mode

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

## v3.14.0 - Melee DPS Training Mode (Phase 1)

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

## v3.13.0 - Tank Skill Quizzes

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

## v3.12.0 - Tank Training Mode

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

## v3.11.0 - Training Mode: Skill Quizzes

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

## v3.10.0 - Performance-Based Lesson Recommendations

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

## v3.9.0 - Training Mode: Lessons Tab

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
