using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Samurai (SAM) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Nike, the Greek goddess of victory.
/// </summary>
public static class SAMActions
{
    #region Single Target Combo GCDs

    /// <summary>
    /// Hakaze - Combo starter (Lv.1)
    /// Gyofu replaces this at level 92.
    /// </summary>
    public static readonly ActionDefinition Hakaze = new()
    {
        ActionId = 7477,
        Name = "Hakaze",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 200
    };

    /// <summary>
    /// Gyofu - Enhanced combo starter (Lv.92)
    /// Replaces Hakaze.
    /// </summary>
    public static readonly ActionDefinition Gyofu = new()
    {
        ActionId = 36963,
        Name = "Gyofu",
        MinLevel = 92,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 240
    };

    /// <summary>
    /// Jinpu - Grants Fugetsu buff, leads to Gekko (Lv.4)
    /// </summary>
    public static readonly ActionDefinition Jinpu = new()
    {
        ActionId = 7478,
        Name = "Jinpu",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 320, // Combo potency
        AppliedStatusId = StatusIds.Fugetsu,
        AppliedStatusDuration = 40f
    };

    /// <summary>
    /// Shifu - Grants Fuka buff, leads to Kasha (Lv.18)
    /// </summary>
    public static readonly ActionDefinition Shifu = new()
    {
        ActionId = 7479,
        Name = "Shifu",
        MinLevel = 18,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 320, // Combo potency
        AppliedStatusId = StatusIds.Fuka,
        AppliedStatusDuration = 40f
    };

    /// <summary>
    /// Yukikaze - Setsu finisher (Lv.50)
    /// Grants Setsu Sen.
    /// </summary>
    public static readonly ActionDefinition Yukikaze = new()
    {
        ActionId = 7480,
        Name = "Yukikaze",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 380 // Combo potency
    };

    /// <summary>
    /// Gekko - Getsu finisher, rear positional (Lv.30)
    /// Grants Getsu Sen.
    /// </summary>
    public static readonly ActionDefinition Gekko = new()
    {
        ActionId = 7481,
        Name = "Gekko",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 440 // Combo potency from rear
    };

    /// <summary>
    /// Kasha - Ka finisher, flank positional (Lv.40)
    /// Grants Ka Sen.
    /// </summary>
    public static readonly ActionDefinition Kasha = new()
    {
        ActionId = 7482,
        Name = "Kasha",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 440 // Combo potency from flank
    };

    #endregion

    #region AoE Combo GCDs

    /// <summary>
    /// Fuko - AoE combo starter (Lv.86)
    /// Replaced Fuga.
    /// </summary>
    public static readonly ActionDefinition Fuko = new()
    {
        ActionId = 25780,
        Name = "Fuko",
        MinLevel = 86,
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
    /// Fuga - Legacy AoE combo starter (Lv.26)
    /// Replaced by Fuko at level 86.
    /// </summary>
    public static readonly ActionDefinition Fuga = new()
    {
        ActionId = 7483,
        Name = "Fuga",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 90
    };

    /// <summary>
    /// Mangetsu - AoE Getsu finisher (Lv.35)
    /// Grants Getsu Sen.
    /// </summary>
    public static readonly ActionDefinition Mangetsu = new()
    {
        ActionId = 7484,
        Name = "Mangetsu",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 120, // Combo potency
        AppliedStatusId = StatusIds.Fugetsu,
        AppliedStatusDuration = 40f
    };

    /// <summary>
    /// Oka - AoE Ka finisher (Lv.45)
    /// Grants Ka Sen.
    /// </summary>
    public static readonly ActionDefinition Oka = new()
    {
        ActionId = 7485,
        Name = "Oka",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 120, // Combo potency
        AppliedStatusId = StatusIds.Fuka,
        AppliedStatusDuration = 40f
    };

    #endregion

    #region Iaijutsu (Sen Spenders)

    /// <summary>
    /// Iaijutsu - Base Iaijutsu action (changes based on Sen)
    /// </summary>
    public static readonly ActionDefinition Iaijutsu = new()
    {
        ActionId = 7867,
        Name = "Iaijutsu",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.8f,
        RecastTime = 2.5f,
        Range = 6f,
        MpCost = 0,
        DamagePotency = 0 // Varies by Sen count
    };

    /// <summary>
    /// Higanbana - 1 Sen Iaijutsu (Lv.30)
    /// 60s DoT, highest potency per Sen for long fights.
    /// </summary>
    public static readonly ActionDefinition Higanbana = new()
    {
        ActionId = 7489,
        Name = "Higanbana",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 1.8f,
        RecastTime = 2.5f,
        Range = 6f,
        MpCost = 0,
        DamagePotency = 200, // Initial + DoT
        AppliedStatusId = StatusIds.Higanbana,
        AppliedStatusDuration = 60f
    };

    /// <summary>
    /// Tenka Goken - 2 Sen Iaijutsu (Lv.40)
    /// AoE burst damage.
    /// </summary>
    public static readonly ActionDefinition TenkaGoken = new()
    {
        ActionId = 7488,
        Name = "Tenka Goken",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.8f,
        RecastTime = 2.5f,
        Range = 8f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 300
    };

    /// <summary>
    /// Midare Setsugekka - 3 Sen Iaijutsu (Lv.50)
    /// Highest single-target burst.
    /// </summary>
    public static readonly ActionDefinition MidareSetsugekka = new()
    {
        ActionId = 7487,
        Name = "Midare Setsugekka",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.8f,
        RecastTime = 2.5f,
        Range = 6f,
        MpCost = 0,
        DamagePotency = 640
    };

    #endregion

    #region Tsubame-gaeshi (Iaijutsu Follow-ups)

    /// <summary>
    /// Tsubame-gaeshi - Base action that transforms based on last Iaijutsu (Lv.76)
    /// </summary>
    public static readonly ActionDefinition TsubameGaeshi = new()
    {
        ActionId = 16483,
        Name = "Tsubame-gaeshi",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f, // 2 charges at Lv.84
        Range = 6f,
        MpCost = 0,
        DamagePotency = 0 // Varies by last Iaijutsu
    };

    /// <summary>
    /// Kaeshi: Higanbana - Follow-up to Higanbana (Lv.76)
    /// </summary>
    public static readonly ActionDefinition KaeshiHiganbana = new()
    {
        ActionId = 16484,
        Name = "Kaeshi: Higanbana",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 6f,
        MpCost = 0,
        DamagePotency = 200,
        AppliedStatusId = StatusIds.Higanbana,
        AppliedStatusDuration = 60f
    };

    /// <summary>
    /// Kaeshi: Goken - Follow-up to Tenka Goken (Lv.76)
    /// </summary>
    public static readonly ActionDefinition KaeshiGoken = new()
    {
        ActionId = 16485,
        Name = "Kaeshi: Goken",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 8f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 300
    };

    /// <summary>
    /// Kaeshi: Setsugekka - Follow-up to Midare Setsugekka (Lv.76)
    /// </summary>
    public static readonly ActionDefinition KaeshiSetsugekka = new()
    {
        ActionId = 16486,
        Name = "Kaeshi: Setsugekka",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 6f,
        MpCost = 0,
        DamagePotency = 640
    };

    #endregion

    #region Ogi Namikiri (Lv.90)

    /// <summary>
    /// Ogi Namikiri - Premium burst GCD (Lv.90)
    /// Requires Ogi Namikiri Ready buff from Ikishoten.
    /// </summary>
    public static readonly ActionDefinition OgiNamikiri = new()
    {
        ActionId = 25781,
        Name = "Ogi Namikiri",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.8f,
        RecastTime = 2.5f,
        Range = 8f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 860
    };

    /// <summary>
    /// Kaeshi: Namikiri - Follow-up to Ogi Namikiri (Lv.90)
    /// </summary>
    public static readonly ActionDefinition KaeshiNamikiri = new()
    {
        ActionId = 25782,
        Name = "Kaeshi: Namikiri",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 8f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 860
    };

    /// <summary>
    /// Tendo Goken - Enhanced Tenka Goken during Tendo (Lv.100)
    /// Game auto-replaces TenkaGoken when Tendo buff is active. 2 Sen spender.
    /// </summary>
    public static readonly ActionDefinition TendoGoken = new()
    {
        ActionId = 36965,
        Name = "Tendo Goken",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.8f,
        RecastTime = 2.5f,
        Range = 8f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 410
    };

    /// <summary>
    /// Tendo Setsugekka - Enhanced Midare Setsugekka during Tendo (Lv.100)
    /// Game auto-replaces MidareSetsugekka when Tendo buff is active. 3 Sen spender.
    /// </summary>
    public static readonly ActionDefinition TendoSetsugekka = new()
    {
        ActionId = 36966,
        Name = "Tendo Setsugekka",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.8f,
        RecastTime = 2.5f,
        Range = 6f,
        MpCost = 0,
        DamagePotency = 1100
    };

    /// <summary>
    /// Tendo Kaeshi Goken - Follow-up to Tendo Goken (Lv.100)
    /// Fired via Tsubame-gaeshi button after Tendo Goken.
    /// </summary>
    public static readonly ActionDefinition TendoKaeshiGoken = new()
    {
        ActionId = 36967,
        Name = "Tendo Kaeshi Goken",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 8f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 410
    };

    /// <summary>
    /// Tendo Kaeshi Setsugekka - Follow-up to Tendo Setsugekka (Lv.100)
    /// Fired via Tsubame-gaeshi button after Tendo Setsugekka.
    /// </summary>
    public static readonly ActionDefinition TendoKaeshiSetsugekka = new()
    {
        ActionId = 36968,
        Name = "Tendo Kaeshi Setsugekka",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 6f,
        MpCost = 0,
        DamagePotency = 1100
    };

    #endregion

    #region Kenki Spenders (oGCD)

    /// <summary>
    /// Hissatsu: Shinten - Single target Kenki spender (Lv.52)
    /// Requires 25 Kenki.
    /// </summary>
    public static readonly ActionDefinition Shinten = new()
    {
        ActionId = 7490,
        Name = "Hissatsu: Shinten",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 250
    };

    /// <summary>
    /// Hissatsu: Kyuten - AoE Kenki spender (Lv.62)
    /// Requires 25 Kenki.
    /// </summary>
    public static readonly ActionDefinition Kyuten = new()
    {
        ActionId = 7491,
        Name = "Hissatsu: Kyuten",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 120
    };

    /// <summary>
    /// Hissatsu: Senei - Single target burst Kenki spender (Lv.72)
    /// Requires 25 Kenki. 120s cooldown.
    /// </summary>
    public static readonly ActionDefinition Senei = new()
    {
        ActionId = 16481,
        Name = "Hissatsu: Senei",
        MinLevel = 72,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 800
    };

    /// <summary>
    /// Hissatsu: Guren - AoE burst Kenki spender (Lv.70)
    /// Requires 25 Kenki. 120s cooldown. Shares recast with Senei.
    /// </summary>
    public static readonly ActionDefinition Guren = new()
    {
        ActionId = 7496,
        Name = "Hissatsu: Guren",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 0f,
        Radius = 10f,
        MpCost = 0,
        DamagePotency = 500
    };

    /// <summary>
    /// Zanshin - Enhanced Kenki spender after Ogi Namikiri (Lv.96)
    /// Requires Zanshin Ready buff.
    /// </summary>
    public static readonly ActionDefinition Zanshin = new()
    {
        ActionId = 36964,
        Name = "Zanshin",
        MinLevel = 96,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 880
    };

    #endregion

    #region Meditation Spender

    /// <summary>
    /// Shoha - Meditation spender (Lv.80)
    /// Requires 3 Meditation stacks.
    /// </summary>
    public static readonly ActionDefinition Shoha = new()
    {
        ActionId = 16487,
        Name = "Shoha",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 15f,
        Range = 3f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 640
    };

    #endregion

    #region Buff Actions (oGCD)

    /// <summary>
    /// Meikyo Shisui - Combo skip buff (Lv.50)
    /// 3 stacks, allows using combo finishers directly.
    /// </summary>
    public static readonly ActionDefinition MeikyoShisui = new()
    {
        ActionId = 7499,
        Name = "Meikyo Shisui",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 55f, // 2 charges
        MpCost = 0,
        AppliedStatusId = StatusIds.MeikyoShisui,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Ikishoten - Kenki generator + Ogi Namikiri Ready (Lv.68)
    /// Grants 50 Kenki and Ogi Namikiri Ready buff.
    /// </summary>
    public static readonly ActionDefinition Ikishoten = new()
    {
        ActionId = 16482,
        Name = "Ikishoten",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.OgiNamikiriReady,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Hagakure - Sen to Kenki converter (Lv.68)
    /// Converts Sen to 10 Kenki each.
    /// </summary>
    public static readonly ActionDefinition Hagakure = new()
    {
        ActionId = 7495,
        Name = "Hagakure",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 5f,
        MpCost = 0
    };

    #endregion

    #region Utility Actions (oGCD)

    /// <summary>
    /// Hissatsu: Gyoten - Gap closer (Lv.54)
    /// Requires 10 Kenki.
    /// </summary>
    public static readonly ActionDefinition Gyoten = new()
    {
        ActionId = 7492,
        Name = "Hissatsu: Gyoten",
        MinLevel = 54,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 10f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Hissatsu: Yaten - Backstep (Lv.56)
    /// Requires 10 Kenki. Grants Enpi Ready.
    /// </summary>
    public static readonly ActionDefinition Yaten = new()
    {
        ActionId = 7493,
        Name = "Hissatsu: Yaten",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Movement | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 10f,
        Range = 5f,
        MpCost = 0,
        DamagePotency = 100,
        AppliedStatusId = StatusIds.EnhancedEnpi,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Enpi - Ranged attack (Lv.15)
    /// Enhanced when Enpi Ready is active.
    /// </summary>
    public static readonly ActionDefinition Enpi = new()
    {
        ActionId = 7486,
        Name = "Enpi",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 15f,
        MpCost = 0,
        DamagePotency = 100 // Enhanced: 280
    };

    /// <summary>
    /// Third Eye - Damage mitigation (Lv.6)
    /// Grants Fugetsu equivalent buff for 4s.
    /// </summary>
    public static readonly ActionDefinition ThirdEye = new()
    {
        ActionId = 7498,
        Name = "Third Eye",
        MinLevel = 6,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 15f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ThirdEye,
        AppliedStatusDuration = 4f
    };

    /// <summary>
    /// Tengentsu - Enhanced Third Eye (Lv.82)
    /// Also grants Tengentsu Ready for heal on taking damage.
    /// </summary>
    public static readonly ActionDefinition Tengentsu = new()
    {
        ActionId = 25857,
        Name = "Tengentsu",
        MinLevel = 82,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 15f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Tengentsu,
        AppliedStatusDuration = 4f
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Damage buffs
        public const uint Fugetsu = 1298;  // 13% damage up (from Jinpu path)
        public const uint Fuka = 1299;     // 13% haste (from Shifu path)

        // Special state buffs
        public const uint MeikyoShisui = 1233;       // Combo skip (3 stacks)
        public const uint OgiNamikiriReady = 2959;   // Can use Ogi Namikiri
        public const uint KaeshiNamikiriReady = 2960; // Can use Kaeshi: Namikiri
        public const uint ZanshinReady = 3855;       // Can use Zanshin

        // Tsubame-gaeshi readiness
        public const uint TsubameGaeshiReady = 4216; // Can use Tsubame-gaeshi

        // Tendo (Meikyo Shisui enhancement at Lv.100)
        public const uint Tendo = 3856; // Granted by Meikyo Shisui at Lv.100+, enables Tendo Goken/Setsugekka
        public const uint TendoKaeshiGoken = 4217; // Follow-up proc after Tendo Goken
        public const uint TendoKaeshiSetsugekka = 4218; // Follow-up proc after Tendo Setsugekka

        // DoT
        public const uint Higanbana = 1228; // 60s DoT

        // Utility buffs
        public const uint EnhancedEnpi = 1236;    // Enhanced Enpi (from Yaten)
        public const uint ThirdEye = 1232;        // Damage mitigation
        public const uint Tengentsu = 3853;       // Enhanced Third Eye

        // Role buffs
        public const uint TrueNorth = 1250;
        public const uint Bloodbath = 84;
        public const uint ArmsLength = 1209;
        public const uint Feint = 1195;
    }

    #endregion

    #region Sen Enum

    /// <summary>
    /// Sen types for tracking.
    /// Stored as bit flags in the gauge.
    /// </summary>
    [System.Flags]
    public enum SenType : byte
    {
        None = 0,
        Setsu = 1, // Snow (Yukikaze)
        Getsu = 2, // Moon (Gekko/Mangetsu)
        Ka = 4     // Flower (Kasha/Oka)
    }

    /// <summary>
    /// Iaijutsu type based on Sen count.
    /// </summary>
    public enum IaijutsuType
    {
        None,
        Higanbana,       // 1 Sen
        TenkaGoken,      // 2 Sen
        MidareSetsugekka // 3 Sen
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the combo starter action for the player's level.
    /// </summary>
    public static ActionDefinition GetComboStarter(byte level)
    {
        if (level >= Gyofu.MinLevel)
            return Gyofu;
        return Hakaze;
    }

    /// <summary>
    /// Gets the AoE combo starter for the player's level.
    /// </summary>
    public static ActionDefinition GetAoeComboStarter(byte level)
    {
        if (level >= Fuko.MinLevel)
            return Fuko;
        return Fuga;
    }

    /// <summary>
    /// Gets the Iaijutsu type for the given Sen count.
    /// </summary>
    public static IaijutsuType GetIaijutsuType(int senCount)
    {
        return senCount switch
        {
            1 => IaijutsuType.Higanbana,
            2 => IaijutsuType.TenkaGoken,
            3 => IaijutsuType.MidareSetsugekka,
            _ => IaijutsuType.None
        };
    }

    /// <summary>
    /// Gets the Iaijutsu action for the given type.
    /// </summary>
    public static ActionDefinition GetIaijutsuAction(IaijutsuType type)
    {
        return type switch
        {
            IaijutsuType.Higanbana => Higanbana,
            IaijutsuType.TenkaGoken => TenkaGoken,
            IaijutsuType.MidareSetsugekka => MidareSetsugekka,
            _ => Iaijutsu
        };
    }

    /// <summary>
    /// Gets the Kaeshi action for the given Iaijutsu type.
    /// </summary>
    public static ActionDefinition GetKaeshiAction(IaijutsuType lastIaijutsu)
    {
        return lastIaijutsu switch
        {
            IaijutsuType.Higanbana => KaeshiHiganbana,
            IaijutsuType.TenkaGoken => KaeshiGoken,
            IaijutsuType.MidareSetsugekka => KaeshiSetsugekka,
            _ => TsubameGaeshi
        };
    }

    /// <summary>
    /// Gets the best single target Kenki spender at the player's level.
    /// </summary>
    public static ActionDefinition GetKenkiSpender(byte level, bool hasZanshinReady)
    {
        // Zanshin takes priority when ready
        if (hasZanshinReady && level >= Zanshin.MinLevel)
            return Zanshin;
        if (level >= Shinten.MinLevel)
            return Shinten;
        return Shinten; // Always available at 52+
    }

    /// <summary>
    /// Gets the burst Kenki oGCD for single target.
    /// </summary>
    public static ActionDefinition GetBurstKenkiSpender(byte level, bool isAoe)
    {
        if (isAoe && level >= Guren.MinLevel)
            return Guren;
        if (level >= Senei.MinLevel)
            return Senei;
        return Guren;
    }

    /// <summary>
    /// Gets the Third Eye action appropriate for level.
    /// </summary>
    public static ActionDefinition GetThirdEyeAction(byte level)
    {
        if (level >= Tengentsu.MinLevel)
            return Tengentsu;
        return ThirdEye;
    }

    /// <summary>
    /// Counts the number of active Sen.
    /// </summary>
    public static int CountSen(SenType sen)
    {
        var count = 0;
        if ((sen & SenType.Setsu) != 0) count++;
        if ((sen & SenType.Getsu) != 0) count++;
        if ((sen & SenType.Ka) != 0) count++;
        return count;
    }

    /// <summary>
    /// Formats Sen for debug display.
    /// </summary>
    public static string FormatSen(SenType sen)
    {
        var s = (sen & SenType.Setsu) != 0 ? "S" : "-";
        var g = (sen & SenType.Getsu) != 0 ? "G" : "-";
        var k = (sen & SenType.Ka) != 0 ? "K" : "-";
        return $"[{s}{g}{k}]";
    }

    #endregion
}
