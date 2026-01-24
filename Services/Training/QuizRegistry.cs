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
        // Register all quizzes
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
            "whm" => WhmQuizzes.AllQuizzes,
            "sch" => SchQuizzes.AllQuizzes,
            "ast" => AstQuizzes.AllQuizzes,
            "sge" => SgeQuizzes.AllQuizzes,
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
