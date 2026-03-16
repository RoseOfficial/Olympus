using System;
using System.Collections.Generic;
using System.Linq;
using Olympus.Data;
using Olympus.Models.Action;

namespace Olympus.Services.Action;

/// <summary>
/// Service for managing action definitions across all jobs.
/// Actions are registered at initialization and queried during runtime.
/// </summary>
public sealed class ActionLibraryService : IActionLibrary
{
    private readonly Dictionary<uint, ActionDefinition> _actionsByIdCache = new(256);
    private readonly Dictionary<uint, List<ActionDefinition>> _actionsByJob = new();

    /// <summary>
    /// Creates a new action library and registers all known job actions.
    /// </summary>
    public ActionLibraryService()
    {
        RegisterWhiteMageActions();
        RegisterScholarActions();
        RegisterAstrologianActions();
        RegisterSageActions();
        RegisterWarriorActions();
        RegisterPaladinActions();
        RegisterDarkKnightActions();
        RegisterGunbreakerActions();
        RegisterMonkActions();
        RegisterDragoonActions();
        RegisterNinjaActions();
        RegisterSamuraiActions();
        RegisterReaperActions();
        RegisterViperActions();
        RegisterBardActions();
        RegisterMachinistActions();
        RegisterDancerActions();
        RegisterBlackMageActions();
        RegisterSummonerActions();
        RegisterRedMageActions();
        RegisterPictomancerActions();
    }

    /// <inheritdoc />
    public ActionDefinition? GetAction(uint actionId)
    {
        return _actionsByIdCache.TryGetValue(actionId, out var action) ? action : null;
    }

    /// <inheritdoc />
    public IEnumerable<ActionDefinition> GetJobActions(uint jobId)
    {
        return _actionsByJob.TryGetValue(jobId, out var actions) ? actions : Enumerable.Empty<ActionDefinition>();
    }

    /// <inheritdoc />
    public IEnumerable<ActionDefinition> GetHealingActions(uint jobId)
    {
        return GetJobActions(jobId).Where(a => a.IsHeal);
    }

    /// <inheritdoc />
    public IEnumerable<ActionDefinition> GetDamageActions(uint jobId)
    {
        return GetJobActions(jobId).Where(a => a.IsDamage);
    }

    /// <inheritdoc />
    public IEnumerable<ActionDefinition> GetActionsAtLevel(uint jobId, byte level)
    {
        return GetJobActions(jobId).Where(a => a.MinLevel <= level);
    }

    /// <inheritdoc />
    public bool HasAction(uint actionId)
    {
        return _actionsByIdCache.ContainsKey(actionId);
    }

    /// <summary>
    /// Registers an action for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="action">The action definition.</param>
    private void RegisterAction(uint jobId, ActionDefinition action)
    {
        // Add to ID lookup cache
        _actionsByIdCache[action.ActionId] = action;

        // Add to job-specific list
        if (!_actionsByJob.TryGetValue(jobId, out var jobActions))
        {
            jobActions = new List<ActionDefinition>(32);
            _actionsByJob[jobId] = jobActions;
        }
        jobActions.Add(action);
    }

    /// <summary>
    /// Registers multiple actions for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="actions">The action definitions to register.</param>
    private void RegisterActions(uint jobId, params ActionDefinition[] actions)
    {
        foreach (var action in actions)
        {
            RegisterAction(jobId, action);
        }
    }

    /// <summary>
    /// Registers all White Mage (WHM) and Conjurer (CNJ) actions.
    /// </summary>
    private void RegisterWhiteMageActions()
    {
        const uint whmJobId = JobRegistry.WhiteMage;
        const uint cnjJobId = JobRegistry.Conjurer;

        // GCD Heals
        RegisterActions(whmJobId,
            WHMActions.Cure,
            WHMActions.CureII,
            WHMActions.CureIII,
            WHMActions.Regen,
            WHMActions.Medica,
            WHMActions.MedicaII,
            WHMActions.MedicaIII,
            WHMActions.AfflatusSolace,
            WHMActions.AfflatusRapture);

        // oGCD Heals
        RegisterActions(whmJobId,
            WHMActions.Tetragrammaton,
            WHMActions.Benediction,
            WHMActions.Assize,
            WHMActions.Asylum);

        // Damage
        RegisterActions(whmJobId,
            WHMActions.Stone,
            WHMActions.StoneII,
            WHMActions.StoneIII,
            WHMActions.StoneIV,
            WHMActions.Glare,
            WHMActions.GlareIII,
            WHMActions.GlareIV,
            WHMActions.Holy,
            WHMActions.HolyIII,
            WHMActions.AfflatusMisery);

        // DoTs
        RegisterActions(whmJobId,
            WHMActions.Aero,
            WHMActions.AeroII,
            WHMActions.Dia);

        // Defensive
        RegisterActions(whmJobId,
            WHMActions.DivineBenison,
            WHMActions.Aquaveil,
            WHMActions.Temperance,
            WHMActions.LiturgyOfTheBell,
            WHMActions.PlenaryIndulgence,
            WHMActions.DivineCaress);

        // Buffs
        RegisterActions(whmJobId,
            WHMActions.PresenceOfMind,
            WHMActions.ThinAir);

        // Role Actions
        RegisterActions(whmJobId,
            WHMActions.Esuna,
            WHMActions.Raise,
            WHMActions.Swiftcast,
            WHMActions.LucidDreaming,
            WHMActions.Surecast,
            WHMActions.Rescue);

        // Also register CNJ base class actions
        RegisterActions(cnjJobId,
            WHMActions.Cure,
            WHMActions.Stone,
            WHMActions.Aero,
            WHMActions.Medica,
            WHMActions.Raise,
            WHMActions.Esuna);
    }

    /// <summary>
    /// Registers all Scholar (SCH) and Arcanist (ACN) actions.
    /// </summary>
    private void RegisterScholarActions()
    {
        const uint schJobId = JobRegistry.Scholar;
        const uint acnJobId = JobRegistry.Arcanist;

        // GCD Heals
        RegisterActions(schJobId,
            SCHActions.Physick,
            SCHActions.Adloquium,
            SCHActions.Manifestation,
            SCHActions.Succor,
            SCHActions.Concitation,
            SCHActions.Accession);

        // Damage GCDs
        RegisterActions(schJobId,
            SCHActions.Ruin,
            SCHActions.RuinII,
            SCHActions.Broil,
            SCHActions.BroilII,
            SCHActions.BroilIII,
            SCHActions.BroilIV,
            SCHActions.ArtOfWar,
            SCHActions.ArtOfWarII);

        // DoTs
        RegisterActions(schJobId,
            SCHActions.Bio,
            SCHActions.BioII,
            SCHActions.Biolysis);

        // oGCD Heals
        RegisterActions(schJobId,
            SCHActions.Lustrate,
            SCHActions.Indomitability,
            SCHActions.Excogitation,
            SCHActions.SacredSoil,
            SCHActions.Protraction);

        // oGCD Utility
        RegisterActions(schJobId,
            SCHActions.Aetherflow,
            SCHActions.EnergyDrain,
            SCHActions.Recitation,
            SCHActions.EmergencyTactics,
            SCHActions.DeploymentTactics,
            SCHActions.Dissipation,
            SCHActions.ChainStratagem,
            SCHActions.Expedient,
            SCHActions.BanefulImpaction);

        // Fairy
        RegisterActions(schJobId,
            SCHActions.SummonEos,
            SCHActions.WhisperingDawn,
            SCHActions.FeyIllumination,
            SCHActions.FeyBlessing,
            SCHActions.Aetherpact,
            SCHActions.FeyUnion,
            SCHActions.DissolveUnion,
            SCHActions.SummonSeraph,
            SCHActions.Consolation,
            SCHActions.Seraphism);

        // Role Actions
        RegisterActions(schJobId,
            SCHActions.Swiftcast,
            SCHActions.LucidDreaming,
            SCHActions.Surecast,
            SCHActions.Rescue,
            SCHActions.Esuna,
            SCHActions.Resurrection);

        // Arcanist base class — starter subset
        RegisterActions(acnJobId,
            SCHActions.Physick,
            SCHActions.Ruin,
            SCHActions.Bio,
            SCHActions.Succor,
            SCHActions.Resurrection,
            SCHActions.Esuna);
    }

    /// <summary>
    /// Registers all Astrologian (AST) actions.
    /// </summary>
    private void RegisterAstrologianActions()
    {
        const uint astJobId = JobRegistry.Astrologian;

        // GCD Heals
        RegisterActions(astJobId,
            ASTActions.Benefic,
            ASTActions.BeneficII,
            ASTActions.AspectedBenefic,
            ASTActions.Helios,
            ASTActions.AspectedHelios,
            ASTActions.HeliosConjunction);

        // Damage GCDs
        RegisterActions(astJobId,
            ASTActions.Malefic,
            ASTActions.MaleficII,
            ASTActions.MaleficIII,
            ASTActions.MaleficIV,
            ASTActions.FallMalefic,
            ASTActions.Gravity,
            ASTActions.GravityII);

        // DoTs
        RegisterActions(astJobId,
            ASTActions.Combust,
            ASTActions.CombustII,
            ASTActions.CombustIII);

        // oGCD Heals
        RegisterActions(astJobId,
            ASTActions.EssentialDignity,
            ASTActions.CelestialIntersection,
            ASTActions.CelestialOpposition,
            ASTActions.Exaltation,
            ASTActions.Horoscope,
            ASTActions.HoroscopeEnd,
            ASTActions.Macrocosmos,
            ASTActions.Microcosmos,
            ASTActions.EarthlyStar,
            ASTActions.StellarDetonation);

        // Cards
        RegisterActions(astJobId,
            ASTActions.AstralDraw,
            ASTActions.UmbralDraw,
            ASTActions.PlayI,
            ASTActions.PlayII,
            ASTActions.TheBalance,
            ASTActions.TheSpear,
            ASTActions.TheBole,
            ASTActions.TheArrow,
            ASTActions.TheEwer,
            ASTActions.TheSpire,
            ASTActions.PlayIII,
            ASTActions.MinorArcana,
            ASTActions.LadyOfCrowns,
            ASTActions.LordOfCrowns);

        // Buffs / Utility
        RegisterActions(astJobId,
            ASTActions.Astrodyne,
            ASTActions.Divination,
            ASTActions.Oracle,
            ASTActions.Lightspeed,
            ASTActions.Synastry,
            ASTActions.NeutralSect,
            ASTActions.CollectiveUnconscious,
            ASTActions.SunSign);

        // Role Actions
        RegisterActions(astJobId,
            ASTActions.Swiftcast,
            ASTActions.LucidDreaming,
            ASTActions.Surecast,
            ASTActions.Rescue,
            ASTActions.Esuna,
            ASTActions.Ascend);
    }

    /// <summary>
    /// Registers all Sage (SGE) actions.
    /// </summary>
    private void RegisterSageActions()
    {
        const uint sgeJobId = JobRegistry.Sage;

        // GCD Heals
        RegisterActions(sgeJobId,
            SGEActions.Diagnosis,
            SGEActions.Prognosis,
            SGEActions.EukrasianDiagnosis,
            SGEActions.EukrasianPrognosis,
            SGEActions.EukrasianPrognosisII,
            SGEActions.Pneuma);

        // Damage GCDs
        RegisterActions(sgeJobId,
            SGEActions.Dosis,
            SGEActions.DosisII,
            SGEActions.DosisIII,
            SGEActions.Dyskrasia,
            SGEActions.DyskrasiaII,
            SGEActions.Toxikon,
            SGEActions.ToxikonII,
            SGEActions.Phlegma,
            SGEActions.PhlegmaII,
            SGEActions.PhlegmaIII);

        // Eukrasian DoTs
        RegisterActions(sgeJobId,
            SGEActions.EukrasianDosis,
            SGEActions.EukrasianDosisII,
            SGEActions.EukrasianDosisIII,
            SGEActions.EukrasianDyskrasia);

        // oGCD Heals
        RegisterActions(sgeJobId,
            SGEActions.Druochole,
            SGEActions.Taurochole,
            SGEActions.Ixochole,
            SGEActions.Kerachole,
            SGEActions.PhysisII,
            SGEActions.Holos,
            SGEActions.Pepsis,
            SGEActions.Rhizomata,
            SGEActions.Haima,
            SGEActions.Panhaima);

        // Kardia / Defensive
        RegisterActions(sgeJobId,
            SGEActions.Kardia,
            SGEActions.Soteria,
            SGEActions.Eukrasia,
            SGEActions.Krasis,
            SGEActions.Zoe,
            SGEActions.Philosophia,
            SGEActions.Psyche,
            SGEActions.Icarus);

        // Role Actions
        RegisterActions(sgeJobId,
            SGEActions.Swiftcast,
            SGEActions.LucidDreaming,
            SGEActions.Surecast,
            SGEActions.Rescue,
            SGEActions.Esuna,
            SGEActions.Egeiro);
    }

    /// <summary>
    /// Registers all Warrior (WAR) and Marauder (MRD) actions.
    /// </summary>
    private void RegisterWarriorActions()
    {
        const uint warJobId = JobRegistry.Warrior;
        const uint mrdJobId = JobRegistry.Marauder;

        // Combo GCDs
        RegisterActions(warJobId,
            WARActions.HeavySwing,
            WARActions.Maim,
            WARActions.StormsPath,
            WARActions.StormsEye,
            WARActions.Overpower,
            WARActions.MythrilTempest);

        // Spenders
        RegisterActions(warJobId,
            WARActions.InnerBeast,
            WARActions.FellCleave,
            WARActions.InnerChaos,
            WARActions.SteelCyclone,
            WARActions.Decimate,
            WARActions.ChaoticCyclone,
            WARActions.PrimalRend,
            WARActions.PrimalRuination);

        // oGCDs
        RegisterActions(warJobId,
            WARActions.Tomahawk,
            WARActions.Upheaval,
            WARActions.Orogeny,
            WARActions.Onslaught,
            WARActions.Berserk,
            WARActions.InnerRelease,
            WARActions.Infuriate);

        // Defensive / Mitigation
        RegisterActions(warJobId,
            WARActions.Defiance,
            WARActions.Holmgang,
            WARActions.Vengeance,
            WARActions.Damnation,
            WARActions.RawIntuition,
            WARActions.Bloodwhetting,
            WARActions.ThrillOfBattle,
            WARActions.Equilibrium,
            WARActions.ShakeItOff,
            WARActions.NascentFlash);

        // Role Actions
        RegisterActions(warJobId,
            WARActions.Rampart,
            WARActions.Reprisal,
            WARActions.Provoke,
            WARActions.Shirk,
            WARActions.ArmsLength,
            WARActions.LowBlow,
            WARActions.Interject);

        // Marauder base class — starter subset
        RegisterActions(mrdJobId,
            WARActions.HeavySwing,
            WARActions.Maim,
            WARActions.Overpower,
            WARActions.Tomahawk,
            WARActions.Provoke,
            WARActions.Shirk,
            WARActions.Rampart);
    }

    /// <summary>
    /// Registers all Paladin (PLD) and Gladiator (GLA) actions.
    /// </summary>
    private void RegisterPaladinActions()
    {
        const uint pldJobId = JobRegistry.Paladin;
        const uint glaJobId = JobRegistry.Gladiator;

        // Combo GCDs
        RegisterActions(pldJobId,
            PLDActions.FastBlade,
            PLDActions.RiotBlade,
            PLDActions.RoyalAuthority,
            PLDActions.RageOfHalone,
            PLDActions.GoringBlade,
            PLDActions.BladeOfHonor,
            PLDActions.Atonement,
            PLDActions.Supplication,
            PLDActions.Sepulchre,
            PLDActions.TotalEclipse,
            PLDActions.Prominence);

        // Holy Spirit / Requiescat
        RegisterActions(pldJobId,
            PLDActions.HolySpirit,
            PLDActions.HolyCircle,
            PLDActions.Confiteor,
            PLDActions.BladeOfFaith,
            PLDActions.BladeOfTruth,
            PLDActions.BladeOfValor);

        // oGCDs
        RegisterActions(pldJobId,
            PLDActions.CircleOfScorn,
            PLDActions.Expiacion,
            PLDActions.SpiritsWithin,
            PLDActions.Intervene,
            PLDActions.FightOrFlight,
            PLDActions.Requiescat);

        // Defensive / Mitigation
        RegisterActions(pldJobId,
            PLDActions.IronWill,
            PLDActions.Sheltron,
            PLDActions.HolySheltron,
            PLDActions.Sentinel,
            PLDActions.Guardian,
            PLDActions.Bulwark,
            PLDActions.HallowedGround,
            PLDActions.DivineVeil,
            PLDActions.PassageOfArms,
            PLDActions.Cover,
            PLDActions.Clemency);

        // Role Actions
        RegisterActions(pldJobId,
            PLDActions.Rampart,
            PLDActions.Reprisal,
            PLDActions.Provoke,
            PLDActions.Shirk,
            PLDActions.ArmsLength,
            PLDActions.LowBlow,
            PLDActions.Interject,
            PLDActions.ShieldLob);

        // Gladiator base class — starter subset
        RegisterActions(glaJobId,
            PLDActions.FastBlade,
            PLDActions.RiotBlade,
            PLDActions.TotalEclipse,
            PLDActions.ShieldLob,
            PLDActions.Provoke,
            PLDActions.Shirk,
            PLDActions.Rampart);
    }

    /// <summary>
    /// Registers all Dark Knight (DRK) actions.
    /// </summary>
    private void RegisterDarkKnightActions()
    {
        const uint drkJobId = JobRegistry.DarkKnight;

        // Combo GCDs
        RegisterActions(drkJobId,
            DRKActions.HardSlash,
            DRKActions.SyphonStrike,
            DRKActions.Souleater,
            DRKActions.Unleash,
            DRKActions.StalwartSoul);

        // Spenders
        RegisterActions(drkJobId,
            DRKActions.Bloodspiller,
            DRKActions.Quietus,
            DRKActions.ScarletDelirium,
            DRKActions.Comeuppance,
            DRKActions.Torcleaver,
            DRKActions.Disesteem);

        // oGCDs — Damage
        RegisterActions(drkJobId,
            DRKActions.EdgeOfDarkness,
            DRKActions.EdgeOfShadow,
            DRKActions.FloodOfDarkness,
            DRKActions.FloodOfShadow,
            DRKActions.Shadowbringer,
            DRKActions.CarveAndSpit,
            DRKActions.AbyssalDrain,
            DRKActions.SaltedEarth,
            DRKActions.SaltAndDarkness,
            DRKActions.Plunge,
            DRKActions.Shadowstride);

        // Buffs / Defensive
        RegisterActions(drkJobId,
            DRKActions.BloodWeapon,
            DRKActions.Delirium,
            DRKActions.LivingShadow,
            DRKActions.Grit,
            DRKActions.TheBlackestNight,
            DRKActions.LivingDead,
            DRKActions.ShadowWall,
            DRKActions.ShadowedVigil,
            DRKActions.DarkMind,
            DRKActions.DarkMissionary,
            DRKActions.Oblation,
            DRKActions.Unmend);

        // Role Actions
        RegisterActions(drkJobId,
            DRKActions.Rampart,
            DRKActions.Reprisal,
            DRKActions.Provoke,
            DRKActions.Shirk,
            DRKActions.ArmsLength,
            DRKActions.LowBlow,
            DRKActions.Interject);
    }

    /// <summary>
    /// Registers all Gunbreaker (GNB) actions.
    /// </summary>
    private void RegisterGunbreakerActions()
    {
        const uint gnbJobId = JobRegistry.Gunbreaker;

        // Combo GCDs
        RegisterActions(gnbJobId,
            GNBActions.KeenEdge,
            GNBActions.BrutalShell,
            GNBActions.SolidBarrel,
            GNBActions.DemonSlice,
            GNBActions.DemonSlaughter);

        // Gnashing Fang combo
        RegisterActions(gnbJobId,
            GNBActions.GnashingFang,
            GNBActions.SavageClaw,
            GNBActions.WickedTalon,
            GNBActions.Continuation,
            GNBActions.JugularRip,
            GNBActions.AbdomenTear,
            GNBActions.EyeGouge,
            GNBActions.Hypervelocity);

        // Spenders
        RegisterActions(gnbJobId,
            GNBActions.BurstStrike,
            GNBActions.FatedCircle,
            GNBActions.DoubleDown,
            GNBActions.ReignOfBeasts,
            GNBActions.NobleBlood,
            GNBActions.LionHeart);

        // oGCDs
        RegisterActions(gnbJobId,
            GNBActions.LightningShot,
            GNBActions.RoughDivide,
            GNBActions.Trajectory,
            GNBActions.DangerZone,
            GNBActions.BlastingZone,
            GNBActions.BowShock,
            GNBActions.SonicBreak,
            GNBActions.NoMercy,
            GNBActions.Bloodfest);

        // Defensive / Mitigation
        RegisterActions(gnbJobId,
            GNBActions.RoyalGuard,
            GNBActions.Camouflage,
            GNBActions.Nebula,
            GNBActions.GreatNebula,
            GNBActions.HeartOfStone,
            GNBActions.HeartOfCorundum,
            GNBActions.Superbolide,
            GNBActions.Aurora,
            GNBActions.HeartOfLight);

        // Role Actions
        RegisterActions(gnbJobId,
            GNBActions.Rampart,
            GNBActions.Reprisal,
            GNBActions.Provoke,
            GNBActions.Shirk,
            GNBActions.ArmsLength,
            GNBActions.LowBlow,
            GNBActions.Interject);
    }

    /// <summary>
    /// Registers all Monk (MNK) and Pugilist (PGL) actions.
    /// </summary>
    private void RegisterMonkActions()
    {
        const uint mnkJobId = JobRegistry.Monk;
        const uint pglJobId = JobRegistry.Pugilist;

        // Form GCDs
        RegisterActions(mnkJobId,
            MNKActions.Bootshine,
            MNKActions.DragonKick,
            MNKActions.LeapingOpo,
            MNKActions.TrueStrike,
            MNKActions.TwinSnakes,
            MNKActions.RisingRaptor,
            MNKActions.SnapPunch,
            MNKActions.Demolish,
            MNKActions.PouncingCoeurl,
            MNKActions.ArmOfTheDestroyer,
            MNKActions.ShadowOfTheDestroyer,
            MNKActions.FourPointFury,
            MNKActions.Rockbreaker);

        // Burst / Finishers
        RegisterActions(mnkJobId,
            MNKActions.ElixirField,
            MNKActions.FlintStrike,
            MNKActions.CelestialRevolution,
            MNKActions.RisingPhoenix,
            MNKActions.PhantomRush,
            MNKActions.ElixirBurst,
            MNKActions.WindsReply,
            MNKActions.FiresReply);

        // Chakra / oGCDs
        RegisterActions(mnkJobId,
            MNKActions.TheForbiddenChakra,
            MNKActions.Enlightenment,
            MNKActions.HowlingFist,
            MNKActions.SteelPeak,
            MNKActions.RiddleOfFire,
            MNKActions.Brotherhood,
            MNKActions.PerfectBalance,
            MNKActions.RiddleOfWind,
            MNKActions.RiddleOfEarth,
            MNKActions.Thunderclap,
            MNKActions.Mantra,
            MNKActions.Meditation,
            MNKActions.Anatman,
            MNKActions.SixSidedStar,
            MNKActions.FormShift);

        // Role Actions
        RegisterActions(mnkJobId,
            MNKActions.SecondWind,
            MNKActions.Bloodbath,
            MNKActions.Feint,
            MNKActions.ArmsLength,
            MNKActions.TrueNorth,
            MNKActions.LegSweep);

        // Pugilist base class
        RegisterActions(pglJobId,
            MNKActions.Bootshine,
            MNKActions.TrueStrike,
            MNKActions.SnapPunch,
            MNKActions.ArmOfTheDestroyer,
            MNKActions.Mantra,
            MNKActions.SecondWind,
            MNKActions.LegSweep);
    }

    /// <summary>
    /// Registers all Dragoon (DRG) and Lancer (LNC) actions.
    /// </summary>
    private void RegisterDragoonActions()
    {
        const uint drgJobId = JobRegistry.Dragoon;
        const uint lncJobId = JobRegistry.Lancer;

        // Combo GCDs
        RegisterActions(drgJobId,
            DRGActions.TrueThrust,
            DRGActions.VorpalThrust,
            DRGActions.FullThrust,
            DRGActions.HeavensThrust,
            DRGActions.Disembowel,
            DRGActions.ChaosThrust,
            DRGActions.ChaoticSpring,
            DRGActions.FangAndClaw,
            DRGActions.WheelingThrust,
            DRGActions.Drakesbane,
            DRGActions.DoomSpike,
            DRGActions.SonicThrust,
            DRGActions.CoerthanTorment);

        // Jumps / oGCDs
        RegisterActions(drgJobId,
            DRGActions.Jump,
            DRGActions.HighJump,
            DRGActions.MirageDive,
            DRGActions.SpineshatterDive,
            DRGActions.DragonfireDive,
            DRGActions.Geirskogul,
            DRGActions.Nastrond,
            DRGActions.Stardiver,
            DRGActions.WyrmwindThrust,
            DRGActions.RiseOfTheDragon,
            DRGActions.Starcross);

        // Buffs / Utility
        RegisterActions(drgJobId,
            DRGActions.LanceCharge,
            DRGActions.BattleLitany,
            DRGActions.LifeSurge,
            DRGActions.DragonSight,
            DRGActions.PiercingTalon,
            DRGActions.ElusiveJump,
            DRGActions.WingedGlide);

        // Role Actions
        RegisterActions(drgJobId,
            DRGActions.SecondWind,
            DRGActions.Bloodbath,
            DRGActions.Feint,
            DRGActions.ArmsLength,
            DRGActions.TrueNorth,
            DRGActions.LegSweep);

        // Lancer base class
        RegisterActions(lncJobId,
            DRGActions.TrueThrust,
            DRGActions.VorpalThrust,
            DRGActions.DoomSpike,
            DRGActions.PiercingTalon,
            DRGActions.SecondWind,
            DRGActions.LegSweep);
    }

    /// <summary>
    /// Registers all Ninja (NIN) and Rogue (ROG) actions.
    /// </summary>
    private void RegisterNinjaActions()
    {
        const uint ninJobId = JobRegistry.Ninja;
        const uint rogJobId = JobRegistry.Rogue;

        // Combo GCDs
        RegisterActions(ninJobId,
            NINActions.SpinningEdge,
            NINActions.GustSlash,
            NINActions.AeolianEdge,
            NINActions.ArmorCrush,
            NINActions.DeathBlossom,
            NINActions.HakkeMujinsatsu);

        // Ninjutsu
        RegisterActions(ninJobId,
            NINActions.Ten,
            NINActions.Chi,
            NINActions.Jin,
            NINActions.Ninjutsu,
            NINActions.FumaShuriken,
            NINActions.Raiton,
            NINActions.Katon,
            NINActions.Hyoton,
            NINActions.Huton,
            NINActions.Doton,
            NINActions.Suiton,
            NINActions.GokaMekkyaku,
            NINActions.HyoshoRanryu,
            NINActions.RabbitMedium);

        // oGCDs
        RegisterActions(ninJobId,
            NINActions.Bhavacakra,
            NINActions.HellfrogMedium,
            NINActions.ZeshoMeppo,
            NINActions.DeathfrogMedium,
            NINActions.Mug,
            NINActions.Dokumori,
            NINActions.KunaisBane,
            NINActions.TrickAttack,
            NINActions.Kassatsu,
            NINActions.TenChiJin,
            NINActions.Bunshin,
            NINActions.PhantomKamaitachi,
            NINActions.Meisui,
            NINActions.ForkedRaiju,
            NINActions.FleetingRaiju,
            NINActions.TenriJindo,
            NINActions.Shukuchi,
            NINActions.ShadeShift);

        // Role Actions
        RegisterActions(ninJobId,
            NINActions.SecondWind,
            NINActions.Bloodbath,
            NINActions.Feint,
            NINActions.ArmsLength,
            NINActions.TrueNorth,
            NINActions.LegSweep);

        // Rogue base class
        RegisterActions(rogJobId,
            NINActions.SpinningEdge,
            NINActions.GustSlash,
            NINActions.AeolianEdge,
            NINActions.DeathBlossom,
            NINActions.ShadeShift,
            NINActions.SecondWind,
            NINActions.LegSweep);
    }

    /// <summary>
    /// Registers all Samurai (SAM) actions.
    /// </summary>
    private void RegisterSamuraiActions()
    {
        const uint samJobId = JobRegistry.Samurai;

        // Combo GCDs
        RegisterActions(samJobId,
            SAMActions.Hakaze,
            SAMActions.Gyofu,
            SAMActions.Jinpu,
            SAMActions.Shifu,
            SAMActions.Yukikaze,
            SAMActions.Gekko,
            SAMActions.Kasha,
            SAMActions.Fuko,
            SAMActions.Fuga,
            SAMActions.Mangetsu,
            SAMActions.Oka);

        // Iaijutsu / Tsubame
        RegisterActions(samJobId,
            SAMActions.Iaijutsu,
            SAMActions.Higanbana,
            SAMActions.TenkaGoken,
            SAMActions.MidareSetsugekka,
            SAMActions.TsubameGaeshi,
            SAMActions.KaeshiHiganbana,
            SAMActions.KaeshiGoken,
            SAMActions.KaeshiSetsugekka,
            SAMActions.OgiNamikiri,
            SAMActions.KaeshiNamikiri);

        // oGCDs
        RegisterActions(samJobId,
            SAMActions.Shinten,
            SAMActions.Kyuten,
            SAMActions.Senei,
            SAMActions.Guren,
            SAMActions.Zanshin,
            SAMActions.Shoha,
            SAMActions.MeikyoShisui,
            SAMActions.Ikishoten,
            SAMActions.Hagakure,
            SAMActions.Gyoten,
            SAMActions.Yaten,
            SAMActions.Enpi,
            SAMActions.ThirdEye,
            SAMActions.Tengentsu);

        // Role Actions
        RegisterActions(samJobId,
            SAMActions.SecondWind,
            SAMActions.Bloodbath,
            SAMActions.Feint,
            SAMActions.ArmsLength,
            SAMActions.TrueNorth,
            SAMActions.LegSweep);
    }

    /// <summary>
    /// Registers all Reaper (RPR) actions.
    /// </summary>
    private void RegisterReaperActions()
    {
        const uint rprJobId = JobRegistry.Reaper;

        // Combo GCDs
        RegisterActions(rprJobId,
            RPRActions.Slice,
            RPRActions.WaxingSlice,
            RPRActions.InfernalSlice,
            RPRActions.SpinningScythe,
            RPRActions.NightmareScythe);

        // DoT / Shadow
        RegisterActions(rprJobId,
            RPRActions.ShadowOfDeath,
            RPRActions.WhorlOfDeath);

        // Shroud GCDs
        RegisterActions(rprJobId,
            RPRActions.Gibbet,
            RPRActions.Gallows,
            RPRActions.Guillotine,
            RPRActions.VoidReaping,
            RPRActions.CrossReaping,
            RPRActions.GrimReaping,
            RPRActions.Communio,
            RPRActions.Perfectio);

        // oGCDs
        RegisterActions(rprJobId,
            RPRActions.BloodStalk,
            RPRActions.GrimSwathe,
            RPRActions.Gluttony,
            RPRActions.UnveiledGibbet,
            RPRActions.UnveiledGallows,
            RPRActions.Enshroud,
            RPRActions.LemuresSlice,
            RPRActions.LemuresScythe,
            RPRActions.Sacrificium,
            RPRActions.ArcaneCircle,
            RPRActions.PlentifulHarvest,
            RPRActions.SoulSlice,
            RPRActions.SoulScythe,
            RPRActions.HarvestMoon,
            RPRActions.Soulsow,
            RPRActions.HellsIngress,
            RPRActions.HellsEgress,
            RPRActions.Regress,
            RPRActions.Harpe,
            RPRActions.ArcaneCrest);

        // Role Actions
        RegisterActions(rprJobId,
            RPRActions.SecondWind,
            RPRActions.Bloodbath,
            RPRActions.Feint,
            RPRActions.ArmsLength,
            RPRActions.TrueNorth,
            RPRActions.LegSweep);
    }

    /// <summary>
    /// Registers all Viper (VPR) actions.
    /// </summary>
    private void RegisterViperActions()
    {
        const uint vprJobId = JobRegistry.Viper;

        // Combo GCDs — single target
        RegisterActions(vprJobId,
            VPRActions.SteelFangs,
            VPRActions.ReavingFangs,
            VPRActions.HuntersSting,
            VPRActions.SwiftskinsString,
            VPRActions.FlankstingStrike,
            VPRActions.FlanksbaneFang,
            VPRActions.HindstingStrike,
            VPRActions.HindsbaneFang);

        // Combo GCDs — AoE
        RegisterActions(vprJobId,
            VPRActions.SteelMaw,
            VPRActions.ReavingMaw,
            VPRActions.HuntersBite,
            VPRActions.SwiftskinsBite,
            VPRActions.JaggedMaw,
            VPRActions.BloodiedMaw);

        // Vicepit / Vicewinder
        RegisterActions(vprJobId,
            VPRActions.Vicewinder,
            VPRActions.HuntersCoil,
            VPRActions.SwiftskinsCoil,
            VPRActions.Vicepit,
            VPRActions.HuntersDen,
            VPRActions.SwiftskinsDen);

        // Twinblade oGCDs
        RegisterActions(vprJobId,
            VPRActions.Twinfang,
            VPRActions.Twinblood,
            VPRActions.TwinfangBite,
            VPRActions.TwinbloodBite,
            VPRActions.TwinfangThresh,
            VPRActions.TwinbloodThresh);

        // Reawaken
        RegisterActions(vprJobId,
            VPRActions.UncoiledFury,
            VPRActions.UncoiledTwinfang,
            VPRActions.UncoiledTwinblood,
            VPRActions.WrithingSnap,
            VPRActions.Reawaken,
            VPRActions.FirstGeneration,
            VPRActions.SecondGeneration,
            VPRActions.ThirdGeneration,
            VPRActions.FourthGeneration,
            VPRActions.FirstLegacy,
            VPRActions.SecondLegacy,
            VPRActions.ThirdLegacy,
            VPRActions.FourthLegacy,
            VPRActions.Ouroboros,
            VPRActions.SerpentsIre,
            VPRActions.DeathRattle,
            VPRActions.LastLash,
            VPRActions.SerpentsTail);

        // Role Actions
        RegisterActions(vprJobId,
            VPRActions.SecondWind,
            VPRActions.Bloodbath,
            VPRActions.Feint,
            VPRActions.ArmsLength,
            VPRActions.TrueNorth,
            VPRActions.LegSweep);
    }

    /// <summary>
    /// Registers all Bard (BRD) and Archer (ARC) actions.
    /// </summary>
    private void RegisterBardActions()
    {
        const uint brdJobId = JobRegistry.Bard;
        const uint arcJobId = JobRegistry.Archer;

        // GCDs
        RegisterActions(brdJobId,
            BRDActions.HeavyShot,
            BRDActions.BurstShot,
            BRDActions.StraightShot,
            BRDActions.RefulgentArrow,
            BRDActions.VenomousBite,
            BRDActions.CausticBite,
            BRDActions.Windbite,
            BRDActions.Stormbite,
            BRDActions.IronJaws,
            BRDActions.ApexArrow,
            BRDActions.BlastArrow,
            BRDActions.ResonantArrow,
            BRDActions.RadiantEncore,
            BRDActions.QuickNock,
            BRDActions.Ladonsbite,
            BRDActions.Shadowbite);

        // Songs / oGCDs
        RegisterActions(brdJobId,
            BRDActions.MagesBallad,
            BRDActions.ArmysPaeon,
            BRDActions.WanderersMinuet,
            BRDActions.Bloodletter,
            BRDActions.HeartbreakShot,
            BRDActions.RainOfDeath,
            BRDActions.EmpyrealArrow,
            BRDActions.Sidewinder,
            BRDActions.PitchPerfect,
            BRDActions.RagingStrikes,
            BRDActions.BattleVoice,
            BRDActions.RadiantFinale,
            BRDActions.Barrage);

        // Utility
        RegisterActions(brdJobId,
            BRDActions.Troubadour,
            BRDActions.NaturesMinne,
            BRDActions.WardensPaean,
            BRDActions.RepellingShot);

        // Role Actions
        RegisterActions(brdJobId,
            BRDActions.SecondWind,
            BRDActions.ArmsLength,
            BRDActions.HeadGraze,
            BRDActions.Peloton);

        // Archer base class
        RegisterActions(arcJobId,
            BRDActions.HeavyShot,
            BRDActions.StraightShot,
            BRDActions.VenomousBite,
            BRDActions.QuickNock,
            BRDActions.RepellingShot,
            BRDActions.SecondWind,
            BRDActions.HeadGraze);
    }

    /// <summary>
    /// Registers all Machinist (MCH) actions.
    /// </summary>
    private void RegisterMachinistActions()
    {
        const uint mchJobId = JobRegistry.Machinist;

        // Combo GCDs
        RegisterActions(mchJobId,
            MCHActions.HeatedSplitShot,
            MCHActions.SplitShot,
            MCHActions.HeatedSlugShot,
            MCHActions.SlugShot,
            MCHActions.HeatedCleanShot,
            MCHActions.CleanShot);

        // Special GCDs
        RegisterActions(mchJobId,
            MCHActions.Drill,
            MCHActions.AirAnchor,
            MCHActions.HotShot,
            MCHActions.ChainSaw,
            MCHActions.Excavator,
            MCHActions.FullMetalField,
            MCHActions.HeatBlast,
            MCHActions.BlazingShot,
            MCHActions.AutoCrossbow,
            MCHActions.SpreadShot,
            MCHActions.Scattergun,
            MCHActions.Bioblaster);

        // oGCDs
        RegisterActions(mchJobId,
            MCHActions.GaussRound,
            MCHActions.Ricochet,
            MCHActions.DoubleCheck,
            MCHActions.Checkmate,
            MCHActions.Reassemble,
            MCHActions.BarrelStabilizer,
            MCHActions.Wildfire,
            MCHActions.Hypercharge,
            MCHActions.AutomatonQueen,
            MCHActions.RookAutoturret,
            MCHActions.QueenOverdrive,
            MCHActions.RookOverdrive);

        // Role Actions
        RegisterActions(mchJobId,
            MCHActions.SecondWind,
            MCHActions.ArmsLength,
            MCHActions.HeadGraze,
            MCHActions.Peloton,
            MCHActions.Tactician,
            MCHActions.Dismantle);
    }

    /// <summary>
    /// Registers all Dancer (DNC) actions.
    /// </summary>
    private void RegisterDancerActions()
    {
        const uint dncJobId = JobRegistry.Dancer;

        // GCDs — Procs
        RegisterActions(dncJobId,
            DNCActions.Cascade,
            DNCActions.Fountain,
            DNCActions.ReverseCascade,
            DNCActions.Fountainfall,
            DNCActions.Windmill,
            DNCActions.Bladeshower,
            DNCActions.RisingWindmill,
            DNCActions.Bloodshower);

        // Dance Steps
        RegisterActions(dncJobId,
            DNCActions.StandardStep,
            DNCActions.TechnicalStep,
            DNCActions.Emboite,
            DNCActions.Entrechat,
            DNCActions.Jete,
            DNCActions.Pirouette,
            DNCActions.StandardFinish,
            DNCActions.TechnicalFinish,
            DNCActions.Tillana);

        // Esprit Spenders
        RegisterActions(dncJobId,
            DNCActions.SaberDance,
            DNCActions.DanceOfTheDawn,
            DNCActions.StarfallDance,
            DNCActions.LastDance,
            DNCActions.FinishingMove);

        // Feather oGCDs
        RegisterActions(dncJobId,
            DNCActions.FanDance,
            DNCActions.FanDanceII,
            DNCActions.FanDanceIII,
            DNCActions.FanDanceIV);

        // Buffs / Utility
        RegisterActions(dncJobId,
            DNCActions.Devilment,
            DNCActions.Flourish,
            DNCActions.ClosedPosition,
            DNCActions.Ending,
            DNCActions.ShieldSamba,
            DNCActions.CuringWaltz,
            DNCActions.Improvisation,
            DNCActions.ImprovisedFinish,
            DNCActions.EnAvant);

        // Role Actions
        RegisterActions(dncJobId,
            DNCActions.SecondWind,
            DNCActions.ArmsLength,
            DNCActions.HeadGraze,
            DNCActions.Peloton);
    }

    /// <summary>
    /// Registers all Black Mage (BLM) and Thaumaturge (THM) actions.
    /// </summary>
    private void RegisterBlackMageActions()
    {
        const uint blmJobId = JobRegistry.BlackMage;
        const uint thmJobId = JobRegistry.Thaumaturge;

        // Fire spells
        RegisterActions(blmJobId,
            BLMActions.Fire,
            BLMActions.Fire2,
            BLMActions.Fire3,
            BLMActions.Fire4,
            BLMActions.HighFire2,
            BLMActions.Despair,
            BLMActions.Flare,
            BLMActions.FlareStar);

        // Blizzard spells
        RegisterActions(blmJobId,
            BLMActions.Blizzard,
            BLMActions.Blizzard2,
            BLMActions.Blizzard3,
            BLMActions.Blizzard4,
            BLMActions.HighBlizzard2,
            BLMActions.Freeze,
            BLMActions.UmbralSoul);

        // Thunder DoTs
        RegisterActions(blmJobId,
            BLMActions.Thunder,
            BLMActions.Thunder2,
            BLMActions.Thunder3,
            BLMActions.Thunder4,
            BLMActions.HighThunder,
            BLMActions.HighThunder2);

        // Special GCDs
        RegisterActions(blmJobId,
            BLMActions.Xenoglossy,
            BLMActions.Foul,
            BLMActions.Paradox,
            BLMActions.Scathe);

        // oGCDs / Utility
        RegisterActions(blmJobId,
            BLMActions.Transpose,
            BLMActions.Triplecast,
            BLMActions.Manafont,
            BLMActions.Amplifier,
            BLMActions.LeyLines,
            BLMActions.BetweenTheLines,
            BLMActions.Retrace,
            BLMActions.Manaward);

        // Role Actions
        RegisterActions(blmJobId,
            BLMActions.Swiftcast,
            BLMActions.Surecast,
            BLMActions.LucidDreaming,
            BLMActions.Addle,
            BLMActions.Sleep);

        // Thaumaturge base class
        RegisterActions(thmJobId,
            BLMActions.Fire,
            BLMActions.Blizzard,
            BLMActions.Thunder,
            BLMActions.Fire2,
            BLMActions.Blizzard2,
            BLMActions.Transpose,
            BLMActions.Swiftcast,
            BLMActions.LucidDreaming,
            BLMActions.Addle);
    }

    /// <summary>
    /// Registers all Summoner (SMN) actions.
    /// Note: Arcanist (ACN) base class actions are shared with Scholar;
    /// they are already registered in RegisterScholarActions().
    /// </summary>
    private void RegisterSummonerActions()
    {
        const uint smnJobId = JobRegistry.Summoner;

        // Ruin progression
        RegisterActions(smnJobId,
            SMNActions.Ruin,
            SMNActions.Ruin2,
            SMNActions.Ruin3,
            SMNActions.Ruin4,
            SMNActions.Outburst,
            SMNActions.TriDisaster);

        // Primal GCDs
        RegisterActions(smnJobId,
            SMNActions.AstralImpulse,
            SMNActions.AstralFlare,
            SMNActions.FountainOfFire,
            SMNActions.BrandOfPurgatory,
            SMNActions.UmbralImpulse,
            SMNActions.UmbralFlare,
            SMNActions.RubyRite,
            SMNActions.RubyCatastrophe,
            SMNActions.TopazRite,
            SMNActions.TopazCatastrophe,
            SMNActions.EmeraldRite,
            SMNActions.EmeraldCatastrophe,
            SMNActions.CrimsonCyclone,
            SMNActions.CrimsonStrike,
            SMNActions.Slipstream);

        // Summons
        RegisterActions(smnJobId,
            SMNActions.SummonCarbuncle,
            SMNActions.SummonBahamut,
            SMNActions.SummonPhoenix,
            SMNActions.SummonSolarBahamut,
            SMNActions.SummonIfrit,
            SMNActions.SummonIfrit2,
            SMNActions.SummonTitan,
            SMNActions.SummonTitan2,
            SMNActions.SummonGaruda,
            SMNActions.SummonGaruda2);

        // oGCDs
        RegisterActions(smnJobId,
            SMNActions.EnergyDrain,
            SMNActions.EnergySiphon,
            SMNActions.Necrotize,
            SMNActions.Fester,
            SMNActions.Painflare,
            SMNActions.SearingLight,
            SMNActions.SearingFlash,
            SMNActions.RadiantAegis,
            SMNActions.EnkindleBahamut,
            SMNActions.EnkindlePhoenix,
            SMNActions.EnkindleSolarBahamut,
            SMNActions.Deathflare,
            SMNActions.Rekindle,
            SMNActions.Sunflare,
            SMNActions.LuxSolaris,
            SMNActions.MountainBuster);

        // Role Actions
        RegisterActions(smnJobId,
            SMNActions.Swiftcast,
            SMNActions.LucidDreaming,
            SMNActions.Addle,
            SMNActions.Surecast,
            SMNActions.Resurrection);
    }

    /// <summary>
    /// Registers all Red Mage (RDM) actions.
    /// </summary>
    private void RegisterRedMageActions()
    {
        const uint rdmJobId = JobRegistry.RedMage;

        // Cast GCDs
        RegisterActions(rdmJobId,
            RDMActions.Jolt,
            RDMActions.Jolt2,
            RDMActions.Jolt3,
            RDMActions.Verthunder,
            RDMActions.Veraero,
            RDMActions.Verthunder2,
            RDMActions.Veraero2,
            RDMActions.Verthunder3,
            RDMActions.Veraero3,
            RDMActions.Verfire,
            RDMActions.Verstone,
            RDMActions.Impact);

        // Melee combo
        RegisterActions(rdmJobId,
            RDMActions.Riposte,
            RDMActions.EnchantedRiposte,
            RDMActions.Zwerchhau,
            RDMActions.EnchantedZwerchhau,
            RDMActions.Redoublement,
            RDMActions.EnchantedRedoublement,
            RDMActions.Verflare,
            RDMActions.Verholy,
            RDMActions.Scorch,
            RDMActions.Resolution,
            RDMActions.GrandImpact,
            RDMActions.EnchantedMoulinet,
            RDMActions.EnchantedMoulinetDeux,
            RDMActions.EnchantedMoulinetTrois);

        // oGCDs
        RegisterActions(rdmJobId,
            RDMActions.Fleche,
            RDMActions.ContreSixte,
            RDMActions.CorpsACorps,
            RDMActions.Engagement,
            RDMActions.Displacement,
            RDMActions.ViceOfThorns,
            RDMActions.Prefulgence,
            RDMActions.Embolden,
            RDMActions.Manafication,
            RDMActions.Acceleration);

        // Role Actions
        RegisterActions(rdmJobId,
            RDMActions.Swiftcast,
            RDMActions.LucidDreaming,
            RDMActions.Vercure,
            RDMActions.Verraise,
            RDMActions.MagickBarrier,
            RDMActions.Addle,
            RDMActions.Surecast);
    }

    /// <summary>
    /// Registers all Pictomancer (PCT) actions.
    /// </summary>
    private void RegisterPictomancerActions()
    {
        const uint pctJobId = JobRegistry.Pictomancer;

        // Basic paint GCDs
        RegisterActions(pctJobId,
            PCTActions.FireInRed,
            PCTActions.AeroInGreen,
            PCTActions.WaterInBlue,
            PCTActions.BlizzardInCyan,
            PCTActions.StoneInYellow,
            PCTActions.ThunderInMagenta,
            PCTActions.Fire2InRed,
            PCTActions.Aero2InGreen,
            PCTActions.Water2InBlue,
            PCTActions.Blizzard2InCyan,
            PCTActions.Stone2InYellow,
            PCTActions.Thunder2InMagenta,
            PCTActions.HolyInWhite,
            PCTActions.CometInBlack);

        // Motifs
        RegisterActions(pctJobId,
            PCTActions.CreatureMotif,
            PCTActions.PomMotif,
            PCTActions.WingMotif,
            PCTActions.ClawMotif,
            PCTActions.MawMotif,
            PCTActions.WeaponMotif,
            PCTActions.HammerMotif,
            PCTActions.LandscapeMotif,
            PCTActions.StarrySkyMotif);

        // Muse / Living Muse
        RegisterActions(pctJobId,
            PCTActions.LivingMuse,
            PCTActions.PomMuse,
            PCTActions.WingedMuse,
            PCTActions.ClawedMuse,
            PCTActions.FangedMuse,
            PCTActions.SteelMuse,
            PCTActions.StrikingMuse,
            PCTActions.ScenicMuse,
            PCTActions.StarryMuse);

        // Hammer / Finishers
        RegisterActions(pctJobId,
            PCTActions.HammerStamp,
            PCTActions.HammerBrush,
            PCTActions.PolishingHammer,
            PCTActions.MogOfTheAges,
            PCTActions.RetributionOfTheMadeen,
            PCTActions.RainbowDrip,
            PCTActions.StarPrism);

        // Utility
        RegisterActions(pctJobId,
            PCTActions.SubtractivePalette,
            PCTActions.TemperaCoat,
            PCTActions.TemperaGrassa,
            PCTActions.Smudge);

        // Role Actions
        RegisterActions(pctJobId,
            PCTActions.Swiftcast,
            PCTActions.LucidDreaming,
            PCTActions.Surecast,
            PCTActions.Addle);
    }
}
