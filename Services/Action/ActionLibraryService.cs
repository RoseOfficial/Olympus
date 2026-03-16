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
        // Future: melee, ranged, casters
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
}
