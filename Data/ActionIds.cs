namespace Olympus.Data;

/// <summary>
/// Centralized action ID constants for quick reference when adding new features.
/// These IDs correspond to the ActionId properties in job-specific *Actions.cs files.
/// </summary>
/// <remarks>
/// USAGE: When adding new actions, look up the ID here or add it if missing.
/// The primary source of truth for action metadata (potency, cast time, etc.)
/// remains in the job-specific files (WHMActions, SCHActions, etc.).
/// </remarks>
public static class ActionIds
{
    #region Role Actions (Shared across roles)

    // Healer Role Actions
    public const uint Swiftcast = 7561;
    public const uint LucidDreaming = 7562;
    public const uint Surecast = 7559;
    public const uint Rescue = 7571;
    public const uint Esuna = 7568;

    // Tank Role Actions
    public const uint Rampart = 7531;
    public const uint Provoke = 7533;
    public const uint Reprisal = 7535;
    public const uint Shirk = 7537;
    public const uint Interject = 7538;
    public const uint LowBlow = 7540;
    public const uint ArmsLength = 7548;

    #endregion

    #region White Mage (WHM)

    // WHM GCD Heals
    public const uint Cure = 120;
    public const uint CureII = 135;
    public const uint CureIII = 131;
    public const uint Regen = 137;
    public const uint Medica = 124;
    public const uint MedicaII = 133;
    public const uint MedicaIII = 37010;
    public const uint AfflatusSolace = 16531;
    public const uint AfflatusRapture = 16534;
    public const uint Raise_WHM = 125;

    // WHM GCD Damage
    public const uint Stone = 119;
    public const uint StoneII = 127;
    public const uint StoneIII = 3568;
    public const uint StoneIV = 7431;
    public const uint Glare = 16533;
    public const uint GlareIII = 25859;
    public const uint GlareIV = 37009;
    public const uint Holy = 139;
    public const uint HolyIII = 25860;
    public const uint AfflatusMisery = 16535;

    // WHM DoTs
    public const uint Aero = 121;
    public const uint AeroII = 132;
    public const uint Dia = 16532;

    // WHM oGCD
    public const uint Benediction = 140;
    public const uint Tetragrammaton = 3570;
    public const uint DivineBenison = 7432;
    public const uint Aquaveil = 25861;
    public const uint Asylum = 3569;
    public const uint Assize = 3571;
    public const uint PlenaryIndulgence = 7433;
    public const uint Temperance = 16536;
    public const uint LiturgyOfTheBell = 25862;
    public const uint DivineCaress = 37011;
    public const uint PresenceOfMind = 136;
    public const uint ThinAir = 7430;
    public const uint AetherialShift = 37008;

    #endregion

    #region Scholar (SCH)

    // SCH GCD Heals
    public const uint Physick = 190;
    public const uint Adloquium = 185;
    public const uint Succor = 186;
    public const uint Resurrection_SCH = 173;

    // SCH GCD Damage
    public const uint Broil = 3584;
    public const uint BroilII = 7435;
    public const uint BroilIII = 16541;
    public const uint BroilIV = 25865;
    public const uint ArtOfWar = 16539;
    public const uint ArtOfWarII = 25866;

    // SCH DoTs
    public const uint Bio = 17864;
    public const uint BioII = 17865;
    public const uint Biolysis = 16540;

    // SCH oGCD
    public const uint Lustrate = 189;
    public const uint Indomitability = 3583;
    public const uint Excogitation = 7434;
    public const uint SacredSoil = 188;
    public const uint Aetherpact = 7437;
    public const uint Dissipation = 3587;
    public const uint Recitation = 16542;
    public const uint Protraction = 25867;
    public const uint Expedient = 25868;
    public const uint ChainStratagem = 7436;
    public const uint DeploymentTactics = 3585;
    public const uint EmergencyTactics = 3586;
    public const uint Summon = 17215;
    public const uint SummonSeraph = 16545;
    public const uint Consolation = 16546;
    public const uint Seraphism = 37014;

    #endregion

    #region Astrologian (AST)

    // AST GCD Heals
    public const uint Benefic = 3594;
    public const uint BeneficII = 3610;
    public const uint Helios = 3600;
    public const uint AspectedHelios = 3601;
    public const uint AspectedBenefic = 3595;
    public const uint Ascend = 3603;

    // AST GCD Damage
    public const uint Malefic = 3596;
    public const uint MaleficII = 3598;
    public const uint MaleficIII = 7442;
    public const uint MaleficIV = 16555;
    public const uint FallMalefic = 25871;
    public const uint Gravity = 3615;
    public const uint GravityII = 25872;

    // AST DoTs
    public const uint Combust = 3599;
    public const uint CombustII = 3608;
    public const uint CombustIII = 16554;

    // AST oGCD
    public const uint EssentialDignity = 3614;
    public const uint CelestialIntersection = 16556;
    public const uint CelestialOpposition = 16553;
    public const uint CollectiveUnconscious = 3613;
    public const uint EarthlyStar = 7439;
    public const uint Horoscope = 16557;
    public const uint NeutralSect = 16559;
    public const uint Exaltation = 25873;
    public const uint Macrocosmos = 25874;
    public const uint Lightspeed = 3606;
    public const uint Synastry = 3612;
    public const uint Divination = 16552;

    // AST Cards
    public const uint Draw = 3590;
    public const uint Play = 17055;
    public const uint AstralDraw = 37017;
    public const uint UmbralDraw = 37018;
    public const uint MinorArcana = 7443;
    public const uint LordOfCrowns = 7444;
    public const uint LadyOfCrowns = 7445;

    #endregion

    #region Sage (SGE)

    // SGE GCD Heals
    public const uint Diagnosis = 24284;
    public const uint Prognosis = 24286;
    public const uint EukrasianDiagnosis = 24291;
    public const uint EukrasianPrognosis = 24292;
    public const uint Egeiro = 24287;

    // SGE GCD Damage
    public const uint Dosis = 24283;
    public const uint DosisII = 24306;
    public const uint DosisIII = 24312;
    public const uint Phlegma = 24289;
    public const uint PhlegmaII = 24307;
    public const uint PhlegmaIII = 24313;
    public const uint Dyskrasia = 24297;
    public const uint DyskrasiaII = 24315;
    public const uint Toxikon = 24304;
    public const uint ToxikonII = 24316;
    public const uint Psyche = 37033;

    // SGE DoTs
    public const uint EukrasianDosis = 24293;
    public const uint EukrasianDosisII = 24308;
    public const uint EukrasianDosisIII = 24314;

    // SGE oGCD
    public const uint Kardia = 24285;
    public const uint Soteria = 24294;
    public const uint Druochole = 24296;
    public const uint Kerachole = 24298;
    public const uint Ixochole = 24299;
    public const uint Taurochole = 24303;
    public const uint Holos = 24310;
    public const uint Physis = 24288;
    public const uint PhysisII = 24302;
    public const uint Pneuma = 24318;
    public const uint Zoe = 24300;
    public const uint Pepsis = 24301;
    public const uint Rhizomata = 24309;
    public const uint Haima = 24305;
    public const uint Panhaima = 24311;
    public const uint Krasis = 24317;
    public const uint Philosophia = 37035;

    #endregion

    #region Paladin (PLD)

    // PLD GCD Combo
    public const uint FastBlade = 9;
    public const uint RiotBlade = 15;
    public const uint RageOfHalone = 21;
    public const uint RoyalAuthority = 3539;
    public const uint Atonement = 16460;
    public const uint Supplication = 36918;
    public const uint Sepulchre = 36919;

    // PLD GCD AoE
    public const uint TotalEclipse = 7381;
    public const uint Prominence = 16457;

    // PLD GCD Magic
    public const uint HolySpirit = 7384;
    public const uint HolyCircle = 16458;
    public const uint Confiteor = 16459;
    public const uint BladeOfFaith = 25748;
    public const uint BladeOfTruth = 25749;
    public const uint BladeOfValor = 25750;
    public const uint BladeOfHonor = 36922;

    // PLD oGCD Damage
    public const uint SpiritsWithin = 29;
    public const uint Expiacion = 25747;
    public const uint CircleOfScorn = 23;
    public const uint Intervene = 16461;
    public const uint Imperator = 36921;

    // PLD oGCD Defensive
    public const uint Sheltron = 3542;
    public const uint HolySheltron = 25746;
    public const uint Sentinel = 17;
    public const uint Guardian = 36920;
    public const uint Bulwark = 22;
    public const uint DivineVeil = 3540;
    public const uint PassageOfArms = 7385;
    public const uint Cover = 27;
    public const uint HallowedGround = 30;
    public const uint Clemency = 3541;

    // PLD Buffs
    public const uint FightOrFlight = 20;
    public const uint Requiescat = 7383;
    public const uint IronWill = 28;

    #endregion

    #region Status Effect IDs (Common)

    // WHM Status Effects
    public const uint Status_Regen = 158;
    public const uint Status_MedicaII = 150;
    public const uint Status_MedicaIII = 3986;
    public const uint Status_Dia = 1871;
    public const uint Status_DivineBenison = 1218;
    public const uint Status_Aquaveil = 2708;
    public const uint Status_Temperance = 1872;
    public const uint Status_PresenceOfMind = 157;
    public const uint Status_ThinAir = 1217;
    public const uint Status_Swiftcast = 167;
    public const uint Status_LucidDreaming = 1204;
    public const uint Status_Freecure = 155;

    #endregion
}
