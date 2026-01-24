namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Registry of all skill quizzes for Training Mode.
/// </summary>
public static class QuizRegistry
{
    private static readonly Dictionary<string, QuizDefinition> QuizzesByLessonId = new();
    private static readonly Dictionary<string, QuizDefinition> QuizzesById = new();

    static QuizRegistry()
    {
        // Register all healer quizzes
        foreach (var quiz in WhmQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in SchQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in AstQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in SgeQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        // Register all tank quizzes
        foreach (var quiz in PldQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in WarQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in DrkQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in GnbQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        // Register all melee DPS quizzes
        foreach (var quiz in DrgQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in NinQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in SamQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in MnkQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in RprQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in VprQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        // Register ranged physical DPS quizzes
        foreach (var quiz in MchQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }

        foreach (var quiz in BrdQuizzes.AllQuizzes)
        {
            QuizzesByLessonId[quiz.LessonId] = quiz;
            QuizzesById[quiz.QuizId] = quiz;
        }
    }

    /// <summary>
    /// Gets the quiz for a specific lesson.
    /// </summary>
    public static QuizDefinition? GetQuizForLesson(string lessonId)
    {
        return QuizzesByLessonId.TryGetValue(lessonId, out var quiz) ? quiz : null;
    }

    /// <summary>
    /// Gets a quiz by its ID.
    /// </summary>
    public static QuizDefinition? GetQuiz(string quizId)
    {
        return QuizzesById.TryGetValue(quizId, out var quiz) ? quiz : null;
    }

    /// <summary>
    /// Gets all quizzes for a specific job.
    /// </summary>
    public static IReadOnlyList<QuizDefinition> GetQuizzesForJob(string jobPrefix)
    {
        return jobPrefix.ToLowerInvariant() switch
        {
            // Healers
            "whm" => WhmQuizzes.AllQuizzes,
            "sch" => SchQuizzes.AllQuizzes,
            "ast" => AstQuizzes.AllQuizzes,
            "sge" => SgeQuizzes.AllQuizzes,
            // Tanks
            "pld" => PldQuizzes.AllQuizzes,
            "war" => WarQuizzes.AllQuizzes,
            "drk" => DrkQuizzes.AllQuizzes,
            "gnb" => GnbQuizzes.AllQuizzes,
            // Melee DPS
            "drg" => DrgQuizzes.AllQuizzes,
            "nin" => NinQuizzes.AllQuizzes,
            "sam" => SamQuizzes.AllQuizzes,
            "mnk" => MnkQuizzes.AllQuizzes,
            "rpr" => RprQuizzes.AllQuizzes,
            "vpr" => VprQuizzes.AllQuizzes,
            // Ranged Physical DPS
            "mch" => MchQuizzes.AllQuizzes,
            "brd" => BrdQuizzes.AllQuizzes,
            _ => Array.Empty<QuizDefinition>(),
        };
    }
}

/// <summary>
/// WHM (Apollo) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class WhmQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "whm.lesson_1.quiz",
        LessonId = "whm.lesson_1",
        Title = "Quiz: Healer Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "whm.lesson_1.q1",
                ConceptId = WhmConcepts.HealingPriority,
                Scenario = "You're in a dungeon. The tank is at 60% HP with Regen active. A DPS just took avoidable damage and is at 45% HP. No immediate boss mechanics are coming.",
                Question = "Who should you prioritize healing?",
                Options = new[] { "The tank - they always come first", "The DPS - they're lower HP", "Neither - Regen will handle the tank and the DPS can wait", "The DPS first, then refresh Regen on tank" },
                CorrectIndex = 2,
                Explanation = "With Regen active and no incoming damage, the tank is fine at 60%. The DPS at 45% isn't in danger. Unnecessary healing is a DPS loss - cast Glare instead and let Regen do its job.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_1.q2",
                ConceptId = WhmConcepts.TankPriority,
                Scenario = "A boss is auto-attacking the tank for consistent damage. The tank is at 50% HP. You have Tetragrammaton available.",
                Question = "What's the best approach?",
                Options = new[] { "Cast Cure II immediately", "Use Tetragrammaton between GCDs", "Wait until tank is lower", "Use Benediction to be safe" },
                CorrectIndex = 1,
                Explanation = "Tetragrammaton is an oGCD heal - weave it between Glare casts. This maintains your DPS while healing. Cure II costs a GCD, and Benediction is overkill at 50% HP.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_1.q3",
                ConceptId = WhmConcepts.PartyWideDamage,
                Scenario = "The boss casts a raidwide that hits everyone for 40% of their HP. Everyone is now at 60% HP. You have Afflatus Rapture and Medica II available.",
                Question = "What's the optimal response?",
                Options = new[] { "Medica II for the HoT", "Afflatus Rapture for instant healing", "Afflatus Rapture, then continue DPS - the party is fine", "Medica II followed by Medica" },
                CorrectIndex = 2,
                Explanation = "At 60% HP with no follow-up damage, the party isn't in danger. Afflatus Rapture is a 'free' heal (builds Blood Lily) and instant. One heal is enough - overcasting wastes GCDs.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_1.q4",
                ConceptId = WhmConcepts.OgcdWeaving,
                Scenario = "You just cast Glare. The tank needs healing and you have Tetragrammaton available. Your GCD is still rolling.",
                Question = "When should you use Tetragrammaton?",
                Options = new[] { "Wait for GCD to finish, then use it", "Use it immediately during the GCD window", "Save it for a bigger emergency", "Use Cure II instead since GCD is rolling" },
                CorrectIndex = 1,
                Explanation = "oGCDs like Tetragrammaton should be 'weaved' during the GCD window (after casting but before next GCD is ready). This lets you heal without losing DPS uptime.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_1.q5",
                ConceptId = WhmConcepts.HealingPriority,
                Scenario = "In a trial, both tanks are at 70% HP. Tank A has your Regen. Tank B is the off-tank with no HoTs.",
                Question = "The boss is about to use a tankbuster on Tank A. What do you do?",
                Options = new[] { "Heal Tank B since they have no HoT", "Pre-shield Tank A with Divine Benison", "Heal Tank A after the buster hits", "Use Medica II to heal both" },
                CorrectIndex = 1,
                Explanation = "Tank A is taking the buster - they need attention. Divine Benison (shield) applied BEFORE damage reduces the buster's impact. Healing after is reactive; shielding before is proactive.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "whm.lesson_2.quiz",
        LessonId = "whm.lesson_2",
        Title = "Quiz: Emergency Response",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "whm.lesson_2.q1",
                ConceptId = WhmConcepts.BenedictionUsage,
                Scenario = "The tank is at 15% HP after a tankbuster. The boss is casting another attack in 3 seconds. You have both Benediction and Tetragrammaton available.",
                Question = "What should you use?",
                Options = new[] { "Benediction - they're critically low", "Tetragrammaton - save Benediction", "Cure II for reliable healing", "Both Tetragrammaton and Benediction" },
                CorrectIndex = 0,
                Explanation = "At 15% with another attack coming in 3 seconds, this IS an emergency. Benediction guarantees survival instantly. Tetragrammaton might not heal enough, and Cure II takes too long.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_2.q2",
                ConceptId = WhmConcepts.TetragrammatonUsage,
                Scenario = "The tank is at 40% HP during normal combat. Benediction is on cooldown. Tetragrammaton is available.",
                Question = "What's the best action?",
                Options = new[] { "Cure II since Bene is down", "Tetragrammaton immediately", "Wait until they're lower", "Afflatus Solace to build Blood Lily" },
                CorrectIndex = 1,
                Explanation = "Tetragrammaton at 40% is perfect - it's free, instant, and the tank isn't in immediate danger but needs healing. Don't wait for them to get dangerously low.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_2.q3",
                ConceptId = WhmConcepts.EmergencyHealing,
                Scenario = "A DPS stands in an AoE and drops to 20% HP. The boss is targeting them with a follow-up attack. Benediction and Tetragrammaton are both available.",
                Question = "What should you do?",
                Options = new[] { "Benediction - save them at all costs", "Tetragrammaton - save Bene for tank", "Let them die - it's their fault", "Cure II since they're not the tank" },
                CorrectIndex = 1,
                Explanation = "Tetragrammaton should heal a DPS from 20% adequately. Reserve Benediction for tank emergencies. DPS have less HP than tanks, so Tetra's healing is proportionally larger on them.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_2.q4",
                ConceptId = WhmConcepts.BenedictionUsage,
                Scenario = "You're 4 minutes into a fight. Benediction hasn't been used yet. The tank is at 55% HP with no immediate danger.",
                Question = "What should you do with Benediction?",
                Options = new[] { "Save it for a real emergency", "Use it now - it's been too long", "Use Tetragrammaton instead", "The tank is fine, cast Glare" },
                CorrectIndex = 3,
                Explanation = "55% HP with no danger means no healing needed. However, holding Benediction for 4+ minutes is wasteful - you've likely missed uses. The answer here is to DPS, but be ready to use Bene proactively.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_2.q5",
                ConceptId = WhmConcepts.EmergencyHealing,
                Scenario = "Two party members are at 25% HP. The boss's next attack will hit one of them. You can only save one with instant healing.",
                Question = "Who do you prioritize?",
                Options = new[] { "The tank - always", "The DPS - they're squishier", "Whoever the boss is targeting", "The one with lower current HP" },
                CorrectIndex = 2,
                Explanation = "In an emergency where you can only save one, save the person who will actually die. The boss targeting determines who needs the heal - the other person can wait one GCD.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "whm.lesson_3.quiz",
        LessonId = "whm.lesson_3",
        Title = "Quiz: The Lily System",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "whm.lesson_3.q1",
                ConceptId = WhmConcepts.LilyManagement,
                Scenario = "You have 3 lilies and 0 Blood Lily stacks. The party is at full HP with no damage coming for 20 seconds.",
                Question = "What should you do?",
                Options = new[] { "Hold the lilies for when damage comes", "Use Afflatus Rapture to avoid overcapping", "Use Afflatus Solace on the tank", "Convert lilies to Blood Lily for Misery" },
                CorrectIndex = 1,
                Explanation = "At 3 lilies, you're capped and wasting generation. Even with no damage, spend a lily (Rapture for AoE overheal is fine) to avoid losing the next lily that generates in 20s.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_3.q2",
                ConceptId = WhmConcepts.AfflatusMiseryTiming,
                Scenario = "You have Afflatus Misery ready (Blood Lily full). The boss is about to jump away for 10 seconds of downtime.",
                Question = "When should you use Afflatus Misery?",
                Options = new[] { "Before the boss jumps", "During downtime on adds", "Save it for after boss returns", "Use it on the boss immediately" },
                CorrectIndex = 0,
                Explanation = "Use Misery before downtime! It's your strongest attack. During downtime you can't hit the boss, so using it after wastes potential damage. If adds spawn, use it on them.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_3.q3",
                ConceptId = WhmConcepts.AfflatusSolaceUsage,
                Scenario = "The tank needs healing. You have 2 lilies and Tetragrammaton available.",
                Question = "What's the most efficient choice?",
                Options = new[] { "Afflatus Solace - build Blood Lily", "Tetragrammaton - it's free", "Both for maximum healing", "Cure II - save resources" },
                CorrectIndex = 1,
                Explanation = "Tetragrammaton is a true 'free' heal - oGCD with no GCD cost. Afflatus Solace costs a GCD. Use Tetra first; use Solace when you need to spend lilies or Tetra is on CD.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_3.q4",
                ConceptId = WhmConcepts.BloodLilyBuilding,
                Scenario = "You have 2 Blood Lily stacks. The boss will die in approximately 30 seconds. You have 1 lily.",
                Question = "What's your priority?",
                Options = new[] { "Save the lily for emergencies", "Spend it to complete Blood Lily for Misery", "Use Cure II instead to save the lily", "It doesn't matter at this point" },
                CorrectIndex = 1,
                Explanation = "With 30 seconds left, spending 1 lily + Misery cast = ~5 seconds. You have time to complete Blood Lily and cast Misery before the boss dies. A completed Misery > holding a lily.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_3.q5",
                ConceptId = WhmConcepts.AfflatusRaptureUsage,
                Scenario = "A raidwide just hit. Party is at 65% HP. You have 3 lilies and Assize available.",
                Question = "What's the optimal healing response?",
                Options = new[] { "Assize + Afflatus Rapture", "Just Afflatus Rapture", "Just Assize", "Medica II for the HoT" },
                CorrectIndex = 2,
                Explanation = "At 65% HP, Assize alone (400 potency party heal + damage) is sufficient. Save your lilies - you're capped but one Assize handles this. Rapture would be overhealing.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "whm.lesson_4.quiz",
        LessonId = "whm.lesson_4",
        Title = "Quiz: Proactive Healing",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "whm.lesson_4.q1",
                ConceptId = WhmConcepts.RegenMaintenance,
                Scenario = "You're about to pull a dungeon boss. The tank is at full HP.",
                Question = "What should you do before the pull?",
                Options = new[] { "Wait for damage before healing", "Apply Regen to the tank", "Apply Divine Benison", "Cast Glare to start DPS early" },
                CorrectIndex = 1,
                Explanation = "Pre-pull Regen lets you start with Glare spam while the HoT handles early damage. Divine Benison is also good but Regen provides sustained value. Both is ideal.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_4.q2",
                ConceptId = WhmConcepts.DivineBenisonUsage,
                Scenario = "The boss is casting a tankbuster. It will hit in 4 seconds. The tank is at 80% HP.",
                Question = "What should you do?",
                Options = new[] { "Nothing - tank is healthy", "Apply Divine Benison now", "Wait for the hit, then heal", "Use Aquaveil instead" },
                CorrectIndex = 1,
                Explanation = "Divine Benison (shield) before the buster reduces incoming damage. Shields applied BEFORE damage are proactive and more efficient than healing after. Both Benison and Aquaveil work here.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_4.q3",
                ConceptId = WhmConcepts.AssizeUsage,
                Scenario = "Assize just came off cooldown. The party is at 95% HP. The boss is targetable.",
                Question = "Should you use Assize?",
                Options = new[] { "No - save it for when healing is needed", "Yes - it deals damage too", "No - it would be overhealing", "Wait for party to take damage first" },
                CorrectIndex = 1,
                Explanation = "Assize deals damage AND heals. Even at 95% HP, use it on cooldown for the damage. The healing is a bonus. Holding Assize for 'better healing' loses significant DPS.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_4.q4",
                ConceptId = WhmConcepts.ProactiveHealing,
                Scenario = "You know a raidwide is coming in 8 seconds. Party is at full HP. You have Medica II and Divine Benison available.",
                Question = "What's the best preparation?",
                Options = new[] { "Nothing - heal after the damage", "Medica II now for the HoT", "Divine Benison on tank only", "Both Medica II and Divine Benison" },
                CorrectIndex = 1,
                Explanation = "Medica II's HoT will tick during and after the raidwide, providing sustained recovery. Pre-casting it means the party heals automatically while you DPS. Divine Benison is single-target only.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_4.q5",
                ConceptId = WhmConcepts.ShieldTiming,
                Scenario = "You just applied Divine Benison to the tank. The shield hasn't been consumed. Another tankbuster is coming in 5 seconds.",
                Question = "What should you do?",
                Options = new[] { "Apply another Divine Benison", "The existing shield will handle it", "Use Aquaveil for more mitigation", "Cast Cure II to build HP buffer" },
                CorrectIndex = 2,
                Explanation = "Shields don't stack from the same source - you can't double Benison. Since the shield is already up, add Aquaveil (15% mitigation) for additional protection. They complement each other.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "whm.lesson_5.quiz",
        LessonId = "whm.lesson_5",
        Title = "Quiz: Defensive Cooldowns",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "whm.lesson_5.q1",
                ConceptId = WhmConcepts.TemperanceUsage,
                Scenario = "A heavy raidwide is coming in 5 seconds. The party is at full HP. Temperance is available.",
                Question = "When should you use Temperance?",
                Options = new[] { "After the raidwide hits", "Right now, before damage", "Save it for healing phase", "During the raidwide cast" },
                CorrectIndex = 1,
                Explanation = "Temperance provides 10% party mitigation. Use it BEFORE damage hits so the mitigation applies. Using it after means the damage was unmitigated.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_5.q2",
                ConceptId = WhmConcepts.AquaveilUsage,
                Scenario = "The boss is casting a tankbuster. Divine Benison is on cooldown. Aquaveil is available.",
                Question = "What should you do?",
                Options = new[] { "Nothing - Benison is down", "Use Aquaveil on the tank", "Heal through it reactively", "Use Temperance instead" },
                CorrectIndex = 1,
                Explanation = "Aquaveil provides 15% mitigation on one target for 8 seconds - perfect for tankbusters. It's your backup when Benison is down, or use both together for big busters.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_5.q3",
                ConceptId = WhmConcepts.LiturgyOfTheBellUsage,
                Scenario = "You know the boss will deal 5 instances of party damage over the next 15 seconds. Liturgy of the Bell is available.",
                Question = "When should you place Liturgy?",
                Options = new[] { "After the first hit", "Before the damage sequence starts", "Save it for emergencies", "During the last hit for healing" },
                CorrectIndex = 1,
                Explanation = "Liturgy of the Bell heals when damage is taken (up to 5 times). Place it BEFORE the damage sequence so all 5 hits trigger healing. Placing it late wastes stacks.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_5.q4",
                ConceptId = WhmConcepts.TemperanceUsage,
                Scenario = "Temperance is active (15 seconds remaining). A second raidwide is coming in 20 seconds.",
                Question = "Will Temperance help with the second raidwide?",
                Options = new[] { "Yes - it lasts long enough", "No - it will expire first", "Partially - reduced effect", "Yes - if you refresh it" },
                CorrectIndex = 1,
                Explanation = "Temperance lasts 20 seconds but has 15 seconds remaining. The raidwide in 20 seconds will hit after Temperance expires. Plan cooldown timing around mechanic timings.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_5.q5",
                ConceptId = WhmConcepts.AquaveilUsage,
                Scenario = "The tank is about to take a double tankbuster (two hits rapidly). You have both Divine Benison and Aquaveil.",
                Question = "What's the optimal defensive setup?",
                Options = new[] { "Just Divine Benison", "Just Aquaveil", "Both Divine Benison and Aquaveil", "Save one for after" },
                CorrectIndex = 2,
                Explanation = "Double tankbusters are dangerous. Benison's shield absorbs some of hit 1, while Aquaveil's 15% mitigation reduces BOTH hits. Stack them for heavy tank damage.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "whm.lesson_6.quiz",
        LessonId = "whm.lesson_6",
        Title = "Quiz: DPS Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "whm.lesson_6.q1",
                ConceptId = WhmConcepts.DotMaintenance,
                Scenario = "Dia has 4 seconds remaining on the boss. You're about to cast your next GCD.",
                Question = "Should you refresh Dia?",
                Options = new[] { "Yes - refresh now", "No - wait until it falls off", "No - wait until 3 seconds or less", "Yes - always keep it above 5 seconds" },
                CorrectIndex = 2,
                Explanation = "Refresh DoTs at 3 seconds or less to avoid 'clipping' (losing ticks). Refreshing at 4 seconds wastes 1 second of the previous Dia. Wait for the optimal refresh window.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_6.q2",
                ConceptId = WhmConcepts.GlarePriority,
                Scenario = "The party is at full HP. The tank has Regen. No mechanics are coming for 10 seconds.",
                Question = "What should you be doing?",
                Options = new[] { "Refresh Regen early", "Cast Glare repeatedly", "Prepare heals for later", "Use Presence of Mind" },
                CorrectIndex = 1,
                Explanation = "When healing isn't needed, DPS. Glare is your filler GCD - spam it. Refreshing Regen early wastes ticks. Presence of Mind is for burst phases, not idle time.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_6.q3",
                ConceptId = WhmConcepts.DpsOptimization,
                Scenario = "You need to move for a mechanic. You have Dia ready and 1 lily. Boss has 10 seconds on Dia.",
                Question = "What should you use while moving?",
                Options = new[] { "Dia refresh", "Afflatus Solace/Rapture", "Nothing - just move", "Swiftcast Glare" },
                CorrectIndex = 1,
                Explanation = "Lily heals are instant - perfect for movement. Dia isn't worth refreshing at 10 seconds remaining. Use a lily to keep your GCD rolling while moving.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_6.q4",
                ConceptId = WhmConcepts.GlarePriority,
                Scenario = "The boss will become untargetable in 2.5 seconds (one GCD). You have a lily and Dia has 25 seconds remaining.",
                Question = "What's your final GCD?",
                Options = new[] { "Glare for damage", "Afflatus Solace on tank", "Dia for DoT extension", "Afflatus Misery if ready" },
                CorrectIndex = 3,
                Explanation = "Before downtime, use your strongest ability. If Misery is ready, use it (1240 potency). If not, Glare (310 potency). Don't waste the GCD on unnecessary healing or DoT refresh.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_6.q5",
                ConceptId = WhmConcepts.DpsOptimization,
                Scenario = "In a dungeon pull, 5 enemies are stacked. You have Holy and Glare available.",
                Question = "What should you spam?",
                Options = new[] { "Glare - single target is always better", "Holy - it hits all enemies", "Alternate between them", "Depends on tank HP" },
                CorrectIndex = 1,
                Explanation = "Holy hits all enemies in range. At 5 targets, Holy deals 5x the effective damage of Glare. Plus Holy stuns enemies, reducing damage to the tank. Spam Holy in AoE.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "whm.lesson_7.quiz",
        LessonId = "whm.lesson_7",
        Title = "Quiz: Utility & Coordination",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "whm.lesson_7.q1",
                ConceptId = WhmConcepts.EsunaUsage,
                Scenario = "A party member has a debuff with a white bar above the icon. They're at 80% HP.",
                Question = "What should you do?",
                Options = new[] { "Heal them first", "Esuna immediately", "Wait to see if it's dangerous", "Ignore it - they're healthy" },
                CorrectIndex = 1,
                Explanation = "White bar = cleansable with Esuna. Remove it immediately - many debuffs deal damage over time or have nasty effects. Don't wait for it to become problematic.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_7.q2",
                ConceptId = WhmConcepts.RaiseDecision,
                Scenario = "A DPS died. You have Swiftcast available. Your co-healer (SCH) is casting Broil.",
                Question = "What should you do?",
                Options = new[] { "Swiftcast + Raise immediately", "Wait for SCH to raise", "Hardcast Raise to save Swiftcast", "Wait until combat ends" },
                CorrectIndex = 0,
                Explanation = "Your Swiftcast is available and the SCH is DPSing. Take the raise - Swiftcast + Raise is the standard combo. Don't wait and cause unnecessary downtime for the dead player.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_7.q3",
                ConceptId = WhmConcepts.CoHealerAwareness,
                Scenario = "A raidwide just hit. Your co-healer (AST) used Celestial Opposition. Party is at 50% HP.",
                Question = "What should you do?",
                Options = new[] { "Afflatus Rapture for immediate healing", "Wait - C.Opposition has a HoT", "Medica II to stack HoTs", "Cure III for burst healing" },
                CorrectIndex = 1,
                Explanation = "Celestial Opposition applies a HoT. At 50% with a HoT ticking, the party will recover. Adding more healing is overhealing. Trust your co-healer's abilities and DPS instead.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_7.q4",
                ConceptId = WhmConcepts.PartyCoordination,
                Scenario = "You're progressing a savage fight. A big raidwide is coming. Your co-healer says they'll use their party mitigation.",
                Question = "What should you do with Temperance?",
                Options = new[] { "Use it too for safety", "Save it for the next raidwide", "Use it anyway - more mitigation is better", "Ask them to not use theirs" },
                CorrectIndex = 1,
                Explanation = "Stacking party mitigations on one raidwide is wasteful - mitigation has diminishing returns. Coordinate: one healer mitigates this raidwide, the other saves for the next one.",
            },
            new QuizQuestion
            {
                QuestionId = "whm.lesson_7.q5",
                ConceptId = WhmConcepts.RaiseDecision,
                Scenario = "Two DPS died at the same time. You have Swiftcast. Your co-healer also has Swiftcast.",
                Question = "How should the raises be handled?",
                Options = new[] { "You raise both (hardcast second)", "Each healer raises one", "Wait and see who acts first", "Raise the higher DPS first" },
                CorrectIndex = 1,
                Explanation = "With two dead and two Swiftcasts available, each healer should raise one. This gets both DPS up faster than one healer doing both raises. Coordinate quickly.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// SCH (Athena) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class SchQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "sch.lesson_1.quiz",
        LessonId = "sch.lesson_1",
        Title = "Quiz: Scholar Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sch.lesson_1.q1",
                ConceptId = SchConcepts.EmergencyHealing,
                Scenario = "The tank drops to 20% HP after a tankbuster. You have 2 Aetherflow stacks and Lustrate available.",
                Question = "What's your emergency response?",
                Options = new[] { "Adloquium for shield + heal", "Lustrate immediately", "Physick for MP efficiency", "Wait for fairy healing" },
                CorrectIndex = 1,
                Explanation = "Lustrate is instant oGCD healing - perfect for emergencies. Adloquium is a GCD cast (too slow), Physick is weak, and fairy healing is too slow for 20% HP.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_1.q2",
                ConceptId = SchConcepts.ShieldTiming,
                Scenario = "A tankbuster will hit in 4 seconds. The tank is at full HP with no shields.",
                Question = "What should you do?",
                Options = new[] { "Nothing - tank is full", "Cast Adloquium now", "Wait for buster, then heal", "Use Lustrate preemptively" },
                CorrectIndex = 1,
                Explanation = "SCH is a shield healer - apply Adloquium BEFORE the buster hits. The shield absorbs damage, reducing the hit. Shields applied after damage are wasted.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_1.q3",
                ConceptId = SchConcepts.LustrateUsage,
                Scenario = "Tank is at 45% HP. You have 1 Aetherflow stack. Aetherflow comes off CD in 5 seconds.",
                Question = "Should you use Lustrate?",
                Options = new[] { "No - save for emergency", "Yes - heal the tank", "No - wait for Aetherflow refresh", "Use Physick instead" },
                CorrectIndex = 1,
                Explanation = "At 45% with Aetherflow refreshing soon, use Lustrate. You'll have 3 fresh stacks in 5 seconds. Holding stacks when Aetherflow is about to refresh wastes resources.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_1.q4",
                ConceptId = SchConcepts.ShieldTiming,
                Scenario = "You cast Adloquium. The tank now has a Galvanize shield. Another Adloquium would fully overheal.",
                Question = "What happens if you cast Adloquium again?",
                Options = new[] { "Shields stack for more protection", "New shield replaces old if larger", "The heal applies, shield is wasted", "Nothing - can't reapply" },
                CorrectIndex = 2,
                Explanation = "Adloquium always applies the heal. The shield only refreshes if the new shield would be larger. If overhealing, the heal is wasted but shield might refresh.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_1.q5",
                ConceptId = SchConcepts.EmergencyHealing,
                Scenario = "Two party members are low (30% HP). You have 2 Aetherflow stacks.",
                Question = "How do you handle this?",
                Options = new[] { "Indomitability for both", "Lustrate each one", "Succor for party heal", "One Lustrate, one Indom" },
                CorrectIndex = 0,
                Explanation = "Indomitability (1 stack) heals the whole party instantly. Using 2 Lustrates costs 2 stacks for the same result. Indom is more efficient for multiple low targets.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "sch.lesson_2.quiz",
        LessonId = "sch.lesson_2",
        Title = "Quiz: Aetherflow Mastery",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sch.lesson_2.q1",
                ConceptId = SchConcepts.AetherflowManagement,
                Scenario = "You have 2 Aetherflow stacks. Aetherflow ability comes off cooldown in 3 seconds. No healing needed.",
                Question = "What should you do?",
                Options = new[] { "Hold stacks for emergencies", "Energy Drain twice to dump stacks", "Use one Energy Drain", "Wait for CD, then refresh" },
                CorrectIndex = 1,
                Explanation = "Never let Aetherflow refresh while holding stacks - you'd waste 2 stacks. Dump them into Energy Drain for damage before refreshing to get your full 3 new stacks.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_2.q2",
                ConceptId = SchConcepts.AetherflowRefresh,
                Scenario = "Aetherflow just came off cooldown. You have 0 stacks.",
                Question = "When should you use Aetherflow?",
                Options = new[] { "Immediately", "Wait for damage first", "Save it for emergency", "After next GCD" },
                CorrectIndex = 0,
                Explanation = "Use Aetherflow immediately when it's ready and you have 0 stacks. It's oGCD, gives you resources AND MP. Delaying means your next refresh is delayed too.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_2.q3",
                ConceptId = SchConcepts.EnergyDrainUsage,
                Scenario = "You have 3 Aetherflow stacks. Party is healthy. Aetherflow has 45 seconds remaining.",
                Question = "How should you use your stacks?",
                Options = new[] { "Hold all for healing", "Energy Drain all three", "Use 1-2 Energy Drains, keep 1 for safety", "Sacred Soil for mitigation" },
                CorrectIndex = 2,
                Explanation = "With 45 seconds until refresh, you have time to use stacks. Energy Drain for DPS, but keep 1 stack for unexpected damage. You'll generate more stacks before refresh.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_2.q4",
                ConceptId = SchConcepts.AetherflowManagement,
                Scenario = "Heavy damage phase starting. You have 1 Aetherflow stack. Aetherflow CD has 30 seconds remaining.",
                Question = "What's your plan?",
                Options = new[] { "Save the stack for emergency", "Use it on Sacred Soil preemptively", "Energy Drain for DPS", "Lustrate the tank now" },
                CorrectIndex = 1,
                Explanation = "During damage phases, healing stacks > DPS stacks. Sacred Soil provides mitigation AND HoT for the whole party. Use your stack proactively for the incoming damage.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_2.q5",
                ConceptId = SchConcepts.EnergyDrainUsage,
                Scenario = "The boss is about to die (5% HP). You have 2 Aetherflow stacks.",
                Question = "What do you do with your stacks?",
                Options = new[] { "Save them", "Energy Drain both", "Indomitability for safety", "It doesn't matter" },
                CorrectIndex = 1,
                Explanation = "Boss about to die = no healing needed. Convert those stacks to damage with Energy Drain. Every bit of DPS helps finish the fight faster. Don't waste resources.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "sch.lesson_3.quiz",
        LessonId = "sch.lesson_3",
        Title = "Quiz: Fairy Management",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sch.lesson_3.q1",
                ConceptId = SchConcepts.WhisperingDawnUsage,
                Scenario = "A raidwide just hit. Party is at 60% HP. Whispering Dawn is available.",
                Question = "Should you use Whispering Dawn?",
                Options = new[] { "No - party isn't critical", "Yes - free party HoT", "No - save for bigger damage", "Yes - but also Succor" },
                CorrectIndex = 1,
                Explanation = "Whispering Dawn is a free oGCD party HoT. At 60%, the HoT will heal everyone to full over time. Use it - you don't need to save it when it accomplishes the healing goal.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_3.q2",
                ConceptId = SchConcepts.FeyBlessingUsage,
                Scenario = "Party needs immediate healing. Whispering Dawn was just used. Fey Blessing available.",
                Question = "What should you do?",
                Options = new[] { "Wait for Whispering Dawn", "Use Fey Blessing", "Cast Succor", "Indomitability" },
                CorrectIndex = 1,
                Explanation = "Fey Blessing is instant party healing - different from Whispering Dawn's HoT. Use it for immediate healing needs. It's oGCD and doesn't cost Aetherflow.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_3.q3",
                ConceptId = SchConcepts.FeyUnionUsage,
                Scenario = "Tank is taking heavy sustained damage. Fairy Gauge is at 80. Other fairy abilities on CD.",
                Question = "What's the best use of Fey Union?",
                Options = new[] { "Save gauge for Seraph", "Activate Fey Union on tank", "Don't use - too expensive", "Wait until gauge is full" },
                CorrectIndex = 1,
                Explanation = "Fey Union tethers your fairy to the tank for continuous healing. At 80 gauge with heavy tank damage, this is exactly when to use it. It drains gauge over time.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_3.q4",
                ConceptId = SchConcepts.FeyIlluminationUsage,
                Scenario = "Heavy party damage phase starting in 5 seconds. Fey Illumination available.",
                Question = "When should you use Fey Illumination?",
                Options = new[] { "After damage hits", "Right before damage", "During the damage", "Save for emergency" },
                CorrectIndex = 1,
                Explanation = "Fey Illumination provides magic mitigation AND healing boost. Use it BEFORE damage so the mitigation applies. The healing boost helps your follow-up heals too.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_3.q5",
                ConceptId = SchConcepts.FairyManagement,
                Scenario = "Your fairy is far from the party. A raidwide is about to hit.",
                Question = "Will fairy abilities still work?",
                Options = new[] { "No - fairy must be close", "Yes - abilities have range", "Only Embrace works", "Need to resummon fairy" },
                CorrectIndex = 1,
                Explanation = "Fairy abilities like Whispering Dawn and Fey Blessing have their own range centered on the fairy. However, they work from significant distance. Only Embrace is truly proximity-based.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "sch.lesson_4.quiz",
        LessonId = "sch.lesson_4",
        Title = "Quiz: Advanced Fairy & Seraph",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sch.lesson_4.q1",
                ConceptId = SchConcepts.SeraphUsage,
                Scenario = "Heavy damage phase with multiple raidwides coming. Seraph available.",
                Question = "When should you summon Seraph?",
                Options = new[] { "After first raidwide", "Before the phase starts", "Save for emergency", "During downtime" },
                CorrectIndex = 1,
                Explanation = "Seraph lasts 22 seconds and provides Consolation shields. Summon before a heavy phase to get full value from her abilities during the damage.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_4.q2",
                ConceptId = SchConcepts.DissipationUsage,
                Scenario = "You need more Aetherflow stacks immediately. Dissipation and Aetherflow are both available.",
                Question = "Which should you use?",
                Options = new[] { "Dissipation - more stacks", "Aetherflow - keeps fairy", "Both for 6 stacks", "Depends on situation" },
                CorrectIndex = 3,
                Explanation = "Dissipation sacrifices fairy for 3 stacks + 20% healing boost. Aetherflow just gives 3 stacks. Use Aetherflow normally; Dissipation when you need burst healing AND extra stacks.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_4.q3",
                ConceptId = SchConcepts.ExcogitationUsage,
                Scenario = "A tankbuster will hit in 10 seconds. Tank is at full HP. You have Excogitation available.",
                Question = "Should you use Excogitation now?",
                Options = new[] { "No - tank is full", "Yes - it triggers when they drop", "No - waste of Aetherflow", "Wait until after buster" },
                CorrectIndex = 1,
                Explanation = "Excogitation triggers automatically when the target drops below 50% HP. Apply it BEFORE the tankbuster - when the buster hits and drops them, Excog heals instantly.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_4.q4",
                ConceptId = SchConcepts.SeraphUsage,
                Scenario = "Seraph is active. You have 2 Consolation charges. A raidwide hits in 3 seconds.",
                Question = "How should you use Consolation?",
                Options = new[] { "Use both charges now", "One before, one after", "Save both for after", "Use one before damage" },
                CorrectIndex = 3,
                Explanation = "Consolation provides a shield. Use one charge BEFORE the raidwide for mitigation. Save the second charge for follow-up damage or healing. Don't double-shield the same hit.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_4.q5",
                ConceptId = SchConcepts.DissipationUsage,
                Scenario = "Dissipation is active (fairy gone). Tank needs emergency healing.",
                Question = "What resources do you have?",
                Options = new[] { "Fairy abilities still work", "Only Aetherflow abilities", "Just GCD heals", "Lustrate and GCD heals" },
                CorrectIndex = 3,
                Explanation = "Dissipation sacrifices your fairy, so no fairy abilities. But you have Aetherflow abilities (Lustrate, etc.) AND the 20% healing boost on your GCD heals. Use Lustrate for emergency.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "sch.lesson_5.quiz",
        LessonId = "sch.lesson_5",
        Title = "Quiz: Shield Economy",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sch.lesson_5.q1",
                ConceptId = SchConcepts.DeploymentTactics,
                Scenario = "Raidwide in 5 seconds. You just crit an Adloquium (Catalyze) on the tank.",
                Question = "What should you do with Deployment Tactics?",
                Options = new[] { "Use it to spread the shield", "Save it for emergencies", "Let tank keep the shield", "Apply new Adlo first" },
                CorrectIndex = 0,
                Explanation = "Deployment Tactics spreads your target's shield to the party. A crit Adlo (Catalyze) is especially powerful. Spread it to give everyone a massive shield before the raidwide.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_5.q2",
                ConceptId = SchConcepts.EmergencyTacticsUsage,
                Scenario = "Party is at 30% HP and already has shields from Succor. Another hit is coming.",
                Question = "You need to heal but shields would be wasted. What do you use?",
                Options = new[] { "Succor anyway", "Emergency Tactics + Succor", "Indomitability", "Wait for shields to break" },
                CorrectIndex = 1,
                Explanation = "Emergency Tactics converts your next Adlo/Succor shield into pure healing. Party has shields but needs HP - E.Tactics Succor heals without wasting the shield portion.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_5.q3",
                ConceptId = SchConcepts.RecitationUsage,
                Scenario = "You want to maximize a shield before a big tankbuster. Recitation available.",
                Question = "What combination maximizes the shield?",
                Options = new[] { "Recitation + Succor", "Recitation + Adloquium", "Recitation + Excogitation", "Recitation + Indomitability" },
                CorrectIndex = 1,
                Explanation = "Recitation guarantees a critical hit. Adloquium when crit produces Catalyze (extra shield). Recitation + Adlo = guaranteed massive shield for tankbusters.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_5.q4",
                ConceptId = SchConcepts.SuccorUsage,
                Scenario = "Raidwide coming. Party is at full HP. You have Succor and Adlo available.",
                Question = "Which should you use for party shielding?",
                Options = new[] { "Adlo on tank", "Succor for party shield", "Adlo + Deploy", "Neither - heal after" },
                CorrectIndex = 1,
                Explanation = "Succor shields the whole party in one GCD. Adlo + Deploy requires 2 actions. For simple pre-raidwide shielding, Succor is more efficient unless you crit the Adlo.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_5.q5",
                ConceptId = SchConcepts.AdloquiumUsage,
                Scenario = "Tank has a shield. You cast another Adloquium that would create a smaller shield.",
                Question = "What happens?",
                Options = new[] { "Shields stack", "Old shield replaced", "New shield ignored, heal applies", "Nothing happens" },
                CorrectIndex = 2,
                Explanation = "The heal portion of Adloquium always applies. The shield only replaces if the new shield is larger. If smaller, the existing shield stays and the new shield is wasted.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "sch.lesson_6.quiz",
        LessonId = "sch.lesson_6",
        Title = "Quiz: oGCD Healing & Mitigation",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sch.lesson_6.q1",
                ConceptId = SchConcepts.SacredSoilUsage,
                Scenario = "Heavy party damage incoming over the next 15 seconds. Sacred Soil available.",
                Question = "Where and when should you place Sacred Soil?",
                Options = new[] { "After damage starts", "Before damage, under the party", "Save it for emergency", "Under the boss" },
                CorrectIndex = 1,
                Explanation = "Sacred Soil provides 10% mitigation and a powerful HoT. Place it BEFORE damage in a spot where the party will stand. The mitigation and HoT handle sustained damage excellently.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_6.q2",
                ConceptId = SchConcepts.IndomitabilityUsage,
                Scenario = "Party at 50% HP. You have 1 Aetherflow and Indom available. Sacred Soil is on CD.",
                Question = "Should you use Indomitability?",
                Options = new[] { "Yes - instant party heal", "No - save for emergency", "No - 50% is fine", "Use Succor instead" },
                CorrectIndex = 0,
                Explanation = "Indomitability is instant party healing for 1 Aetherflow. At 50% HP, it's a good use - brings party to safe levels immediately. Don't hold it if healing is needed.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_6.q3",
                ConceptId = SchConcepts.ExpedientUsage,
                Scenario = "The party needs to move across the arena quickly for a mechanic. Expedient available.",
                Question = "Should you use Expedient?",
                Options = new[] { "No - it's for healing", "Yes - party sprint + mitigation", "No - save for damage", "Only if people are low" },
                CorrectIndex = 1,
                Explanation = "Expedient gives the whole party a speed boost AND 10% mitigation. It's perfect for movement mechanics. The mitigation is a bonus, but the speed is often the main value.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_6.q4",
                ConceptId = SchConcepts.SacredSoilUsage,
                Scenario = "Tank is taking heavy damage in one spot. You have 1 Aetherflow. Sacred Soil and Lustrate both available.",
                Question = "Which is better for sustained tank healing?",
                Options = new[] { "Lustrate - direct heal", "Sacred Soil - mitigation + HoT", "Both", "Neither - use GCD" },
                CorrectIndex = 1,
                Explanation = "Sacred Soil's HoT is extremely powerful (100 potency per tick) plus 10% mitigation. For sustained damage in one spot, it outvalues a single Lustrate over its duration.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_6.q5",
                ConceptId = SchConcepts.IndomitabilityUsage,
                Scenario = "Sacred Soil is down. Party needs healing. Indomitability has 10 seconds on CD.",
                Question = "What should you do?",
                Options = new[] { "Wait for Indom", "Succor", "Fey Blessing + Whispering Dawn", "Emergency Tactics + Succor" },
                CorrectIndex = 2,
                Explanation = "Fairy abilities are free oGCDs. Fey Blessing for instant heal, Whispering Dawn for HoT - together they handle party healing without spending Aetherflow or GCDs.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "sch.lesson_7.quiz",
        LessonId = "sch.lesson_7",
        Title = "Quiz: DPS & Coordination",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sch.lesson_7.q1",
                ConceptId = SchConcepts.ChainStratagemTiming,
                Scenario = "Fight just started. DPS are using their openers. Chain Stratagem available.",
                Question = "When should you use Chain Stratagem?",
                Options = new[] { "Save for later burst", "Use in opener window", "Wait for boss HP threshold", "After healers stabilize" },
                CorrectIndex = 1,
                Explanation = "Chain Stratagem (10% crit rate on boss) should align with party burst. Openers are the biggest burst - use Chain early to maximize raid DPS during everyone's buffs.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_7.q2",
                ConceptId = SchConcepts.DotMaintenance,
                Scenario = "Biolysis has 5 seconds remaining. You're about to cast Broil.",
                Question = "Should you refresh Biolysis?",
                Options = new[] { "Yes - keep DoT up", "No - wait until 3s or less", "No - wait until it falls", "Yes - always refresh early" },
                CorrectIndex = 1,
                Explanation = "Refresh DoTs at 3 seconds or less to avoid clipping. At 5 seconds, casting Biolysis wastes 5 seconds of the previous application. Wait a GCD or two.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_7.q3",
                ConceptId = SchConcepts.CoHealerAwareness,
                Scenario = "You're paired with a WHM. A raidwide just hit. WHM used Afflatus Rapture.",
                Question = "What should you focus on?",
                Options = new[] { "Succor to top off", "Indomitability to stack healing", "Keep DPSing - WHM handled it", "Apply shields for next hit" },
                CorrectIndex = 2,
                Explanation = "Your co-healer handled the healing. Adding more healing is overhealing and DPS loss. Trust your co-healer and continue your DPS rotation. Shield if another hit is coming.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_7.q4",
                ConceptId = SchConcepts.RaiseDecision,
                Scenario = "A DPS died. You have Swiftcast. Your co-healer (AST) is casting Gravity.",
                Question = "Who should raise?",
                Options = new[] { "Wait for AST", "You - Swiftcast ready", "Hardcast Resurrection", "Red Mage if available" },
                CorrectIndex = 1,
                Explanation = "Your Swiftcast is ready and the AST is busy with AoE damage. Take the raise - Swiftcast + Resurrection gets the DPS back immediately. Don't wait.",
            },
            new QuizQuestion
            {
                QuestionId = "sch.lesson_7.q5",
                ConceptId = SchConcepts.DpsOptimization,
                Scenario = "Party is healthy, Biolysis is up, Chain Stratagem on CD. What should you be doing?",
                Question = "What's your filler action?",
                Options = new[] { "Physick to stay busy", "Broil IV spam", "Energy Drain spam", "Ruin II for mobility" },
                CorrectIndex = 1,
                Explanation = "Broil IV is your filler DPS GCD. Spam it when healing isn't needed and DoT is up. Energy Drain uses Aetherflow you might need. Ruin II is for movement only (less damage).",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// AST (Astraea) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class AstQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "ast.lesson_1.quiz",
        LessonId = "ast.lesson_1",
        Title = "Quiz: Astrologian Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "ast.lesson_1.q1",
                ConceptId = AstConcepts.EssentialDignityUsage,
                Scenario = "Tank is at 25% HP. Essential Dignity has 2 charges available.",
                Question = "How effective will Essential Dignity be?",
                Options = new[] { "Normal potency", "Increased potency due to low HP", "Reduced potency", "Same as at full HP" },
                CorrectIndex = 1,
                Explanation = "Essential Dignity heals MORE when the target is at low HP. At 25%, it's at maximum potency. This makes it ideal for emergency situations.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_1.q2",
                ConceptId = AstConcepts.HotManagement,
                Scenario = "Combat is starting. Tank is at full HP.",
                Question = "What should you apply first?",
                Options = new[] { "Benefic II", "Aspected Benefic", "Nothing", "Essential Dignity" },
                CorrectIndex = 1,
                Explanation = "AST is a regen healer - start with Aspected Benefic (HoT) on the tank. The HoT heals incoming damage over time, letting you DPS.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_1.q3",
                ConceptId = AstConcepts.EmergencyHealing,
                Scenario = "Tank at 15% HP. Essential Dignity on CD.",
                Question = "What's your emergency option?",
                Options = new[] { "Benefic II", "Benefic", "Celestial Intersection", "Aspected Benefic" },
                CorrectIndex = 2,
                Explanation = "Celestial Intersection is an oGCD that provides shield and HoT instantly. When Essential Dignity is down, it's your best emergency oGCD.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_1.q4",
                ConceptId = AstConcepts.EssentialDignityUsage,
                Scenario = "Tank at 70% HP. You have 2 Essential Dignity charges.",
                Question = "Should you use Essential Dignity?",
                Options = new[] { "Yes - use freely", "No - tank is too healthy", "Yes - avoid capping", "No - save for emergency" },
                CorrectIndex = 1,
                Explanation = "At 70% HP, Essential Dignity's potency is lower. Wait until tank is lower to maximize its healing value.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_1.q5",
                ConceptId = AstConcepts.HotManagement,
                Scenario = "Aspected Benefic has 10s remaining. Tank at 85% HP.",
                Question = "Should you refresh it?",
                Options = new[] { "Yes", "No - wait until lower", "No - would overheal", "Yes - always maintain" },
                CorrectIndex = 2,
                Explanation = "At 85% with 10s of HoT remaining, refreshing would overheal. Let it tick down - refresh when it's needed AND about to fall off.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "ast.lesson_2.quiz",
        LessonId = "ast.lesson_2",
        Title = "Quiz: Card System Basics",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "ast.lesson_2.q1",
                ConceptId = AstConcepts.DrawTiming,
                Scenario = "Draw just came off CD. You have no card.",
                Question = "When should you Draw?",
                Options = new[] { "Wait for burst", "Draw immediately", "Save for emergency", "Wait for tank damage" },
                CorrectIndex = 1,
                Explanation = "Draw on cooldown. Cards have limited duration, and the sooner you Draw, the sooner you can Draw again.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_2.q2",
                ConceptId = AstConcepts.CardManagement,
                Scenario = "You have a card. Party has 2 melee and 2 ranged DPS.",
                Question = "Who should receive the card?",
                Options = new[] { "The tank", "Any DPS", "The healer", "Best performing DPS" },
                CorrectIndex = 3,
                Explanation = "Cards give damage buffs. Play them on the DPS who will get the most value - usually whoever is performing best.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_2.q3",
                ConceptId = AstConcepts.MinorArcanaUsage,
                Scenario = "You have a card but all DPS are dead.",
                Question = "What should you do?",
                Options = new[] { "Hold for resurrect", "Play on tank", "Minor Arcana", "Let expire" },
                CorrectIndex = 2,
                Explanation = "Minor Arcana converts your card to Lord/Lady of Crowns. If you can't use the card optimally, convert it rather than waste it.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_2.q4",
                ConceptId = AstConcepts.DrawTiming,
                Scenario = "Draw is ready during heavy healing phase.",
                Question = "Should you Draw?",
                Options = new[] { "No - focus on healing", "Yes - Draw is oGCD", "Wait for stability", "Cards aren't important" },
                CorrectIndex = 1,
                Explanation = "Draw is an oGCD - weave it between heals. Even during intense healing, keep the card cycle rolling.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_2.q5",
                ConceptId = AstConcepts.CardManagement,
                Scenario = "A DPS will burst in 5 seconds. You have a card.",
                Question = "When should you play the card?",
                Options = new[] { "Immediately", "Right before burst", "During burst", "After burst" },
                CorrectIndex = 1,
                Explanation = "Play just before burst so the buff is active during their highest damage. Playing during or after wastes buff duration.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "ast.lesson_3.quiz",
        LessonId = "ast.lesson_3",
        Title = "Quiz: Card System Advanced",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "ast.lesson_3.q1",
                ConceptId = AstConcepts.DivinationTiming,
                Scenario = "Fight started. DPS using openers. Divination available.",
                Question = "When should you use Divination?",
                Options = new[] { "Save for later", "Use in opener", "Wait for 2min", "After first mechanic" },
                CorrectIndex = 1,
                Explanation = "Divination (6% party buff) should align with party burst. Openers are the biggest burst - use it early.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_3.q2",
                ConceptId = AstConcepts.OracleUsage,
                Scenario = "You just used Divination. Oracle is available.",
                Question = "What should you do with Oracle?",
                Options = new[] { "Save it", "Use immediately", "Wait for more targets", "Not worth using" },
                CorrectIndex = 1,
                Explanation = "Oracle is granted after Divination and is strong damage. Use it during the burst window you created.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_3.q3",
                ConceptId = AstConcepts.AstrodyneBuilding,
                Scenario = "You've played 2 cards. Need 3 for Astrodyne.",
                Question = "What should you prioritize?",
                Options = new[] { "Wait for perfect card", "Play any card", "Save third card", "Astrodyne isn't important" },
                CorrectIndex = 1,
                Explanation = "Getting Astrodyne active is valuable. Play your third card even if target isn't perfect.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_3.q4",
                ConceptId = AstConcepts.DivinationTiming,
                Scenario = "Divination ready. Major burst in 45s. Small burst now.",
                Question = "When to use Divination?",
                Options = new[] { "Now", "Save for 45s", "Now - will realign", "Depends on fight" },
                CorrectIndex = 3,
                Explanation = "Depends on fight length. If Divination will be ready for major burst, use now. If not, save it. Learn fight timings.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_3.q5",
                ConceptId = AstConcepts.AstrodyneBuilding,
                Scenario = "Played 3 cards with same seal. Astrodyne activates.",
                Question = "What buffs do you receive?",
                Options = new[] { "All three", "One only", "Two", "None" },
                CorrectIndex = 1,
                Explanation = "Astrodyne buffs depend on seal variety. Three same = 1 buff. You want different seals for more buffs.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "ast.lesson_4.quiz",
        LessonId = "ast.lesson_4",
        Title = "Quiz: HoT Economy",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "ast.lesson_4.q1",
                ConceptId = AstConcepts.AspectedBeneficUsage,
                Scenario = "Tank has no HoT. 75% HP, steady damage.",
                Question = "What should you apply?",
                Options = new[] { "Benefic II", "Aspected Benefic", "Essential Dignity", "Nothing" },
                CorrectIndex = 1,
                Explanation = "The HoT from Aspected Benefic handles steady damage over time, letting you DPS. Save oGCDs for bigger drops.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_4.q2",
                ConceptId = AstConcepts.CelestialOppositionUsage,
                Scenario = "Raidwide hit. Party at 60%. Celestial Opposition available.",
                Question = "Should you use it?",
                Options = new[] { "No - save it", "Yes - free oGCD", "No - party is fine", "Use Helios instead" },
                CorrectIndex = 1,
                Explanation = "Celestial Opposition is a free oGCD party heal + HoT. At 60%, it handles recovery without using a GCD.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_4.q3",
                ConceptId = AstConcepts.SynastryUsage,
                Scenario = "Heavy tank damage phase. Synastry available.",
                Question = "How to use Synastry?",
                Options = new[] { "Apply to DPS", "Apply to yourself", "Apply to tank", "Save for emergency" },
                CorrectIndex = 2,
                Explanation = "Synastry duplicates 40% of your heals to the linked target. Apply to tank during heavy damage.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_4.q4",
                ConceptId = AstConcepts.AspectedHeliosUsage,
                Scenario = "Multiple raidwides in 20s. Party at full HP.",
                Question = "Pre-apply Aspected Helios?",
                Options = new[] { "No - wait", "Yes - HoT helps", "No - would overheal", "Only after damage" },
                CorrectIndex = 1,
                Explanation = "Pre-applied HoT means party recovers automatically between hits. Proactive HoTs are AST's strength.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_4.q5",
                ConceptId = AstConcepts.CelestialOppositionUsage,
                Scenario = "C.Opposition ready. Raidwide in 30s. Need healing now.",
                Question = "Should you use it?",
                Options = new[] { "Save for raidwide", "Use now - 60s CD", "Use Helios instead", "Depends on HP" },
                CorrectIndex = 1,
                Explanation = "60s cooldown means it'll be ready again for the raidwide. Don't hold oGCDs unnecessarily.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "ast.lesson_5.quiz",
        LessonId = "ast.lesson_5",
        Title = "Quiz: Earthly Star Mastery",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "ast.lesson_5.q1",
                ConceptId = AstConcepts.EarthlyStarPlacement,
                Scenario = "Raidwide in 12 seconds. Star available.",
                Question = "When to place Earthly Star?",
                Options = new[] { "Right before hit", "Now - needs 10s to mature", "After raidwide", "5 seconds before" },
                CorrectIndex = 1,
                Explanation = "Star takes 10s to mature. Place NOW so it's at full power when the raidwide hits in 12 seconds.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_5.q2",
                ConceptId = AstConcepts.EarthlyStarMaturation,
                Scenario = "Star placed 5s ago. Need healing now.",
                Question = "Should you detonate early?",
                Options = new[] { "No - wait", "Yes - some healing helps", "Use other heals", "Star is useless" },
                CorrectIndex = 2,
                Explanation = "At 5s, Star is half power. Use other heals if possible. Only detonate early if absolutely necessary.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_5.q3",
                ConceptId = AstConcepts.EarthlyStarPlacement,
                Scenario = "Party will stack in 15 seconds for a mechanic.",
                Question = "Where to place Star?",
                Options = new[] { "Current location", "Stack point", "Under boss", "Arena center" },
                CorrectIndex = 1,
                Explanation = "Place where party WILL BE when you detonate. Stack point means mature Star hits everyone.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_5.q4",
                ConceptId = AstConcepts.EarthlyStarMaturation,
                Scenario = "Star out for 18s. Auto-detonates at 20s.",
                Question = "What should you do?",
                Options = new[] { "Let auto-detonate", "Detonate manually", "Doesn't matter", "Place new one" },
                CorrectIndex = 0,
                Explanation = "At 18s, Star is mature. Let it auto-detonate unless you need healing immediately or party is moving.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_5.q5",
                ConceptId = AstConcepts.EarthlyStarPlacement,
                Scenario = "Fight started. No raidwide for 30s.",
                Question = "Should you place Star?",
                Options = new[] { "No - save for raidwide", "Yes - use on CD", "Wait 20 seconds", "Place for tank" },
                CorrectIndex = 1,
                Explanation = "Star deals damage too. Use for DPS value. 60s CD means it's ready for the 30s raidwide anyway.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "ast.lesson_6.quiz",
        LessonId = "ast.lesson_6",
        Title = "Quiz: Defensive Cooldowns",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "ast.lesson_6.q1",
                ConceptId = AstConcepts.NeutralSectUsage,
                Scenario = "Heavy raidwide coming. Party full HP.",
                Question = "What should you do?",
                Options = new[] { "Save Neutral Sect", "Neutral Sect + Asp. Helios", "Just Asp. Helios", "Wait for damage" },
                CorrectIndex = 1,
                Explanation = "Neutral Sect adds shields to Aspected spells. Pre-raidwide, use both for HoT + shield combo.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_6.q2",
                ConceptId = AstConcepts.MacrocosmosUsage,
                Scenario = "Heavy damage in next 5 seconds. Macrocosmos available.",
                Question = "When to use Macrocosmos?",
                Options = new[] { "After damage", "Before damage", "During damage", "Save it" },
                CorrectIndex = 1,
                Explanation = "Macrocosmos stores damage taken. Apply BEFORE damage so it captures it, then detonate to heal back.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_6.q3",
                ConceptId = AstConcepts.ExaltationUsage,
                Scenario = "Tankbuster in 5 seconds. Exaltation available.",
                Question = "Should you use Exaltation?",
                Options = new[] { "No - doesn't shield", "Yes - mitigation + delayed heal", "Wait for buster", "Use C.Intersection" },
                CorrectIndex = 1,
                Explanation = "Exaltation provides 10% mitigation AND heals when it expires. Apply before for mitigation value.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_6.q4",
                ConceptId = AstConcepts.CollectiveUnconsciousUsage,
                Scenario = "Heavy sustained party damage. C.Unconscious available.",
                Question = "How does it work?",
                Options = new[] { "Instant heal", "Channeled mit + HoT", "Shield only", "Mitigation only" },
                CorrectIndex = 1,
                Explanation = "Collective Unconscious is channeled - provides mitigation while channeling and applies a HoT.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_6.q5",
                ConceptId = AstConcepts.LightspeedUsage,
                Scenario = "Need to move but also keep casting.",
                Question = "What ability helps?",
                Options = new[] { "Swiftcast", "Lightspeed", "Presence of Mind", "Nothing" },
                CorrectIndex = 1,
                Explanation = "Lightspeed makes GCDs instant for 15s. Full mobility while maintaining your rotation.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "ast.lesson_7.quiz",
        LessonId = "ast.lesson_7",
        Title = "Quiz: DPS & Coordination",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "ast.lesson_7.q1",
                ConceptId = AstConcepts.DotMaintenance,
                Scenario = "Combust has 4s remaining.",
                Question = "Should you refresh?",
                Options = new[] { "Yes - keep DoT up", "No - wait until 3s", "Yes - always refresh", "Let it fall off" },
                CorrectIndex = 1,
                Explanation = "Refresh at 3s or less to avoid clipping. At 4s, wait one more GCD.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_7.q2",
                ConceptId = AstConcepts.DpsOptimization,
                Scenario = "Party healthy. Combust up. Cards played. No mechanics.",
                Question = "What should you do?",
                Options = new[] { "Pre-heal", "Malefic spam", "Draw cards", "Apply HoTs" },
                CorrectIndex = 1,
                Explanation = "Fall Malefic is your filler. When healing isn't needed and DoT is up, spam Malefic for damage.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_7.q3",
                ConceptId = AstConcepts.CoHealerAwareness,
                Scenario = "SGE co-healer uses Kerachole before raidwide.",
                Question = "What should you do?",
                Options = new[] { "Add Asp. Helios", "Trust them, DPS", "Use Neutral Sect", "Prepare heals" },
                CorrectIndex = 1,
                Explanation = "Kerachole provides mitigation and HoT. Trust your co-healer and focus on DPS.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_7.q4",
                ConceptId = AstConcepts.RaiseDecision,
                Scenario = "DPS died. Swiftcast ready. WHM co-healer is casting Glare.",
                Question = "Who should raise?",
                Options = new[] { "Wait for WHM", "You - Swiftcast ready", "Hardcast Ascend", "Wait for SMN/RDM" },
                CorrectIndex = 1,
                Explanation = "Your Swiftcast is ready and WHM is DPSing. Take the raise immediately.",
            },
            new QuizQuestion
            {
                QuestionId = "ast.lesson_7.q5",
                ConceptId = AstConcepts.EsunaUsage,
                Scenario = "Party member has Doom (white bar).",
                Question = "What should you do?",
                Options = new[] { "Heal through it", "Esuna immediately", "Let it timeout", "Can't cleanse" },
                CorrectIndex = 1,
                Explanation = "Doom with white bar is cleansable. If not cleansed, player dies. Esuna is top priority.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// SGE (Asclepius) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class SgeQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "sge.lesson_1.quiz",
        LessonId = "sge.lesson_1",
        Title = "Quiz: Sage Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sge.lesson_1.q1",
                ConceptId = SgeConcepts.KardiaManagement,
                Scenario = "Combat starting. You haven't applied Kardia yet.",
                Question = "Who should receive Kardia?",
                Options = new[] { "Yourself", "The main tank", "Best DPS", "Co-healer" },
                CorrectIndex = 1,
                Explanation = "Kardia heals the target when you deal damage. Apply it to the tank - they take consistent damage and benefit most.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_1.q2",
                ConceptId = SgeConcepts.EmergencyHealing,
                Scenario = "Tank at 20% HP. You have Druochole and Taurochole available.",
                Question = "Which is better for emergency healing?",
                Options = new[] { "Druochole - just heal", "Taurochole - heal + mit", "Both", "Neither - use GCD" },
                CorrectIndex = 1,
                Explanation = "Taurochole heals AND provides 10% mitigation. For emergency healing on tank, the extra mitigation helps prevent further drops.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_1.q3",
                ConceptId = SgeConcepts.KardiaTargetSelection,
                Scenario = "Tank swap happening. New tank will take aggro.",
                Question = "Should you swap Kardia?",
                Options = new[] { "No - it's fine", "Yes - to new tank", "Wait and see", "Put on both tanks" },
                CorrectIndex = 1,
                Explanation = "Kardia should be on whoever is actively tanking. Swap it to the new main tank so your damage heals them.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_1.q4",
                ConceptId = SgeConcepts.KardiaManagement,
                Scenario = "You're spamming Dosis. Tank has Kardia.",
                Question = "What's happening to the tank?",
                Options = new[] { "Nothing", "Passive healing from Kardia", "Damage from Kardia", "Shield from Kardia" },
                CorrectIndex = 1,
                Explanation = "Kardia heals the target every time you deal damage with a GCD. Your Dosis spam = constant tank healing. DPS = healing on SGE.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_1.q5",
                ConceptId = SgeConcepts.EmergencyHealing,
                Scenario = "DPS at 25% HP. Tank is fine. No Addersgall available.",
                Question = "What's your best option?",
                Options = new[] { "Diagnosis", "Druochole", "Kardia swap + Dosis", "Nothing - they're fine" },
                CorrectIndex = 0,
                Explanation = "With no Addersgall, Diagnosis (GCD heal) is your option. Kardia on DPS temporarily could work but is clunky. Diagnosis gets them to safe HP.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "sge.lesson_2.quiz",
        LessonId = "sge.lesson_2",
        Title = "Quiz: Addersgall Economy",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sge.lesson_2.q1",
                ConceptId = SgeConcepts.AddersgallManagement,
                Scenario = "You have 3 Addersgall. A stack regenerates in 2 seconds.",
                Question = "What should you do?",
                Options = new[] { "Hold all stacks", "Spend one immediately", "Wait for regen", "Spend two" },
                CorrectIndex = 1,
                Explanation = "At 3 stacks with regen incoming, you'll overcap and waste a stack. Spend at least one before the regen.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_2.q2",
                ConceptId = SgeConcepts.KeracholeUsage,
                Scenario = "Raidwide coming. Party at full HP. Kerachole available.",
                Question = "Should you use Kerachole?",
                Options = new[] { "No - party is full", "Yes - mitigation + HoT", "Save for after damage", "Use Ixochole instead" },
                CorrectIndex = 1,
                Explanation = "Kerachole provides 10% mitigation AND a HoT. Use it BEFORE the raidwide for mitigation value, then HoT heals the damage.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_2.q3",
                ConceptId = SgeConcepts.TaurocholeUsage,
                Scenario = "Tankbuster incoming. Tank at 80% HP.",
                Question = "What should you apply?",
                Options = new[] { "Druochole after buster", "Taurochole before buster", "Nothing - tank is fine", "Kerachole" },
                CorrectIndex = 1,
                Explanation = "Taurochole provides heal + 10% mitigation. Use BEFORE the buster so mitigation reduces the hit, then heal tops them off.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_2.q4",
                ConceptId = SgeConcepts.IxocholeUsage,
                Scenario = "Party at 50% HP after raidwide. Kerachole on CD.",
                Question = "What should you use?",
                Options = new[] { "E.Prognosis", "Ixochole", "Physis", "All three" },
                CorrectIndex = 1,
                Explanation = "Ixochole is instant party healing for 1 Addersgall. At 50% HP, it brings party to safety quickly. GCD heals are slower.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_2.q5",
                ConceptId = SgeConcepts.DruocholeUsage,
                Scenario = "Tank at 60% HP. You have 1 Addersgall. No major damage coming.",
                Question = "Should you use Druochole?",
                Options = new[] { "Yes - heal the tank", "No - they're fine", "Wait for lower HP", "Use Taurochole" },
                CorrectIndex = 1,
                Explanation = "60% HP with no incoming damage is safe. Your Kardia heals + natural regen handle this. Save the Addersgall for when it's needed.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "sge.lesson_3.quiz",
        LessonId = "sge.lesson_3",
        Title = "Quiz: Kardia Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sge.lesson_3.q1",
                ConceptId = SgeConcepts.SoteriaUsage,
                Scenario = "Tank taking heavy damage. Soteria available.",
                Question = "What does Soteria do?",
                Options = new[] { "Heal the tank", "50% Kardia boost", "Shield the tank", "AoE mitigation" },
                CorrectIndex = 1,
                Explanation = "Soteria boosts Kardia healing by 50% for 15 seconds. Use it during high tank damage phases to increase your passive healing.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_3.q2",
                ConceptId = SgeConcepts.PhilosophiaUsage,
                Scenario = "Heavy party-wide damage phase. Philosophia available.",
                Question = "What's special about Philosophia?",
                Options = new[] { "Party mitigation", "Party-wide Kardia", "Instant party heal", "MP restoration" },
                CorrectIndex = 1,
                Explanation = "Philosophia makes your damage heal the ENTIRE party (like Kardia for everyone) for 20 seconds. Amazing for sustained AoE damage.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_3.q3",
                ConceptId = SgeConcepts.SoteriaUsage,
                Scenario = "Soteria available. Tank at 90% HP taking light damage.",
                Question = "Should you use Soteria?",
                Options = new[] { "Yes - boost healing", "No - save for heavy damage", "Yes - use on cooldown", "No - tank is fine" },
                CorrectIndex = 1,
                Explanation = "Soteria is best during heavy tank damage. At 90% with light damage, your base Kardia is enough. Save Soteria for when you need the boost.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_3.q4",
                ConceptId = SgeConcepts.PhilosophiaUsage,
                Scenario = "Boss dies in 10 seconds. Philosophia still available.",
                Question = "Should you use it?",
                Options = new[] { "No - boss dying", "Yes - helps with final damage", "Save for next pull", "Doesn't matter" },
                CorrectIndex = 1,
                Explanation = "If there's any party damage in the final 10 seconds, Philosophia provides value. Don't hold cooldowns when the fight is ending.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_3.q5",
                ConceptId = SgeConcepts.SoteriaUsage,
                Scenario = "Soteria is active. You need to stop DPSing to heal with GCDs.",
                Question = "How does this affect Kardia?",
                Options = new[] { "Kardia still heals", "Kardia stops healing", "Reduced Kardia healing", "Soteria extends" },
                CorrectIndex = 1,
                Explanation = "Kardia only heals when you deal damage. If you stop DPSing to use GCD heals, Kardia (and Soteria's boost) stops working.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "sge.lesson_4.quiz",
        LessonId = "sge.lesson_4",
        Title = "Quiz: Eukrasia System",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sge.lesson_4.q1",
                ConceptId = SgeConcepts.EukrasiaDecisions,
                Scenario = "You want to apply a shield to the tank.",
                Question = "What's the correct sequence?",
                Options = new[] { "Diagnosis", "Eukrasia + Diagnosis", "E.Diagnosis directly", "Shield + Diagnosis" },
                CorrectIndex = 1,
                Explanation = "Eukrasia modifies your next Diagnosis into E.Diagnosis (shield). Press Eukrasia first, then Diagnosis.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_4.q2",
                ConceptId = SgeConcepts.EukrasianDosisUsage,
                Scenario = "E.Dosis (DoT) has 5 seconds remaining on boss.",
                Question = "Should you refresh?",
                Options = new[] { "Yes - keep DoT up", "No - wait until 3s", "Yes - always refresh early", "Let it fall off" },
                CorrectIndex = 1,
                Explanation = "Refresh DoTs at 3 seconds or less. At 5 seconds, refreshing wastes ticks. Wait a bit longer.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_4.q3",
                ConceptId = SgeConcepts.EukrasianPrognosisUsage,
                Scenario = "Raidwide in 3 seconds. Party at full HP.",
                Question = "What should you do?",
                Options = new[] { "Nothing - they're full", "E.Prognosis for party shield", "Kerachole only", "Both E.Prog and Kerachole" },
                CorrectIndex = 1,
                Explanation = "E.Prognosis gives party-wide shields. Before raidwide, shields reduce incoming damage. Apply proactively.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_4.q4",
                ConceptId = SgeConcepts.EukrasianDiagnosisUsage,
                Scenario = "Tankbuster coming. Tank already has a shield from E.Diagnosis.",
                Question = "Can you apply another E.Diagnosis shield?",
                Options = new[] { "Yes - shields stack", "No - only refreshes if larger", "Yes - always refreshes", "No - shield blocks it" },
                CorrectIndex = 1,
                Explanation = "Shields from the same source don't stack. A new E.Diagnosis only refreshes if the new shield would be larger.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_4.q5",
                ConceptId = SgeConcepts.EukrasiaDecisions,
                Scenario = "You need to move and can only fit one instant GCD.",
                Question = "What's the best choice?",
                Options = new[] { "Dosis (damage)", "Toxikon (instant)", "E.Dosis (DoT)", "Phlegma (instant)" },
                CorrectIndex = 3,
                Explanation = "Phlegma is instant and strong damage. During movement when you can only fit one GCD, it's ideal. Save it for these moments.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "sge.lesson_5.quiz",
        LessonId = "sge.lesson_5",
        Title = "Quiz: oGCD Healing Toolkit",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sge.lesson_5.q1",
                ConceptId = SgeConcepts.PhysisUsage,
                Scenario = "Raidwide just hit. Party at 60%. Physis available.",
                Question = "Should you use Physis?",
                Options = new[] { "No - save for worse", "Yes - HoT + healing boost", "No - 60% is fine", "Use Ixochole instead" },
                CorrectIndex = 1,
                Explanation = "Physis provides party HoT AND boosts healing received. At 60%, it recovers the party efficiently. Use it.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_5.q2",
                ConceptId = SgeConcepts.HolosUsage,
                Scenario = "Heavy raidwide coming. Party at full HP.",
                Question = "How should you use Holos?",
                Options = new[] { "After the raidwide", "Before - shield + heal", "Save for emergency", "Not useful here" },
                CorrectIndex = 1,
                Explanation = "Holos provides party shield (mitigation) AND heal. Use BEFORE raidwide for the shield value, then the heal tops off remaining damage.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_5.q3",
                ConceptId = SgeConcepts.PneumaUsage,
                Scenario = "Party needs healing. Boss is targetable. Pneuma available.",
                Question = "What's special about Pneuma?",
                Options = new[] { "Pure healing", "Damage + party heal", "Shield only", "Single target" },
                CorrectIndex = 1,
                Explanation = "Pneuma deals line AoE damage AND heals the party. It's both DPS and healing - use it when the boss is targetable and party needs HP.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_5.q4",
                ConceptId = SgeConcepts.KrasisUsage,
                Scenario = "Tank about to take heavy damage. Krasis available.",
                Question = "What does Krasis do?",
                Options = new[] { "Direct heal", "20% healing boost on target", "Shield", "Mitigation" },
                CorrectIndex = 1,
                Explanation = "Krasis increases healing received by 20% for the target. Apply it before healing the tank for boosted effectiveness.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_5.q5",
                ConceptId = SgeConcepts.PhysisUsage,
                Scenario = "Physis is ready. Light damage, party at 85% HP.",
                Question = "Should you use Physis?",
                Options = new[] { "Yes - use on cooldown", "No - save for heavier damage", "Yes - 60s CD", "Depends on next mechanic" },
                CorrectIndex = 3,
                Explanation = "If heavy damage is coming soon, save Physis for that. If nothing major is coming, using it now for the HoT is fine since it's 60s CD.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "sge.lesson_6.quiz",
        LessonId = "sge.lesson_6",
        Title = "Quiz: Defensive Cooldowns",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sge.lesson_6.q1",
                ConceptId = SgeConcepts.HaimaUsage,
                Scenario = "Tankbuster with multi-hit (3 hits rapidly). Haima available.",
                Question = "Is Haima good here?",
                Options = new[] { "No - single hit only", "Yes - shields refresh", "Save for party", "Use Panhaima instead" },
                CorrectIndex = 1,
                Explanation = "Haima's shields refresh when broken (up to 5 times). Multi-hit busters are PERFECT for Haima - each hit triggers a new shield.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_6.q2",
                ConceptId = SgeConcepts.PanhaimaUsage,
                Scenario = "Multiple raidwides over 15 seconds. Panhaima available.",
                Question = "When to use Panhaima?",
                Options = new[] { "After first hit", "Before the sequence", "Save for tank", "Between hits" },
                CorrectIndex = 1,
                Explanation = "Panhaima puts stacking shields on the party (refresh on break). Use BEFORE a multi-hit sequence to maximize shield value.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_6.q3",
                ConceptId = SgeConcepts.PepsisUsage,
                Scenario = "Party has E.Prognosis shields. Unexpected damage requires healing.",
                Question = "What does Pepsis do?",
                Options = new[] { "Adds more shields", "Converts shields to heal", "Removes shields", "Nothing useful" },
                CorrectIndex = 1,
                Explanation = "Pepsis instantly converts your existing shields into healing. If shields would expire unused, Pepsis gets value from them.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_6.q4",
                ConceptId = SgeConcepts.ZoeUsage,
                Scenario = "Tank critically low. You need a big heal. Zoe available.",
                Question = "How do you use Zoe?",
                Options = new[] { "Use alone", "Zoe then Diagnosis", "Zoe then Druochole", "Zoe then E.Diagnosis" },
                CorrectIndex = 1,
                Explanation = "Zoe boosts your next GCD heal by 50%. Use Zoe then Diagnosis for a massive single-target heal. Druochole is oGCD (not boosted).",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_6.q5",
                ConceptId = SgeConcepts.RhizomataUsage,
                Scenario = "You have 0 Addersgall. Rhizomata available.",
                Question = "What does Rhizomata provide?",
                Options = new[] { "Instant heal", "1 Addersgall stack", "Party shield", "MP restoration" },
                CorrectIndex = 1,
                Explanation = "Rhizomata grants 1 Addersgall stack on a 90s cooldown. Use it when you need Addersgall abilities but are out of stacks.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "sge.lesson_7.quiz",
        LessonId = "sge.lesson_7",
        Title = "Quiz: DPS & Coordination",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sge.lesson_7.q1",
                ConceptId = SgeConcepts.DpsOptimization,
                Scenario = "Party healthy. E.Dosis up. Kardia on tank.",
                Question = "What should you be doing?",
                Options = new[] { "Pre-shield party", "Dosis spam", "Prepare heals", "Apply more HoTs" },
                CorrectIndex = 1,
                Explanation = "Dosis spam = damage + Kardia healing. When party is healthy and DoT is up, maximize Dosis uptime. SGE heals through DPS.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_7.q2",
                ConceptId = SgeConcepts.PhlegmaUsage,
                Scenario = "Burst window starting. You have 2 Phlegma charges.",
                Question = "How should you use Phlegma?",
                Options = new[] { "Save for movement", "Use both in burst", "Space them out", "One now, one later" },
                CorrectIndex = 1,
                Explanation = "Phlegma is strong instant damage. Use charges during burst windows (under raid buffs) for maximum value. Save for movement only if burst is far off.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_7.q3",
                ConceptId = SgeConcepts.CoHealerAwareness,
                Scenario = "Paired with WHM. They use Temperance before raidwide.",
                Question = "What should you do?",
                Options = new[] { "Add Kerachole", "Trust them, DPS", "Use Panhaima", "Stack mitigations" },
                CorrectIndex = 1,
                Explanation = "WHM's Temperance provides party mitigation. Stacking more mitigation has diminishing returns. Trust co-healer, focus on DPS.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_7.q4",
                ConceptId = SgeConcepts.ToxikonUsage,
                Scenario = "You have 2 Addersting (from broken E.Diagnosis shields). Need to move.",
                Question = "What should you use?",
                Options = new[] { "Dosis", "Toxikon", "Phlegma", "Nothing" },
                CorrectIndex = 1,
                Explanation = "Toxikon is instant damage using Addersting. Perfect for movement. You earned those stacks from shields - use them.",
            },
            new QuizQuestion
            {
                QuestionId = "sge.lesson_7.q5",
                ConceptId = SgeConcepts.RaiseDecision,
                Scenario = "DPS died. Swiftcast ready. AST co-healer casting Malefic.",
                Question = "Who should raise?",
                Options = new[] { "Wait for AST", "You - Swiftcast ready", "Hardcast Egeiro", "Wait for SMN" },
                CorrectIndex = 1,
                Explanation = "Your Swiftcast is ready and AST is DPSing. Take the raise - get the DPS back immediately.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// PLD (Themis) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class PldQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "pld.lesson_1.quiz",
        LessonId = "pld.lesson_1",
        Title = "Quiz: Oath Gauge & Stance",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "pld.lesson_1.q1",
                ConceptId = PldConcepts.OathGauge,
                Scenario = "You're at 90 Oath Gauge. A tankbuster is coming in 5 seconds.",
                Question = "What should you do with your Oath?",
                Options = new[] { "Wait until 100", "Use Sheltron now", "Save for emergency", "Use Intervention on co-tank" },
                CorrectIndex = 1,
                Explanation = "At 90 Oath with a tankbuster coming, use Sheltron now. It provides mitigation for the buster and prevents overcapping at 100.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_1.q2",
                ConceptId = PldConcepts.Sheltron,
                Scenario = "You need to mitigate damage for a party member who's about to take a targeted hit.",
                Question = "Which ability should you use?",
                Options = new[] { "Sheltron", "Cover", "Intervention", "Divine Veil" },
                CorrectIndex = 2,
                Explanation = "Intervention spends Oath to give Sheltron's mitigation to another party member. Cover takes damage for them, which is riskier.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_1.q3",
                ConceptId = PldConcepts.OathGauge,
                Scenario = "You're off-tanking and haven't been hit in a while. Your Oath is at 20.",
                Question = "Why isn't your Oath building?",
                Options = new[] { "Iron Will is off", "Not using combos", "Haven't auto-attacked", "Oath builds slowly" },
                CorrectIndex = 2,
                Explanation = "Oath Gauge builds from auto-attacks, not combos. If you're not in melee range auto-attacking, you won't generate Oath.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_1.q4",
                ConceptId = PldConcepts.Sheltron,
                Scenario = "Holy Sheltron just expired. Tank autos are hitting hard.",
                Question = "What does Holy Sheltron provide besides the block?",
                Options = new[] { "Damage boost", "HoT and mitigation", "MP restore", "Enmity boost" },
                CorrectIndex = 1,
                Explanation = "Holy Sheltron provides a heal-over-time (Knight's Resolve) and mitigation, making it excellent for sustained damage.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_1.q5",
                ConceptId = PldConcepts.OathGauge,
                Scenario = "Combat just started. Your Oath is at 0.",
                Question = "What's the fastest way to build Oath?",
                Options = new[] { "Use Riot Blade", "Auto-attack the boss", "Use Holy Spirit", "Wait for regen" },
                CorrectIndex = 1,
                Explanation = "Oath only builds from auto-attacks in combat. Stay in melee range and let auto-attacks generate gauge.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "pld.lesson_2.quiz",
        LessonId = "pld.lesson_2",
        Title = "Quiz: Defensive Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "pld.lesson_2.q1",
                ConceptId = PldConcepts.Sentinel,
                Scenario = "Big tankbuster coming. Rampart is on CD. Sentinel is available.",
                Question = "Should you use Sentinel?",
                Options = new[] { "No - save for emergency", "Yes - it's 30% mitigation", "No - use Sheltron only", "Wait for Rampart" },
                CorrectIndex = 1,
                Explanation = "Sentinel provides 30% mitigation - use it for big tankbusters. Don't save it for emergencies that might not come.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_2.q2",
                ConceptId = PldConcepts.MitigationStacking,
                Scenario = "You have Rampart, Sentinel, and Sheltron available. Huge tankbuster incoming.",
                Question = "What's the best mitigation approach?",
                Options = new[] { "Use all three together", "Rampart + Sheltron", "Sentinel + Sheltron", "Sentinel only" },
                CorrectIndex = 2,
                Explanation = "Sentinel (30%) + Sheltron is usually enough. Save Rampart for the next buster. Stacking all three is overkill and wastes CDs.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_2.q3",
                ConceptId = PldConcepts.Bulwark,
                Scenario = "Large trash pull in a dungeon. Many enemies auto-attacking you.",
                Question = "Which cooldown synergizes best with Sheltron here?",
                Options = new[] { "Sentinel", "Bulwark", "Reprisal", "Rampart" },
                CorrectIndex = 1,
                Explanation = "Bulwark increases block rate, which synergizes with Sheltron's guaranteed blocks for sustained auto-attack mitigation.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_2.q4",
                ConceptId = PldConcepts.Sentinel,
                Scenario = "You used Sentinel at the start of the fight. It's now coming off cooldown.",
                Question = "When should you use it again?",
                Options = new[] { "Immediately on CD", "Save for tankbuster", "Wait until low HP", "Alternate with Rampart" },
                CorrectIndex = 1,
                Explanation = "Plan your Sentinel usage around major damage events like tankbusters. Using it proactively is better than reactively.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_2.q5",
                ConceptId = PldConcepts.MitigationStacking,
                Scenario = "Rampart has 5 seconds remaining. Another small hit incoming.",
                Question = "Should you add another cooldown?",
                Options = new[] { "Yes - stack mitigations", "No - Rampart is enough", "Use Reprisal", "Depends on damage" },
                CorrectIndex = 1,
                Explanation = "For small hits, one mitigation is enough. Don't stack cooldowns on minor damage - save them for bigger hits.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "pld.lesson_3.quiz",
        LessonId = "pld.lesson_3",
        Title = "Quiz: Hallowed Ground",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "pld.lesson_3.q1",
                ConceptId = PldConcepts.HallowedGround,
                Scenario = "Boss is casting a massive tankbuster. You're at 30% HP.",
                Question = "Should you use Hallowed Ground?",
                Options = new[] { "No - save it", "Yes - survive the hit", "No - use Sentinel", "Wait until lower" },
                CorrectIndex = 1,
                Explanation = "At 30% with a massive hit coming, Hallowed Ground guarantees survival. It's the best tank invuln - take 0 damage.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_3.q2",
                ConceptId = PldConcepts.InvulnTiming,
                Scenario = "Hallowed Ground is on a 420 second (7 minute) cooldown.",
                Question = "How should this affect your usage?",
                Options = new[] { "Use only once per fight", "Use whenever needed", "Save for enrage", "Use early to get 2 uses" },
                CorrectIndex = 3,
                Explanation = "In longer fights, using Hallowed early means you might get a second use. Plan around the fight timeline.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_3.q3",
                ConceptId = PldConcepts.HallowedGround,
                Scenario = "Huge wall-to-wall pull in a dungeon. Healer is struggling.",
                Question = "Is Hallowed Ground useful here?",
                Options = new[] { "No - boss only", "Yes - take 0 damage", "No - too long CD", "Use Sentinel instead" },
                CorrectIndex = 1,
                Explanation = "Hallowed Ground is excellent for big dungeon pulls - 10 seconds of immunity gives healer time to stabilize.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_3.q4",
                ConceptId = PldConcepts.InvulnTiming,
                Scenario = "Comparing PLD's invuln to other tanks.",
                Question = "What makes Hallowed Ground special?",
                Options = new[] { "Shortest CD", "No healer attention needed", "Longest duration", "Highest potency" },
                CorrectIndex = 1,
                Explanation = "Hallowed Ground makes you fully invulnerable - no HP drop like Superbolide, no healing requirement like Living Dead.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_3.q5",
                ConceptId = PldConcepts.HallowedGround,
                Scenario = "You used Hallowed Ground. 10 seconds of invulnerability.",
                Question = "What should you focus on during this window?",
                Options = new[] { "Stay defensive", "Maximum DPS", "Help healers", "Move away" },
                CorrectIndex = 1,
                Explanation = "During Hallowed, you can't take damage. Use this window for maximum offense - no need for defensive actions.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "pld.lesson_4.quiz",
        LessonId = "pld.lesson_4",
        Title = "Quiz: Fight or Flight",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "pld.lesson_4.q1",
                ConceptId = PldConcepts.FightOrFlight,
                Scenario = "Fight just started. When should you use Fight or Flight?",
                Question = "What's the optimal timing?",
                Options = new[] { "Before first GCD", "After first GCD", "After combo finisher", "Wait for burst" },
                CorrectIndex = 1,
                Explanation = "Use Fight or Flight after your first GCD to maximize the 20s window. Using before the GCD wastes time.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_4.q2",
                ConceptId = PldConcepts.GoringBlade,
                Scenario = "Goring Blade combo finisher available. Fight or Flight is active.",
                Question = "What does Goring Blade apply?",
                Options = new[] { "Shield", "DoT damage", "Buff", "Heal" },
                CorrectIndex = 1,
                Explanation = "Goring Blade applies a powerful DoT. Using it during Fight or Flight means the DoT does 25% more damage.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_4.q3",
                ConceptId = PldConcepts.AtonementChain,
                Scenario = "You just used Royal Authority. 3 Atonement stacks available.",
                Question = "What's the correct Atonement chain order?",
                Options = new[] { "Atonement x3", "Atonement-Supplication-Sepulchre", "Any order", "Sepulchre first" },
                CorrectIndex = 1,
                Explanation = "The chain is Atonement > Supplication > Sepulchre. Each hit in the chain does increasing damage.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_4.q4",
                ConceptId = PldConcepts.BurstWindow,
                Scenario = "Fight or Flight is up. You have Expiacion and Circle of Scorn available.",
                Question = "How should you use these oGCDs?",
                Options = new[] { "Save for after FoF", "Use during FoF window", "Alternate usage", "Doesn't matter" },
                CorrectIndex = 1,
                Explanation = "Use oGCDs during Fight or Flight to benefit from the 25% damage boost. Pack your burst into the window.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_4.q5",
                ConceptId = PldConcepts.FightOrFlight,
                Scenario = "Fight or Flight has 3 seconds remaining. You need to move.",
                Question = "What should you prioritize?",
                Options = new[] { "Keep hitting boss", "Use Holy Spirit", "Move and Shield Lob", "Just move" },
                CorrectIndex = 1,
                Explanation = "Holy Spirit is instant during Requiescat, but even without, it maintains DPS while moving during the FoF window.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "pld.lesson_5.quiz",
        LessonId = "pld.lesson_5",
        Title = "Quiz: Magic Phase",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "pld.lesson_5.q1",
                ConceptId = PldConcepts.Requiescat,
                Scenario = "Requiescat just came off cooldown. You're mid-physical combo.",
                Question = "When should you use Requiescat?",
                Options = new[] { "Immediately", "After combo finishes", "During movement", "With Fight or Flight" },
                CorrectIndex = 1,
                Explanation = "Finish your physical combo first, then use Requiescat to transition to magic phase. Don't interrupt combos.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_5.q2",
                ConceptId = PldConcepts.HolySpirit,
                Scenario = "Requiescat is active. You need to cast Holy Spirit.",
                Question = "What's the cast time during Requiescat?",
                Options = new[] { "2.5 seconds", "1.5 seconds", "Instant", "No change" },
                CorrectIndex = 2,
                Explanation = "Requiescat makes Holy Spirit and Holy Circle instant cast. This is why magic phase is great for movement.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_5.q3",
                ConceptId = PldConcepts.Confiteor,
                Scenario = "You have 4 Requiescat stacks. What combo should you start?",
                Question = "What's the correct sequence?",
                Options = new[] { "4x Holy Spirit", "Confiteor combo", "Mix of both", "3x Holy Spirit + Confiteor" },
                CorrectIndex = 1,
                Explanation = "The Confiteor combo (Confiteor > Faith > Truth > Valor) is your strongest magic burst. Use it to consume stacks.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_5.q4",
                ConceptId = PldConcepts.BladeCombo,
                Scenario = "You're in the middle of Blade of Faith. Boss becomes untargetable.",
                Question = "What happens to your combo?",
                Options = new[] { "Combo breaks", "Combo continues", "Stacks are lost", "Must restart" },
                CorrectIndex = 1,
                Explanation = "The Blade combo is a true combo chain - it continues even through brief untargetable phases. Don't panic.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_5.q5",
                ConceptId = PldConcepts.MagicPhase,
                Scenario = "Lots of movement required for upcoming mechanic.",
                Question = "Which phase handles movement better?",
                Options = new[] { "Physical phase", "Magic phase (Requiescat)", "Both equally", "Neither - stand still" },
                CorrectIndex = 1,
                Explanation = "Magic phase with Requiescat gives instant casts. Save magic phase for high-movement mechanics when possible.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "pld.lesson_6.quiz",
        LessonId = "pld.lesson_6",
        Title = "Quiz: Party Protection",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "pld.lesson_6.q1",
                ConceptId = PldConcepts.DivineVeil,
                Scenario = "Divine Veil is active on you. Party-wide damage incoming.",
                Question = "What triggers Divine Veil's party shield?",
                Options = new[] { "Taking damage", "Receiving a heal", "Time expiring", "Using a GCD" },
                CorrectIndex = 1,
                Explanation = "Divine Veil activates when you receive healing. Coordinate with healers to ensure the shield deploys before damage.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_6.q2",
                ConceptId = PldConcepts.Cover,
                Scenario = "A party member is about to take a lethal hit. Cover is available.",
                Question = "What does Cover do?",
                Options = new[] { "Reduces their damage", "You take their damage", "Shields them", "Heals them" },
                CorrectIndex = 1,
                Explanation = "Cover makes you take all physical damage for the target. Useful but dangerous - you need to survive the hit.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_6.q3",
                ConceptId = PldConcepts.PassageOfArms,
                Scenario = "Party stacking behind you for a raidwide. Passage of Arms available.",
                Question = "How does Passage of Arms work?",
                Options = new[] { "Instant mitigation", "Channeled, blocks attacks", "Shield on allies", "Damage reduction zone" },
                CorrectIndex = 3,
                Explanation = "Passage of Arms creates a cone behind you that reduces damage. It's channeled but the buff persists briefly after canceling.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_6.q4",
                ConceptId = PldConcepts.Clemency,
                Scenario = "Healer is dead. Tank is at 30% HP.",
                Question = "When is Clemency appropriate?",
                Options = new[] { "Never use it", "Emergency only like this", "Use freely", "Only in dungeons" },
                CorrectIndex = 1,
                Explanation = "Clemency is a DPS loss (GCD cost + MP). Use it only in emergencies when healers can't help.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_6.q5",
                ConceptId = PldConcepts.PartyProtection,
                Scenario = "You used Divine Veil but it expired without activating.",
                Question = "What went wrong?",
                Options = new[] { "Used too early", "Didn't receive healing", "Party moved away", "Wrong ability" },
                CorrectIndex = 1,
                Explanation = "Divine Veil requires you to receive healing to activate. If no heal comes, the ability is wasted.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "pld.lesson_7.quiz",
        LessonId = "pld.lesson_7",
        Title = "Quiz: Advanced Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "pld.lesson_7.q1",
                ConceptId = PldConcepts.TankSwap,
                Scenario = "Tank swap mechanic. You're currently off-tank.",
                Question = "What's the correct swap sequence?",
                Options = new[] { "Just Provoke", "Provoke after buster", "Provoke, then co-tank Shirks", "Wait for healer" },
                CorrectIndex = 2,
                Explanation = "Proper tank swap: you Provoke, then co-tank Shirks to you. This ensures clean aggro transfer without enmity ping-pong.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_7.q2",
                ConceptId = PldConcepts.Expiacion,
                Scenario = "Expiacion is off cooldown. Fight or Flight is coming in 10 seconds.",
                Question = "Should you use Expiacion now?",
                Options = new[] { "Yes - don't hold it", "No - wait for FoF", "Depends on fight length", "Use Circle of Scorn instead" },
                CorrectIndex = 2,
                Explanation = "If FoF is close and you won't lose a use, wait. If holding would cost a use over the fight, use it now.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_7.q3",
                ConceptId = PldConcepts.CircleOfScorn,
                Scenario = "Multiple enemies. Circle of Scorn available.",
                Question = "What makes Circle of Scorn valuable in AoE?",
                Options = new[] { "Higher potency", "DoT on all targets", "Instant cast", "No cooldown" },
                CorrectIndex = 1,
                Explanation = "Circle of Scorn applies a DoT to all enemies hit. In AoE situations, this is significant damage over time.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_7.q4",
                ConceptId = PldConcepts.Intervene,
                Scenario = "Need to close gap to boss. You have 2 Intervene charges.",
                Question = "How should you manage Intervene charges?",
                Options = new[] { "Use both immediately", "Save both for burst", "Use one, save one", "Only for movement" },
                CorrectIndex = 2,
                Explanation = "Keep at least one charge for mobility. Use excess charges during burst windows for damage.",
            },
            new QuizQuestion
            {
                QuestionId = "pld.lesson_7.q5",
                ConceptId = PldConcepts.TankSwap,
                Scenario = "You're main tank. Debuff requires a swap. Co-tank Provokes.",
                Question = "What should you do after?",
                Options = new[] { "Keep attacking", "Shirk to co-tank", "Use defensive CD", "Stop attacking" },
                CorrectIndex = 1,
                Explanation = "After being Provoked off, Shirk to the new main tank. This gives them a comfortable aggro lead.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// WAR (Ares) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class WarQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "war.lesson_1.quiz",
        LessonId = "war.lesson_1",
        Title = "Quiz: Beast Gauge & Tempest",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "war.lesson_1.q1",
                ConceptId = WarConcepts.BeastGauge,
                Scenario = "You're at 90 Beast Gauge. Storm's Path would give 10 more.",
                Question = "What should you do?",
                Options = new[] { "Continue combo normally", "Use Fell Cleave first", "Let gauge cap", "Skip combo finisher" },
                CorrectIndex = 1,
                Explanation = "At 90 gauge, using Storm's Path would overcap. Spend gauge with Fell Cleave first, then continue combo.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_1.q2",
                ConceptId = WarConcepts.SurgingTempest,
                Scenario = "Surging Tempest has 10 seconds remaining.",
                Question = "Should you use Storm's Eye to refresh?",
                Options = new[] { "Yes - keep buff up", "No - wait until lower", "Depends on combo", "Use Storm's Path instead" },
                CorrectIndex = 1,
                Explanation = "Surging Tempest can be extended up to 60s. At 10s remaining, you still have time. Wait until 10s or less to refresh.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_1.q3",
                ConceptId = WarConcepts.BeastGauge,
                Scenario = "Combat starting. Your Beast Gauge is empty.",
                Question = "How do you build gauge?",
                Options = new[] { "Auto-attacks", "Combo finishers", "Any GCD", "oGCDs" },
                CorrectIndex = 1,
                Explanation = "Beast Gauge builds from combo finishers (Storm's Path gives 10, Storm's Eye gives 10). Auto-attacks don't generate gauge.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_1.q4",
                ConceptId = WarConcepts.SurgingTempest,
                Scenario = "Surging Tempest fell off during downtime.",
                Question = "What's the priority when boss returns?",
                Options = new[] { "Use Fell Cleave", "Refresh Surging Tempest", "Use Inner Release", "Normal rotation" },
                CorrectIndex = 1,
                Explanation = "Surging Tempest is a 10% damage buff. Getting it back up takes priority over burst.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_1.q5",
                ConceptId = WarConcepts.BeastGauge,
                Scenario = "You have 50 gauge. Inner Release is in 5 seconds.",
                Question = "Should you spend gauge now?",
                Options = new[] { "Yes - avoid overcap", "No - save for IR", "Spend only 20", "Doesn't matter" },
                CorrectIndex = 1,
                Explanation = "Save gauge for Inner Release. During IR, Fell Cleave is free, but entering with gauge means more damage outside IR.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "war.lesson_2.quiz",
        LessonId = "war.lesson_2",
        Title = "Quiz: Defensive Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "war.lesson_2.q1",
                ConceptId = WarConcepts.Bloodwhetting,
                Scenario = "Large dungeon pull. 8 enemies hitting you.",
                Question = "What makes Bloodwhetting so strong here?",
                Options = new[] { "Flat damage reduction", "Healing per enemy hit", "Shield on use", "Longer duration" },
                CorrectIndex = 1,
                Explanation = "Bloodwhetting heals you for each enemy hit by your attacks. With 8 enemies, your AoEs heal massively.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_2.q2",
                ConceptId = WarConcepts.Vengeance,
                Scenario = "Vengeance is up. Tank autos hurt.",
                Question = "What does Vengeance provide besides mitigation?",
                Options = new[] { "Healing", "Damage reflect", "Enmity boost", "Gauge generation" },
                CorrectIndex = 1,
                Explanation = "Vengeance reflects damage back to attackers when they hit you. Great for trash pulls.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_2.q3",
                ConceptId = WarConcepts.MitigationStacking,
                Scenario = "You used Rampart. Tankbuster incoming.",
                Question = "What pairs well with Rampart?",
                Options = new[] { "Another Rampart", "Bloodwhetting", "Nothing - enough", "Vengeance" },
                CorrectIndex = 1,
                Explanation = "Bloodwhetting adds more mitigation and heals through the hit. Good pairing without wasting bigger CDs.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_2.q4",
                ConceptId = WarConcepts.RawIntuition,
                Scenario = "Bloodwhetting isn't available yet. Raw Intuition is.",
                Question = "What's the difference between them?",
                Options = new[] { "Same ability", "Raw heals less", "Bloodwhetting is AoE", "Different cooldowns" },
                CorrectIndex = 1,
                Explanation = "Raw Intuition is the earlier version with the same mitigation but less healing. Bloodwhetting upgrades it.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_2.q5",
                ConceptId = WarConcepts.Bloodwhetting,
                Scenario = "Single target boss. Bloodwhetting available.",
                Question = "Is Bloodwhetting less valuable in single target?",
                Options = new[] { "Yes - much weaker", "Still strong", "Don't use it", "Only for multi-target" },
                CorrectIndex = 1,
                Explanation = "Bloodwhetting still provides mitigation and healing in single target. Less healing than AoE, but still excellent.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "war.lesson_3.quiz",
        LessonId = "war.lesson_3",
        Title = "Quiz: Holmgang",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "war.lesson_3.q1",
                ConceptId = WarConcepts.Holmgang,
                Scenario = "You're at 50% HP. Huge tankbuster coming.",
                Question = "What happens when you use Holmgang?",
                Options = new[] { "Full immunity", "Can't drop below 1 HP", "50% damage reduction", "Heal to full" },
                CorrectIndex = 1,
                Explanation = "Holmgang prevents you from dropping below 1 HP. You'll take the damage but survive.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_3.q2",
                ConceptId = WarConcepts.InvulnTiming,
                Scenario = "Holmgang has a 240 second cooldown.",
                Question = "How does this compare to other tank invulns?",
                Options = new[] { "Longest CD", "Shortest CD", "Average CD", "Same as PLD" },
                CorrectIndex = 1,
                Explanation = "Holmgang has the shortest invuln cooldown (4 minutes), meaning you can use it more often in fights.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_3.q3",
                ConceptId = WarConcepts.Holmgang,
                Scenario = "Holmgang activated. You're at 1 HP.",
                Question = "What's the best recovery option?",
                Options = new[] { "Wait for healer", "Bloodwhetting + attacks", "Use Equilibrium", "All of the above" },
                CorrectIndex = 3,
                Explanation = "WAR has excellent self-sustain. Bloodwhetting heals through attacks, Equilibrium is a strong self-heal. Use everything.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_3.q4",
                ConceptId = WarConcepts.InvulnTiming,
                Scenario = "Boss has back-to-back tankbusters 3 minutes apart.",
                Question = "Can you Holmgang both?",
                Options = new[] { "No - too short CD", "Yes - 240s CD fits", "Only with help", "Depends on timing" },
                CorrectIndex = 1,
                Explanation = "Holmgang is 240 seconds (4 minutes). For busters 3 minutes apart, you could use it on both with good timing.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_3.q5",
                ConceptId = WarConcepts.Holmgang,
                Scenario = "Holmgang binds you to the target.",
                Question = "What does this binding do?",
                Options = new[] { "Extra damage", "Can't move far away", "Target can't move", "Nothing important" },
                CorrectIndex = 1,
                Explanation = "Holmgang's bind keeps you and the target within range of each other. Usually not an issue, but can affect movement.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "war.lesson_4.quiz",
        LessonId = "war.lesson_4",
        Title = "Quiz: Inner Release",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "war.lesson_4.q1",
                ConceptId = WarConcepts.InnerRelease,
                Scenario = "Inner Release is ready. You have 30 Beast Gauge.",
                Question = "Should you spend gauge before IR?",
                Options = new[] { "Yes - avoid waste", "No - save it", "Spend to 0", "Doesn't matter" },
                CorrectIndex = 1,
                Explanation = "During Inner Release, Fell Cleave costs 0 gauge. Save your gauge for use after IR ends.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_4.q2",
                ConceptId = WarConcepts.FellCleave,
                Scenario = "Inner Release active. 3 free Fell Cleaves available.",
                Question = "How many Fell Cleaves can you fit in the window?",
                Options = new[] { "Only 3", "5 with good timing", "Unlimited", "4 maximum" },
                CorrectIndex = 1,
                Explanation = "Inner Release lasts 15 seconds with 3 free stacks, but you can fit 5 Fell Cleaves with proper GCD timing.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_4.q3",
                ConceptId = WarConcepts.Infuriate,
                Scenario = "Infuriate is ready. You have 0 gauge. IR is on CD.",
                Question = "What does Infuriate provide?",
                Options = new[] { "50 gauge only", "50 gauge + Nascent Chaos", "Free Fell Cleave", "Damage buff" },
                CorrectIndex = 1,
                Explanation = "Infuriate grants 50 Beast Gauge AND the Nascent Chaos buff, which enables Inner Chaos instead of Fell Cleave.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_4.q4",
                ConceptId = WarConcepts.IRWindow,
                Scenario = "Inner Release window. Primal Rend available.",
                Question = "When should you use Primal Rend?",
                Options = new[] { "Immediately", "After Fell Cleaves", "Save for later", "Before IR" },
                CorrectIndex = 1,
                Explanation = "Use Fell Cleaves first during IR (they're free), then use Primal Rend before the buff expires.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_4.q5",
                ConceptId = WarConcepts.InnerRelease,
                Scenario = "Inner Release just ended. You have 80 gauge.",
                Question = "What should you do with that gauge?",
                Options = new[] { "Save for next IR", "Spend on Fell Cleave", "Use Inner Chaos", "Wait" },
                CorrectIndex = 2,
                Explanation = "If you have Nascent Chaos from Infuriate, use Inner Chaos (stronger than Fell Cleave). Otherwise, spend gauge normally.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "war.lesson_5.quiz",
        LessonId = "war.lesson_5",
        Title = "Quiz: Self-Sustain",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "war.lesson_5.q1",
                ConceptId = WarConcepts.ThrillOfBattle,
                Scenario = "Big damage phase starting. Thrill of Battle ready.",
                Question = "What's the best time to use Thrill?",
                Options = new[] { "After damage hits", "Before damage hits", "When low HP", "With Equilibrium" },
                CorrectIndex = 1,
                Explanation = "Thrill of Battle increases max HP by 20%. Using it before damage means you have more HP to absorb the hit.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_5.q2",
                ConceptId = WarConcepts.Equilibrium,
                Scenario = "You're at 40% HP. Equilibrium available.",
                Question = "What does Equilibrium provide?",
                Options = new[] { "Instant heal only", "Instant heal + HoT", "Shield", "Damage buff" },
                CorrectIndex = 1,
                Explanation = "Equilibrium provides a strong instant heal plus a heal-over-time effect. Great for recovery.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_5.q3",
                ConceptId = WarConcepts.NascentChaos,
                Scenario = "Infuriate ready. You want maximum damage.",
                Question = "What should you use Nascent Chaos for?",
                Options = new[] { "Fell Cleave", "Inner Chaos", "Save it", "Chaotic Cyclone" },
                CorrectIndex = 1,
                Explanation = "Nascent Chaos enables Inner Chaos (single target) or Chaotic Cyclone (AoE). Inner Chaos is your strongest single hit.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_5.q4",
                ConceptId = WarConcepts.ThrillOfBattle,
                Scenario = "Thrill of Battle and Equilibrium both available.",
                Question = "Which order maximizes healing?",
                Options = new[] { "Equilibrium first", "Thrill first", "Doesn't matter", "Use together" },
                CorrectIndex = 1,
                Explanation = "Use Thrill first to increase max HP. Equilibrium heals more when your HP pool is larger.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_5.q5",
                ConceptId = WarConcepts.Equilibrium,
                Scenario = "You're main tanking. HP is stable at 80%.",
                Question = "Should you use Equilibrium?",
                Options = new[] { "Yes - prevent drops", "No - save for emergency", "Yes - use on CD", "Depends on incoming damage" },
                CorrectIndex = 3,
                Explanation = "At 80% with no immediate danger, you can save Equilibrium. But if damage is coming, using it proactively is fine.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "war.lesson_6.quiz",
        LessonId = "war.lesson_6",
        Title = "Quiz: Party Protection",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "war.lesson_6.q1",
                ConceptId = WarConcepts.ShakeItOff,
                Scenario = "You have Rampart active. Party raidwide coming.",
                Question = "What happens if you use Shake It Off now?",
                Options = new[] { "Bigger shield", "Normal shield", "Wastes Rampart", "Can't use during Rampart" },
                CorrectIndex = 0,
                Explanation = "Shake It Off consumes personal buffs (Rampart, Vengeance, etc.) to increase the party shield strength.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_6.q2",
                ConceptId = WarConcepts.NascentFlash,
                Scenario = "Bloodwhetting active. DPS is taking damage.",
                Question = "What does Nascent Flash do?",
                Options = new[] { "Heals the DPS", "Shares Bloodwhetting healing", "Shields the DPS", "Transfers aggro" },
                CorrectIndex = 1,
                Explanation = "Nascent Flash shares your Bloodwhetting healing with a party member. Your attacks heal both of you.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_6.q3",
                ConceptId = WarConcepts.PartyProtection,
                Scenario = "Shake It Off is ready. No personal buffs active.",
                Question = "Is Shake It Off still useful?",
                Options = new[] { "No - needs buffs", "Yes - base shield works", "Reduced effect", "Save for buffs" },
                CorrectIndex = 1,
                Explanation = "Shake It Off still provides a party shield without buffs. The buff consumption just makes it stronger.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_6.q4",
                ConceptId = WarConcepts.ShakeItOff,
                Scenario = "Big raidwide in 5 seconds. Vengeance has 3 seconds left.",
                Question = "Should you Shake It Off now?",
                Options = new[] { "Yes - maximize shield", "No - Vengeance still useful", "Wait for more buffs", "Shake without Vengeance" },
                CorrectIndex = 0,
                Explanation = "With Vengeance about to expire anyway, use Shake It Off to convert it into party protection for the raidwide.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_6.q5",
                ConceptId = WarConcepts.NascentFlash,
                Scenario = "Large pull. Healer is struggling. You have Nascent Flash.",
                Question = "Who should receive Nascent Flash?",
                Options = new[] { "The healer", "Yourself", "No one - self heal", "The tank" },
                CorrectIndex = 0,
                Explanation = "Nascent Flash on the healer shares your Bloodwhetting healing, keeping them alive while they struggle to catch up.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "war.lesson_7.quiz",
        LessonId = "war.lesson_7",
        Title = "Quiz: Advanced Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "war.lesson_7.q1",
                ConceptId = WarConcepts.GaugePooling,
                Scenario = "Inner Release in 15 seconds. You have 40 gauge.",
                Question = "What's the optimal gauge management?",
                Options = new[] { "Spend to 0", "Build to 80-90", "Stay at 40", "Use Infuriate now" },
                CorrectIndex = 1,
                Explanation = "Pool gauge to 50-80 before IR. This gives you gauge to spend after IR ends, maximizing damage.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_7.q2",
                ConceptId = WarConcepts.PrimalRend,
                Scenario = "Primal Rend available from Inner Release. Boss jumps away.",
                Question = "How long can you hold Primal Rend?",
                Options = new[] { "Must use immediately", "30 seconds", "Until next IR", "Forever" },
                CorrectIndex = 1,
                Explanation = "Primal Rend can be held for 30 seconds. This allows flexibility for mechanics or raid buff alignment.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_7.q3",
                ConceptId = WarConcepts.Upheaval,
                Scenario = "Upheaval is off cooldown. You're mid-GCD.",
                Question = "When should you use Upheaval?",
                Options = new[] { "Wait for GCD", "Weave it now", "Save for IR", "After combo" },
                CorrectIndex = 1,
                Explanation = "Upheaval is an oGCD. Weave it between GCDs. Don't let it sit off cooldown.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_7.q4",
                ConceptId = WarConcepts.Onslaught,
                Scenario = "3 Onslaught charges available. Inner Release active.",
                Question = "How should you use Onslaught charges?",
                Options = new[] { "Save for movement", "Use all in IR", "Use 1-2, save rest", "Space throughout fight" },
                CorrectIndex = 2,
                Explanation = "Use some Onslaught charges during IR for damage, but keep at least one for emergency gap closing.",
            },
            new QuizQuestion
            {
                QuestionId = "war.lesson_7.q5",
                ConceptId = WarConcepts.TankSwap,
                Scenario = "Tank swap needed. You're off-tank with IR ready.",
                Question = "Should you swap then use IR?",
                Options = new[] { "Yes - more damage", "No - use IR first", "Swap during IR", "Let co-tank keep boss" },
                CorrectIndex = 0,
                Explanation = "Provoke first to grab the boss, then use Inner Release. You want to be hitting the boss during your burst.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// DRK (Nyx) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class DrkQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "drk.lesson_1.quiz",
        LessonId = "drk.lesson_1",
        Title = "Quiz: Blood Gauge & Darkside",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drk.lesson_1.q1",
                ConceptId = DrkConcepts.Darkside,
                Scenario = "Darkside has 5 seconds remaining. You have 6000 MP.",
                Question = "What should you do?",
                Options = new[] { "Let Darkside fall", "Edge of Shadow to refresh", "Wait for damage", "Use Blood Gauge" },
                CorrectIndex = 1,
                Explanation = "Edge of Shadow costs 3000 MP and refreshes Darkside. At 5 seconds, refresh it before the 10% damage buff falls off.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_1.q2",
                ConceptId = DrkConcepts.BloodGauge,
                Scenario = "Blood Gauge at 80. Souleater combo ready.",
                Question = "What happens with Souleater?",
                Options = new[] { "Gain 20 Blood", "Overcap to 100", "No Blood gain", "Gain 10 Blood" },
                CorrectIndex = 0,
                Explanation = "Souleater gives 20 Blood Gauge. At 80, you'd go to 100 - which is fine, but watch for overcapping.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_1.q3",
                ConceptId = DrkConcepts.DarksideMaintenance,
                Scenario = "Darkside timer at 40 seconds. MP at 9000.",
                Question = "Should you Edge of Shadow?",
                Options = new[] { "Yes - prevent MP overcap", "No - Darkside is fine", "Save for TBN", "Use Flood instead" },
                CorrectIndex = 0,
                Explanation = "At 9000 MP, you risk overcapping (max 10000). Use Edge of Shadow to dump MP and extend Darkside.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_1.q4",
                ConceptId = DrkConcepts.BloodGauge,
                Scenario = "You're at 50 Blood. Bloodspiller or save for Delirium?",
                Question = "Delirium comes off CD in 20 seconds.",
                Options = new[] { "Bloodspiller now", "Save all Blood", "Spend down to 30", "Wait for 100" },
                CorrectIndex = 0,
                Explanation = "With 20 seconds until Delirium, you have time to spend and regenerate. Use Bloodspiller to avoid overcapping.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_1.q5",
                ConceptId = DrkConcepts.Darkside,
                Scenario = "Combat just started. Darkside isn't active.",
                Question = "How do you activate Darkside?",
                Options = new[] { "Toggle on manually", "Use Edge of Shadow", "Just attack", "Use Grit" },
                CorrectIndex = 1,
                Explanation = "Darkside activates when you use Edge of Shadow or Flood of Shadow. Use it early to get your damage buff.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "drk.lesson_2.quiz",
        LessonId = "drk.lesson_2",
        Title = "Quiz: Defensive Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drk.lesson_2.q1",
                ConceptId = DrkConcepts.DarkMind,
                Scenario = "Magic tankbuster incoming. Dark Mind available.",
                Question = "Is Dark Mind effective here?",
                Options = new[] { "No - use Shadow Wall", "Yes - 20% magic mit", "Partially effective", "Only for AoE" },
                CorrectIndex = 1,
                Explanation = "Dark Mind provides 20% magic damage mitigation. Perfect for magic tankbusters.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_2.q2",
                ConceptId = DrkConcepts.Oblation,
                Scenario = "Oblation has 2 charges. Light damage incoming.",
                Question = "Should you use Oblation?",
                Options = new[] { "No - save for big hits", "Yes - it's 10% mit", "Use both charges", "Only on others" },
                CorrectIndex = 1,
                Explanation = "Oblation is only 10% but has 2 charges and short CD. Use it freely for consistent mitigation.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_2.q3",
                ConceptId = DrkConcepts.ShadowWall,
                Scenario = "Huge tankbuster. Shadow Wall and Rampart both available.",
                Question = "Which should you use?",
                Options = new[] { "Both together", "Shadow Wall only", "Rampart only", "Shadow Wall + Oblation" },
                CorrectIndex = 3,
                Explanation = "Shadow Wall (30%) + Oblation (10%) handles big hits. Save Rampart for the next attack.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_2.q4",
                ConceptId = DrkConcepts.DarkMind,
                Scenario = "Physical tankbuster incoming. Only Dark Mind available.",
                Question = "Should you use Dark Mind?",
                Options = new[] { "Yes - some mitigation", "No - it won't help", "Use anyway", "With Oblation" },
                CorrectIndex = 1,
                Explanation = "Dark Mind ONLY mitigates magic damage. It does nothing against physical attacks.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_2.q5",
                ConceptId = DrkConcepts.MitigationStacking,
                Scenario = "You have Shadow Wall, Oblation, and TBN available.",
                Question = "What's the optimal stacking order?",
                Options = new[] { "All at once", "Shadow Wall + TBN", "Shadow Wall + Oblation", "TBN only" },
                CorrectIndex = 1,
                Explanation = "Shadow Wall (30%) + TBN (25% HP shield) is usually enough. Save Oblation for the next hit.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "drk.lesson_3.quiz",
        LessonId = "drk.lesson_3",
        Title = "Quiz: Living Dead",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drk.lesson_3.q1",
                ConceptId = DrkConcepts.LivingDead,
                Scenario = "You activated Living Dead. You're at 80% HP.",
                Question = "What happens if you take fatal damage?",
                Options = new[] { "You die", "Walking Dead activates", "You heal to full", "Living Dead extends" },
                CorrectIndex = 1,
                Explanation = "If you would die during Living Dead, you become Walking Dead instead - unable to die for 10 seconds.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_3.q2",
                ConceptId = DrkConcepts.WalkingDead,
                Scenario = "You're in Walking Dead state. Timer running.",
                Question = "What must happen before it ends?",
                Options = new[] { "Nothing - you survive", "Be healed to 100%", "Kill something", "Use a cooldown" },
                CorrectIndex = 1,
                Explanation = "Walking Dead requires healing to 100% HP before it ends, or you die when the buff expires.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_3.q3",
                ConceptId = DrkConcepts.WalkingDead,
                Scenario = "Walking Dead active. You have access to AoE attacks.",
                Question = "What helps you survive?",
                Options = new[] { "Stop attacking", "AoE attacks heal you", "Healers only", "Defensive CDs" },
                CorrectIndex = 1,
                Explanation = "During Walking Dead, your attacks heal you. AoE attacks on multiple enemies heal more.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_3.q4",
                ConceptId = DrkConcepts.InvulnTiming,
                Scenario = "Living Dead is a 300 second cooldown.",
                Question = "How should this affect usage?",
                Options = new[] { "Use once only", "Save for emergencies", "Use proactively on big damage", "Never use" },
                CorrectIndex = 2,
                Explanation = "300s (5 min) means you can use it on planned damage events. Coordinate with healers for optimal use.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_3.q5",
                ConceptId = DrkConcepts.LivingDead,
                Scenario = "You use Living Dead. Timer expires without taking lethal damage.",
                Question = "What happens?",
                Options = new[] { "You die anyway", "Nothing - buff ends", "Walking Dead triggers", "CD doesn't reset" },
                CorrectIndex = 1,
                Explanation = "If you don't take lethal damage during Living Dead, it just expires. No Walking Dead, no penalty.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "drk.lesson_4.quiz",
        LessonId = "drk.lesson_4",
        Title = "Quiz: The Blackest Night",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drk.lesson_4.q1",
                ConceptId = DrkConcepts.TheBlackestNight,
                Scenario = "Tankbuster coming. TBN available. You have 5000 MP.",
                Question = "Should you use TBN?",
                Options = new[] { "No - save MP", "Yes - shield will break", "Only if low HP", "Use Oblation instead" },
                CorrectIndex = 1,
                Explanation = "Tankbusters will break TBN's shield, giving you Dark Arts (free Edge of Shadow). It's both defensive AND offensive.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_4.q2",
                ConceptId = DrkConcepts.DarkArts,
                Scenario = "TBN's shield broke completely.",
                Question = "What do you get from Dark Arts?",
                Options = new[] { "MP refund", "Free Edge/Flood of Shadow", "Damage buff", "Heal" },
                CorrectIndex = 1,
                Explanation = "Dark Arts grants a free Edge of Shadow (or Flood). The 3000 MP you spent on TBN is effectively refunded as damage.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_4.q3",
                ConceptId = DrkConcepts.TBNManagement,
                Scenario = "Light auto-attacks only. TBN available.",
                Question = "Should you use TBN?",
                Options = new[] { "Yes - free shield", "No - won't break", "Only at low HP", "Use on cooldown" },
                CorrectIndex = 1,
                Explanation = "If TBN's shield doesn't fully break, you lose the MP and don't get Dark Arts. Save it for bigger damage.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_4.q4",
                ConceptId = DrkConcepts.TheBlackestNight,
                Scenario = "Party member about to take big damage. Your TBN available.",
                Question = "Can you TBN them?",
                Options = new[] { "No - self only", "Yes - works on anyone", "Only tanks", "Only in combat" },
                CorrectIndex = 1,
                Explanation = "TBN can be placed on any party member. If THEIR shield breaks, YOU get Dark Arts.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_4.q5",
                ConceptId = DrkConcepts.DarkArts,
                Scenario = "You have Dark Arts. Edge of Shadow normally costs 3000 MP.",
                Question = "How much MP does Dark Arts Edge cost?",
                Options = new[] { "3000 MP", "1500 MP", "0 MP", "Costs Blood" },
                CorrectIndex = 2,
                Explanation = "Dark Arts makes your next Edge of Shadow or Flood of Shadow completely free. Pure value.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "drk.lesson_5.quiz",
        LessonId = "drk.lesson_5",
        Title = "Quiz: Blood Weapon & Delirium",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drk.lesson_5.q1",
                ConceptId = DrkConcepts.BloodWeapon,
                Scenario = "Blood Weapon ready. You're about to start your combo.",
                Question = "What does Blood Weapon provide?",
                Options = new[] { "Damage buff", "MP + Blood on attacks", "Free Bloodspillers", "Crit boost" },
                CorrectIndex = 1,
                Explanation = "Blood Weapon makes your attacks restore MP and Blood Gauge for 15 seconds. Start it before your burst.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_5.q2",
                ConceptId = DrkConcepts.Delirium,
                Scenario = "Delirium activated. You have 3 free Bloodspiller charges.",
                Question = "What's special about Delirium Bloodspillers?",
                Options = new[] { "Higher potency", "Free + guaranteed crit", "AoE damage", "Heals you" },
                CorrectIndex = 1,
                Explanation = "Delirium's Bloodspillers cost no Blood Gauge and are guaranteed critical hits. Maximum burst.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_5.q3",
                ConceptId = DrkConcepts.Bloodspiller,
                Scenario = "You have 50 Blood. Delirium not available.",
                Question = "Should you use Bloodspiller?",
                Options = new[] { "No - save for Delirium", "Yes - spend Blood", "Wait for 100", "Use Quietus" },
                CorrectIndex = 1,
                Explanation = "Don't overcap Blood. Use Bloodspiller to spend gauge when Delirium is on CD.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_5.q4",
                ConceptId = DrkConcepts.BloodWeapon,
                Scenario = "Blood Weapon has 3 seconds left. Mid-combo.",
                Question = "How many GCDs should you fit?",
                Options = new[] { "1-2", "3", "5", "As many as possible" },
                CorrectIndex = 2,
                Explanation = "Blood Weapon lasts 15 seconds. Fit 5 GCDs to maximize MP and Blood generation.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_5.q5",
                ConceptId = DrkConcepts.EdgeOfShadow,
                Scenario = "Blood Weapon active. MP at 9000.",
                Question = "Should you Edge of Shadow?",
                Options = new[] { "No - Blood Weapon gives MP", "Yes - prevent overcap", "Wait until 10000", "Use Flood instead" },
                CorrectIndex = 1,
                Explanation = "Blood Weapon generates MP. At 9000, you'll overcap. Dump MP with Edge of Shadow.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "drk.lesson_6.quiz",
        LessonId = "drk.lesson_6",
        Title = "Quiz: Party Protection",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drk.lesson_6.q1",
                ConceptId = DrkConcepts.DarkMissionary,
                Scenario = "Magic raidwide incoming. Dark Missionary available.",
                Question = "What does Dark Missionary provide?",
                Options = new[] { "Full mitigation", "10% magic mitigation to party", "Shield", "Heal" },
                CorrectIndex = 1,
                Explanation = "Dark Missionary gives 10% magic damage mitigation to the entire party for 15 seconds.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_6.q2",
                ConceptId = DrkConcepts.LivingShadow,
                Scenario = "Living Shadow ready. Burst window starting.",
                Question = "When should you summon Living Shadow?",
                Options = new[] { "Save for later", "During burst window", "After burst", "Only for AoE" },
                CorrectIndex = 1,
                Explanation = "Living Shadow attacks for 24 seconds and does massive damage. Summon during burst to maximize raid buff value.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_6.q3",
                ConceptId = DrkConcepts.DarkMissionary,
                Scenario = "Physical raidwide coming. Dark Missionary available.",
                Question = "Should you use Dark Missionary?",
                Options = new[] { "Yes - any mitigation helps", "No - magic only", "Partial effect", "Use Reprisal instead" },
                CorrectIndex = 1,
                Explanation = "Dark Missionary only mitigates magic damage. For physical raidwides, use Reprisal instead.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_6.q4",
                ConceptId = DrkConcepts.LivingShadow,
                Scenario = "You summoned Living Shadow. Boss becomes untargetable.",
                Question = "What happens to Living Shadow?",
                Options = new[] { "Disappears", "Waits and continues", "Wastes time", "Attacks anyway" },
                CorrectIndex = 1,
                Explanation = "Living Shadow persists through untargetable phases and continues attacking when the boss returns.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_6.q5",
                ConceptId = DrkConcepts.PartyProtection,
                Scenario = "TBN on party member. Dark Missionary ready. Big hit coming.",
                Question = "Can you stack these protections?",
                Options = new[] { "No - one at a time", "Yes - they stack", "Only on yourself", "TBN replaces Mit" },
                CorrectIndex = 1,
                Explanation = "TBN (shield) and Dark Missionary (mitigation) stack. TBN absorbs damage reduced by the mitigation.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "drk.lesson_7.quiz",
        LessonId = "drk.lesson_7",
        Title = "Quiz: Advanced Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drk.lesson_7.q1",
                ConceptId = DrkConcepts.CarveAndSpit,
                Scenario = "Carve and Spit off cooldown. MP at 6000.",
                Question = "What does Carve and Spit provide besides damage?",
                Options = new[] { "Blood Gauge", "MP restoration", "Shield", "Heal" },
                CorrectIndex = 1,
                Explanation = "Carve and Spit restores MP in addition to dealing damage. Use it to help maintain MP levels.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_7.q2",
                ConceptId = DrkConcepts.SaltedEarth,
                Scenario = "Salted Earth ready. Boss standing still.",
                Question = "Where should you place Salted Earth?",
                Options = new[] { "Under yourself", "Under the boss", "Doesn't matter", "Behind boss" },
                CorrectIndex = 1,
                Explanation = "Salted Earth is a ground DoT zone. Place it under the boss so it deals damage the entire duration.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_7.q3",
                ConceptId = DrkConcepts.Shadowbringer,
                Scenario = "Shadowbringer has 2 charges. Burst window active.",
                Question = "How should you use the charges?",
                Options = new[] { "Space them out", "Both during burst", "Save one", "Use with Blood Weapon" },
                CorrectIndex = 1,
                Explanation = "Shadowbringer charges should be used during burst windows to maximize damage under raid buffs.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_7.q4",
                ConceptId = DrkConcepts.TankSwap,
                Scenario = "Tank swap required. You need to take aggro.",
                Question = "What should you do after Provoking?",
                Options = new[] { "Nothing", "Wait for Shirk", "TBN yourself", "Use Living Shadow" },
                CorrectIndex = 2,
                Explanation = "After Provoking, you're now main tank. TBN yourself for the incoming damage you'll take.",
            },
            new QuizQuestion
            {
                QuestionId = "drk.lesson_7.q5",
                ConceptId = DrkConcepts.Disesteem,
                Scenario = "Salted Earth is active. Salt and Darkness available.",
                Question = "What combo can you do?",
                Options = new[] { "Nothing special", "Salt and Darkness for burst", "Salted Earth again", "Only during Delirium" },
                CorrectIndex = 1,
                Explanation = "While Salted Earth is active, Salt and Darkness becomes available for additional burst damage.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// GNB (Hephaestus) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class GnbQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "gnb.lesson_1.quiz",
        LessonId = "gnb.lesson_1",
        Title = "Quiz: Cartridge Gauge",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_1.q1",
                ConceptId = GnbConcepts.CartridgeGauge,
                Scenario = "You're at 2 cartridges. Solid Barrel combo ready.",
                Question = "What happens after Solid Barrel?",
                Options = new[] { "Gain 1 cartridge", "Overcap warning", "Go to 3 cartridges", "No change" },
                CorrectIndex = 2,
                Explanation = "Solid Barrel generates 1 cartridge. At 2, you'll go to 3 (max). Don't Solid Barrel at 3 cartridges.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_1.q2",
                ConceptId = GnbConcepts.BurstStrike,
                Scenario = "You have 1 cartridge. No Mercy isn't available.",
                Question = "Should you use Burst Strike?",
                Options = new[] { "Yes - spend cartridge", "No - save for No Mercy", "Only if overcapping", "Use Gnashing Fang" },
                CorrectIndex = 2,
                Explanation = "Generally save cartridges for No Mercy windows. Use Burst Strike only to prevent overcapping.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_1.q3",
                ConceptId = GnbConcepts.CartridgeGauge,
                Scenario = "Combat starting. Cartridge gauge empty.",
                Question = "How do you build cartridges?",
                Options = new[] { "Auto-attacks", "Any GCD", "Solid Barrel combo finisher", "oGCDs" },
                CorrectIndex = 2,
                Explanation = "Cartridges come from Solid Barrel (1) and Bloodfest (3). Only combo finisher generates gauge.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_1.q4",
                ConceptId = GnbConcepts.BurstStrike,
                Scenario = "Burst Strike used. Hypervelocity is now available.",
                Question = "What is Hypervelocity?",
                Options = new[] { "Next combo action", "Free oGCD damage", "Defensive ability", "Movement skill" },
                CorrectIndex = 1,
                Explanation = "Burst Strike grants Hypervelocity (Ready to Blast), a free oGCD follow-up attack.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_1.q5",
                ConceptId = GnbConcepts.CartridgeGauge,
                Scenario = "3 cartridges. Gnashing Fang costs 1, Double Down costs 2.",
                Question = "Which should you use first in burst?",
                Options = new[] { "Gnashing Fang", "Double Down", "Burst Strike", "Save them" },
                CorrectIndex = 1,
                Explanation = "Double Down is your highest damage cartridge spender. Use it early in No Mercy for maximum value.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "gnb.lesson_2.quiz",
        LessonId = "gnb.lesson_2",
        Title = "Quiz: Defensive Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_2.q1",
                ConceptId = GnbConcepts.HeartOfCorundum,
                Scenario = "Tankbuster incoming. Heart of Corundum available.",
                Question = "What does Heart of Corundum provide?",
                Options = new[] { "Shield only", "Mitigation + heal + shield", "Just mitigation", "Heal over time" },
                CorrectIndex = 1,
                Explanation = "Heart of Corundum provides 15% mitigation, a heal when damaged, and a shield. Extremely powerful.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_2.q2",
                ConceptId = GnbConcepts.Aurora,
                Scenario = "Tank autos hurting. Aurora has 2 charges.",
                Question = "What does Aurora provide?",
                Options = new[] { "Mitigation", "Heal over time", "Shield", "Damage buff" },
                CorrectIndex = 1,
                Explanation = "Aurora is a HoT (heal over time). Use it freely with 2 charges to smooth out damage.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_2.q3",
                ConceptId = GnbConcepts.Nebula,
                Scenario = "Big tankbuster. Nebula and Heart of Corundum available.",
                Question = "What's the best combination?",
                Options = new[] { "Nebula only", "Heart of Corundum only", "Both together", "Camouflage instead" },
                CorrectIndex = 2,
                Explanation = "For big tankbusters, stack Nebula (30%) with Heart of Corundum (15% + heal + shield).",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_2.q4",
                ConceptId = GnbConcepts.Camouflage,
                Scenario = "Sustained auto-attacks. Camouflage available.",
                Question = "What makes Camouflage good for autos?",
                Options = new[] { "Highest mitigation", "Increased parry rate", "Longest duration", "Heals on hit" },
                CorrectIndex = 1,
                Explanation = "Camouflage boosts parry rate, which is particularly effective against frequent auto-attacks.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_2.q5",
                ConceptId = GnbConcepts.MitigationStacking,
                Scenario = "You used Rampart. Another hit coming soon.",
                Question = "What pairs well with active Rampart?",
                Options = new[] { "Nebula", "Heart of Corundum", "More Rampart", "Nothing needed" },
                CorrectIndex = 1,
                Explanation = "Heart of Corundum (25s CD) pairs well with Rampart (90s CD). Save Nebula for when Rampart is down.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "gnb.lesson_3.quiz",
        LessonId = "gnb.lesson_3",
        Title = "Quiz: Superbolide",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_3.q1",
                ConceptId = GnbConcepts.Superbolide,
                Scenario = "You're at 80% HP. Superbolide available.",
                Question = "What happens when you use Superbolide?",
                Options = new[] { "Stay at 80%", "Drop to 1 HP, then invuln", "Take 50% HP", "Invuln without HP drop" },
                CorrectIndex = 1,
                Explanation = "Superbolide drops you to 1 HP immediately, then makes you invulnerable for 10 seconds.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_3.q2",
                ConceptId = GnbConcepts.InvulnTiming,
                Scenario = "Superbolide has 360 second cooldown (6 minutes).",
                Question = "How does this compare to other tank invulns?",
                Options = new[] { "Longest CD", "Second shortest", "Average", "Shortest" },
                CorrectIndex = 1,
                Explanation = "Superbolide (360s) is second shortest after Holmgang (240s). Can be used fairly often.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_3.q3",
                ConceptId = GnbConcepts.Superbolide,
                Scenario = "You're at 20% HP. Lethal hit coming.",
                Question = "Is Superbolide worth using here?",
                Options = new[] { "No - already low", "Yes - survive the hit", "Use defensives instead", "Only if healers can't help" },
                CorrectIndex = 1,
                Explanation = "At 20% with a lethal hit coming, Superbolide saves you. You drop to 1 HP but don't die.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_3.q4",
                ConceptId = GnbConcepts.InvulnTiming,
                Scenario = "Superbolide used. You're at 1 HP with 10s invuln.",
                Question = "What should healers do?",
                Options = new[] { "Panic heal immediately", "Wait then heal", "Nothing - you're invuln", "Heal to stabilize" },
                CorrectIndex = 3,
                Explanation = "You're invuln for 10s but at 1 HP. Healers should heal you during the invuln window so you're stable when it ends.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_3.q5",
                ConceptId = GnbConcepts.Superbolide,
                Scenario = "Big pull in dungeon. About to die.",
                Question = "Is Superbolide good here?",
                Options = new[] { "No - boss only", "Yes - 10s of safety", "Use other CDs first", "Only in raids" },
                CorrectIndex = 1,
                Explanation = "Superbolide works great in dungeons for big pulls. 10 seconds of invulnerability lets you and healer stabilize.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "gnb.lesson_4.quiz",
        LessonId = "gnb.lesson_4",
        Title = "Quiz: No Mercy Window",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_4.q1",
                ConceptId = GnbConcepts.NoMercy,
                Scenario = "No Mercy coming off CD. You have 1 cartridge.",
                Question = "What should you do before No Mercy?",
                Options = new[] { "Use No Mercy now", "Build to 2+ cartridges", "Use the 1 cartridge", "Wait for Bloodfest" },
                CorrectIndex = 1,
                Explanation = "Enter No Mercy with 2+ cartridges so you can use Double Down (costs 2). Build gauge first.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_4.q2",
                ConceptId = GnbConcepts.DoubleDown,
                Scenario = "No Mercy active. 3 cartridges available.",
                Question = "What should you use first?",
                Options = new[] { "Gnashing Fang", "Double Down", "Burst Strike x3", "Sonic Break" },
                CorrectIndex = 1,
                Explanation = "Double Down is your highest potency attack. Use it early in No Mercy to guarantee it lands in the window.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_4.q3",
                ConceptId = GnbConcepts.Bloodfest,
                Scenario = "Bloodfest is ready. You have 0 cartridges.",
                Question = "What does Bloodfest provide?",
                Options = new[] { "1 cartridge", "2 cartridges", "3 cartridges", "Max cartridges" },
                CorrectIndex = 2,
                Explanation = "Bloodfest instantly grants 3 cartridges. Use it during No Mercy when you've spent your cartridges.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_4.q4",
                ConceptId = GnbConcepts.NoMercyWindow,
                Scenario = "No Mercy has 5 seconds left. Gnashing Fang available.",
                Question = "Should you start Gnashing Fang?",
                Options = new[] { "No - not enough time", "Yes - fits in window", "Save for next NM", "Only the first hit" },
                CorrectIndex = 1,
                Explanation = "Gnashing Fang combo is fast. 5 seconds is enough time to complete it within No Mercy.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_4.q5",
                ConceptId = GnbConcepts.NoMercy,
                Scenario = "No Mercy provides 20% damage boost for 20 seconds.",
                Question = "What should you pack into this window?",
                Options = new[] { "Only cartridge moves", "Everything possible", "Save some for later", "Defensive abilities" },
                CorrectIndex = 1,
                Explanation = "Pack all your big damage into No Mercy: Double Down, Gnashing Fang combo, Sonic Break, oGCDs.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "gnb.lesson_5.quiz",
        LessonId = "gnb.lesson_5",
        Title = "Quiz: Gnashing Fang Combo",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_5.q1",
                ConceptId = GnbConcepts.GnashingFang,
                Scenario = "Gnashing Fang ready. You have 1 cartridge.",
                Question = "What does Gnashing Fang start?",
                Options = new[] { "A single attack", "A 3-hit combo chain", "An AoE attack", "A defensive move" },
                CorrectIndex = 1,
                Explanation = "Gnashing Fang starts a 3-hit combo: Gnashing Fang > Savage Claw > Wicked Talon, each with a Continuation follow-up.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_5.q2",
                ConceptId = GnbConcepts.Continuation,
                Scenario = "You just used Gnashing Fang. Jugular Rip is available.",
                Question = "What is Jugular Rip?",
                Options = new[] { "Next combo GCD", "Free oGCD follow-up", "Alternative finisher", "Defensive ability" },
                CorrectIndex = 1,
                Explanation = "Jugular Rip is the Continuation oGCD after Gnashing Fang. Weave it before your next GCD.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_5.q3",
                ConceptId = GnbConcepts.ContinuationChain,
                Scenario = "Full Gnashing Fang combo with Continuations.",
                Question = "What's the complete sequence?",
                Options = new[] { "GF > SC > WT", "GF > JR > SC > AT > WT > EG", "GF > JR > JR > JR", "Any order works" },
                CorrectIndex = 1,
                Explanation = "Gnashing Fang > Jugular Rip > Savage Claw > Abdomen Tear > Wicked Talon > Eye Gouge. GCD > oGCD pattern.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_5.q4",
                ConceptId = GnbConcepts.Continuation,
                Scenario = "You used Savage Claw but forgot Abdomen Tear.",
                Question = "Can you still use Abdomen Tear?",
                Options = new[] { "Yes - anytime", "No - window passed", "Only after Wicked Talon", "Reset the combo" },
                CorrectIndex = 1,
                Explanation = "Each Continuation must be used before the next GCD. Miss it and it's gone. Practice the rhythm.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_5.q5",
                ConceptId = GnbConcepts.GnashingFang,
                Scenario = "Gnashing Fang is on 30 second cooldown.",
                Question = "How often can you do this combo?",
                Options = new[] { "Once per minute", "Twice per minute", "Every No Mercy", "Once per fight" },
                CorrectIndex = 1,
                Explanation = "30 second cooldown means you can do Gnashing Fang combo twice per minute, aligning with No Mercy windows.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "gnb.lesson_6.quiz",
        LessonId = "gnb.lesson_6",
        Title = "Quiz: Party Protection",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_6.q1",
                ConceptId = GnbConcepts.HeartOfLight,
                Scenario = "Magic raidwide incoming. Heart of Light available.",
                Question = "What does Heart of Light provide?",
                Options = new[] { "Physical mitigation", "10% magic mitigation to party", "Shield", "Heal" },
                CorrectIndex = 1,
                Explanation = "Heart of Light provides 10% magic damage mitigation to the entire party for 15 seconds.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_6.q2",
                ConceptId = GnbConcepts.HeartOfCorundum,
                Scenario = "Party member about to take big damage.",
                Question = "Can you use Heart of Corundum on them?",
                Options = new[] { "No - self only", "Yes - works on anyone", "Only tanks", "Only in dungeons" },
                CorrectIndex = 1,
                Explanation = "Heart of Corundum can target any party member, giving them the mitigation, heal, and shield.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_6.q3",
                ConceptId = GnbConcepts.HeartOfLight,
                Scenario = "Physical raidwide coming. Heart of Light available.",
                Question = "Should you use Heart of Light?",
                Options = new[] { "Yes - any mitigation helps", "No - magic only", "Partial effect", "Use Aurora instead" },
                CorrectIndex = 1,
                Explanation = "Heart of Light only mitigates magic damage. For physical raidwides, use Reprisal instead.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_6.q4",
                ConceptId = GnbConcepts.Aurora,
                Scenario = "Healer taking damage. Aurora available.",
                Question = "Can you Aurora the healer?",
                Options = new[] { "No - self only", "Yes - it's a targeted HoT", "Only in combat", "Only if they're low" },
                CorrectIndex = 1,
                Explanation = "Aurora can be placed on any party member. Use it on struggling healers or DPS taking damage.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_6.q5",
                ConceptId = GnbConcepts.PartyProtection,
                Scenario = "Co-tank about to take a tankbuster. Your Heart of Corundum ready.",
                Question = "Should you use it on them?",
                Options = new[] { "No - they have their own", "Yes - help them survive", "Only if they ask", "Save for yourself" },
                CorrectIndex = 1,
                Explanation = "Heart of Corundum on the co-tank stacks with their own mitigation, ensuring survival on big hits.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "gnb.lesson_7.quiz",
        LessonId = "gnb.lesson_7",
        Title = "Quiz: Advanced Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_7.q1",
                ConceptId = GnbConcepts.SonicBreak,
                Scenario = "Sonic Break ready. No Mercy active.",
                Question = "Why use Sonic Break in No Mercy?",
                Options = new[] { "It's instant", "DoT benefits from buff", "Highest potency", "Grants cartridge" },
                CorrectIndex = 1,
                Explanation = "Sonic Break applies a DoT. Using it during No Mercy means the DoT ticks also get the 20% damage buff.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_7.q2",
                ConceptId = GnbConcepts.BowShock,
                Scenario = "Multiple enemies. Bow Shock available.",
                Question = "What makes Bow Shock good for AoE?",
                Options = new[] { "Higher base damage", "DoT on all targets", "Generates cartridges", "No cooldown" },
                CorrectIndex = 1,
                Explanation = "Bow Shock applies a DoT to all targets hit. In AoE, this is significant damage over time.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_7.q3",
                ConceptId = GnbConcepts.BlastingZone,
                Scenario = "Blasting Zone off cooldown. No burst window.",
                Question = "Should you use Blasting Zone?",
                Options = new[] { "Save for No Mercy", "Use on cooldown", "Only in AoE", "Only at low HP" },
                CorrectIndex = 1,
                Explanation = "Blasting Zone is a 30 second CD oGCD. Use it on cooldown - don't lose uses waiting for burst.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_7.q4",
                ConceptId = GnbConcepts.ReignOfBeasts,
                Scenario = "No Mercy grants access to Reign of Beasts.",
                Question = "What is Reign of Beasts?",
                Options = new[] { "A defensive cooldown", "A 3-hit combo chain", "AoE attack", "Gap closer" },
                CorrectIndex = 1,
                Explanation = "Reign of Beasts is a new 3-hit combo granted during No Mercy. Another burst combo to fit in the window.",
            },
            new QuizQuestion
            {
                QuestionId = "gnb.lesson_7.q5",
                ConceptId = GnbConcepts.TankSwap,
                Scenario = "Tank swap needed. You have No Mercy coming up.",
                Question = "What's the optimal order?",
                Options = new[] { "Swap then burst", "Burst then swap", "Swap during burst", "Let co-tank keep aggro" },
                CorrectIndex = 0,
                Explanation = "Provoke first to get the boss, then use No Mercy. You want to be actively attacking during your burst.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// DRG (Zeus) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class DrgQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "drg.lesson_1.quiz",
        LessonId = "drg.lesson_1",
        Title = "Quiz: Dragoon Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drg.lesson_1.q1",
                ConceptId = DrgConcepts.ComboBasics,
                Scenario = "You just started a fight. Your first GCD should be True Thrust.",
                Question = "Which combo path grants Power Surge?",
                Options = new[] { "True Thrust → Vorpal Thrust", "True Thrust → Disembowel", "Any combo path", "Full Thrust combo" },
                CorrectIndex = 1,
                Explanation = "Disembowel grants Power Surge (+10% damage). Always start with this combo to get the buff up first.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_1.q2",
                ConceptId = DrgConcepts.PowerSurge,
                Scenario = "Power Surge has 8 seconds remaining. You just finished Full Thrust combo.",
                Question = "What should your next combo be?",
                Options = new[] { "Full Thrust combo again", "Chaos Thrust combo", "Doesn't matter", "Use oGCDs only" },
                CorrectIndex = 1,
                Explanation = "With 8 seconds left, use Chaos Thrust combo to refresh Power Surge before it falls off. Never let this buff drop.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_1.q3",
                ConceptId = DrgConcepts.Positionals,
                Scenario = "You need to use Chaos Thrust. Where should you stand?",
                Question = "What's the correct position for Chaos Thrust?",
                Options = new[] { "Flank (side)", "Rear (behind)", "Front", "Any position" },
                CorrectIndex = 1,
                Explanation = "Chaos Thrust requires rear positional for bonus damage. Full Thrust requires flank. Missing = 100 potency loss.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_1.q4",
                ConceptId = DrgConcepts.ComboBasics,
                Scenario = "Your standard rotation alternates combos.",
                Question = "What's the basic single-target pattern?",
                Options = new[] { "Spam Full Thrust combo", "Alternate Chaos/Full combos", "Random order", "Only Chaos Thrust" },
                CorrectIndex = 1,
                Explanation = "Alternate: Chaos Thrust combo → Full Thrust combo → Chaos Thrust combo. This maintains both the DoT and Power Surge.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_1.q5",
                ConceptId = DrgConcepts.PowerSurge,
                Scenario = "Power Surge fell off mid-fight. Party is in burst phase.",
                Question = "What's your priority?",
                Options = new[] { "Continue Full Thrust combo", "Get Power Surge back immediately", "Use jumps for damage", "Wait for buff window" },
                CorrectIndex = 1,
                Explanation = "Power Surge is +10% to ALL damage. Even during burst, losing this buff is worse than delaying other actions. Get it back ASAP.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "drg.lesson_2.quiz",
        LessonId = "drg.lesson_2",
        Title = "Quiz: Jump Management",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drg.lesson_2.q1",
                ConceptId = DrgConcepts.HighJump,
                Scenario = "High Jump is off cooldown. You just used a GCD.",
                Question = "When should you use High Jump?",
                Options = new[] { "Before next GCD", "After next GCD", "During GCD window (weave)", "Save for burst" },
                CorrectIndex = 2,
                Explanation = "Weave High Jump during the GCD window (after your GCD, before the next is ready). This prevents clipping.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_2.q2",
                ConceptId = DrgConcepts.MirageDive,
                Scenario = "You just used High Jump. You have Dive Ready buff.",
                Question = "What should you do with Dive Ready?",
                Options = new[] { "Save it for later", "Use Mirage Dive soon", "It expires on its own", "Wait for Low Jump" },
                CorrectIndex = 1,
                Explanation = "Dive Ready enables Mirage Dive, which builds Eye gauge. Use it before your next High Jump or it's wasted.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_2.q3",
                ConceptId = DrgConcepts.AnimationLock,
                Scenario = "Boss has an AoE indicator under you. High Jump is ready.",
                Question = "Should you use High Jump now?",
                Options = new[] { "Yes - jumps have iframes", "Yes - you return to position", "No - animation lock is dangerous", "Depends on the AoE" },
                CorrectIndex = 2,
                Explanation = "Jumps have ~0.8s animation lock where you can't move or act. Using during AoE means you'll get hit. Move first.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_2.q4",
                ConceptId = DrgConcepts.HighJump,
                Scenario = "You used High Jump. Where will you end up?",
                Question = "What happens to your position?",
                Options = new[] { "Stay at boss", "Return to original spot", "Random location", "Behind boss" },
                CorrectIndex = 1,
                Explanation = "High Jump returns you to your original position. You don't fly away - this makes it safe for positional maintenance.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_2.q5",
                ConceptId = DrgConcepts.AnimationLock,
                Scenario = "You need to weave two oGCDs. High Jump + another ability.",
                Question = "Why is double-weaving with High Jump risky?",
                Options = new[] { "It's not risky", "High Jump is slow", "Animation lock causes clipping", "Loses DPS" },
                CorrectIndex = 2,
                Explanation = "High Jump's animation lock is longer than most oGCDs. Double-weaving with it often causes GCD clip. Single-weave is safer.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "drg.lesson_3.quiz",
        LessonId = "drg.lesson_3",
        Title = "Quiz: Eye Gauge & Geirskogul",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drg.lesson_3.q1",
                ConceptId = DrgConcepts.EyeGauge,
                Scenario = "You have 1 Eye. Mirage Dive is available.",
                Question = "What happens when you use Mirage Dive?",
                Options = new[] { "Lose the Eye", "Gain second Eye", "Enter Life of Dragon", "Nothing special" },
                CorrectIndex = 1,
                Explanation = "Mirage Dive grants 1 Eye (max 2). You need 2 Eyes before using Geirskogul to enter Life of the Dragon.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_3.q2",
                ConceptId = DrgConcepts.Geirskogul,
                Scenario = "You have 2 Eyes. Geirskogul is ready.",
                Question = "What happens when you use Geirskogul at 2 Eyes?",
                Options = new[] { "Just deals damage", "Enters Life of Dragon", "Consumes both Eyes", "Grants more Eyes" },
                CorrectIndex = 1,
                Explanation = "At 2 Eyes, Geirskogul consumes 1 Eye and transforms you into Life of the Dragon for 20 seconds.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_3.q3",
                ConceptId = DrgConcepts.LifeOfDragon,
                Scenario = "You have 1 Eye. Geirskogul is ready.",
                Question = "Should you use Geirskogul now?",
                Options = new[] { "Yes - on cooldown", "No - wait for 2 Eyes", "Only in burst", "Doesn't matter" },
                CorrectIndex = 1,
                Explanation = "Using Geirskogul at 1 Eye just deals damage without entering Life of the Dragon. Build to 2 Eyes first.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_3.q4",
                ConceptId = DrgConcepts.EyeGauge,
                Scenario = "You have 2 Eyes. Mirage Dive is available but Geirskogul is on CD.",
                Question = "Should you use Mirage Dive?",
                Options = new[] { "Yes - use it anyway", "No - Eyes would be wasted", "Save for later", "Use then Geirskogul" },
                CorrectIndex = 1,
                Explanation = "At 2 Eyes, you're capped. Using Mirage Dive wastes an Eye. Wait for Geirskogul to consume an Eye first.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_3.q5",
                ConceptId = DrgConcepts.Geirskogul,
                Scenario = "You're about to enter a burst window. You have 0 Eyes.",
                Question = "What's your Eye-building priority?",
                Options = new[] { "Skip Eyes, burst now", "Build 2 Eyes for Life phase", "Build 1 Eye only", "Eyes don't matter" },
                CorrectIndex = 1,
                Explanation = "Life of the Dragon (from 2 Eyes) unlocks Nastrond and Stardiver. Plan Eye building so you enter Life during burst.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "drg.lesson_4.quiz",
        LessonId = "drg.lesson_4",
        Title = "Quiz: Life of the Dragon",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drg.lesson_4.q1",
                ConceptId = DrgConcepts.Nastrond,
                Scenario = "You're in Life of the Dragon. 18 seconds remaining.",
                Question = "What does Geirskogul become during Life?",
                Options = new[] { "Stays Geirskogul", "Becomes Nastrond", "Becomes Stardiver", "Disappears" },
                CorrectIndex = 1,
                Explanation = "During Life of the Dragon, Geirskogul transforms into Nastrond - same button, no Eye cost, powerful line AoE.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_4.q2",
                ConceptId = DrgConcepts.Stardiver,
                Scenario = "Life of the Dragon active. Stardiver ready. 5 seconds on Life timer.",
                Question = "Should you use Stardiver?",
                Options = new[] { "No - save for next Life", "Yes - use before Life ends", "No - Nastrond is better", "Wait for buff window" },
                CorrectIndex = 1,
                Explanation = "Stardiver can only be used during Life of the Dragon. With 5s left, use it now or lose the opportunity.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_4.q3",
                ConceptId = DrgConcepts.Nastrond,
                Scenario = "Life of the Dragon lasts 20 seconds. Nastrond shares Geirskogul's cooldown.",
                Question = "How many Nastronds can you fit in one Life window?",
                Options = new[] { "1-2", "3-4", "5-6", "Unlimited" },
                CorrectIndex = 1,
                Explanation = "With 20s Life duration and Nastrond's ~10s CD, you can fit 3-4 Nastronds per Life phase.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_4.q4",
                ConceptId = DrgConcepts.LifeOptimization,
                Scenario = "Lance Charge is active. You have 2 Eyes and Geirskogul ready.",
                Question = "Is this a good time to enter Life?",
                Options = new[] { "No - save Life for later", "Yes - buffs affect Life abilities", "Doesn't matter", "Only during Battle Litany" },
                CorrectIndex = 1,
                Explanation = "Entering Life during Lance Charge means your Nastronds and Stardiver get the +10% damage buff. Optimal timing.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_4.q5",
                ConceptId = DrgConcepts.Stardiver,
                Scenario = "You used Stardiver. A new button appeared.",
                Question = "What is Starcross?",
                Options = new[] { "Separate cooldown", "Follow-up to Stardiver", "AoE version", "Defensive ability" },
                CorrectIndex = 1,
                Explanation = "Starcross is a free follow-up that appears after Stardiver. Use it immediately for extra damage.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "drg.lesson_5.quiz",
        LessonId = "drg.lesson_5",
        Title = "Quiz: Burst Window Setup",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drg.lesson_5.q1",
                ConceptId = DrgConcepts.LanceCharge,
                Scenario = "Fight just started. Lance Charge is ready.",
                Question = "When should you use Lance Charge?",
                Options = new[] { "Before first GCD", "After opener starts", "Wait for 2 minutes", "Save for emergencies" },
                CorrectIndex = 1,
                Explanation = "Use Lance Charge early in your opener to buff as many GCDs and oGCDs as possible. It's on 60s CD - don't hold it.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_5.q2",
                ConceptId = DrgConcepts.BattleLitany,
                Scenario = "Battle Litany is ready. Other DPS are using their 2-minute buffs.",
                Question = "What does Battle Litany provide?",
                Options = new[] { "+10% damage to you", "+10% crit to party", "+10% speed", "Damage reduction" },
                CorrectIndex = 1,
                Explanation = "Battle Litany grants +10% crit rate to the entire party. Coordinate it with other 2-minute raid buffs for maximum impact.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_5.q3",
                ConceptId = DrgConcepts.BurstWindow,
                Scenario = "Lance Charge is active. Battle Litany is also active. You have 2 Eyes.",
                Question = "What should you prioritize?",
                Options = new[] { "Save Eyes for later", "Enter Life of Dragon", "Only use GCDs", "Wait for cooldowns" },
                CorrectIndex = 1,
                Explanation = "Double buff window is the perfect time for Life of the Dragon. Your Nastronds and Stardiver will hit much harder.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_5.q4",
                ConceptId = DrgConcepts.BuffAlignment,
                Scenario = "Lance Charge (60s CD) and Battle Litany (120s CD) are both ready.",
                Question = "How should you handle the different cooldowns?",
                Options = new[] { "Always use together", "Lance Charge on CD, Litany at 2m", "Hold Lance for Litany", "Alternate them" },
                CorrectIndex = 1,
                Explanation = "Use Lance Charge on cooldown (60s cycle). Battle Litany aligns with 2-minute raid buffs. They sync every other Lance Charge.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_5.q5",
                ConceptId = DrgConcepts.LanceCharge,
                Scenario = "Lance Charge has 5 seconds remaining. Boss is about to jump away.",
                Question = "What should you prioritize?",
                Options = new[] { "Save abilities for after", "Use all damage during buff", "Just auto-attack", "Stop attacking" },
                CorrectIndex = 1,
                Explanation = "Squeeze out every buffed action before Lance Charge expires or the boss leaves. Jumps, Nastrond, whatever's available.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "drg.lesson_6.quiz",
        LessonId = "drg.lesson_6",
        Title = "Quiz: Life Surge & Critical Hits",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drg.lesson_6.q1",
                ConceptId = DrgConcepts.LifeSurge,
                Scenario = "Life Surge has 2 charges. You're about to use Heavens' Thrust.",
                Question = "Should you use Life Surge?",
                Options = new[] { "No - save charges", "Yes - Heavens' Thrust is ideal", "Only during buffs", "Use on any GCD" },
                CorrectIndex = 1,
                Explanation = "Life Surge guarantees a critical hit. Use it on Heavens' Thrust (highest potency GCD) for maximum value.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_6.q2",
                ConceptId = DrgConcepts.LifeSurge,
                Scenario = "Life Surge is at 2 charges. No combo finisher coming soon.",
                Question = "What should you do?",
                Options = new[] { "Wait for finisher", "Use on any GCD to avoid cap", "Save both charges", "Use on DoT only" },
                CorrectIndex = 1,
                Explanation = "At 2 charges, you're capped and losing potential charges. Better to use on a weaker GCD than waste the cooldown.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_6.q3",
                ConceptId = DrgConcepts.TrueNorthUsage,
                Scenario = "Boss is against the wall. You can't reach the rear for Chaos Thrust.",
                Question = "What should you use?",
                Options = new[] { "Skip Chaos Thrust", "Use True North", "Just miss the positional", "Move the boss" },
                CorrectIndex = 1,
                Explanation = "True North removes positional requirements for 10s. Use it when you physically can't reach the correct position.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_6.q4",
                ConceptId = DrgConcepts.PositionalRecovery,
                Scenario = "True North has 2 charges. You're at flank but need rear.",
                Question = "Should you use True North?",
                Options = new[] { "Yes - positionals matter", "No - save for mechanics", "Only if both charges", "Walk to rear instead" },
                CorrectIndex = 3,
                Explanation = "If you can reach rear in time, walk there and save True North. Only use it when repositioning isn't possible.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_6.q5",
                ConceptId = DrgConcepts.BuffAlignment,
                Scenario = "Life Surge ready. Battle Litany is active (+10% crit).",
                Question = "Is Life Surge's guaranteed crit wasted during Litany?",
                Options = new[] { "Yes - crit is capped", "No - crit damage still applies", "Partially wasted", "Never use together" },
                CorrectIndex = 1,
                Explanation = "Life Surge guarantees crit, and Battle Litany increases crit rate. Together they still maximize damage - crit damage isn't diminished.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "drg.lesson_7.quiz",
        LessonId = "drg.lesson_7",
        Title = "Quiz: Advanced Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "drg.lesson_7.q1",
                ConceptId = DrgConcepts.WyrmwindThrust,
                Scenario = "You have 2 Firstminds Focus. Boss is far away.",
                Question = "What can you use at range?",
                Options = new[] { "Nothing - melee only", "Wyrmwind Thrust", "Piercing Talon only", "Wait for boss" },
                CorrectIndex = 1,
                Explanation = "Wyrmwind Thrust is ranged and uses Firstminds Focus. It's your disengage tool - use it when you can't reach the boss.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_7.q2",
                ConceptId = DrgConcepts.FirstmindsFocus,
                Scenario = "You just used Heavens' Thrust. Firstminds Focus increased.",
                Question = "How do you build Firstminds Focus?",
                Options = new[] { "Any combo finisher", "Heavens'/Raiden Thrust only", "All GCDs", "oGCDs only" },
                CorrectIndex = 1,
                Explanation = "Firstminds Focus builds from Raiden Thrust and Heavens' Thrust (the upgraded combo finishers). 2 Focus = Wyrmwind Thrust.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_7.q3",
                ConceptId = DrgConcepts.DotMaintenance,
                Scenario = "Chaotic Spring DoT has 5 seconds remaining. You're in the middle of Full Thrust combo.",
                Question = "What should you do?",
                Options = new[] { "Finish current combo", "Interrupt for Chaos combo", "Refresh immediately", "Let DoT drop" },
                CorrectIndex = 0,
                Explanation = "5 seconds is enough time to finish your current combo, then start Chaos Thrust combo. Breaking combo loses more than a few DoT ticks.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_7.q4",
                ConceptId = DrgConcepts.AoeRotation,
                Scenario = "3 enemies. Single-target combo is in progress.",
                Question = "What's the AoE rotation?",
                Options = new[] { "Continue single-target", "Doom Spike → Sonic Thrust → Coerthan", "Just use Dragonfire Dive", "AoE is DPS loss" },
                CorrectIndex = 1,
                Explanation = "At 3+ targets, use the AoE combo: Doom Spike → Sonic Thrust → Coerthan Torment. It's higher total damage.",
            },
            new QuizQuestion
            {
                QuestionId = "drg.lesson_7.q5",
                ConceptId = DrgConcepts.DragonfireDive,
                Scenario = "Multiple enemies grouped. Dragonfire Dive and Stardiver both available.",
                Question = "What makes Dragonfire Dive good for AoE?",
                Options = new[] { "Higher single-target", "It's AoE damage", "Grants buffs", "No animation lock" },
                CorrectIndex = 1,
                Explanation = "Dragonfire Dive deals AoE damage on landing. In multi-target situations, it's more valuable than in single-target.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// NIN (Hermes) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class NinQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "nin.lesson_1.quiz",
        LessonId = "nin.lesson_1",
        Title = "Quiz: Ninja Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "nin.lesson_1.q1",
                ConceptId = NinConcepts.ComboBasics,
                Scenario = "You just started a fight. Your first GCD should be Spinning Edge.",
                Question = "What is the basic Ninja combo sequence?",
                Options = new[] { "Spinning Edge → Aeolian Edge", "Spinning Edge → Gust Slash → finisher", "Any order works", "Armor Crush → Aeolian Edge" },
                CorrectIndex = 1,
                Explanation = "The basic combo is Spinning Edge → Gust Slash → finisher (Aeolian Edge or Armor Crush). Always complete full combos for maximum potency.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_1.q2",
                ConceptId = NinConcepts.Positionals,
                Scenario = "You need to use Aeolian Edge. Where should you stand?",
                Question = "What's the correct position for Aeolian Edge?",
                Options = new[] { "Flank (side)", "Rear (behind)", "Front", "Any position" },
                CorrectIndex = 1,
                Explanation = "Aeolian Edge requires rear positional for bonus damage. Armor Crush requires flank. Missing positionals loses significant potency.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_1.q3",
                ConceptId = NinConcepts.Kazematoi,
                Scenario = "You have 0 Kazematoi stacks and need burst damage soon.",
                Question = "How do you build Kazematoi stacks?",
                Options = new[] { "Use Aeolian Edge", "Use Armor Crush", "Use Ninjutsu", "Use Ninki spenders" },
                CorrectIndex = 1,
                Explanation = "Armor Crush (flank positional finisher) grants Kazematoi stacks. Aeolian Edge consumes them for enhanced damage.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_1.q4",
                ConceptId = NinConcepts.Positionals,
                Scenario = "You're at flank and need to use Aeolian Edge.",
                Question = "What happens if you use Aeolian Edge from flank instead of rear?",
                Options = new[] { "Same damage", "Lose potency bonus", "Combo breaks", "It won't execute" },
                CorrectIndex = 1,
                Explanation = "Missing positionals loses the bonus potency. The attack still works, but you deal less damage.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_1.q5",
                ConceptId = NinConcepts.ComboBasics,
                Scenario = "You used Spinning Edge, then got distracted. 15 seconds passed.",
                Question = "What happens to your combo?",
                Options = new[] { "Nothing - continue", "Combo breaks after time", "Damage increases", "Must use oGCD" },
                CorrectIndex = 1,
                Explanation = "Combos time out after ~15 seconds of not continuing. You'll need to restart from Spinning Edge.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "nin.lesson_2.quiz",
        LessonId = "nin.lesson_2",
        Title = "Quiz: Mudra Mastery",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "nin.lesson_2.q1",
                ConceptId = NinConcepts.MudraSystem,
                Scenario = "You want to use Raiton for single-target damage.",
                Question = "What mudra sequence creates Raiton?",
                Options = new[] { "Ten → Chi → Jin", "Ten → Chi (or Chi → Ten)", "Jin → Chi → Ten", "Any two mudras" },
                CorrectIndex = 1,
                Explanation = "Raiton is created by Ten → Chi or Chi → Ten. It's your bread-and-butter damage Ninjutsu.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_2.q2",
                ConceptId = NinConcepts.MudraSystem,
                Scenario = "You need Suiton to enable Kunai's Bane.",
                Question = "What mudra sequence creates Suiton?",
                Options = new[] { "Ten → Chi", "Chi → Jin", "Ten → Chi → Jin", "Jin → Ten" },
                CorrectIndex = 2,
                Explanation = "Suiton requires three mudras: Ten → Chi → Jin. It grants a buff that enables Kunai's Bane.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_2.q3",
                ConceptId = NinConcepts.NinjutsuWeaving,
                Scenario = "You just pressed a GCD. Mudras are ready.",
                Question = "When should you input mudras?",
                Options = new[] { "Before GCD", "During GCD window", "After next GCD", "Anytime" },
                CorrectIndex = 1,
                Explanation = "Mudras are oGCDs - weave them during the GCD window. The resulting Ninjutsu is a GCD itself.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_2.q4",
                ConceptId = NinConcepts.MudraSystem,
                Scenario = "You input the wrong mudra sequence.",
                Question = "What happens when you mess up mudras?",
                Options = new[] { "Nothing happens", "Rabbit Medium appears", "Game crashes", "Damage enemy" },
                CorrectIndex = 1,
                Explanation = "Invalid mudra sequences create Rabbit Medium (a bunny) - no damage, just embarrassment. Try again next time!",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_2.q5",
                ConceptId = NinConcepts.Huton,
                Scenario = "At lower levels, Huton grants a speed buff.",
                Question = "What mudra sequence creates Huton?",
                Options = new[] { "Ten → Chi", "Chi → Ten", "Jin → Chi → Ten", "Ten → Jin" },
                CorrectIndex = 2,
                Explanation = "Huton is Jin → Chi → Ten. At higher levels this buff is passive, but understanding mudras helps overall mastery.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "nin.lesson_3.quiz",
        LessonId = "nin.lesson_3",
        Title = "Quiz: Ninki & Spenders",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "nin.lesson_3.q1",
                ConceptId = NinConcepts.NinkiGauge,
                Scenario = "Your Ninki gauge is at 90. Bhavacakra costs 50.",
                Question = "What should you do?",
                Options = new[] { "Wait for 100", "Use Bhavacakra now", "Save for later", "Use weaponskills only" },
                CorrectIndex = 1,
                Explanation = "At 90 Ninki, you're close to capping. Use Bhavacakra to avoid overcapping and wasting gauge generation.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_3.q2",
                ConceptId = NinConcepts.Bhavacakra,
                Scenario = "Single target fight. You have 50 Ninki.",
                Question = "Which Ninki spender should you use?",
                Options = new[] { "Hellfrog Medium", "Bhavacakra", "Save it", "Either works" },
                CorrectIndex = 1,
                Explanation = "Bhavacakra is the single-target spender. Hellfrog Medium is for AoE (3+ targets).",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_3.q3",
                ConceptId = NinConcepts.NinkiPooling,
                Scenario = "Kunai's Bane is coming up in 10 seconds. You have 40 Ninki.",
                Question = "What's the optimal approach?",
                Options = new[] { "Spend Ninki now", "Pool Ninki for burst", "It doesn't matter", "Stop attacking" },
                CorrectIndex = 1,
                Explanation = "Pool Ninki before burst windows. Spending Bhavacakra during Kunai's Bane buff deals more damage.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_3.q4",
                ConceptId = NinConcepts.NinkiGauge,
                Scenario = "You used Mug/Dokumori on the boss.",
                Question = "How much Ninki does Mug/Dokumori generate?",
                Options = new[] { "20", "40", "50", "0" },
                CorrectIndex = 1,
                Explanation = "Mug/Dokumori generates 40 Ninki in addition to dealing damage. It's a significant gauge boost for your burst.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_3.q5",
                ConceptId = NinConcepts.Bhavacakra,
                Scenario = "You have 100 Ninki during Kunai's Bane window.",
                Question = "How many Bhavacakras can you use?",
                Options = new[] { "1", "2", "3", "Unlimited" },
                CorrectIndex = 1,
                Explanation = "At 100 Ninki, you can use 2 Bhavacakras (50 each). Weave them between GCDs during the buff window.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "nin.lesson_4.quiz",
        LessonId = "nin.lesson_4",
        Title = "Quiz: Burst Window Basics",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "nin.lesson_4.q1",
                ConceptId = NinConcepts.Suiton,
                Scenario = "You want to use Kunai's Bane but don't have Suiton buff.",
                Question = "Can you use Kunai's Bane without Suiton?",
                Options = new[] { "Yes - anytime", "No - Suiton is required", "Only in AoE", "Only on cooldown" },
                CorrectIndex = 1,
                Explanation = "Kunai's Bane requires the Suiton buff to be active. Always set up Suiton before your burst window.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_4.q2",
                ConceptId = NinConcepts.KunaisBane,
                Scenario = "You successfully applied Kunai's Bane to the boss.",
                Question = "What does Kunai's Bane do?",
                Options = new[] { "Direct damage only", "+10% damage debuff on target", "Speed buff", "MP restore" },
                CorrectIndex = 1,
                Explanation = "Kunai's Bane applies a +10% damage taken debuff to the target. All your attacks deal more damage during this window.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_4.q3",
                ConceptId = NinConcepts.MugDokumori,
                Scenario = "Mug/Dokumori is off cooldown. Kunai's Bane window is starting.",
                Question = "When should you use Mug/Dokumori?",
                Options = new[] { "After Kunai's Bane", "Before Kunai's Bane", "Doesn't matter", "Save for next burst" },
                CorrectIndex = 1,
                Explanation = "Use Mug/Dokumori just before Kunai's Bane to maximize damage and generate Ninki for your burst window.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_4.q4",
                ConceptId = NinConcepts.KunaisBane,
                Scenario = "Kunai's Bane has a 60 second cooldown.",
                Question = "How does this align with party buffs?",
                Options = new[] { "Doesn't align", "Every other raid buff", "Every raid buff window", "Random timing" },
                CorrectIndex = 2,
                Explanation = "60s cooldown means Kunai's Bane aligns with standard raid buff windows (60s/120s). Coordinate with your party.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_4.q5",
                ConceptId = NinConcepts.Suiton,
                Scenario = "You used Suiton but the boss became untargetable before Kunai's Bane.",
                Question = "What happens to your Suiton buff?",
                Options = new[] { "Stays forever", "Expires after time", "Transfers to next target", "Immediately lost" },
                CorrectIndex = 1,
                Explanation = "Suiton buff has a duration. If you can't use Kunai's Bane in time, the buff expires and you'll need to reapply.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "nin.lesson_5.quiz",
        LessonId = "nin.lesson_5",
        Title = "Quiz: Advanced Burst: TCJ & Kassatsu",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "nin.lesson_5.q1",
                ConceptId = NinConcepts.Kassatsu,
                Scenario = "Kassatsu is ready. You're about to use Ninjutsu.",
                Question = "What Ninjutsu should you use after Kassatsu?",
                Options = new[] { "Raiton", "Suiton", "Hyosho Ranryu", "Huton" },
                CorrectIndex = 2,
                Explanation = "Kassatsu enables Hyosho Ranryu, an ice Ninjutsu with massive potency. This is your highest damage Ninjutsu.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_5.q2",
                ConceptId = NinConcepts.TenChiJin,
                Scenario = "You activated Ten Chi Jin. Boss starts an AoE.",
                Question = "What happens if you move during TCJ?",
                Options = new[] { "Nothing", "TCJ continues", "TCJ cancels", "Extra damage" },
                CorrectIndex = 2,
                Explanation = "Ten Chi Jin cancels if you move or take damage. Position safely before activating it.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_5.q3",
                ConceptId = NinConcepts.TcjOptimization,
                Scenario = "You're using TCJ for 2-minute burst. What's the standard sequence?",
                Question = "What Ninjutsu sequence during TCJ?",
                Options = new[] { "Raiton only", "Fuma → Raiton → Suiton", "Suiton → Raiton → Fuma", "Three Raitons" },
                CorrectIndex = 1,
                Explanation = "Standard TCJ sequence is Fuma Shuriken → Raiton → Suiton. Suiton last sets up Kunai's Bane.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_5.q4",
                ConceptId = NinConcepts.Kassatsu,
                Scenario = "Kassatsu and Kunai's Bane are both ready.",
                Question = "How should you time Kassatsu?",
                Options = new[] { "Before Kunai's Bane", "After Kunai's Bane", "During Kunai's Bane", "Separate from burst" },
                CorrectIndex = 2,
                Explanation = "Use Kassatsu during Kunai's Bane so Hyosho Ranryu benefits from the +10% damage debuff.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_5.q5",
                ConceptId = NinConcepts.TenChiJin,
                Scenario = "TCJ has a 120 second cooldown.",
                Question = "How does TCJ align with raid buffs?",
                Options = new[] { "Every 60s burst", "Every 120s burst", "Doesn't align", "Use whenever" },
                CorrectIndex = 1,
                Explanation = "TCJ's 120s cooldown aligns with 2-minute raid buff windows. Save it for coordinated party burst.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "nin.lesson_6.quiz",
        LessonId = "nin.lesson_6",
        Title = "Quiz: Procs & Movement",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "nin.lesson_6.q1",
                ConceptId = NinConcepts.RaijuProcs,
                Scenario = "You used Raiton. Raiju Ready buff appeared.",
                Question = "What's the difference between Forked and Fleeting Raiju?",
                Options = new[] { "Damage difference", "Forked is gap closer, Fleeting is stationary", "Same ability", "One is AoE" },
                CorrectIndex = 1,
                Explanation = "Forked Raiju closes distance to target. Fleeting Raiju is used from current position. Choose based on whether you need to move.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_6.q2",
                ConceptId = NinConcepts.RaijuProcs,
                Scenario = "You have Raiju Ready. Boss is far away.",
                Question = "Which Raiju should you use?",
                Options = new[] { "Fleeting Raiju", "Forked Raiju", "Either works", "Wait for boss" },
                CorrectIndex = 1,
                Explanation = "Use Forked Raiju to close the gap to the boss while dealing damage. It's a gap closer with damage.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_6.q3",
                ConceptId = NinConcepts.Bunshin,
                Scenario = "Bunshin is ready. Burst window is starting.",
                Question = "When should you use Bunshin?",
                Options = new[] { "After burst", "Before burst starts", "During downtime", "Save for AoE" },
                CorrectIndex = 1,
                Explanation = "Use Bunshin before burst so your shadow clone attacks during buffed abilities, maximizing damage.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_6.q4",
                ConceptId = NinConcepts.PhantomKamaitachi,
                Scenario = "You used Bunshin. A new ability appeared.",
                Question = "What is Phantom Kamaitachi?",
                Options = new[] { "oGCD follow-up", "GCD proc from Bunshin", "Passive buff", "AoE Ninki spender" },
                CorrectIndex = 1,
                Explanation = "Phantom Kamaitachi is a GCD proc granted by Bunshin. It's AoE damage - use it before it expires.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_6.q5",
                ConceptId = NinConcepts.TenriJindo,
                Scenario = "You just used Kunai's Bane. A new ability appeared.",
                Question = "What is Tenri Jindo?",
                Options = new[] { "Passive buff", "Follow-up proc after Kunai's Bane", "Replacement for Raiton", "Defensive ability" },
                CorrectIndex = 1,
                Explanation = "Tenri Jindo is a powerful follow-up that appears after Kunai's Bane (Lv.100). Use it immediately for big damage.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "nin.lesson_7.quiz",
        LessonId = "nin.lesson_7",
        Title = "Quiz: Optimization & AoE",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "nin.lesson_7.q1",
                ConceptId = NinConcepts.KazematoiManagement,
                Scenario = "Burst window is coming. You have 0 Kazematoi stacks.",
                Question = "What should you do before burst?",
                Options = new[] { "Doesn't matter", "Build Kazematoi with Armor Crush", "Use Aeolian Edge", "Skip positionals" },
                CorrectIndex = 1,
                Explanation = "Build Kazematoi stacks with Armor Crush before burst, then spend enhanced Aeolian Edges during the buff window.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_7.q2",
                ConceptId = NinConcepts.TrueNorthUsage,
                Scenario = "Boss is against the wall. You can't reach rear for Aeolian Edge.",
                Question = "What should you use?",
                Options = new[] { "Skip the attack", "Use True North", "Just miss positional", "Switch to Armor Crush" },
                CorrectIndex = 1,
                Explanation = "True North removes positional requirements for 10s. Use it when you physically can't reach the correct position.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_7.q3",
                ConceptId = NinConcepts.Meisui,
                Scenario = "You have Suiton buff but don't need Kunai's Bane (it's on cooldown).",
                Question = "What can you do with the Suiton buff?",
                Options = new[] { "It's wasted", "Use Meisui for 50 Ninki", "Wait for Kunai's Bane", "Suiton has no other use" },
                CorrectIndex = 1,
                Explanation = "Meisui converts Suiton buff into 50 Ninki. Use it when Kunai's Bane isn't available to avoid wasting the buff.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_7.q4",
                ConceptId = NinConcepts.AoeCombo,
                Scenario = "3 enemies are grouped together.",
                Question = "What's the Ninja AoE combo?",
                Options = new[] { "Spinning Edge → Aeolian Edge", "Death Blossom → Hakke Mujinsatsu", "Just use Katon", "Single-target is better" },
                CorrectIndex = 1,
                Explanation = "At 3+ targets, use Death Blossom → Hakke Mujinsatsu for higher total damage than single-target combos.",
            },
            new QuizQuestion
            {
                QuestionId = "nin.lesson_7.q5",
                ConceptId = NinConcepts.AoeNinjutsu,
                Scenario = "3 enemies will stand still for 20 seconds.",
                Question = "Which AoE Ninjutsu is best?",
                Options = new[] { "Katon", "Doton", "Raiton on each", "Hyosho Ranryu" },
                CorrectIndex = 1,
                Explanation = "Doton creates a ground DoT. If enemies will stand in it for the full duration, it deals more total damage than Katon.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// MNK (Kratos) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class MnkQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "mnk.lesson_1.quiz",
        LessonId = "mnk.lesson_1",
        Title = "Quiz: Monk Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_1.q1",
                ConceptId = MnkConcepts.FormSystem,
                Scenario = "You just used Bootshine. What form are you now in?",
                Question = "What form follows Opo-opo form?",
                Options = new[] { "Opo-opo", "Raptor", "Coeurl", "No form" },
                CorrectIndex = 1,
                Explanation = "After using an Opo-opo form ability (Bootshine/Dragon Kick), you transition to Raptor form.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_1.q2",
                ConceptId = MnkConcepts.Positionals,
                Scenario = "You're about to use Bootshine on a boss.",
                Question = "Where should you stand for the positional bonus?",
                Options = new[] { "Front", "Flank (side)", "Rear (behind)", "Any position" },
                CorrectIndex = 2,
                Explanation = "Bootshine has a rear positional. Standing behind the boss grants bonus potency.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_1.q3",
                ConceptId = MnkConcepts.ComboBasics,
                Scenario = "You're in Raptor form.",
                Question = "Which abilities can you use in Raptor form?",
                Options = new[] { "Bootshine/Dragon Kick", "True Strike/Twin Snakes", "Snap Punch/Demolish", "Any ability" },
                CorrectIndex = 1,
                Explanation = "True Strike and Twin Snakes are Raptor form abilities. Using them transitions you to Coeurl form.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_1.q4",
                ConceptId = MnkConcepts.Positionals,
                Scenario = "You need to use Snap Punch.",
                Question = "Where should you stand for the positional bonus?",
                Options = new[] { "Front", "Flank (side)", "Rear (behind)", "Any position" },
                CorrectIndex = 1,
                Explanation = "Snap Punch has a flank positional. Stand at the boss's side for bonus potency.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_1.q5",
                ConceptId = MnkConcepts.FormSystem,
                Scenario = "You're in Coeurl form and use Snap Punch.",
                Question = "What form do you transition to?",
                Options = new[] { "Stay in Coeurl", "Raptor", "Opo-opo", "Formless" },
                CorrectIndex = 2,
                Explanation = "After Coeurl form abilities, you cycle back to Opo-opo form, completing the form rotation.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "mnk.lesson_2.quiz",
        LessonId = "mnk.lesson_2",
        Title = "Quiz: Maintaining Your Buffs",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_2.q1",
                ConceptId = MnkConcepts.DisciplinedFist,
                Scenario = "Disciplined Fist has 5 seconds remaining.",
                Question = "What should you prioritize?",
                Options = new[] { "Use Bootshine for damage", "Refresh Disciplined Fist", "Use The Forbidden Chakra", "Use Perfect Balance" },
                CorrectIndex = 1,
                Explanation = "Disciplined Fist (+15% damage) is crucial. Refresh it with Twin Snakes or Demolish before it drops.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_2.q2",
                ConceptId = MnkConcepts.DemolishDot,
                Scenario = "Demolish has 5 seconds remaining on the boss.",
                Question = "Should you refresh it now?",
                Options = new[] { "Yes - keep 100% uptime", "No - wait until 3s or less", "No - let it fall off", "Only during burst" },
                CorrectIndex = 1,
                Explanation = "Refresh DoTs at 3 seconds or less to avoid clipping. Refreshing at 5s wastes potential damage ticks.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_2.q3",
                ConceptId = MnkConcepts.DisciplinedFist,
                Scenario = "Which abilities grant Disciplined Fist?",
                Question = "Select the correct answer.",
                Options = new[] { "Bootshine and Dragon Kick", "True Strike and Snap Punch", "Twin Snakes and Demolish", "All weaponskills" },
                CorrectIndex = 2,
                Explanation = "Twin Snakes (Raptor) and Demolish (Coeurl) both grant Disciplined Fist buff.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_2.q4",
                ConceptId = MnkConcepts.Meditation,
                Scenario = "You're waiting for a pull. Chakra gauge is at 0.",
                Question = "What should you do?",
                Options = new[] { "Wait patiently", "Use Meditation to build Chakra", "Use Perfect Balance", "Nothing - Chakra builds in combat" },
                CorrectIndex = 1,
                Explanation = "Meditation builds Chakra out of combat. Start fights with 5 Chakra ready for burst.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_2.q5",
                ConceptId = MnkConcepts.DemolishDot,
                Scenario = "Demolish is a DoT with 18 second duration.",
                Question = "How does Demolish interact with Disciplined Fist?",
                Options = new[] { "No interaction", "Demolish grants Disciplined Fist", "Disciplined Fist extends Demolish", "They're the same buff" },
                CorrectIndex = 1,
                Explanation = "Demolish both applies a DoT to the enemy AND grants you the Disciplined Fist damage buff.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "mnk.lesson_3.quiz",
        LessonId = "mnk.lesson_3",
        Title = "Quiz: The Chakra System",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_3.q1",
                ConceptId = MnkConcepts.ChakraGauge,
                Scenario = "Your Chakra gauge is at 5 stacks.",
                Question = "What should you do?",
                Options = new[] { "Build more Chakra", "Use The Forbidden Chakra", "Save for burst window", "Chakra doesn't cap" },
                CorrectIndex = 1,
                Explanation = "Chakra caps at 5 stacks. Spend it to avoid wasting potential Chakra generation.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_3.q2",
                ConceptId = MnkConcepts.TheForbiddenChakra,
                Scenario = "Single target fight. You have 5 Chakra.",
                Question = "Which Chakra spender should you use?",
                Options = new[] { "The Forbidden Chakra", "Enlightenment", "Steel Peak", "Howling Fist" },
                CorrectIndex = 0,
                Explanation = "The Forbidden Chakra is the single-target Chakra spender. Enlightenment is for AoE (3+ targets).",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_3.q3",
                ConceptId = MnkConcepts.Enlightenment,
                Scenario = "4 enemies are grouped. You have 5 Chakra.",
                Question = "Which ability should you use?",
                Options = new[] { "The Forbidden Chakra", "Enlightenment", "Save Chakra", "Howling Fist only" },
                CorrectIndex = 1,
                Explanation = "Enlightenment is the AoE Chakra spender. At 4 targets, it deals more total damage than single-target.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_3.q4",
                ConceptId = MnkConcepts.ChakraGauge,
                Scenario = "You're about to use Brotherhood.",
                Question = "How does Brotherhood affect Chakra generation?",
                Options = new[] { "No effect", "Grants 5 Chakra instantly", "Increases Chakra generation from crits", "Doubles Chakra gains" },
                CorrectIndex = 2,
                Explanation = "Brotherhood increases the party's critical hit rate, which means more Chakra generation from critical weaponskills.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_3.q5",
                ConceptId = MnkConcepts.ChakraGauge,
                Scenario = "How does Chakra build during combat?",
                Question = "What generates Chakra?",
                Options = new[] { "Time only", "Weaponskills and critical hits", "oGCDs only", "Taking damage" },
                CorrectIndex = 1,
                Explanation = "Chakra builds from using weaponskills and has additional chance to generate on critical hits.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "mnk.lesson_4.quiz",
        LessonId = "mnk.lesson_4",
        Title = "Quiz: Beast Chakra & Masterful Blitz",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_4.q1",
                ConceptId = MnkConcepts.BeastChakra,
                Scenario = "You use Dragon Kick (Opo-opo form ability).",
                Question = "Which Beast Chakra type do you gain?",
                Options = new[] { "Lunar", "Solar", "Celestial", "None" },
                CorrectIndex = 0,
                Explanation = "Opo-opo form abilities (Bootshine, Dragon Kick) grant Lunar Beast Chakra.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_4.q2",
                ConceptId = MnkConcepts.MasterfulBlitz,
                Scenario = "You have 3 Beast Chakra: Lunar, Solar, Celestial.",
                Question = "Which Blitz will Masterful Blitz become?",
                Options = new[] { "Elixir Field", "Rising Phoenix", "Phantom Rush", "Random" },
                CorrectIndex = 2,
                Explanation = "All 3 different Beast Chakra types = Phantom Rush, your highest single-target damage Blitz.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_4.q3",
                ConceptId = MnkConcepts.ElixirField,
                Scenario = "You have 3 Lunar Beast Chakra (all matching).",
                Question = "Which Blitz will Masterful Blitz become?",
                Options = new[] { "Elixir Field", "Rising Phoenix", "Phantom Rush", "Flint Strike" },
                CorrectIndex = 0,
                Explanation = "3 matching Beast Chakra = Elixir Field, a large AoE attack.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_4.q4",
                ConceptId = MnkConcepts.RisingPhoenix,
                Scenario = "You have 2 Solar and 1 Lunar Beast Chakra.",
                Question = "Which Blitz will Masterful Blitz become?",
                Options = new[] { "Elixir Field", "Rising Phoenix", "Phantom Rush", "Elixir Burst" },
                CorrectIndex = 1,
                Explanation = "2 of one type + 1 different = Rising Phoenix (requires Solar chakra present), a cone AoE.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_4.q5",
                ConceptId = MnkConcepts.PhantomRush,
                Scenario = "Single target boss fight. You're about to build Beast Chakra.",
                Question = "Which combination should you aim for?",
                Options = new[] { "3 matching for Elixir Field", "All 3 different for Phantom Rush", "2+1 for Rising Phoenix", "Doesn't matter" },
                CorrectIndex = 1,
                Explanation = "In single-target, Phantom Rush (all 3 different) deals the highest damage.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "mnk.lesson_5.quiz",
        LessonId = "mnk.lesson_5",
        Title = "Quiz: Burst Windows",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_5.q1",
                ConceptId = MnkConcepts.PerfectBalance,
                Scenario = "You activate Perfect Balance.",
                Question = "What does Perfect Balance allow?",
                Options = new[] { "Double damage for 15s", "Use any weaponskill regardless of form", "Reset all cooldowns", "Ignore positionals" },
                CorrectIndex = 1,
                Explanation = "Perfect Balance lets you use any weaponskill regardless of your current form for 3 uses.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_5.q2",
                ConceptId = MnkConcepts.RiddleOfFire,
                Scenario = "Riddle of Fire is your 60-second damage buff.",
                Question = "How much damage increase does it provide?",
                Options = new[] { "+5%", "+10%", "+15%", "+20%" },
                CorrectIndex = 2,
                Explanation = "Riddle of Fire grants +15% damage for 20 seconds. It's your core burst window ability.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_5.q3",
                ConceptId = MnkConcepts.Brotherhood,
                Scenario = "Brotherhood has a 120-second cooldown.",
                Question = "What does Brotherhood provide?",
                Options = new[] { "Personal damage buff only", "Party damage buff + Chakra boost", "Heal over time", "Movement speed" },
                CorrectIndex = 1,
                Explanation = "Brotherhood gives the party 5% damage increase and boosts Chakra generation through increased crit rate.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_5.q4",
                ConceptId = MnkConcepts.BurstAlignment,
                Scenario = "Party raid buffs are coming in 10 seconds.",
                Question = "How should you time your burst abilities?",
                Options = new[] { "Use immediately", "Wait for raid buffs", "Save for emergencies", "Use randomly" },
                CorrectIndex = 1,
                Explanation = "Align your burst (Riddle of Fire, Brotherhood) with party raid buffs for maximum damage amplification.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_5.q5",
                ConceptId = MnkConcepts.PerfectBalance,
                Scenario = "Perfect Balance has 2 charges with 40-second recharge.",
                Question = "How should you use Perfect Balance during burst?",
                Options = new[] { "Save both charges", "Use to quickly build Beast Chakra", "Use for positional freedom", "Only use in emergencies" },
                CorrectIndex = 1,
                Explanation = "Use Perfect Balance during Riddle of Fire to quickly build Beast Chakra for Masterful Blitz.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "mnk.lesson_6.quiz",
        LessonId = "mnk.lesson_6",
        Title = "Quiz: Movement & Utility",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_6.q1",
                ConceptId = MnkConcepts.Thunderclap,
                Scenario = "Boss dashes across the arena. Thunderclap has 3 charges.",
                Question = "How should you use Thunderclap?",
                Options = new[] { "Use all charges to catch up", "Use 1 charge, save others", "Save all charges", "Walk normally" },
                CorrectIndex = 1,
                Explanation = "Use Thunderclap to close gaps, but keep charges in reserve for future movement needs.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_6.q2",
                ConceptId = MnkConcepts.TrueNorthUsage,
                Scenario = "Boss is against a wall. You can't reach the rear for Bootshine.",
                Question = "What should you do?",
                Options = new[] { "Miss the positional", "Use True North", "Use Dragon Kick instead", "Wait for boss to move" },
                CorrectIndex = 1,
                Explanation = "True North removes positional requirements for 10 seconds. Use it when positioning is impossible.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_6.q3",
                ConceptId = MnkConcepts.RiddleOfWind,
                Scenario = "Riddle of Wind increases auto-attack speed.",
                Question = "When should you use Riddle of Wind?",
                Options = new[] { "Only in AoE", "During burst windows", "Only for movement", "Never - it's useless" },
                CorrectIndex = 1,
                Explanation = "Riddle of Wind provides free damage through faster auto-attacks. Use it during burst for maximum value.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_6.q4",
                ConceptId = MnkConcepts.TrueNorthUsage,
                Scenario = "True North has 2 charges with 45-second recharge.",
                Question = "How should you manage True North?",
                Options = new[] { "Use on cooldown", "Save both for emergencies", "Use one, keep one reserve", "Never use it" },
                CorrectIndex = 2,
                Explanation = "Keep at least one charge for unexpected situations. Don't cap at 2, but don't waste them either.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_6.q5",
                ConceptId = MnkConcepts.Thunderclap,
                Scenario = "You need to dodge an AoE but have a GCD coming up.",
                Question = "What's the best approach?",
                Options = new[] { "Cancel GCD and run", "Use Thunderclap to dodge and continue", "Take the hit", "Wait for GCD" },
                CorrectIndex = 1,
                Explanation = "Thunderclap can be used for quick repositioning while maintaining GCD uptime.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "mnk.lesson_7.quiz",
        LessonId = "mnk.lesson_7",
        Title = "Quiz: AoE & Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_7.q1",
                ConceptId = MnkConcepts.AoeCombo,
                Scenario = "5 enemies are grouped together.",
                Question = "What's the AoE combo sequence?",
                Options = new[] { "Bootshine spam", "Arm of the Destroyer → Four-point Fury → Rockbreaker", "Dragon Kick spam", "Single-target rotation" },
                CorrectIndex = 1,
                Explanation = "Arm of the Destroyer → Four-point Fury → Rockbreaker is the AoE combo, hitting all nearby enemies.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_7.q2",
                ConceptId = MnkConcepts.AoeThreshold,
                Scenario = "2 enemies are present.",
                Question = "Should you use AoE or single-target rotation?",
                Options = new[] { "AoE rotation", "Single-target rotation", "Mix of both", "Doesn't matter" },
                CorrectIndex = 1,
                Explanation = "Switch to AoE at 3+ targets. At 2 targets, single-target rotation is more efficient.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_7.q3",
                ConceptId = MnkConcepts.HowlingFist,
                Scenario = "Large trash pull in a dungeon. Chakra is at 5.",
                Question = "Which Chakra spender should you use?",
                Options = new[] { "The Forbidden Chakra", "Enlightenment", "Save for boss", "Meditation" },
                CorrectIndex = 1,
                Explanation = "Enlightenment is the AoE Chakra spender. In dungeon pulls with 3+ enemies, it deals more total damage.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_7.q4",
                ConceptId = MnkConcepts.AoeCombo,
                Scenario = "You're doing the AoE rotation.",
                Question = "Does the AoE combo maintain Disciplined Fist?",
                Options = new[] { "No - need single-target", "Yes - automatically maintained", "Only Demolish works", "AoE removes buffs" },
                CorrectIndex = 1,
                Explanation = "The AoE combo abilities also grant and maintain Disciplined Fist, so you don't need to switch to single-target.",
            },
            new QuizQuestion
            {
                QuestionId = "mnk.lesson_7.q5",
                ConceptId = MnkConcepts.AoeThreshold,
                Scenario = "Dungeon pull: 6 enemies, one is almost dead.",
                Question = "Should you switch to single-target for the low enemy?",
                Options = new[] { "Yes - finish it quickly", "No - continue AoE", "Use oGCDs on low target", "Focus healer" },
                CorrectIndex = 1,
                Explanation = "With 6 targets, AoE damage is still better. The low enemy will die from AoE damage anyway.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// SAM (Nike) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class SamQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "sam.lesson_1.quiz",
        LessonId = "sam.lesson_1",
        Title = "Quiz: Samurai Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sam.lesson_1.q1",
                ConceptId = SamConcepts.ComboBasics,
                Scenario = "You want to obtain the Getsu Sen.",
                Question = "Which combo route grants Getsu Sen?",
                Options = new[] { "Hakaze → Shifu → Kasha", "Hakaze → Jinpu → Gekko", "Hakaze → Yukikaze", "Any finisher" },
                CorrectIndex = 1,
                Explanation = "Hakaze → Jinpu → Gekko is the Jinpu path, which grants Getsu Sen from Gekko. The Shifu path grants Ka Sen.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_1.q2",
                ConceptId = SamConcepts.FugetsuBuff,
                Scenario = "Your Fugetsu buff is about to expire. Fuka has 20 seconds remaining.",
                Question = "Which buff should you prioritize refreshing?",
                Options = new[] { "Fuka - it's the haste buff", "Fugetsu - it's the damage buff", "Doesn't matter", "Neither - do Iaijutsu" },
                CorrectIndex = 1,
                Explanation = "Fugetsu (+13% damage) should be prioritized over Fuka (+13% haste). Damage affects all your abilities.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_1.q3",
                ConceptId = SamConcepts.SenSystem,
                Scenario = "You have Getsu and Ka Sen. You need Setsu Sen.",
                Question = "Which ability grants Setsu Sen?",
                Options = new[] { "Gekko", "Kasha", "Yukikaze", "Jinpu" },
                CorrectIndex = 2,
                Explanation = "Yukikaze is the only finisher that grants Setsu Sen. It's a 2-hit combo (Hakaze → Yukikaze) without a positional.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_1.q4",
                ConceptId = SamConcepts.FukaBuff,
                Scenario = "You're starting a fight. What should you establish first?",
                Question = "What's the optimal buff priority in the opener?",
                Options = new[] { "Fuka first for speed", "Fugetsu first for damage", "Get both ASAP", "Neither - go straight to Iaijutsu" },
                CorrectIndex = 2,
                Explanation = "In the opener, you want both buffs up quickly. The standard opener establishes both Fugetsu and Fuka before your first Midare.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_1.q5",
                ConceptId = SamConcepts.ComboBasics,
                Scenario = "At level 92+, your combo starter changes.",
                Question = "What replaces Hakaze at level 92?",
                Options = new[] { "Jinpu", "Shifu", "Gyofu", "Nothing changes" },
                CorrectIndex = 2,
                Explanation = "Gyofu replaces Hakaze at level 92, dealing higher potency. It's still used the same way as your combo starter.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "sam.lesson_2.quiz",
        LessonId = "sam.lesson_2",
        Title = "Quiz: Kenki & Meditation",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sam.lesson_2.q1",
                ConceptId = SamConcepts.KenkiGauge,
                Scenario = "Your Kenki gauge is at 85. You're about to use a combo that generates 10 Kenki.",
                Question = "What should you do?",
                Options = new[] { "Complete the combo first", "Use Shinten to avoid overcap", "Wait for 100 Kenki", "Save for burst" },
                CorrectIndex = 1,
                Explanation = "At 85 Kenki with 10 more coming, you'll overcap (95 after, missing 5). Use Shinten (25 cost) to make room.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_2.q2",
                ConceptId = SamConcepts.KenkiSpending,
                Scenario = "Single target fight. You have 50 Kenki.",
                Question = "Which Kenki spender should you use?",
                Options = new[] { "Kyuten", "Shinten", "Save for Senei", "Hagakure" },
                CorrectIndex = 1,
                Explanation = "Shinten is the single-target Kenki spender. Kyuten is for AoE (3+ targets). Senei costs 25 Kenki and has a cooldown.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_2.q3",
                ConceptId = SamConcepts.Meditation,
                Scenario = "You have 3 Meditation stacks. When should you use Shoha?",
                Question = "What's the optimal Shoha timing?",
                Options = new[] { "Immediately - don't hold stacks", "During burst window", "Before Iaijutsu", "Save for emergencies" },
                CorrectIndex = 1,
                Explanation = "Use Shoha during burst windows when possible. At 3 stacks you can't build more, so spend it to avoid waste.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_2.q4",
                ConceptId = SamConcepts.KenkiGauge,
                Scenario = "You're about to use Ikishoten. Your Kenki is at 60.",
                Question = "What's wrong with this situation?",
                Options = new[] { "Nothing is wrong", "Ikishoten will overcap Kenki", "Should use Meikyo first", "Need more Sen" },
                CorrectIndex = 1,
                Explanation = "Ikishoten grants 50 Kenki. At 60 Kenki, using it would put you at 110, wasting 10 gauge. Spend to 50 or below first.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_2.q5",
                ConceptId = SamConcepts.Meditation,
                Scenario = "You gain Meditation stacks from using Iaijutsu.",
                Question = "How many Meditation stacks does one Iaijutsu grant?",
                Options = new[] { "1 stack", "2 stacks", "3 stacks", "Depends on Sen count" },
                CorrectIndex = 0,
                Explanation = "Each Iaijutsu grants 1 Meditation stack. You need 3 Iaijutsu casts to get Shoha ready.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "sam.lesson_3.quiz",
        LessonId = "sam.lesson_3",
        Title = "Quiz: Iaijutsu System",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sam.lesson_3.q1",
                ConceptId = SamConcepts.HiganbanaDoT,
                Scenario = "You're in a single-target fight. Higanbana is not on the boss. You have 1 Sen.",
                Question = "Should you use Higanbana now?",
                Options = new[] { "Yes - apply the DoT immediately", "No - build to 3 Sen for Midare", "Only in opener", "Depends on fight length" },
                CorrectIndex = 3,
                Explanation = "Higanbana is worth applying only if the boss will live long enough for the DoT to tick (60s duration). In short fights, Midare is better.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_3.q2",
                ConceptId = SamConcepts.MidareSetsugekka,
                Scenario = "You have 2 Sen (Getsu and Ka). The boss is at 5% HP.",
                Question = "What should you do?",
                Options = new[] { "Use Tenka Goken (2 Sen)", "Build to 3 Sen for Midare", "Use Higanbana", "Depends on adds" },
                CorrectIndex = 1,
                Explanation = "Even at low boss HP, building to 3 Sen for Midare Setsugekka is worth it. Tenka Goken is only for multi-target.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_3.q3",
                ConceptId = SamConcepts.TenkaGoken,
                Scenario = "3 enemies are grouped together. You have 2 Sen.",
                Question = "Which Iaijutsu should you use?",
                Options = new[] { "Midare Setsugekka", "Tenka Goken", "Higanbana on each", "Build to 3 Sen" },
                CorrectIndex = 1,
                Explanation = "Tenka Goken hits all nearby enemies. At 3 targets, using 2 Sen for AoE damage is more efficient than building for single-target Midare.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_3.q4",
                ConceptId = SamConcepts.HiganbanaDoT,
                Scenario = "Higanbana has 5 seconds remaining on the boss.",
                Question = "Should you refresh it now?",
                Options = new[] { "Yes - keep 100% uptime", "No - wait until 3s or less", "No - let it fall off", "Only during burst" },
                CorrectIndex = 1,
                Explanation = "Refresh DoTs at 3 seconds or less to avoid clipping (losing ticks). Refreshing at 5s wastes potential damage.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_3.q5",
                ConceptId = SamConcepts.IaijutsuSelection,
                Scenario = "You have 1 Sen in single-target. Higanbana is already on the boss with 40s remaining.",
                Question = "What should you do with this Sen?",
                Options = new[] { "Use Higanbana anyway", "Build to 3 Sen for Midare", "Use Hagakure", "Wait for Higanbana to expire" },
                CorrectIndex = 1,
                Explanation = "Don't overwrite Higanbana early. Build to 3 Sen for Midare Setsugekka instead - it's your primary damage tool.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "sam.lesson_4.quiz",
        LessonId = "sam.lesson_4",
        Title = "Quiz: Tsubame-gaeshi & Meikyo",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sam.lesson_4.q1",
                ConceptId = SamConcepts.TsubameGaeshi,
                Scenario = "You just used Midare Setsugekka. Tsubame-gaeshi is available.",
                Question = "What should you do next?",
                Options = new[] { "Start a new combo", "Use Tsubame-gaeshi immediately", "Use Shinten first", "Wait for next GCD" },
                CorrectIndex = 1,
                Explanation = "Tsubame-gaeshi should be used immediately after Iaijutsu. Kaeshi: Setsugekka is massive damage - never skip it.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_4.q2",
                ConceptId = SamConcepts.MeikyoShisui,
                Scenario = "Meikyo Shisui is ready. You have 0 Sen and need to burst soon.",
                Question = "What's the optimal Meikyo usage?",
                Options = new[] { "Use 3 Yukikaze for fast Sen", "Use Gekko → Kasha → Yukikaze", "Use 3 Gekko for damage", "Save for emergencies" },
                CorrectIndex = 1,
                Explanation = "Use each finisher once to get all 3 Sen types (Getsu, Ka, Setsu) for immediate Midare Setsugekka.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_4.q3",
                ConceptId = SamConcepts.MeikyoFinisherPriority,
                Scenario = "During Meikyo, you already have Getsu and Ka Sen. You have 1 Meikyo stack left.",
                Question = "Which finisher should you use?",
                Options = new[] { "Gekko - highest potency", "Kasha - flank is easier", "Yukikaze - get Setsu", "Doesn't matter" },
                CorrectIndex = 2,
                Explanation = "You're missing Setsu Sen. Use Yukikaze to complete your Sen collection for Midare Setsugekka.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_4.q4",
                ConceptId = SamConcepts.TsubameGaeshi,
                Scenario = "You used Tenka Goken in AoE. What Kaeshi ability appears?",
                Question = "What follows Tenka Goken?",
                Options = new[] { "Kaeshi: Setsugekka", "Kaeshi: Goken", "Kaeshi: Higanbana", "Nothing - only Midare has follow-up" },
                CorrectIndex = 1,
                Explanation = "Tsubame-gaeshi creates Kaeshi versions matching your Iaijutsu. Tenka Goken → Kaeshi: Goken for AoE.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_4.q5",
                ConceptId = SamConcepts.MeikyoShisui,
                Scenario = "Meikyo Shisui has a 55 second cooldown.",
                Question = "How does this affect your rotation?",
                Options = new[] { "Use on cooldown always", "Align with 60s buffs", "Save for emergencies", "Use only in opener" },
                CorrectIndex = 1,
                Explanation = "At 55s cooldown, Meikyo roughly aligns with 60s burst windows. Plan usage around party buffs.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "sam.lesson_5.quiz",
        LessonId = "sam.lesson_5",
        Title = "Quiz: Ikishoten Burst Window",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sam.lesson_5.q1",
                ConceptId = SamConcepts.IkishotenBurst,
                Scenario = "Ikishoten is ready. Your Kenki is at 30.",
                Question = "Is it safe to use Ikishoten?",
                Options = new[] { "Yes - won't overcap", "No - too much Kenki", "Only during burst", "Need 0 Kenki" },
                CorrectIndex = 0,
                Explanation = "At 30 Kenki, Ikishoten (+50) puts you at 80. That's safe. Never use Ikishoten above 50 Kenki.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_5.q2",
                ConceptId = SamConcepts.OgiNamikiri,
                Scenario = "You used Ikishoten. What GCD should come next?",
                Question = "What's the correct sequence?",
                Options = new[] { "Normal combo", "Ogi Namikiri", "Midare Setsugekka", "Meikyo Shisui" },
                CorrectIndex = 1,
                Explanation = "Ikishoten grants Ogi Namikiri Ready. Use Ogi Namikiri immediately for your highest potency GCD.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_5.q3",
                ConceptId = SamConcepts.Zanshin,
                Scenario = "You just used Kaeshi: Namikiri. Zanshin is now available.",
                Question = "What is Zanshin?",
                Options = new[] { "A GCD follow-up", "An enhanced Kenki spender", "A defensive cooldown", "A movement ability" },
                CorrectIndex = 1,
                Explanation = "Zanshin is an enhanced oGCD Kenki spender available after the Ogi Namikiri sequence. Big damage!",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_5.q4",
                ConceptId = SamConcepts.SeneiTiming,
                Scenario = "Senei (high-potency oGCD) is ready. You're in burst window.",
                Question = "When should you use Senei?",
                Options = new[] { "Before Ikishoten", "During burst window", "After burst ends", "On cooldown regardless" },
                CorrectIndex = 1,
                Explanation = "Senei deals high damage. Use it during burst windows when party buffs are active for maximum effect.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_5.q5",
                ConceptId = SamConcepts.IkishotenBurst,
                Scenario = "Ikishoten has a 120 second cooldown.",
                Question = "How does this align with party bursts?",
                Options = new[] { "Every minute", "Every 2 minutes", "Every 90 seconds", "Doesn't align" },
                CorrectIndex = 1,
                Explanation = "120s = 2 minutes. Ikishoten aligns perfectly with standard 2-minute raid buff windows.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "sam.lesson_6.quiz",
        LessonId = "sam.lesson_6",
        Title = "Quiz: Positionals & True North",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sam.lesson_6.q1",
                ConceptId = SamConcepts.Positionals,
                Scenario = "You need to use Gekko for Getsu Sen.",
                Question = "Where should you stand?",
                Options = new[] { "Front", "Flank (side)", "Rear (behind)", "Any position" },
                CorrectIndex = 2,
                Explanation = "Gekko has a rear positional. Stand behind the boss for bonus potency.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_6.q2",
                ConceptId = SamConcepts.Positionals,
                Scenario = "You need to use Kasha for Ka Sen.",
                Question = "Where should you stand?",
                Options = new[] { "Front", "Flank (side)", "Rear (behind)", "Any position" },
                CorrectIndex = 1,
                Explanation = "Kasha has a flank positional. Stand at the boss's side for bonus potency.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_6.q3",
                ConceptId = SamConcepts.TrueNorthUsage,
                Scenario = "Boss is against the wall. You can't reach rear for Gekko.",
                Question = "What should you do?",
                Options = new[] { "Miss the positional", "Use True North", "Use Kasha instead", "Wait for boss to move" },
                CorrectIndex = 1,
                Explanation = "True North removes positional requirements for 10 seconds. Use it when you physically can't reach the correct position.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_6.q4",
                ConceptId = SamConcepts.PositionalRecovery,
                Scenario = "You missed Gekko's positional. The attack still went off.",
                Question = "What happened?",
                Options = new[] { "Attack failed", "Full damage dealt", "Reduced potency", "Combo broke" },
                CorrectIndex = 2,
                Explanation = "Missing a positional means the attack still works but deals reduced potency. You lose the bonus damage.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_6.q5",
                ConceptId = SamConcepts.TrueNorthUsage,
                Scenario = "True North has 2 charges with 45s recharge each.",
                Question = "How should you manage True North usage?",
                Options = new[] { "Use freely - always available", "Save both for emergencies", "Use one, keep one in reserve", "Never needed" },
                CorrectIndex = 2,
                Explanation = "Keep at least one charge available for unexpected situations. Don't cap at 2 charges, but don't waste them either.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "sam.lesson_7.quiz",
        LessonId = "sam.lesson_7",
        Title = "Quiz: Advanced Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "sam.lesson_7.q1",
                ConceptId = SamConcepts.BurstAlignment,
                Scenario = "Party raid buffs are coming in 5 seconds. Ikishoten is ready.",
                Question = "When should you use Ikishoten?",
                Options = new[] { "Now - on cooldown", "When buffs go up", "After buffs expire", "Doesn't matter" },
                CorrectIndex = 1,
                Explanation = "Align Ikishoten with party raid buffs for maximum damage during Ogi Namikiri and the entire burst sequence.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_7.q2",
                ConceptId = SamConcepts.HagakureUsage,
                Scenario = "Boss is about to become untargetable for 20 seconds. You have 3 Sen.",
                Question = "What should you do with your Sen?",
                Options = new[] { "Use Midare before downtime", "Use Hagakure for Kenki", "Hold Sen for after", "Doesn't matter" },
                CorrectIndex = 0,
                Explanation = "Use Midare before downtime if possible. Only use Hagakure if you can't get Midare off in time - Midare damage > Kenki value.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_7.q3",
                ConceptId = SamConcepts.AoeRotation,
                Scenario = "4 enemies are grouped. You're doing the AoE rotation.",
                Question = "What Sen types can you build with AoE combos?",
                Options = new[] { "All three (Getsu, Ka, Setsu)", "Only Getsu and Ka", "Only Setsu", "None - AoE doesn't grant Sen" },
                CorrectIndex = 1,
                Explanation = "Fuko → Mangetsu grants Getsu. Fuko → Oka grants Ka. You cannot build Setsu Sen in AoE - it requires Yukikaze.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_7.q4",
                ConceptId = SamConcepts.MeikyoBuffRefresh,
                Scenario = "Fugetsu has 3 seconds left. Meikyo Shisui is available.",
                Question = "Should you use Meikyo to refresh Fugetsu?",
                Options = new[] { "No - save Meikyo for Sen", "Yes - quick buff refresh", "Only if Fuka is also low", "Use normal combo instead" },
                CorrectIndex = 1,
                Explanation = "Meikyo can quickly refresh buffs in emergencies. Gekko refreshes Fugetsu without needing the full combo.",
            },
            new QuizQuestion
            {
                QuestionId = "sam.lesson_7.q5",
                ConceptId = SamConcepts.HagakureUsage,
                Scenario = "You have 3 Sen. Phase transition is in 2 GCDs.",
                Question = "You can't use Midare in time. What's your best option?",
                Options = new[] { "Waste the Sen", "Hagakure for 30 Kenki", "Use Tenka Goken", "Hold for after transition" },
                CorrectIndex = 1,
                Explanation = "Hagakure converts Sen to Kenki (10 per Sen). 30 Kenki is better than losing Sen entirely to phase transition.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// RPR (Thanatos) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class RprQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "rpr.lesson_1.quiz",
        LessonId = "rpr.lesson_1",
        Title = "Quiz: Reaper Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_1.q1",
                ConceptId = RprConcepts.ComboBasics,
                Scenario = "You just finished a Slice → Waxing Slice → Infernal Slice combo.",
                Question = "How much Soul Gauge did you gain from this combo?",
                Options = new[] { "30 Soul Gauge", "20 Soul Gauge", "10 Soul Gauge", "50 Soul Gauge" },
                CorrectIndex = 2,
                Explanation = "The basic combo (Slice → Waxing Slice → Infernal Slice) grants 10 Soul Gauge upon completion of Infernal Slice.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_1.q2",
                ConceptId = RprConcepts.SoulSlice,
                Scenario = "Soul Slice has 2 charges. You have 40 Soul Gauge.",
                Question = "Should you use Soul Slice?",
                Options = new[] { "No - save charges for emergencies", "Yes - use on cooldown to avoid overcapping", "No - wait until Soul Gauge is at 0", "Only use one charge" },
                CorrectIndex = 1,
                Explanation = "Soul Slice grants 50 Soul Gauge per use. At 40 Soul, using it won't overcap (90 max). Use charges on cooldown to maximize gauge generation.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_1.q3",
                ConceptId = RprConcepts.DeathsDesign,
                Scenario = "Death's Design has 5 seconds remaining on the boss.",
                Question = "What should you do?",
                Options = new[] { "Let it fall off", "Refresh with Shadow of Death", "Wait until it expires", "Use Whorl of Death" },
                CorrectIndex = 1,
                Explanation = "Death's Design (+10% damage) should never fall off. Refresh it with Shadow of Death when it's getting low to maintain the damage buff.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_1.q4",
                ConceptId = RprConcepts.DeathsDesign,
                Scenario = "Death's Design is at 55 seconds duration on the boss.",
                Question = "Should you use Shadow of Death?",
                Options = new[] { "Yes - keep refreshing", "No - it's near max (60s)", "Yes - always use on cooldown", "No - Death's Design doesn't stack" },
                CorrectIndex = 1,
                Explanation = "Death's Design caps at 60 seconds. At 55s, using Shadow of Death would only add 5s, wasting most of the extension. Wait until it's lower.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_1.q5",
                ConceptId = RprConcepts.SoulGauge,
                Scenario = "You have 95 Soul Gauge and both Soul Slice charges are available.",
                Question = "What should you do?",
                Options = new[] { "Use Soul Slice immediately", "Spend Soul first with Blood Stalk", "Use both Soul Slice charges", "Wait for Soul to decay" },
                CorrectIndex = 1,
                Explanation = "At 95 Soul, using Soul Slice (50 gauge) would overcap. Spend Soul first with Blood Stalk (50 cost) to make room for Soul Slice generation.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "rpr.lesson_2.quiz",
        LessonId = "rpr.lesson_2",
        Title = "Quiz: Soul Reaver & Positionals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_2.q1",
                ConceptId = RprConcepts.Gibbet,
                Scenario = "You have Soul Reaver active and are at the boss's side (flank).",
                Question = "Which ability should you use for the positional bonus?",
                Options = new[] { "Gallows", "Gibbet", "Either one", "Guillotine" },
                CorrectIndex = 1,
                Explanation = "Gibbet has a flank positional. Use it from the boss's side for bonus potency.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_2.q2",
                ConceptId = RprConcepts.Gallows,
                Scenario = "You're standing behind the boss (rear) with Soul Reaver active.",
                Question = "Which ability should you use?",
                Options = new[] { "Gibbet", "Gallows", "Either one", "Save Soul Reaver" },
                CorrectIndex = 1,
                Explanation = "Gallows has a rear positional. Use it from behind the boss for bonus potency.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_2.q3",
                ConceptId = RprConcepts.EnhancedProcs,
                Scenario = "You just used Gibbet. What buff did you receive?",
                Question = "Which Enhanced buff do you now have?",
                Options = new[] { "Enhanced Gibbet", "Enhanced Gallows", "Enhanced Guillotine", "No buff" },
                CorrectIndex = 1,
                Explanation = "Gibbet grants Enhanced Gallows. The next Gallows you use will deal bonus damage.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_2.q4",
                ConceptId = RprConcepts.SoulReaver,
                Scenario = "You have 50 Soul Gauge. Gluttony is available.",
                Question = "How many Soul Reaver stacks does Gluttony grant?",
                Options = new[] { "1 stack", "2 stacks", "3 stacks", "No stacks" },
                CorrectIndex = 1,
                Explanation = "Gluttony grants 2 Soul Reaver stacks, making it more efficient than Blood Stalk (1 stack) for the same Soul cost.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_2.q5",
                ConceptId = RprConcepts.Positionals,
                Scenario = "A mechanic forces you to stand in front of the boss. You have Soul Reaver and True North available.",
                Question = "What should you do?",
                Options = new[] { "Skip Soul Reaver abilities", "Use True North and do positional", "Just use Gibbet/Gallows anyway", "Wait for mechanic to end" },
                CorrectIndex = 1,
                Explanation = "True North allows you to ignore positional requirements. Use it when mechanics prevent proper positioning.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "rpr.lesson_3.quiz",
        LessonId = "rpr.lesson_3",
        Title = "Quiz: Shroud Gauge Management",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_3.q1",
                ConceptId = RprConcepts.ShroudGauge,
                Scenario = "You use Gibbet to finish a Soul Reaver stack.",
                Question = "How much Shroud Gauge do you gain?",
                Options = new[] { "5 Shroud", "10 Shroud", "20 Shroud", "50 Shroud" },
                CorrectIndex = 1,
                Explanation = "Each Soul Reaver ability (Gibbet, Gallows, or Guillotine) grants 10 Shroud Gauge.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_3.q2",
                ConceptId = RprConcepts.Enshroud,
                Scenario = "You have 50 Shroud Gauge.",
                Question = "What can you do with this Shroud?",
                Options = new[] { "Nothing yet", "Enter Enshroud", "Use Lemure's Slice", "Convert to Soul Gauge" },
                CorrectIndex = 1,
                Explanation = "Enshroud requires 50 Shroud Gauge to activate. At 50 Shroud, you can enter your burst state.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_3.q3",
                ConceptId = RprConcepts.Guillotine,
                Scenario = "You have 2 Soul Reaver stacks and 4 enemies nearby.",
                Question = "Which Soul Reaver ability should you use?",
                Options = new[] { "Gibbet", "Gallows", "Guillotine", "Alternate Gibbet/Gallows" },
                CorrectIndex = 2,
                Explanation = "Guillotine is the AoE Soul Reaver ability. At 3+ targets, it deals more total damage than single-target options.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_3.q4",
                ConceptId = RprConcepts.ShroudGauge,
                Scenario = "You have 90 Shroud Gauge. Blood Stalk is available.",
                Question = "What should you prioritize?",
                Options = new[] { "Use Blood Stalk for Soul Reaver", "Enter Enshroud first", "Build more Shroud", "Wait for raid buffs" },
                CorrectIndex = 1,
                Explanation = "At 90 Shroud, you're close to capping (100 max). Enter Enshroud to spend Shroud before gaining more from Soul Reaver.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_3.q5",
                ConceptId = RprConcepts.Guillotine,
                Scenario = "You use Guillotine on a pack of enemies.",
                Question = "What buff do you receive?",
                Options = new[] { "Enhanced Gibbet", "Enhanced Gallows", "Both Enhanced buffs", "No Enhanced buff" },
                CorrectIndex = 2,
                Explanation = "Guillotine grants both Enhanced Gibbet and Enhanced Gallows buffs, allowing you to use either enhanced version afterward.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "rpr.lesson_4.quiz",
        LessonId = "rpr.lesson_4",
        Title = "Quiz: Enshroud Burst Window",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_4.q1",
                ConceptId = RprConcepts.LemureShroud,
                Scenario = "You just entered Enshroud.",
                Question = "How many Lemure Shroud stacks do you start with?",
                Options = new[] { "3 stacks", "4 stacks", "5 stacks", "It varies" },
                CorrectIndex = 2,
                Explanation = "Enshroud grants exactly 5 Lemure Shroud stacks, which you consume with Void/Cross Reaping.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_4.q2",
                ConceptId = RprConcepts.VoidShroud,
                Scenario = "You used Void Reaping during Enshroud.",
                Question = "What do you gain besides damage?",
                Options = new[] { "Soul Gauge", "Shroud Gauge", "1 Void Shroud stack", "Nothing else" },
                CorrectIndex = 2,
                Explanation = "Each Void Reaping and Cross Reaping grants 1 Void Shroud stack, which is used for Lemure's Slice oGCDs.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_4.q3",
                ConceptId = RprConcepts.VoidReaping,
                Scenario = "You just used Void Reaping during Enshroud.",
                Question = "What should you use next for maximum damage?",
                Options = new[] { "Another Void Reaping", "Cross Reaping", "Communio", "Lemure's Slice" },
                CorrectIndex = 1,
                Explanation = "Void Reaping grants Enhanced Cross Reaping. Alternate between them for the Enhanced damage bonus.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_4.q4",
                ConceptId = RprConcepts.GrimReaping,
                Scenario = "You're in Enshroud with 5 enemies grouped together.",
                Question = "Which GCD should you use?",
                Options = new[] { "Void Reaping", "Cross Reaping", "Grim Reaping", "Exit Enshroud" },
                CorrectIndex = 2,
                Explanation = "Grim Reaping is the AoE version of Void/Cross Reaping. Use it at 3+ targets during Enshroud.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_4.q5",
                ConceptId = RprConcepts.LemureShroud,
                Scenario = "You're in Enshroud with 2 Lemure Shroud remaining.",
                Question = "How many more GCDs can you use before Communio?",
                Options = new[] { "0 - use Communio now", "1 more Void/Cross Reaping", "2 more Void/Cross Reaping", "As many as you want" },
                CorrectIndex = 1,
                Explanation = "Communio should be used when you have 1 Lemure Shroud remaining. At 2 stacks, use one more Void/Cross Reaping first.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "rpr.lesson_5.quiz",
        LessonId = "rpr.lesson_5",
        Title = "Quiz: Enshroud Finishers",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_5.q1",
                ConceptId = RprConcepts.Communio,
                Scenario = "You have 1 Lemure Shroud stack remaining during Enshroud.",
                Question = "What should you do?",
                Options = new[] { "Use Void Reaping", "Use Cross Reaping", "Use Communio", "Exit Enshroud" },
                CorrectIndex = 2,
                Explanation = "Communio should be used when you have 1 Lemure Shroud remaining. It's your high-potency finisher.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_5.q2",
                ConceptId = RprConcepts.Perfectio,
                Scenario = "You just used Communio.",
                Question = "What proc did you receive?",
                Options = new[] { "Soul Reaver", "Enhanced Gibbet", "Perfectio Ready", "Nothing" },
                CorrectIndex = 2,
                Explanation = "Communio grants Perfectio Ready, allowing you to use the powerful Perfectio follow-up attack.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_5.q3",
                ConceptId = RprConcepts.LemuresSlice,
                Scenario = "You have 4 Void Shroud stacks during Enshroud.",
                Question = "How many Lemure's Slice can you use?",
                Options = new[] { "1", "2", "4", "Unlimited" },
                CorrectIndex = 1,
                Explanation = "Lemure's Slice costs 2 Void Shroud each. With 4 stacks, you can use it twice (2 + 2 = 4).",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_5.q4",
                ConceptId = RprConcepts.Sacrificium,
                Scenario = "You're level 92 and have used all your Void Shroud on Lemure's Slice.",
                Question = "What additional finisher ability is available?",
                Options = new[] { "Second Communio", "Sacrificium", "Plentiful Harvest", "Nothing" },
                CorrectIndex = 1,
                Explanation = "Sacrificium (Lv.92) is an additional oGCD finisher that becomes available after spending Void Shroud during Enshroud.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_5.q5",
                ConceptId = RprConcepts.Communio,
                Scenario = "You have 3 Lemure Shroud remaining but Enshroud is about to expire.",
                Question = "What should you do?",
                Options = new[] { "Try to use all 3 stacks", "Use Communio immediately", "Let Enshroud expire", "Exit with Harvest Moon" },
                CorrectIndex = 1,
                Explanation = "If Enshroud is expiring, use Communio to get the finisher damage rather than losing it entirely. Some damage is better than none.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "rpr.lesson_6.quiz",
        LessonId = "rpr.lesson_6",
        Title = "Quiz: Party Buff Coordination",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_6.q1",
                ConceptId = RprConcepts.ArcaneCircle,
                Scenario = "A fight just started. Your party has multiple raid buff jobs.",
                Question = "When should you use Arcane Circle?",
                Options = new[] { "Immediately at pull", "Wait 30 seconds", "Only during Enshroud", "Save for emergencies" },
                CorrectIndex = 0,
                Explanation = "Use Arcane Circle at the start of the fight to align with other party raid buffs. It's a 2-minute cooldown.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_6.q2",
                ConceptId = RprConcepts.ImmortalSacrifice,
                Scenario = "Arcane Circle is active. All 8 party members are dealing damage.",
                Question = "How many Immortal Sacrifice stacks can you gain?",
                Options = new[] { "4 stacks", "8 stacks", "Unlimited", "Depends on damage" },
                CorrectIndex = 1,
                Explanation = "Each party member under Circle of Sacrifice can grant you 1 Immortal Sacrifice stack, for a maximum of 8 stacks.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_6.q3",
                ConceptId = RprConcepts.PlentifulHarvest,
                Scenario = "You have 8 Immortal Sacrifice stacks.",
                Question = "How much Shroud Gauge does Plentiful Harvest grant?",
                Options = new[] { "0 Shroud", "25 Shroud", "50 Shroud", "100 Shroud" },
                CorrectIndex = 2,
                Explanation = "Plentiful Harvest consumes Immortal Sacrifice stacks and grants 50 Shroud Gauge, enabling quick Enshroud entry.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_6.q4",
                ConceptId = RprConcepts.ArcaneCircle,
                Scenario = "It's been 2 minutes since the fight started. Arcane Circle is ready.",
                Question = "What should you consider before using Arcane Circle?",
                Options = new[] { "Nothing - use immediately", "Check if party buffs are ready", "Wait for Enshroud", "Save for next phase" },
                CorrectIndex = 1,
                Explanation = "Arcane Circle should align with party 2-minute buffs. Check if other raid buffs are being used for maximum benefit.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_6.q5",
                ConceptId = RprConcepts.PlentifulHarvest,
                Scenario = "Arcane Circle just ended. You have Immortal Sacrifice stacks.",
                Question = "When should you use Plentiful Harvest?",
                Options = new[] { "Immediately", "Wait for next Arcane Circle", "During Enshroud only", "Never - save stacks" },
                CorrectIndex = 0,
                Explanation = "Use Plentiful Harvest before the Immortal Sacrifice buff expires. The Shroud gained helps enter Enshroud during buff windows.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "rpr.lesson_7.quiz",
        LessonId = "rpr.lesson_7",
        Title = "Quiz: AoE & Movement",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_7.q1",
                ConceptId = RprConcepts.AoeRotation,
                Scenario = "You're facing 4 enemies in a dungeon.",
                Question = "Which combo should you use?",
                Options = new[] { "Slice → Waxing Slice → Infernal Slice", "Spinning Scythe → Nightmare Scythe", "Shadow of Death spam", "Single-target for focus damage" },
                CorrectIndex = 1,
                Explanation = "At 3+ targets, use the AoE combo (Spinning Scythe → Nightmare Scythe) for more total damage.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_7.q2",
                ConceptId = RprConcepts.HarvestMoon,
                Scenario = "The boss jumped away and you can't reach it. You have Harvest Moon ready.",
                Question = "What should you do?",
                Options = new[] { "Wait for boss to return", "Use Harvest Moon", "Use Shadow of Death", "Do nothing" },
                CorrectIndex = 1,
                Explanation = "Harvest Moon is a ranged GCD perfect for moments when you can't reach the boss. Use it during disengagement.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_7.q3",
                ConceptId = RprConcepts.HarvestMoon,
                Scenario = "You're waiting for a dungeon pull. Harvest Moon is not ready.",
                Question = "What should you do?",
                Options = new[] { "Just wait", "Use Soulsow to prepare Harvest Moon", "Start with Slice", "Use Enshroud" },
                CorrectIndex = 1,
                Explanation = "Soulsow prepares Harvest Moon (5s cast, usable out of combat). Always enter fights with Harvest Moon ready.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_7.q4",
                ConceptId = RprConcepts.AoeRotation,
                Scenario = "You're fighting 5 enemies. Death's Design isn't on any of them.",
                Question = "What should you do first?",
                Options = new[] { "Start AoE combo immediately", "Apply Whorl of Death", "Apply Shadow of Death to each", "Focus one target down" },
                CorrectIndex = 1,
                Explanation = "Whorl of Death applies Death's Design to all enemies in range. Apply it first for the +10% damage on all subsequent attacks.",
            },
            new QuizQuestion
            {
                QuestionId = "rpr.lesson_7.q5",
                ConceptId = RprConcepts.AoeRotation,
                Scenario = "You have 2 enemies. One is nearly dead.",
                Question = "Which rotation should you use?",
                Options = new[] { "AoE - always at 2+ targets", "Single-target on the low HP enemy", "Single-target - AoE is 3+", "Doesn't matter" },
                CorrectIndex = 2,
                Explanation = "Reaper's AoE threshold is 3+ targets. At 2 enemies, single-target rotation is more efficient.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// VPR (Echidna) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class VprQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "vpr.lesson_1.quiz",
        LessonId = "vpr.lesson_1",
        Title = "Quiz: Viper Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_1.q1",
                ConceptId = VprConcepts.ComboBasics,
                Scenario = "You just used Steel Fangs and completed the combo path.",
                Question = "What buff did you receive?",
                Options = new[] { "Swiftscaled (+15% auto-attack)", "Hunter's Instinct (+10% damage)", "Both buffs", "No buff until finisher" },
                CorrectIndex = 1,
                Explanation = "The Steel Fangs path grants Hunter's Instinct (+10% damage buff). The Reaving Fangs path grants Swiftscaled.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_1.q2",
                ConceptId = VprConcepts.BuffCycling,
                Scenario = "Hunter's Instinct has 10 seconds remaining. Swiftscaled has 35 seconds.",
                Question = "Which combo path should you start?",
                Options = new[] { "Reaving Fangs path", "Steel Fangs path", "Either one - doesn't matter", "Skip combo, use Vicewinder" },
                CorrectIndex = 1,
                Explanation = "Hunter's Instinct is about to fall off (10s). Start Steel Fangs path to refresh it before it expires.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_1.q3",
                ConceptId = VprConcepts.HonedBuffs,
                Scenario = "You have the Honed Steel buff. What does this do?",
                Question = "What effect does Honed Steel provide?",
                Options = new[] { "Increases damage of next attack", "Upgrades Steel Fangs to Honed Steel Fangs", "Grants extra Serpent Offering", "Extends Hunter's Instinct" },
                CorrectIndex = 1,
                Explanation = "Honed Steel upgrades your next Steel Fangs into Honed Steel Fangs, which deals more damage.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_1.q4",
                ConceptId = VprConcepts.ComboBasics,
                Scenario = "You want to maintain both Hunter's Instinct and Swiftscaled.",
                Question = "What is the correct approach?",
                Options = new[] { "Only use Steel Fangs path", "Only use Reaving Fangs path", "Alternate between both paths", "Use whichever is available" },
                CorrectIndex = 2,
                Explanation = "Alternate between Steel Fangs and Reaving Fangs paths to maintain both Hunter's Instinct and Swiftscaled buffs.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_1.q5",
                ConceptId = VprConcepts.BuffCycling,
                Scenario = "Both Hunter's Instinct and Swiftscaled have 40 seconds remaining.",
                Question = "What should you do?",
                Options = new[] { "Refresh Hunter's Instinct immediately", "Refresh Swiftscaled immediately", "Continue normal rotation", "Wait until one is low" },
                CorrectIndex = 2,
                Explanation = "Both buffs are healthy at 40s. Continue your normal rotation - you'll naturally refresh them before they expire.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "vpr.lesson_2.quiz",
        LessonId = "vpr.lesson_2",
        Title = "Quiz: Resource Management",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_2.q1",
                ConceptId = VprConcepts.SerpentOffering,
                Scenario = "You have 45 Serpent Offering and just completed a combo finisher.",
                Question = "How much Serpent Offering do you now have?",
                Options = new[] { "45 (finishers don't grant gauge)", "50 Serpent Offering", "55 Serpent Offering", "100 Serpent Offering" },
                CorrectIndex = 2,
                Explanation = "Combo finishers grant 10 Serpent Offering each. 45 + 10 = 55 Serpent Offering.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_2.q2",
                ConceptId = VprConcepts.RattlingCoil,
                Scenario = "You have 3 Rattling Coil stacks. A Twinblade combo is available.",
                Question = "What should you do?",
                Options = new[] { "Start Twinblade combo immediately", "Use Uncoiled Fury first to avoid overcapping", "Save Rattling Coils for later", "Wait for Serpent's Ire" },
                CorrectIndex = 1,
                Explanation = "At max Rattling Coils (3), use Uncoiled Fury before gaining more from Twinblade combos to avoid overcapping.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_2.q3",
                ConceptId = VprConcepts.OfferingGeneration,
                Scenario = "You have 95 Serpent Offering. You need to do another combo finisher.",
                Question = "What should you do?",
                Options = new[] { "Complete the combo anyway", "Enter Reawaken first", "Skip the finisher", "Use Uncoiled Fury" },
                CorrectIndex = 1,
                Explanation = "At 95 Serpent Offering, completing a finisher would overcap. Enter Reawaken (costs 50) to spend gauge first.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_2.q4",
                ConceptId = VprConcepts.RattlingCoil,
                Scenario = "The boss just jumped away. You have 2 Rattling Coil stacks.",
                Question = "What should you do?",
                Options = new[] { "Wait for boss to return", "Use Uncoiled Fury", "Use Writhing Snap", "Do nothing" },
                CorrectIndex = 1,
                Explanation = "Uncoiled Fury is a ranged GCD that spends Rattling Coils. Use it during forced disengagement to maintain DPS.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_2.q5",
                ConceptId = VprConcepts.SerpentOffering,
                Scenario = "You have 50 Serpent Offering. Reawaken is ready.",
                Question = "Can you use Reawaken?",
                Options = new[] { "No - need 100 Serpent Offering", "Yes - 50 is the minimum requirement", "Only with Serpent's Ire buff", "Only if Noxious Gnash is active" },
                CorrectIndex = 3,
                Explanation = "Reawaken requires 50+ Serpent Offering AND Noxious Gnash active on the target. Both conditions must be met.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "vpr.lesson_3.quiz",
        LessonId = "vpr.lesson_3",
        Title = "Quiz: Venom & Positionals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_3.q1",
                ConceptId = VprConcepts.VenomSystem,
                Scenario = "You have the Flankstung Venom buff active.",
                Question = "Where should you position yourself for the next finisher?",
                Options = new[] { "Behind the boss (rear)", "Beside the boss (flank)", "In front of the boss", "Anywhere - venom ignores positionals" },
                CorrectIndex = 1,
                Explanation = "Flankstung indicates FLANK positional. Position beside the boss for the positional bonus.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_3.q2",
                ConceptId = VprConcepts.PositionalFinishers,
                Scenario = "You have Hindsbane Venom. You're standing behind the boss.",
                Question = "Which finisher should you use?",
                Options = new[] { "Flanksbane Fang", "Hindsbane Fang", "Either one - both work", "Wait for different venom" },
                CorrectIndex = 1,
                Explanation = "Hindsbane indicates REAR (hind) positional. Use Hindsbane Fang from behind for the positional bonus.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_3.q3",
                ConceptId = VprConcepts.TrueNorthUsage,
                Scenario = "A mechanic forces you to stand in front of the boss. You have Flankstung Venom and True North available.",
                Question = "What should you do?",
                Options = new[] { "Skip the finisher", "Use True North and do the finisher", "Wait for mechanic to end", "Use a different attack" },
                CorrectIndex = 1,
                Explanation = "True North removes positional requirements for 10s. Use it to hit your finisher without losing damage.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_3.q4",
                ConceptId = VprConcepts.VenomSystem,
                Scenario = "You used Flanksting Strike. What venom buff do you now have?",
                Question = "Which venom buff is active?",
                Options = new[] { "Flankstung Venom", "Hindstung Venom", "Flanksbane Venom", "Hindsbane Venom" },
                CorrectIndex = 1,
                Explanation = "Flanksting Strike grants Hindstung Venom, directing you to use a rear positional next.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_3.q5",
                ConceptId = VprConcepts.Positionals,
                Scenario = "The boss is targeting you (cannot get behind). You have Hindstung Venom and no True North.",
                Question = "What should you do?",
                Options = new[] { "Wait for boss to turn away", "Use the finisher anyway (lose positional)", "Skip your rotation", "Only use Vicewinder" },
                CorrectIndex = 1,
                Explanation = "Using the finisher without positional is better than not using it at all. You lose some potency but maintain rotation flow.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "vpr.lesson_4.quiz",
        LessonId = "vpr.lesson_4",
        Title = "Quiz: Twinblade Combos",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_4.q1",
                ConceptId = VprConcepts.Vicewinder,
                Scenario = "You use Vicewinder on a boss.",
                Question = "What debuff does Vicewinder apply?",
                Options = new[] { "Death's Design", "Noxious Gnash (+10% damage)", "Venomous Bite", "No debuff" },
                CorrectIndex = 1,
                Explanation = "Vicewinder applies Noxious Gnash, a debuff that increases damage dealt to the target by 10%.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_4.q2",
                ConceptId = VprConcepts.TwinfangTwinblood,
                Scenario = "You just used Hunter's Coil during a Twinblade combo.",
                Question = "What should you do next?",
                Options = new[] { "Use Swiftskin's Coil", "Use Twinfang oGCD", "Use Vicewinder again", "Wait for GCD" },
                CorrectIndex = 1,
                Explanation = "Hunter's Coil grants a Twinfang proc. Weave Twinfang immediately as a free oGCD before your next GCD.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_4.q3",
                ConceptId = VprConcepts.NoxiousGnash,
                Scenario = "Noxious Gnash has 5 seconds remaining on the boss. You have 55 Serpent Offering.",
                Question = "Can you enter Reawaken?",
                Options = new[] { "Yes - you have enough Offering", "No - Noxious Gnash is too low", "Yes - Reawaken refreshes it", "Only with True North" },
                CorrectIndex = 1,
                Explanation = "Reawaken requires Noxious Gnash to be active. At 5s, it may fall off during Reawaken. Refresh it first with Vicewinder.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_4.q4",
                ConceptId = VprConcepts.DreadCombo,
                Scenario = "You're behind the boss during a Twinblade combo. Which Coil should you use?",
                Question = "Which ability is correct for rear positioning?",
                Options = new[] { "Hunter's Coil (flank)", "Swiftskin's Coil (rear)", "Either one", "Depends on venom" },
                CorrectIndex = 1,
                Explanation = "Swiftskin's Coil has a rear positional. Use it when behind the boss for bonus potency.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_4.q5",
                ConceptId = VprConcepts.Vicewinder,
                Scenario = "You completed a Twinblade combo (Vicewinder → Coil → Coil).",
                Question = "How many Rattling Coil stacks did you gain?",
                Options = new[] { "0 stacks", "1 stack", "2 stacks", "3 stacks" },
                CorrectIndex = 1,
                Explanation = "Each complete Twinblade combo grants 1 Rattling Coil stack for use with Uncoiled Fury.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "vpr.lesson_5.quiz",
        LessonId = "vpr.lesson_5",
        Title = "Quiz: Reawaken Burst",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_5.q1",
                ConceptId = VprConcepts.ReawakenEntry,
                Scenario = "You have 50 Serpent Offering, Noxious Gnash on target, but Hunter's Instinct has 8 seconds left.",
                Question = "Should you enter Reawaken?",
                Options = new[] { "Yes - all requirements met", "No - buff duration too low", "Yes - Reawaken extends buffs", "Only if Serpent's Ire is ready" },
                CorrectIndex = 1,
                Explanation = "Reawaken requires 10+ seconds on buffs. At 8s, Hunter's Instinct may fall off mid-burst. Refresh buffs first.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_5.q2",
                ConceptId = VprConcepts.GenerationSequence,
                Scenario = "You just entered Reawaken.",
                Question = "What is the correct GCD sequence?",
                Options = new[] { "Fourth → Third → Second → First → Ouroboros", "First → Second → Third → Fourth → Ouroboros", "Any order works", "Ouroboros → First → Second → Third → Fourth" },
                CorrectIndex = 1,
                Explanation = "The Generation sequence must go in order: First → Second → Third → Fourth → Ouroboros (finisher).",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_5.q3",
                ConceptId = VprConcepts.LegacyWeaving,
                Scenario = "You just used First Generation during Reawaken.",
                Question = "What should you weave before Second Generation?",
                Options = new[] { "Nothing - just use Second Generation", "First Legacy oGCD", "Twinfang", "Serpent's Ire" },
                CorrectIndex = 1,
                Explanation = "Each Generation GCD grants a corresponding Legacy oGCD. Weave First Legacy after First Generation.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_5.q4",
                ConceptId = VprConcepts.AnguineTribute,
                Scenario = "You entered Reawaken. How many Anguine Tribute stacks do you have?",
                Question = "What is your starting Anguine Tribute count?",
                Options = new[] { "3 stacks", "4 stacks", "5 stacks", "Depends on Serpent Offering" },
                CorrectIndex = 2,
                Explanation = "Reawaken always grants exactly 5 Anguine Tribute stacks, regardless of how much Serpent Offering you spent.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_5.q5",
                ConceptId = VprConcepts.GenerationSequence,
                Scenario = "You have 1 Anguine Tribute remaining during Reawaken.",
                Question = "What should you use?",
                Options = new[] { "First Generation again", "Any Generation GCD", "Ouroboros", "Exit Reawaken" },
                CorrectIndex = 2,
                Explanation = "At 1 Anguine Tribute, use Ouroboros as your finisher to complete the Reawaken phase.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "vpr.lesson_6.quiz",
        LessonId = "vpr.lesson_6",
        Title = "Quiz: Burst Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_6.q1",
                ConceptId = VprConcepts.SerpentsIre,
                Scenario = "Serpent's Ire is ready. Party raid buffs are going out.",
                Question = "What does Serpent's Ire grant?",
                Options = new[] { "50 Serpent Offering", "Ready to Reawaken + 1 Rattling Coil", "Extends Noxious Gnash", "Resets all cooldowns" },
                CorrectIndex = 1,
                Explanation = "Serpent's Ire grants Ready to Reawaken (free Reawaken) and 1 Rattling Coil stack.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_6.q2",
                ConceptId = VprConcepts.ReadyToReawaken,
                Scenario = "You have Ready to Reawaken buff. You also have 80 Serpent Offering.",
                Question = "What happens when you use Reawaken?",
                Options = new[] { "Spends 50 Offering + uses buff", "Only uses Ready to Reawaken (keeps Offering)", "Spends all 80 Offering", "Cannot use - must spend Offering first" },
                CorrectIndex = 1,
                Explanation = "Ready to Reawaken allows a FREE Reawaken without spending Serpent Offering. Your 80 Offering is preserved.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_6.q3",
                ConceptId = VprConcepts.BurstWindow,
                Scenario = "It's been 1:55 into the fight. Serpent's Ire is ready. No party buffs are visible yet.",
                Question = "What should you do?",
                Options = new[] { "Use Serpent's Ire immediately", "Wait a few seconds for 2-minute party buffs", "Save it for next window", "Use Reawaken without Serpent's Ire" },
                CorrectIndex = 1,
                Explanation = "Party 2-minute buffs align around 2:00. Wait a few seconds to use Serpent's Ire with party buff windows.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_6.q4",
                ConceptId = VprConcepts.TimelineAwareness,
                Scenario = "The boss will become untargetable in 15 seconds. You have 50 Serpent Offering.",
                Question = "Should you enter Reawaken now?",
                Options = new[] { "Yes - complete burst before phase transition", "No - save for after transition", "Only if Serpent's Ire is ready", "Use Uncoiled Fury instead" },
                CorrectIndex = 0,
                Explanation = "Reawaken burst takes ~12-13 seconds. Enter now to complete it before the boss becomes untargetable.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_6.q5",
                ConceptId = VprConcepts.SerpentsIre,
                Scenario = "Serpent's Ire has 10 seconds on cooldown. Raid buffs just went out.",
                Question = "Should you Reawaken now or wait for Serpent's Ire?",
                Options = new[] { "Wait for Serpent's Ire", "Reawaken now - catch the raid buffs", "Use Twinblades instead", "It doesn't matter" },
                CorrectIndex = 1,
                Explanation = "Raid buffs are more important than Ready to Reawaken. Use your regular Reawaken now to catch party buffs.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "vpr.lesson_7.quiz",
        LessonId = "vpr.lesson_7",
        Title = "Quiz: Complete Rotation",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_7.q1",
                ConceptId = VprConcepts.AoeRotation,
                Scenario = "You're facing 4 enemies in a dungeon.",
                Question = "Which combo should you use?",
                Options = new[] { "Steel Fangs → finisher", "Steel Maw → Reaving Maw", "Single-target for focus", "Only Vicewinder" },
                CorrectIndex = 1,
                Explanation = "At 3+ targets, use the AoE combo (Steel Maw → Reaving Maw path) for more total damage.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_7.q2",
                ConceptId = VprConcepts.DualWieldAoe,
                Scenario = "You want to apply Noxious Gnash to multiple enemies.",
                Question = "Which ability should you use?",
                Options = new[] { "Vicewinder on each enemy", "Vicepit (AoE Vicewinder)", "Shadow of Death", "Whorl of Death" },
                CorrectIndex = 1,
                Explanation = "Vicepit is the AoE version of Vicewinder - it applies Noxious Gnash to all enemies hit.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_7.q3",
                ConceptId = VprConcepts.UncoiledFury,
                Scenario = "You have 2 Rattling Coils. The boss is in melee range.",
                Question = "Should you use Uncoiled Fury?",
                Options = new[] { "Yes - always use on cooldown", "No - save for movement", "Yes - prevents overcapping", "Only during Reawaken" },
                CorrectIndex = 1,
                Explanation = "In melee range, save Rattling Coils for movement phases. Uncoiled Fury is a DPS loss if you could use melee GCDs.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_7.q4",
                ConceptId = VprConcepts.AoeRotation,
                Scenario = "You have 2 enemies. One is at 80% HP, one is at 20% HP.",
                Question = "Which rotation should you use?",
                Options = new[] { "AoE rotation on both", "Single-target on low HP enemy", "Single-target - AoE is 3+ targets", "Focus the high HP enemy" },
                CorrectIndex = 2,
                Explanation = "Viper's AoE threshold is 3+ targets. At 2 enemies, single-target rotation is more efficient.",
            },
            new QuizQuestion
            {
                QuestionId = "vpr.lesson_7.q5",
                ConceptId = VprConcepts.UncoiledFury,
                Scenario = "The boss dashed away. You have 0 Rattling Coils and Writhing Snap available.",
                Question = "What should you do?",
                Options = new[] { "Wait for boss to return", "Use Writhing Snap", "Use Sprint to close gap", "Do nothing" },
                CorrectIndex = 1,
                Explanation = "Writhing Snap is your ranged filler when you have no Rattling Coils. Use it during forced disengagement.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// MCH (Prometheus) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class MchQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "mch.lesson_1.quiz",
        LessonId = "mch.lesson_1",
        Title = "Quiz: Machinist Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mch.lesson_1.q1",
                ConceptId = MchConcepts.HeatGauge,
                Scenario = "You complete a Heated Clean Shot combo finisher.",
                Question = "What resource does this primarily build?",
                Options = new[] { "Only Heat", "Only Battery", "Both Heat and Battery", "Neither - finishers don't build gauge" },
                CorrectIndex = 2,
                Explanation = "Combo finishers build both Heat (from the weapon skill) and Battery (+10 from Heated Clean Shot specifically).",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_1.q2",
                ConceptId = MchConcepts.BatteryGauge,
                Scenario = "Your Battery Gauge is at 95. You're about to use Air Anchor.",
                Question = "What should you do first?",
                Options = new[] { "Use Air Anchor immediately", "Summon Automaton Queen first", "Skip Air Anchor", "Wait for Battery to hit 100" },
                CorrectIndex = 1,
                Explanation = "Air Anchor grants +20 Battery. At 95, using it would overcap. Summon Queen first to spend the Battery.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_1.q3",
                ConceptId = MchConcepts.GaugeInteractions,
                Scenario = "You want to use both Hypercharge and Automaton Queen.",
                Question = "Which gauges do these abilities consume?",
                Options = new[] { "Both use Heat", "Both use Battery", "Hypercharge uses Heat, Queen uses Battery", "Hypercharge uses Battery, Queen uses Heat" },
                CorrectIndex = 2,
                Explanation = "Hypercharge costs 50 Heat. Automaton Queen costs 50-100 Battery. They use separate gauges.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_1.q4",
                ConceptId = MchConcepts.HeatGauge,
                Scenario = "Your Heat Gauge is at 100.",
                Question = "What is the problem with this situation?",
                Options = new[] { "Nothing - full gauge is good", "You're losing potential Heat generation", "Queen will automatically trigger", "Hypercharge will fail" },
                CorrectIndex = 1,
                Explanation = "At 100 Heat, any Heat you would generate from combos is wasted. Use Hypercharge to spend Heat before capping.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_1.q5",
                ConceptId = MchConcepts.GaugeOvercapping,
                Scenario = "Heat is at 85, Battery is at 90. Drill and Air Anchor are both available.",
                Question = "What's the optimal order?",
                Options = new[] { "Drill → Air Anchor", "Air Anchor → Drill", "Hypercharge first → then tools", "Summon Queen → Air Anchor → Drill" },
                CorrectIndex = 3,
                Explanation = "At 90 Battery, Air Anchor (+20) would overcap. Summon Queen first, then Air Anchor safely, then Drill.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "mch.lesson_2.quiz",
        LessonId = "mch.lesson_2",
        Title = "Quiz: Tool Mastery",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mch.lesson_2.q1",
                ConceptId = MchConcepts.DrillPriority,
                Scenario = "Drill, Air Anchor, and Chain Saw are all available. Single target boss.",
                Question = "Which has the highest potency priority?",
                Options = new[] { "Air Anchor (best Battery)", "Chain Saw (longest cooldown)", "Drill (highest potency)", "All equal - use any" },
                CorrectIndex = 2,
                Explanation = "Drill has the highest base potency among tools. At level 98+, it has 2 charges making it extra valuable.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_2.q2",
                ConceptId = MchConcepts.ChainSawUsage,
                Scenario = "You just used Chain Saw.",
                Question = "What proc do you have?",
                Options = new[] { "Full Metal Machinist", "Excavator Ready", "Overheated", "Reassembled" },
                CorrectIndex = 1,
                Explanation = "Chain Saw grants Excavator Ready, allowing you to use Excavator as a follow-up high-potency GCD.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_2.q3",
                ConceptId = MchConcepts.AirAnchorUsage,
                Scenario = "Air Anchor is ready. Your Battery is at 40.",
                Question = "What additional resource does Air Anchor provide?",
                Options = new[] { "+10 Battery", "+20 Battery", "+50 Heat", "No additional resource" },
                CorrectIndex = 1,
                Explanation = "Air Anchor grants +20 Battery on hit, making it valuable for Automaton Queen generation.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_2.q4",
                ConceptId = MchConcepts.ProcTracking,
                Scenario = "You have Excavator Ready buff with 5 seconds remaining.",
                Question = "What should you do?",
                Options = new[] { "Wait for better timing", "Use Excavator immediately", "Use Drill instead", "Save it for burst" },
                CorrectIndex = 1,
                Explanation = "Excavator Ready only lasts 30s. With 5s remaining, use it immediately or lose the free high-potency GCD.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_2.q5",
                ConceptId = MchConcepts.DrillPriority,
                Scenario = "Drill has 2 charges at level 98. One charge is available, the other has 15s cooldown.",
                Question = "Should you use the available Drill charge?",
                Options = new[] { "No - save for burst window", "Yes - avoid overcapping charges", "Only with Reassemble", "Wait until both charges are full" },
                CorrectIndex = 1,
                Explanation = "With Drill having 2 charges, letting one sit while the other recharges means lost potential uses over the fight.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "mch.lesson_3.quiz",
        LessonId = "mch.lesson_3",
        Title = "Quiz: Reassemble Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mch.lesson_3.q1",
                ConceptId = MchConcepts.ReassemblePriority,
                Scenario = "Reassemble is ready. Drill, Air Anchor, and Chain Saw are all available.",
                Question = "Which ability should you Reassemble?",
                Options = new[] { "Chain Saw (longest CD)", "Air Anchor (Battery bonus)", "Drill (highest potency)", "Any of them - all equal" },
                CorrectIndex = 2,
                Explanation = "Reassemble should be used on your highest potency tool. Drill has the highest base potency.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_3.q2",
                ConceptId = MchConcepts.ReassembleCharges,
                Scenario = "Reassemble has 2 charges with 55s recharge each. You have both charges available.",
                Question = "What's the risk of holding both charges?",
                Options = new[] { "Charges will expire", "You'll overcap charges as time passes", "Drill damage decreases", "No risk - hold for burst" },
                CorrectIndex = 1,
                Explanation = "With 2 charges max, holding both means the recharge timer is paused. Use charges to keep the recharge active.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_3.q3",
                ConceptId = MchConcepts.GaugeOvercapping,
                Scenario = "Heat is at 95. Drill is ready with Reassemble.",
                Question = "Should you Reassemble + Drill?",
                Options = new[] { "Yes - Drill priority is highest", "No - Hypercharge first to spend Heat", "Only during raid buffs", "Drill doesn't affect Heat" },
                CorrectIndex = 0,
                Explanation = "Drill doesn't generate Heat directly. At 95 Heat, you can safely Reassemble + Drill, then Hypercharge after.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_3.q4",
                ConceptId = MchConcepts.ReassemblePriority,
                Scenario = "Raid buffs are active. You have Reassemble and Chain Saw available. Drill has 8 seconds on cooldown.",
                Question = "Should you Reassemble Chain Saw now?",
                Options = new[] { "Yes - catch the raid buffs", "No - wait for Drill", "Yes - Chain Saw is higher priority", "Use Reassemble on Auto Crossbow" },
                CorrectIndex = 0,
                Explanation = "With raid buffs active, Reassemble + Chain Saw now is better than waiting and potentially missing buffs.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_3.q5",
                ConceptId = MchConcepts.ReassembleCharges,
                Scenario = "Burst window in 60 seconds. You have 2 Reassemble charges and Drill available.",
                Question = "What should you do?",
                Options = new[] { "Hold both for burst", "Use one now, save one for burst", "Use both now", "Reassemble doesn't matter for burst" },
                CorrectIndex = 1,
                Explanation = "Use one Reassemble now to start the recharge timer. By burst window, you'll have 1 recharged + 1 saved = 2 available.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "mch.lesson_4.quiz",
        LessonId = "mch.lesson_4",
        Title = "Quiz: Hypercharge Windows",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mch.lesson_4.q1",
                ConceptId = MchConcepts.HyperchargeActivation,
                Scenario = "Your Heat is at 50. Hypercharge is available.",
                Question = "How much Heat does Hypercharge cost?",
                Options = new[] { "25 Heat", "50 Heat", "75 Heat", "100 Heat" },
                CorrectIndex = 1,
                Explanation = "Hypercharge costs exactly 50 Heat to activate, granting the 10s Overheated status.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_4.q2",
                ConceptId = MchConcepts.HeatBlastRotation,
                Scenario = "You just activated Hypercharge and are Overheated.",
                Question = "How many Heat Blasts can you fit in the 10s window?",
                Options = new[] { "3 Heat Blasts", "4 Heat Blasts", "5 Heat Blasts", "6 Heat Blasts" },
                CorrectIndex = 2,
                Explanation = "Heat Blast has a 1.5s recast during Overheated. In 10 seconds, you can fit exactly 5 Heat Blasts.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_4.q3",
                ConceptId = MchConcepts.OgcdWeaving,
                Scenario = "You just used Heat Blast. Gauss Round and Ricochet are both available.",
                Question = "How should you weave oGCDs during Hypercharge?",
                Options = new[] { "Double weave both", "Single weave one at a time", "Save them for after Hypercharge", "Don't weave - just Heat Blast" },
                CorrectIndex = 1,
                Explanation = "Heat Blast's 1.5s recast only allows single weaving. Alternate Gauss Round and Ricochet between Heat Blasts.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_4.q4",
                ConceptId = MchConcepts.OverheatedState,
                Scenario = "You're in the middle of Hypercharge. Drill just came off cooldown.",
                Question = "Should you use Drill during Hypercharge?",
                Options = new[] { "Yes - Drill is higher priority", "No - continue Heat Blast spam", "Only with Reassemble", "Use it between Heat Blasts" },
                CorrectIndex = 1,
                Explanation = "During Hypercharge, Heat Blast is your GCD of choice. Using Drill would waste Heat Blast opportunities.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_4.q5",
                ConceptId = MchConcepts.HyperchargeTiming,
                Scenario = "Heat is at 50. Drill comes off cooldown in 3 seconds. Hypercharge is ready.",
                Question = "What should you do?",
                Options = new[] { "Hypercharge immediately", "Wait for Drill, use it, then Hypercharge", "Use Drill during Hypercharge", "Skip Hypercharge this cycle" },
                CorrectIndex = 1,
                Explanation = "Wait for Drill (high potency tool), use it, then Hypercharge. This avoids Drill competing with Heat Blast time.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "mch.lesson_5.quiz",
        LessonId = "mch.lesson_5",
        Title = "Quiz: Wildfire Burst",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mch.lesson_5.q1",
                ConceptId = MchConcepts.WildfirePlacement,
                Scenario = "Wildfire is ready. Heat is at 50. You're about to burst.",
                Question = "When should you apply Wildfire relative to Hypercharge?",
                Options = new[] { "After Hypercharge ends", "During Hypercharge", "Before Hypercharge", "It doesn't matter" },
                CorrectIndex = 2,
                Explanation = "Apply Wildfire BEFORE Hypercharge so its 10s duration covers all 5 Heat Blasts for maximum damage.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_5.q2",
                ConceptId = MchConcepts.WildfireAlignment,
                Scenario = "Wildfire counts weapon skills landed during its 10s duration.",
                Question = "How many hits can an optimal Wildfire capture?",
                Options = new[] { "4 hits", "5 hits (Heat Blasts only)", "6 hits (5 Heat Blasts + 1 GCD)", "10 hits" },
                CorrectIndex = 2,
                Explanation = "Optimal Wildfire: 1 GCD before Hypercharge + 5 Heat Blasts = 6 weapon skill hits for maximum explosion damage.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_5.q3",
                ConceptId = MchConcepts.HyperchargeTiming,
                Scenario = "Wildfire has 120s cooldown. Party raid buffs align at 2 minutes.",
                Question = "How do these abilities naturally align?",
                Options = new[] { "They don't - Wildfire is off by 60s", "Wildfire aligns with every other buff window", "Perfect alignment - both 2 minutes", "Wildfire is faster, use twice per window" },
                CorrectIndex = 2,
                Explanation = "Wildfire (120s) naturally aligns with 2-minute party raid buffs. Always pair them for maximum damage.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_5.q4",
                ConceptId = MchConcepts.WildfirePlacement,
                Scenario = "Wildfire is ready but your Heat is only at 30. Raid buffs just went out.",
                Question = "What should you do?",
                Options = new[] { "Skip this Wildfire window", "Wildfire without Hypercharge", "Build Heat quickly, then Wildfire + Hypercharge", "Wait for next buff window" },
                CorrectIndex = 2,
                Explanation = "Build Heat quickly with your combo. A slightly delayed Wildfire + Hypercharge in buffs is better than skipping entirely.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_5.q5",
                ConceptId = MchConcepts.WildfireAlignment,
                Scenario = "The boss will jump away in 8 seconds. Wildfire and Hypercharge are ready.",
                Question = "Should you burst now?",
                Options = new[] { "No - not enough time for full window", "Yes - you have just enough time", "Only Wildfire, skip Hypercharge", "Save for after the jump" },
                CorrectIndex = 1,
                Explanation = "Hypercharge window is ~8s (5 Heat Blasts at 1.5s each). You have just enough time to complete the burst.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "mch.lesson_6.quiz",
        LessonId = "mch.lesson_6",
        Title = "Quiz: Queen Management",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mch.lesson_6.q1",
                ConceptId = MchConcepts.QueenSummoning,
                Scenario = "Your Battery is at 60. Raid buffs are going out.",
                Question = "Should you summon Queen now or wait for 100 Battery?",
                Options = new[] { "Summon now - catch raid buffs", "Wait for 100 - maximize Queen damage", "Never summon below 90", "Battery doesn't affect Queen damage" },
                CorrectIndex = 0,
                Explanation = "Queen damage scales with Battery, but catching raid buffs at 60 Battery often beats a 100 Battery Queen outside buffs.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_6.q2",
                ConceptId = MchConcepts.QueenDamageScaling,
                Scenario = "You summon Automaton Queen at 50 Battery vs 100 Battery.",
                Question = "How does the damage compare?",
                Options = new[] { "Same damage regardless", "50 Battery Queen deals half damage", "50 Battery Queen deals ~60% damage", "50 Battery Queen deals no damage" },
                CorrectIndex = 1,
                Explanation = "Queen damage scales linearly with Battery spent. 50 Battery = 50% of maximum Queen damage.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_6.q3",
                ConceptId = MchConcepts.BatteryAccumulation,
                Scenario = "You have 100 Battery. No raid buffs are coming for 30 seconds.",
                Question = "What should you do?",
                Options = new[] { "Wait for raid buffs", "Summon Queen immediately", "Only use Queen during burst", "Let Battery overcap - it's fine" },
                CorrectIndex = 1,
                Explanation = "At 100 Battery, any further Battery generation is wasted. Summon Queen to resume generating Battery.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_6.q4",
                ConceptId = MchConcepts.QueenSummoning,
                Scenario = "Queen Overdrive is available. Queen is mid-attack sequence.",
                Question = "When should you use Queen Overdrive?",
                Options = new[] { "Immediately for extra damage", "Let Queen finish naturally", "Only during raid buffs", "Queen Overdrive cancels Queen" },
                CorrectIndex = 1,
                Explanation = "Queen naturally finishes with Pile Bunker and Crowned Collider. Overdrive forces an early end - let her finish.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_6.q5",
                ConceptId = MchConcepts.BatteryAccumulation,
                Scenario = "Battery is at 90. Air Anchor is ready. Raid buffs start in 5 seconds.",
                Question = "What's the optimal play?",
                Options = new[] { "Air Anchor → Queen during buffs", "Queen now → Air Anchor", "Skip Air Anchor", "Queen now → skip Air Anchor until after buffs" },
                CorrectIndex = 0,
                Explanation = "Air Anchor (+20 Battery) caps you at 100. Then summon 100 Battery Queen when raid buffs go out for maximum damage.",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "mch.lesson_7.quiz",
        LessonId = "mch.lesson_7",
        Title = "Quiz: Advanced Tactics",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "mch.lesson_7.q1",
                ConceptId = MchConcepts.BurstPartySync,
                Scenario = "It's 1:55 into the fight. Wildfire and full resources are ready.",
                Question = "What should you do?",
                Options = new[] { "Burst immediately", "Wait ~5s for 2-minute party buffs", "Save for the next window", "Only use Queen now" },
                CorrectIndex = 1,
                Explanation = "2-minute party buffs align around 2:00. Wait a few seconds to align Wildfire + Queen with party buffs.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_7.q2",
                ConceptId = MchConcepts.AoeRotation,
                Scenario = "You're facing 4 enemies in a dungeon pull.",
                Question = "Which GCD rotation should you use?",
                Options = new[] { "Single-target combo for focus fire", "Scattergun combo + Bioblaster", "Only Heat Blast spam", "Auto Crossbow without Hypercharge" },
                CorrectIndex = 1,
                Explanation = "At 3+ targets, use the AoE rotation: Scattergun combo for gauge building, Bioblaster for DoT damage.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_7.q3",
                ConceptId = MchConcepts.TargetCountThreshold,
                Scenario = "2 enemies are present. One at 80% HP, one at 30% HP.",
                Question = "Which rotation should you use?",
                Options = new[] { "AoE rotation on both", "Single-target on low HP enemy", "Single-target on high HP enemy", "Alternate between them" },
                CorrectIndex = 1,
                Explanation = "MCH AoE threshold is 3+ targets. At 2, single-target the low HP enemy to eliminate it faster.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_7.q4",
                ConceptId = MchConcepts.PhaseAwareness,
                Scenario = "Boss becomes untargetable in 20 seconds. You have 80 Heat and 100 Battery.",
                Question = "What should you prioritize?",
                Options = new[] { "Save resources for after", "Wildfire + Hypercharge + Queen now", "Only spend Battery", "Only spend Heat" },
                CorrectIndex = 1,
                Explanation = "Dump all resources before the phase transition. Wildfire + Hypercharge uses Heat, Queen uses Battery.",
            },
            new QuizQuestion
            {
                QuestionId = "mch.lesson_7.q5",
                ConceptId = MchConcepts.InterruptUsage,
                Scenario = "An enemy is casting a dangerous ability. Head Graze is available. The cast bar is interruptible.",
                Question = "Should you interrupt?",
                Options = new[] { "Yes - use Head Graze immediately", "No - DPS loss", "Only if no one else can", "Check IPC for interrupt coordination first" },
                CorrectIndex = 3,
                Explanation = "Olympus coordinates interrupts via IPC. Check if another player already interrupted to avoid wasted Head Grazes.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}

/// <summary>
/// BRD (Calliope) quiz content - 7 quizzes with 5 questions each.
/// </summary>
public static class BrdQuizzes
{
    public static readonly QuizDefinition Lesson1Quiz = new()
    {
        QuizId = "brd.lesson_1.quiz",
        LessonId = "brd.lesson_1",
        Title = "Quiz: Bard Fundamentals",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "brd.lesson_1.q1",
                ConceptId = BrdConcepts.SongRotation,
                Scenario = "You're starting a boss fight as Bard.",
                Question = "What is the standard song rotation order?",
                Options = new[] { "Mage's Ballad → Army's Paeon → Wanderer's Minuet", "Army's Paeon → Wanderer's Minuet → Mage's Ballad", "Wanderer's Minuet → Mage's Ballad → Army's Paeon", "Any order - songs are interchangeable" },
                CorrectIndex = 2,
                Explanation = "The standard rotation is WM → MB → AP. Start with Wanderer's Minuet for the crit buff during opener burst.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_1.q2",
                ConceptId = BrdConcepts.MagesBallad,
                Scenario = "You notice Bloodletter keeps resetting during one of your songs.",
                Question = "Which song causes Bloodletter resets?",
                Options = new[] { "Wanderer's Minuet", "Mage's Ballad", "Army's Paeon", "All songs reset Bloodletter" },
                CorrectIndex = 1,
                Explanation = "Mage's Ballad has a chance to reset Bloodletter when your DoTs deal damage. This is its unique Repertoire effect.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_1.q3",
                ConceptId = BrdConcepts.WanderersMinuet,
                Scenario = "Your party is about to use their 2-minute burst cooldowns.",
                Question = "What party buff does Wanderer's Minuet provide?",
                Options = new[] { "+10% damage dealt", "+20% direct hit rate", "+2% critical hit rate", "+15% healing received" },
                CorrectIndex = 2,
                Explanation = "Wanderer's Minuet provides +2% critical hit rate to the party, making it ideal for burst windows.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_1.q4",
                ConceptId = BrdConcepts.SongRotation,
                Scenario = "Your current song has 2 seconds remaining.",
                Question = "What should you do?",
                Options = new[] { "Wait for it to expire naturally", "Switch to the next song immediately", "Use all remaining oGCDs first", "Cancel the song early for DPS" },
                CorrectIndex = 1,
                Explanation = "Switch songs when <3s remaining to avoid dead time. Waiting for natural expiry wastes GCDs.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_1.q5",
                ConceptId = BrdConcepts.ArmysPaeon,
                Scenario = "You're in Army's Paeon phase.",
                Question = "What is the party buff from Army's Paeon?",
                Options = new[] { "+2% critical hit rate", "+4% direct hit rate", "+10% damage dealt", "Reduces GCD recast time" },
                CorrectIndex = 1,
                Explanation = "Army's Paeon provides +4% direct hit rate to the party. It's the 'filler' song between WM and MB.",
            },
        },
    };

    public static readonly QuizDefinition Lesson2Quiz = new()
    {
        QuizId = "brd.lesson_2.quiz",
        LessonId = "brd.lesson_2",
        Title = "Quiz: Repertoire Mastery",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "brd.lesson_2.q1",
                ConceptId = BrdConcepts.RepertoireStacks,
                Scenario = "You're in Wanderer's Minuet phase.",
                Question = "When do Repertoire stacks generate?",
                Options = new[] { "From any weapon skill", "From DoT critical hits only", "From all damage dealt", "Automatically over time" },
                CorrectIndex = 1,
                Explanation = "During WM, Repertoire stacks generate when your DoTs deal critical damage. More crit = more stacks.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_2.q2",
                ConceptId = BrdConcepts.PitchPerfect,
                Scenario = "You have 2 Repertoire stacks and Wanderer's Minuet is about to expire.",
                Question = "What's the optimal Pitch Perfect stack count?",
                Options = new[] { "Use at 1 stack minimum", "Use at 2 stacks", "Use at 3 stacks for maximum potency", "Stacks don't affect potency" },
                CorrectIndex = 2,
                Explanation = "Pitch Perfect at 3 stacks deals 540 potency. Lower stacks deal less. Wait for 3 when possible.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_2.q3",
                ConceptId = BrdConcepts.SongSwitching,
                Scenario = "Wanderer's Minuet has 2 seconds left. You have 2 Repertoire stacks.",
                Question = "What should you do?",
                Options = new[] { "Wait for a 3rd stack", "Use Pitch Perfect now, then switch songs", "Switch songs immediately", "Let the song expire naturally" },
                CorrectIndex = 1,
                Explanation = "With <3s remaining, use PP at 2 stacks to avoid losing them. Losing 2 stacks is worse than suboptimal potency.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_2.q4",
                ConceptId = BrdConcepts.RepertoireStacks,
                Scenario = "Empyreal Arrow is available during Wanderer's Minuet.",
                Question = "What does Empyreal Arrow guarantee during WM?",
                Options = new[] { "Nothing special", "One Repertoire stack", "Three Repertoire stacks", "A Bloodletter reset" },
                CorrectIndex = 1,
                Explanation = "Empyreal Arrow guarantees one Repertoire proc during Wanderer's Minuet, regardless of crit.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_2.q5",
                ConceptId = BrdConcepts.PitchPerfect,
                Scenario = "You have 3 stacks but Wanderer's Minuet just ended.",
                Question = "What happens to your Repertoire stacks?",
                Options = new[] { "They carry over to the next song", "They convert to Soul Voice", "They're lost", "They become Bloodletter resets" },
                CorrectIndex = 2,
                Explanation = "Repertoire stacks are lost when WM ends. Always use Pitch Perfect before switching songs.",
            },
        },
    };

    public static readonly QuizDefinition Lesson3Quiz = new()
    {
        QuizId = "brd.lesson_3.quiz",
        LessonId = "brd.lesson_3",
        Title = "Quiz: Soul Voice & Apex",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "brd.lesson_3.q1",
                ConceptId = BrdConcepts.SoulVoiceGauge,
                Scenario = "You notice your Soul Voice gauge increasing during combat.",
                Question = "How does Soul Voice build?",
                Options = new[] { "From weapon skills", "From Repertoire procs across all songs", "From critical hits only", "From oGCD abilities" },
                CorrectIndex = 1,
                Explanation = "Soul Voice builds from Repertoire procs during any active song - not just Wanderer's Minuet.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_3.q2",
                ConceptId = BrdConcepts.ApexArrow,
                Scenario = "Your Soul Voice gauge is at 75.",
                Question = "What's the minimum gauge to use Apex Arrow effectively?",
                Options = new[] { "Any amount", "50 minimum", "80 minimum", "100 only" },
                CorrectIndex = 2,
                Explanation = "Apex Arrow at 80+ gauge unlocks Blast Arrow. Below 80, you miss the follow-up entirely.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_3.q3",
                ConceptId = BrdConcepts.BlastArrow,
                Scenario = "You just used Apex Arrow at 100 Soul Voice.",
                Question = "What ability becomes available?",
                Options = new[] { "Pitch Perfect", "Refulgent Arrow", "Blast Arrow", "Radiant Encore" },
                CorrectIndex = 2,
                Explanation = "Apex Arrow at 80+ grants Blast Arrow Ready, a powerful follow-up attack. Don't forget to use it!",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_3.q4",
                ConceptId = BrdConcepts.SoulVoiceOvercapping,
                Scenario = "Your Soul Voice is at 100 and continuing to fight.",
                Question = "What's the problem with this situation?",
                Options = new[] { "Nothing - save it for burst", "You're losing potential Soul Voice generation", "Apex Arrow damage decreases", "It will automatically fire" },
                CorrectIndex = 1,
                Explanation = "At 100 gauge, any Soul Voice you would generate is wasted. Use Apex before capping.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_3.q5",
                ConceptId = BrdConcepts.ApexArrow,
                Scenario = "Raid buffs are active. Your Soul Voice is at 85.",
                Question = "Should you use Apex Arrow?",
                Options = new[] { "No - wait for 100", "Yes - 80+ is enough during buffs", "No - save for next burst", "Yes - but skip Blast Arrow" },
                CorrectIndex = 1,
                Explanation = "During burst windows, using Apex at 80+ is optimal. Waiting for 100 may miss buff windows.",
            },
        },
    };

    public static readonly QuizDefinition Lesson4Quiz = new()
    {
        QuizId = "brd.lesson_4.quiz",
        LessonId = "brd.lesson_4",
        Title = "Quiz: Proc Management",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "brd.lesson_4.q1",
                ConceptId = BrdConcepts.StraightShotReady,
                Scenario = "You're using Burst Shot as your filler GCD.",
                Question = "What proc can Burst Shot grant?",
                Options = new[] { "Bloodletter Ready", "Pitch Perfect Ready", "Straight Shot Ready (Hawk's Eye)", "Resonant Arrow Ready" },
                CorrectIndex = 2,
                Explanation = "Burst Shot has a ~35% chance to grant Straight Shot Ready, which upgrades to Refulgent Arrow.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_4.q2",
                ConceptId = BrdConcepts.Barrage,
                Scenario = "Barrage is available. You have a Refulgent Arrow proc.",
                Question = "What's the optimal Barrage target?",
                Options = new[] { "Burst Shot for consistency", "Refulgent Arrow for triple damage", "Apex Arrow for maximum potency", "Iron Jaws to refresh DoTs" },
                CorrectIndex = 1,
                Explanation = "Barrage + Refulgent Arrow = triple damage (840 potency). Always pair Barrage with Refulgent.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_4.q3",
                ConceptId = BrdConcepts.RefulgentArrow,
                Scenario = "You have Straight Shot Ready with 8 seconds remaining.",
                Question = "What's the priority?",
                Options = new[] { "Use it immediately - highest priority proc", "Wait for Barrage", "Let it expire - DPS loss anyway", "Use Burst Shot instead" },
                CorrectIndex = 0,
                Explanation = "Refulgent Arrow is your highest priority proc. Never let it fall off - it's a significant DPS gain.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_4.q4",
                ConceptId = BrdConcepts.ResonantArrow,
                Scenario = "You just used Barrage + Refulgent Arrow.",
                Question = "What follow-up ability is now available?",
                Options = new[] { "Blast Arrow", "Pitch Perfect", "Resonant Arrow", "Radiant Encore" },
                CorrectIndex = 2,
                Explanation = "After using Barrage, you gain Resonant Arrow Ready - a free follow-up attack. Don't miss it!",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_4.q5",
                ConceptId = BrdConcepts.StraightShotReady,
                Scenario = "You have 25 seconds on your Straight Shot Ready proc. Barrage cooldown is 15 seconds.",
                Question = "Should you wait for Barrage?",
                Options = new[] { "Yes - always pair with Barrage", "No - use the proc now, get another before Barrage", "Yes - but only if burst is coming", "No - but hold Barrage for next proc" },
                CorrectIndex = 1,
                Explanation = "With 25s on the proc and 15s on Barrage, use the proc now. You'll likely get another before Barrage returns.",
            },
        },
    };

    public static readonly QuizDefinition Lesson5Quiz = new()
    {
        QuizId = "brd.lesson_5.quiz",
        LessonId = "brd.lesson_5",
        Title = "Quiz: DoT Optimization",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "brd.lesson_5.q1",
                ConceptId = BrdConcepts.IronJaws,
                Scenario = "Both DoTs have 5 seconds remaining.",
                Question = "What's the optimal refresh window for DoTs?",
                Options = new[] { "Refresh at 10+ seconds", "Refresh at 3-7 seconds remaining", "Refresh only at 0 seconds", "Refresh whenever Iron Jaws is available" },
                CorrectIndex = 1,
                Explanation = "Refresh DoTs at 3-7 seconds remaining. This maximizes uptime without clipping.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_5.q2",
                ConceptId = BrdConcepts.IronJaws,
                Scenario = "You need to refresh your DoTs.",
                Question = "What does Iron Jaws do?",
                Options = new[] { "Applies one DoT", "Refreshes only Caustic Bite", "Refreshes both DoTs simultaneously", "Applies new DoTs with current buffs" },
                CorrectIndex = 2,
                Explanation = "Iron Jaws refreshes both Caustic Bite and Stormbite simultaneously - never apply them separately.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_5.q3",
                ConceptId = BrdConcepts.CausticBite,
                Scenario = "You're applying DoTs to a fresh target.",
                Question = "What's the preferred application order?",
                Options = new[] { "Caustic Bite first (lower potency)", "Stormbite first (higher potency)", "Iron Jaws (applies both)", "Either order - they're the same" },
                CorrectIndex = 1,
                Explanation = "Apply Stormbite first - it has higher initial potency. Then Caustic Bite, then maintain with Iron Jaws.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_5.q4",
                ConceptId = BrdConcepts.Stormbite,
                Scenario = "Raging Strikes is active. Your DoTs have 8 seconds remaining.",
                Question = "What should you do?",
                Options = new[] { "Wait until 3-7 seconds", "Refresh now with Iron Jaws", "Reapply both DoTs manually", "Focus on other abilities" },
                CorrectIndex = 1,
                Explanation = "During Raging Strikes, refresh DoTs early with Iron Jaws. They snapshot the buff for the full 45s duration.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_5.q5",
                ConceptId = BrdConcepts.CausticBite,
                Scenario = "Your DoTs fell off during a phase transition.",
                Question = "What do you do when the boss returns?",
                Options = new[] { "Use Iron Jaws immediately", "Apply Stormbite → Caustic Bite → then Iron Jaws later", "Apply Caustic Bite → Stormbite → then Iron Jaws later", "Just use Iron Jaws for everything" },
                CorrectIndex = 1,
                Explanation = "Iron Jaws only refreshes existing DoTs. On a fresh target, manually apply Stormbite → Caustic Bite first.",
            },
        },
    };

    public static readonly QuizDefinition Lesson6Quiz = new()
    {
        QuizId = "brd.lesson_6.quiz",
        LessonId = "brd.lesson_6",
        Title = "Quiz: Burst Window",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "brd.lesson_6.q1",
                ConceptId = BrdConcepts.RagingStrikes,
                Scenario = "You're entering your 2-minute burst window.",
                Question = "What's the correct burst sequence?",
                Options = new[] { "Battle Voice → Radiant Finale → Raging Strikes", "Radiant Finale → Raging Strikes → Battle Voice", "Raging Strikes → Battle Voice → Radiant Finale", "Any order - they're all buffs" },
                CorrectIndex = 2,
                Explanation = "The sequence is Raging Strikes → Battle Voice → Radiant Finale. This ensures all buffs are active together.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_6.q2",
                ConceptId = BrdConcepts.RadiantFinale,
                Scenario = "You've used all three songs during the fight.",
                Question = "How does Radiant Finale potency scale?",
                Options = new[] { "Fixed potency regardless of Coda", "Scales with current song active", "Scales with number of Coda (1-3 songs used)", "Scales with Soul Voice gauge" },
                CorrectIndex = 2,
                Explanation = "Radiant Finale's party buff scales with the number of Coda you've collected (1 per unique song used).",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_6.q3",
                ConceptId = BrdConcepts.BattleVoice,
                Scenario = "Battle Voice is ready. Other DPS are about to use their burst.",
                Question = "What buff does Battle Voice provide?",
                Options = new[] { "+10% damage dealt", "+20% direct hit rate", "+15% critical hit rate", "+25% action speed" },
                CorrectIndex = 1,
                Explanation = "Battle Voice grants +20% direct hit rate to the party. Coordinate with other DPS burst windows.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_6.q4",
                ConceptId = BrdConcepts.RadiantFinale,
                Scenario = "You've only used one song so far this fight.",
                Question = "How many Coda do you have for Radiant Finale?",
                Options = new[] { "0 - Coda come from damage", "1 - one per unique song used", "3 - all songs grant 3", "Depends on Repertoire procs" },
                CorrectIndex = 1,
                Explanation = "You get 1 Coda per unique song used. With only one song used, you have 1 Coda for RF.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_6.q5",
                ConceptId = BrdConcepts.RadiantEncore,
                Scenario = "You just used Radiant Finale.",
                Question = "What follow-up ability becomes available?",
                Options = new[] { "Blast Arrow", "Resonant Arrow", "Radiant Encore", "Pitch Perfect" },
                CorrectIndex = 2,
                Explanation = "Radiant Finale grants Radiant Encore Ready - a free follow-up proc. Use it before it expires!",
            },
        },
    };

    public static readonly QuizDefinition Lesson7Quiz = new()
    {
        QuizId = "brd.lesson_7.quiz",
        LessonId = "brd.lesson_7",
        Title = "Quiz: Advanced Coordination",
        PassingScore = 4,
        Questions = new[]
        {
            new QuizQuestion
            {
                QuestionId = "brd.lesson_7.q1",
                ConceptId = BrdConcepts.EmpyrealArrow,
                Scenario = "Empyreal Arrow is available.",
                Question = "What's the cooldown of Empyreal Arrow?",
                Options = new[] { "30 seconds", "15 seconds", "60 seconds", "45 seconds" },
                CorrectIndex = 1,
                Explanation = "Empyreal Arrow has a 15s cooldown. Use it consistently for guaranteed Repertoire during WM.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_7.q2",
                ConceptId = BrdConcepts.BloodletterManagement,
                Scenario = "You're in Mage's Ballad phase.",
                Question = "How should you handle Bloodletter?",
                Options = new[] { "Save charges for burst", "Spam freely - it resets constantly", "Use once per GCD maximum", "Only use during procs" },
                CorrectIndex = 1,
                Explanation = "During Mage's Ballad, Bloodletter resets constantly from DoT damage. Spam it freely!",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_7.q3",
                ConceptId = BrdConcepts.PartyUtility,
                Scenario = "A raidwide is incoming. Troubadour is available.",
                Question = "When should you use Troubadour?",
                Options = new[] { "After the raidwide hits", "Before the raidwide hits", "Only when healers ask", "Save it for emergencies" },
                CorrectIndex = 1,
                Explanation = "Troubadour is a damage reduction buff. Use it BEFORE the raidwide hits to reduce incoming damage.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_7.q4",
                ConceptId = BrdConcepts.PartyUtility,
                Scenario = "An enemy cast bar is interruptible. Head Graze is available.",
                Question = "What should you check first?",
                Options = new[] { "Interrupt immediately", "Check if it's worth interrupting", "Let tanks handle it", "Check IPC for interrupt coordination" },
                CorrectIndex = 3,
                Explanation = "Olympus coordinates interrupts via IPC. Check if another player already interrupted to avoid wasting Head Graze.",
            },
            new QuizQuestion
            {
                QuestionId = "brd.lesson_7.q5",
                ConceptId = BrdConcepts.EmpyrealArrow,
                Scenario = "You have Empyreal Arrow, Bloodletter (2 charges), and Sidewinder all available.",
                Question = "What's the oGCD weaving priority?",
                Options = new[] { "Bloodletter → Sidewinder → Empyreal", "Empyreal → Sidewinder → Bloodletter", "Sidewinder → Empyreal → Bloodletter", "Use them in any order" },
                CorrectIndex = 1,
                Explanation = "Empyreal Arrow first (guarantees Repertoire), then Sidewinder (higher potency), then Bloodletter.",
            },
        },
    };

    public static readonly QuizDefinition[] AllQuizzes = new[]
    {
        Lesson1Quiz, Lesson2Quiz, Lesson3Quiz, Lesson4Quiz, Lesson5Quiz, Lesson6Quiz, Lesson7Quiz,
    };
}
