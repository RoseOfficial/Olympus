using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Ninja (NIN) and Rogue (ROG) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Hermes, the Greek god of speed and trickery.
/// </summary>
public static class NINActions
{
    #region Combo GCDs

    /// <summary>
    /// Spinning Edge - Combo starter (Lv.1)
    /// </summary>
    public static readonly ActionDefinition SpinningEdge = new()
    {
        ActionId = 2240,
        Name = "Spinning Edge",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300
    };

    /// <summary>
    /// Gust Slash - Combo step 2 (Lv.4)
    /// </summary>
    public static readonly ActionDefinition GustSlash = new()
    {
        ActionId = 2242,
        Name = "Gust Slash",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400 // Combo potency
    };

    /// <summary>
    /// Aeolian Edge - Combo finisher, rear positional (Lv.26)
    /// Consumes Kazematoi for bonus potency.
    /// </summary>
    public static readonly ActionDefinition AeolianEdge = new()
    {
        ActionId = 2255,
        Name = "Aeolian Edge",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500 // Combo potency from rear
    };

    /// <summary>
    /// Armor Crush - Combo finisher, flank positional (Lv.54)
    /// Grants Kazematoi stacks.
    /// </summary>
    public static readonly ActionDefinition ArmorCrush = new()
    {
        ActionId = 3563,
        Name = "Armor Crush",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 480 // Combo potency from flank
    };

    #endregion

    #region AoE Combo GCDs

    /// <summary>
    /// Death Blossom - AoE combo starter (Lv.38)
    /// </summary>
    public static readonly ActionDefinition DeathBlossom = new()
    {
        ActionId = 2254,
        Name = "Death Blossom",
        MinLevel = 38,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Hakke Mujinsatsu - AoE combo finisher (Lv.52)
    /// </summary>
    public static readonly ActionDefinition HakkeMujinsatsu = new()
    {
        ActionId = 16488,
        Name = "Hakke Mujinsatsu",
        MinLevel = 52,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 140 // Combo potency
    };

    #endregion

    #region Mudras

    /// <summary>
    /// Ten - Mudra input (Lv.30)
    /// First mudra in the system.
    /// </summary>
    public static readonly ActionDefinition Ten = new()
    {
        ActionId = 2259,
        Name = "Ten",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 0.5f,
        MpCost = 0
    };

    /// <summary>
    /// Chi - Mudra input (Lv.35)
    /// Second mudra in the system.
    /// </summary>
    public static readonly ActionDefinition Chi = new()
    {
        ActionId = 2261,
        Name = "Chi",
        MinLevel = 35,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 0.5f,
        MpCost = 0
    };

    /// <summary>
    /// Jin - Mudra input (Lv.45)
    /// Third mudra in the system.
    /// </summary>
    public static readonly ActionDefinition Jin = new()
    {
        ActionId = 2263,
        Name = "Jin",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 0.5f,
        MpCost = 0
    };

    #endregion

    #region Ninjutsu

    /// <summary>
    /// Ninjutsu - Execute the mudra sequence (Lv.30)
    /// The actual action used to cast the Ninjutsu after mudra inputs.
    /// </summary>
    public static readonly ActionDefinition Ninjutsu = new()
    {
        ActionId = 2260,
        Name = "Ninjutsu",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 0 // Varies based on combination
    };

    /// <summary>
    /// Fuma Shuriken - Single mudra Ninjutsu (Lv.30)
    /// Ten, Chi, or Jin alone.
    /// </summary>
    public static readonly ActionDefinition FumaShuriken = new()
    {
        ActionId = 2265,
        Name = "Fuma Shuriken",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 500
    };

    /// <summary>
    /// Raiton - Two mudra Ninjutsu: Ten-Chi or Chi-Ten (Lv.35)
    /// Primary single-target damage Ninjutsu.
    /// </summary>
    public static readonly ActionDefinition Raiton = new()
    {
        ActionId = 2267,
        Name = "Raiton",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 740
    };

    /// <summary>
    /// Katon - Two mudra AoE Ninjutsu: Chi-Ten (Lv.35)
    /// </summary>
    public static readonly ActionDefinition Katon = new()
    {
        ActionId = 2266,
        Name = "Katon",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 350
    };

    /// <summary>
    /// Hyoton - Two mudra Ninjutsu: Ten-Jin or Jin-Ten (Lv.45)
    /// Applies Bind.
    /// </summary>
    public static readonly ActionDefinition Hyoton = new()
    {
        ActionId = 2268,
        Name = "Hyoton",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 350
    };

    /// <summary>
    /// Huton - Three mudra Ninjutsu: Jin-Chi-Ten (Lv.45)
    /// Reduces GCD recast time.
    /// </summary>
    public static readonly ActionDefinition Huton = new()
    {
        ActionId = 2269,
        Name = "Huton",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1.5f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Huton,
        AppliedStatusDuration = 60f
    };

    /// <summary>
    /// Doton - Three mudra AoE DoT: Ten-Jin-Chi (Lv.45)
    /// Ground AoE that applies DoT.
    /// </summary>
    public static readonly ActionDefinition Doton = new()
    {
        ActionId = 2270,
        Name = "Doton",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.GroundAoE,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 1.5f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 80 // Per tick
    };

    /// <summary>
    /// Suiton - Three mudra Ninjutsu: Ten-Chi-Jin (Lv.45)
    /// Enables Kunai's Bane usage.
    /// </summary>
    public static readonly ActionDefinition Suiton = new()
    {
        ActionId = 2271,
        Name = "Suiton",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 580,
        AppliedStatusId = StatusIds.Suiton,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Goka Mekkyaku - Kassatsu-enhanced Katon (Lv.76)
    /// </summary>
    public static readonly ActionDefinition GokaMekkyaku = new()
    {
        ActionId = 16491,
        Name = "Goka Mekkyaku",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
    };

    /// <summary>
    /// Hyosho Ranryu - Kassatsu-enhanced Hyoton (Lv.76)
    /// </summary>
    public static readonly ActionDefinition HyoshoRanryu = new()
    {
        ActionId = 16492,
        Name = "Hyosho Ranryu",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1300
    };

    /// <summary>
    /// Rabbit Medium - Invalid mudra combination result
    /// Deals minimal damage when you mess up a mudra sequence.
    /// </summary>
    public static readonly ActionDefinition RabbitMedium = new()
    {
        ActionId = 2272,
        Name = "Rabbit Medium",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 0
    };

    #endregion

    #region Ninki Spenders (oGCD)

    /// <summary>
    /// Bhavacakra - Single target Ninki spender (Lv.68)
    /// Requires 50 Ninki.
    /// </summary>
    public static readonly ActionDefinition Bhavacakra = new()
    {
        ActionId = 7402,
        Name = "Bhavacakra",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 380
    };

    /// <summary>
    /// Hellfrog Medium - AoE Ninki spender (Lv.62)
    /// Requires 50 Ninki.
    /// </summary>
    public static readonly ActionDefinition HellfrogMedium = new()
    {
        ActionId = 7401,
        Name = "Hellfrog Medium",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 160
    };

    /// <summary>
    /// Zesho Meppo - Enhanced Bhavacakra during Meisui (Lv.96)
    /// </summary>
    public static readonly ActionDefinition ZeshoMeppo = new()
    {
        ActionId = 36960,
        Name = "Zesho Meppo",
        MinLevel = 96,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 700
    };

    /// <summary>
    /// Deathfrog Medium - Enhanced Hellfrog Medium during Meisui (Lv.96)
    /// </summary>
    public static readonly ActionDefinition DeathfrogMedium = new()
    {
        ActionId = 36961,
        Name = "Deathfrog Medium",
        MinLevel = 96,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 300
    };

    #endregion

    #region Buff Actions (oGCD)

    /// <summary>
    /// Mug - Damage + Ninki generation (Lv.15)
    /// </summary>
    public static readonly ActionDefinition Mug = new()
    {
        ActionId = 2248,
        Name = "Mug",
        MinLevel = 15,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Dokumori - Enhanced Mug (Lv.66)
    /// Replaces Mug at level 66.
    /// </summary>
    public static readonly ActionDefinition Dokumori = new()
    {
        ActionId = 36957,
        Name = "Dokumori",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300,
        AppliedStatusId = StatusIds.Dokumori,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Kunai's Bane - Vulnerability debuff (Lv.92)
    /// Requires Suiton buff.
    /// </summary>
    public static readonly ActionDefinition KunaisBane = new()
    {
        ActionId = 36958,
        Name = "Kunai's Bane",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600,
        AppliedStatusId = StatusIds.KunaisBane,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Trick Attack - Legacy vulnerability (pre-Lv.92)
    /// Replaced by Kunai's Bane at level 92.
    /// </summary>
    public static readonly ActionDefinition TrickAttack = new()
    {
        ActionId = 2258,
        Name = "Trick Attack",
        MinLevel = 18,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400,
        AppliedStatusId = StatusIds.VulnerabilityUp,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Kassatsu - Enhances next Ninjutsu (Lv.50)
    /// Makes next Ninjutsu cost no mudra charge and deal more damage.
    /// </summary>
    public static readonly ActionDefinition Kassatsu = new()
    {
        ActionId = 2264,
        Name = "Kassatsu",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Kassatsu,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Ten Chi Jin - Instant triple Ninjutsu (Lv.70)
    /// Allows using all 3 mudras in sequence.
    /// </summary>
    public static readonly ActionDefinition TenChiJin = new()
    {
        ActionId = 7403,
        Name = "Ten Chi Jin",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.TenChiJin,
        AppliedStatusDuration = 6f
    };

    /// <summary>
    /// Bunshin - Shadow clone that attacks with you (Lv.80)
    /// </summary>
    public static readonly ActionDefinition Bunshin = new()
    {
        ActionId = 16493,
        Name = "Bunshin",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Bunshin,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Phantom Kamaitachi - Bunshin follow-up (Lv.82)
    /// Granted after 5 stacks of Bunshin.
    /// </summary>
    public static readonly ActionDefinition PhantomKamaitachi = new()
    {
        ActionId = 25774,
        Name = "Phantom Kamaitachi",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
    };

    /// <summary>
    /// Meisui - Converts Suiton to Ninki (Lv.72)
    /// </summary>
    public static readonly ActionDefinition Meisui = new()
    {
        ActionId = 16489,
        Name = "Meisui",
        MinLevel = 72,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Meisui,
        AppliedStatusDuration = 30f
    };

    #endregion

    #region Dawntrail Actions

    /// <summary>
    /// Forked Raiju - Gap closer combo from Raiton (Lv.90)
    /// </summary>
    public static readonly ActionDefinition ForkedRaiju = new()
    {
        ActionId = 25777,
        Name = "Forked Raiju",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 700
    };

    /// <summary>
    /// Fleeting Raiju - Alternative Raiju (Lv.90)
    /// </summary>
    public static readonly ActionDefinition FleetingRaiju = new()
    {
        ActionId = 25778,
        Name = "Fleeting Raiju",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 700
    };

    /// <summary>
    /// Tenri Jindo - Kunai's Bane follow-up (Lv.100)
    /// </summary>
    public static readonly ActionDefinition TenriJindo = new()
    {
        ActionId = 36959,
        Name = "Tenri Jindo",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 1100
    };

    #endregion

    #region Utility Actions (oGCD)

    /// <summary>
    /// Shukuchi - Teleport to target location (Lv.40)
    /// </summary>
    public static readonly ActionDefinition Shukuchi = new()
    {
        ActionId = 2262,
        Name = "Shukuchi",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.GroundAoE,
        EffectTypes = ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 60f, // 2 charges
        Range = 20f,
        MpCost = 0
    };

    /// <summary>
    /// Shade Shift - Personal damage reduction (Lv.2)
    /// </summary>
    public static readonly ActionDefinition ShadeShift = new()
    {
        ActionId = 2241,
        Name = "Shade Shift",
        MinLevel = 2,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ShadeShift,
        AppliedStatusDuration = 20f
    };

    #endregion

    #region Role Actions (oGCD)

    /// <summary>
    /// Second Wind - Self heal (Lv.8)
    /// </summary>
    public static readonly ActionDefinition SecondWind = new()
    {
        ActionId = 7541,
        Name = "Second Wind",
        MinLevel = 8,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        HealPotency = 800
    };

    /// <summary>
    /// Bloodbath - Lifesteal buff (Lv.12)
    /// </summary>
    public static readonly ActionDefinition Bloodbath = new()
    {
        ActionId = 7542,
        Name = "Bloodbath",
        MinLevel = 12,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Bloodbath,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Feint - Enemy damage reduction (Lv.22)
    /// </summary>
    public static readonly ActionDefinition Feint = new()
    {
        ActionId = 7549,
        Name = "Feint",
        MinLevel = 22,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 90f,
        Range = 10f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Feint,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Arm's Length - Knockback immunity (Lv.32)
    /// </summary>
    public static readonly ActionDefinition ArmsLength = new()
    {
        ActionId = 7548,
        Name = "Arm's Length",
        MinLevel = 32,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ArmsLength,
        AppliedStatusDuration = 6f
    };

    /// <summary>
    /// True North - Ignore positional requirements (Lv.50)
    /// </summary>
    public static readonly ActionDefinition TrueNorth = new()
    {
        ActionId = 7546,
        Name = "True North",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 45f, // 2 charges
        MpCost = 0,
        AppliedStatusId = StatusIds.TrueNorth,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Leg Sweep - Stun (Lv.10)
    /// </summary>
    public static readonly ActionDefinition LegSweep = new()
    {
        ActionId = 7863,
        Name = "Leg Sweep",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 3f,
        MpCost = 0
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Ninjutsu buffs
        public const uint Suiton = 507; // Enables Kunai's Bane
        public const uint Huton = 500;  // Reduced GCD
        public const uint Doton = 501;  // Ground DoT active

        // Combat buffs
        public const uint Kassatsu = 497;       // Enhanced next Ninjutsu
        public const uint TenChiJin = 1186;     // Triple Ninjutsu mode
        public const uint Bunshin = 1954;       // Shadow clone active
        public const uint PhantomKamaitachiReady = 2723; // Can use Phantom Kamaitachi
        public const uint RaijuReady = 2690;    // Can use Forked/Fleeting Raiju
        public const uint Meisui = 2689;        // Enhanced Ninki spenders

        // Vulnerability debuffs
        public const uint VulnerabilityUp = 638;  // Trick Attack debuff
        public const uint KunaisBane = 3906;      // Kunai's Bane debuff
        public const uint Dokumori = 3849;        // Dokumori debuff

        // Combo/resource buffs
        public const uint Kazematoi = 3848;       // Aeolian Edge bonus stacks
        public const uint TenriJindoReady = 3851; // Can use Tenri Jindo

        // Defensive
        public const uint ShadeShift = 488;

        // Role buffs
        public const uint TrueNorth = 1250;
        public const uint Bloodbath = 84;
        public const uint ArmsLength = 1209;
        public const uint Feint = 1195;

        // Hidden/Mudra state
        public const uint Mudra = 496; // Mudra input active
    }

    #endregion

    #region Mudra Enums

    /// <summary>
    /// Mudra types for sequence tracking.
    /// </summary>
    public enum MudraType : byte
    {
        None = 0,
        Ten = 1,
        Chi = 2,
        Jin = 3
    }

    /// <summary>
    /// Target Ninjutsu to perform.
    /// </summary>
    public enum NinjutsuType
    {
        None,
        FumaShuriken,   // Single mudra
        Raiton,         // Ten-Chi (ST damage)
        Katon,          // Chi-Ten (AoE damage)
        Hyoton,         // Ten-Jin or Jin-Ten (bind)
        Huton,          // Jin-Chi-Ten (haste)
        Doton,          // Ten-Jin-Chi (ground AoE)
        Suiton,         // Ten-Chi-Jin (enables Kunai's Bane)
        GokaMekkyaku,   // Kassatsu Katon
        HyoshoRanryu,   // Kassatsu Hyoton (big damage)
        RabbitMedium    // Invalid sequence
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best Ninki spender for single target at the player's level.
    /// </summary>
    public static ActionDefinition GetNinkiSpender(byte level, bool hasMeisui)
    {
        if (hasMeisui && level >= ZeshoMeppo.MinLevel)
            return ZeshoMeppo;
        if (level >= Bhavacakra.MinLevel)
            return Bhavacakra;
        return HellfrogMedium;
    }

    /// <summary>
    /// Gets the best AoE Ninki spender at the player's level.
    /// </summary>
    public static ActionDefinition GetAoeNinkiSpender(byte level, bool hasMeisui)
    {
        if (hasMeisui && level >= DeathfrogMedium.MinLevel)
            return DeathfrogMedium;
        return HellfrogMedium;
    }

    /// <summary>
    /// Gets the Mug action appropriate for level.
    /// </summary>
    public static ActionDefinition GetMugAction(byte level)
    {
        if (level >= Dokumori.MinLevel)
            return Dokumori;
        return Mug;
    }

    /// <summary>
    /// Gets the mudra action by type.
    /// </summary>
    public static ActionDefinition GetMudraAction(MudraType type)
    {
        return type switch
        {
            MudraType.Ten => Ten,
            MudraType.Chi => Chi,
            MudraType.Jin => Jin,
            _ => Ten // Default to Ten if invalid
        };
    }

    /// <summary>
    /// Gets the Ninjutsu result for a mudra sequence.
    /// </summary>
    public static NinjutsuType GetNinjutsuResult(MudraType first, MudraType second = MudraType.None, MudraType third = MudraType.None)
    {
        // Single mudra
        if (second == MudraType.None)
            return NinjutsuType.FumaShuriken;

        // Two mudra combinations
        if (third == MudraType.None)
        {
            return (first, second) switch
            {
                (MudraType.Ten, MudraType.Chi) => NinjutsuType.Raiton,
                (MudraType.Chi, MudraType.Ten) => NinjutsuType.Katon,
                (MudraType.Ten, MudraType.Jin) => NinjutsuType.Hyoton,
                (MudraType.Jin, MudraType.Ten) => NinjutsuType.Hyoton,
                (MudraType.Chi, MudraType.Jin) => NinjutsuType.Raiton, // Also works
                (MudraType.Jin, MudraType.Chi) => NinjutsuType.Katon, // Also works
                _ => NinjutsuType.RabbitMedium
            };
        }

        // Three mudra combinations
        return (first, second, third) switch
        {
            (MudraType.Jin, MudraType.Chi, MudraType.Ten) => NinjutsuType.Huton,
            (MudraType.Ten, MudraType.Jin, MudraType.Chi) => NinjutsuType.Doton,
            (MudraType.Ten, MudraType.Chi, MudraType.Jin) => NinjutsuType.Suiton,
            (MudraType.Chi, MudraType.Jin, MudraType.Ten) => NinjutsuType.Huton, // Also works
            (MudraType.Jin, MudraType.Ten, MudraType.Chi) => NinjutsuType.Doton, // Also works
            (MudraType.Chi, MudraType.Ten, MudraType.Jin) => NinjutsuType.Suiton, // Also works
            _ => NinjutsuType.RabbitMedium
        };
    }

    /// <summary>
    /// Gets the mudra sequence needed for a specific Ninjutsu.
    /// Returns the preferred/standard sequence.
    /// </summary>
    public static (MudraType, MudraType, MudraType) GetMudraSequence(NinjutsuType ninjutsu)
    {
        return ninjutsu switch
        {
            NinjutsuType.FumaShuriken => (MudraType.Ten, MudraType.None, MudraType.None),
            NinjutsuType.Raiton => (MudraType.Ten, MudraType.Chi, MudraType.None),
            NinjutsuType.Katon => (MudraType.Chi, MudraType.Ten, MudraType.None),
            NinjutsuType.Hyoton => (MudraType.Ten, MudraType.Jin, MudraType.None),
            NinjutsuType.Huton => (MudraType.Jin, MudraType.Chi, MudraType.Ten),
            NinjutsuType.Doton => (MudraType.Ten, MudraType.Jin, MudraType.Chi),
            NinjutsuType.Suiton => (MudraType.Ten, MudraType.Chi, MudraType.Jin),
            NinjutsuType.GokaMekkyaku => (MudraType.Chi, MudraType.Ten, MudraType.None), // Kassatsu Katon
            NinjutsuType.HyoshoRanryu => (MudraType.Ten, MudraType.Jin, MudraType.None), // Kassatsu Hyoton
            _ => (MudraType.None, MudraType.None, MudraType.None)
        };
    }

    #endregion
}
