using System;
using System.Collections.Generic;
using System.Linq;
using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>Defines a named group of level-gated actions for the spell checklist.</summary>
public record ChecklistGroup(string Name, Func<byte, ActionDefinition[]> GetActions);

/// <summary>
/// Per-job checklist definitions used by the Debug Checklist tab.
/// CNJ (6) maps to the WHM entry. Arcanist (26) maps to the SCH entry.
/// Starter classes and unrecognised job IDs return an empty array.
/// </summary>
public static class SpellChecklistRegistry
{
    /// <summary>
    /// Returns the checklist groups for the given job ID, or an empty array for
    /// unrecognised or base-class jobs.
    /// </summary>
    public static ChecklistGroup[] GetChecklist(uint jobId)
    {
        if (JobRegistry.IsWhiteMage(jobId)) jobId = JobRegistry.WhiteMage;
        if (JobRegistry.IsScholar(jobId))   jobId = JobRegistry.Scholar;

        return jobId switch
        {
            JobRegistry.WhiteMage   => _whm,
            JobRegistry.Scholar     => _sch,
            JobRegistry.Sage        => _sge,
            JobRegistry.Astrologian => _ast,
            JobRegistry.Warrior     => _war,
            JobRegistry.DarkKnight  => _drk,
            JobRegistry.Paladin     => _pld,
            JobRegistry.Gunbreaker  => _gnb,
            JobRegistry.Dragoon     => _drg,
            JobRegistry.Monk        => _mnk,
            JobRegistry.Ninja       => _nin,
            JobRegistry.Samurai     => _sam,
            JobRegistry.Reaper      => _rpr,
            JobRegistry.Viper       => _vpr,
            JobRegistry.Bard        => _brd,
            JobRegistry.Machinist   => _mch,
            JobRegistry.Dancer      => _dnc,
            JobRegistry.BlackMage   => _blm,
            JobRegistry.Summoner    => _smn,
            JobRegistry.RedMage     => _rdm,
            JobRegistry.Pictomancer => _pct,
            _                       => Array.Empty<ChecklistGroup>()
        };
    }

    // ── Healers ───────────────────────────────────────────────────────────

    private static readonly ChecklistGroup[] _whm =
    {
        new("GCD Damage",       l => new[] { WHMActions.Stone, WHMActions.StoneII, WHMActions.StoneIII, WHMActions.StoneIV, WHMActions.Glare, WHMActions.GlareIII }
                                        .Where(a => a.MinLevel <= l).TakeLast(1).ToArray()),
        new("GCD DoT",          l => WHMActions.DotGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Damage",   l => WHMActions.AoEDamageGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD Single Heals", l => new[] { WHMActions.Cure, WHMActions.CureII, WHMActions.CureIII, WHMActions.Regen, WHMActions.AfflatusSolace }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Heals",    l => WHMActions.AoEHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Single Heals",l => new[] { WHMActions.Benediction, WHMActions.Tetragrammaton, WHMActions.DivineBenison }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD AoE Heals",   l => WHMActions.AoEHealOgcds.Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { WHMActions.PresenceOfMind, WHMActions.ThinAir, WHMActions.Temperance, WHMActions.PlenaryIndulgence, WHMActions.Aquaveil, WHMActions.DivineCaress }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",          l => new[] { WHMActions.AetherialShift }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { WHMActions.Swiftcast, WHMActions.LucidDreaming, WHMActions.Surecast, WHMActions.Rescue, WHMActions.Esuna, WHMActions.Raise }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _sch =
    {
        new("GCD Damage",       l => SCHActions.DamageGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD DoT",          l => SCHActions.DotGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Damage",   l => SCHActions.AoEDamageGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD Single Heals", l => SCHActions.SingleHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Heals",    l => SCHActions.AoEHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Heals",       l => new[] { SCHActions.Lustrate, SCHActions.Excogitation, SCHActions.Protraction }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD AoE Heals",   l => new[] { SCHActions.Indomitability, SCHActions.SacredSoil, SCHActions.WhisperingDawn, SCHActions.FeyBlessing, SCHActions.Consolation }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Fairy",            l => new[] { SCHActions.SummonEos, SCHActions.FeyIllumination, SCHActions.Aetherpact, SCHActions.DissolveUnion, SCHActions.SummonSeraph, SCHActions.Seraphism }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Aetherflow",       l => new[] { SCHActions.Aetherflow, SCHActions.EnergyDrain }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { SCHActions.ChainStratagem, SCHActions.Dissipation, SCHActions.Recitation, SCHActions.EmergencyTactics, SCHActions.DeploymentTactics, SCHActions.Expedient, SCHActions.BanefulImpaction }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { SCHActions.Swiftcast, SCHActions.LucidDreaming, SCHActions.Surecast, SCHActions.Rescue, SCHActions.Esuna, SCHActions.Resurrection }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _sge =
    {
        new("GCD Damage",       l => SGEActions.DamageGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD DoT",          l => SGEActions.DotGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Damage",   l => SGEActions.AoEDamageGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("Phlegma",          l => new[] { SGEActions.Phlegma, SGEActions.PhlegmaII, SGEActions.PhlegmaIII }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Toxikon",          l => new[] { SGEActions.Toxikon, SGEActions.ToxikonII }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("GCD Single Heals", l => SGEActions.SingleHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Heals",    l => SGEActions.AoEHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Single Heals",l => new[] { SGEActions.Druochole, SGEActions.Taurochole, SGEActions.Haima }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD AoE Heals",   l => new[] { SGEActions.Ixochole, SGEActions.Kerachole, SGEActions.PhysisII, SGEActions.Panhaima, SGEActions.Holos, SGEActions.Pepsis }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { SGEActions.Soteria, SGEActions.Zoe, SGEActions.Krasis, SGEActions.Pneuma, SGEActions.Rhizomata, SGEActions.Psyche, SGEActions.Philosophia }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Kardia / Eukrasia",l => new[] { SGEActions.Kardia, SGEActions.Eukrasia }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",          l => new[] { SGEActions.Icarus }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { SGEActions.Swiftcast, SGEActions.LucidDreaming, SGEActions.Surecast, SGEActions.Rescue, SGEActions.Esuna, SGEActions.Egeiro }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _ast =
    {
        new("GCD Damage",       l => ASTActions.DamageGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD DoT",          l => ASTActions.DotGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Damage",   l => ASTActions.AoEDamageGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD Special",      l => new[] { ASTActions.Macrocosmos }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("GCD Single Heals", l => new[] { ASTActions.Benefic, ASTActions.BeneficII, ASTActions.AspectedBenefic }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Heals",    l => ASTActions.AoEHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Single Heals",l => new[] { ASTActions.EssentialDignity, ASTActions.CelestialIntersection, ASTActions.Exaltation }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD AoE Heals",   l => new[] { ASTActions.CelestialOpposition, ASTActions.CollectiveUnconscious, ASTActions.EarthlyStar, ASTActions.StellarDetonation, ASTActions.Horoscope, ASTActions.HoroscopeEnd, ASTActions.Microcosmos }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Cards",            l => new[] { ASTActions.AstralDraw, ASTActions.UmbralDraw, ASTActions.PlayI, ASTActions.PlayII, ASTActions.PlayIII, ASTActions.MinorArcana, ASTActions.TheBalance, ASTActions.TheSpear, ASTActions.TheBole, ASTActions.TheArrow, ASTActions.TheEwer, ASTActions.TheSpire, ASTActions.LadyOfCrowns, ASTActions.LordOfCrowns }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { ASTActions.Divination, ASTActions.Oracle, ASTActions.Lightspeed, ASTActions.Synastry, ASTActions.NeutralSect, ASTActions.SunSign, ASTActions.Astrodyne }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { ASTActions.Swiftcast, ASTActions.LucidDreaming, ASTActions.Surecast, ASTActions.Rescue, ASTActions.Esuna, ASTActions.Ascend }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    // ── Tanks ─────────────────────────────────────────────────────────────

    private static readonly ChecklistGroup[] _war =
    {
        new("Combo GCDs",     l => new[] { WARActions.HeavySwing, WARActions.Maim, WARActions.StormsPath, WARActions.StormsEye }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Combo GCDs", l => new[] { WARActions.Overpower, WARActions.MythrilTempest }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders", l => new[] { WARActions.InnerBeast, WARActions.FellCleave, WARActions.InnerChaos, WARActions.SteelCyclone, WARActions.Decimate, WARActions.ChaoticCyclone, WARActions.PrimalRend, WARActions.PrimalRuination }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { WARActions.Upheaval, WARActions.Orogeny, WARActions.Onslaught }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { WARActions.Berserk, WARActions.InnerRelease, WARActions.Infuriate }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",      l => new[] { WARActions.Holmgang, WARActions.Vengeance, WARActions.Damnation, WARActions.RawIntuition, WARActions.Bloodwhetting, WARActions.ThrillOfBattle, WARActions.Equilibrium, WARActions.ShakeItOff, WARActions.NascentFlash }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",        l => new[] { WARActions.Tomahawk, WARActions.Defiance }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { WARActions.Rampart, WARActions.LowBlow, WARActions.Reprisal, WARActions.Provoke, WARActions.Shirk, WARActions.ArmsLength, WARActions.Interject }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _drk =
    {
        new("Combo GCDs",     l => new[] { DRKActions.HardSlash, DRKActions.SyphonStrike, DRKActions.Souleater }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Combo GCDs", l => new[] { DRKActions.Unleash, DRKActions.StalwartSoul }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Delirium Combo", l => new[] { DRKActions.Bloodspiller, DRKActions.Quietus, DRKActions.ScarletDelirium, DRKActions.Comeuppance, DRKActions.Torcleaver, DRKActions.Disesteem }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Edge / Flood",   l => new[] { DRKActions.EdgeOfDarkness, DRKActions.EdgeOfShadow, DRKActions.FloodOfDarkness, DRKActions.FloodOfShadow, DRKActions.Shadowbringer }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { DRKActions.CarveAndSpit, DRKActions.AbyssalDrain, DRKActions.SaltedEarth, DRKActions.SaltAndDarkness, DRKActions.Plunge, DRKActions.Shadowstride }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { DRKActions.BloodWeapon, DRKActions.Delirium, DRKActions.LivingShadow }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",      l => new[] { DRKActions.ShadowWall, DRKActions.ShadowedVigil, DRKActions.LivingDead, DRKActions.DarkMind, DRKActions.Oblation, DRKActions.DarkMissionary, DRKActions.TheBlackestNight }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",        l => new[] { DRKActions.Unmend, DRKActions.Grit }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { DRKActions.Rampart, DRKActions.LowBlow, DRKActions.Reprisal, DRKActions.Provoke, DRKActions.Shirk, DRKActions.ArmsLength, DRKActions.Interject }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _pld =
    {
        new("Combo GCDs",      l => new[] { PLDActions.FastBlade, PLDActions.RiotBlade, PLDActions.RageOfHalone, PLDActions.RoyalAuthority }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Combo GCDs",  l => new[] { PLDActions.TotalEclipse, PLDActions.Prominence }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("GCD DoT",         l => new[] { PLDActions.GoringBlade }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders",  l => new[] { PLDActions.HolySpirit, PLDActions.HolyCircle, PLDActions.Confiteor, PLDActions.BladeOfFaith, PLDActions.BladeOfTruth, PLDActions.BladeOfValor, PLDActions.BladeOfHonor, PLDActions.Atonement, PLDActions.Supplication, PLDActions.Sepulchre }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",     l => new[] { PLDActions.CircleOfScorn, PLDActions.Expiacion, PLDActions.SpiritsWithin, PLDActions.Intervene }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",           l => new[] { PLDActions.FightOrFlight, PLDActions.Requiescat }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",       l => new[] { PLDActions.Sheltron, PLDActions.HolySheltron, PLDActions.Sentinel, PLDActions.Guardian, PLDActions.Bulwark, PLDActions.HallowedGround, PLDActions.DivineVeil, PLDActions.PassageOfArms, PLDActions.Cover, PLDActions.Clemency }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",         l => new[] { PLDActions.ShieldLob, PLDActions.IronWill }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",    l => new[] { PLDActions.Rampart, PLDActions.LowBlow, PLDActions.Reprisal, PLDActions.Provoke, PLDActions.Shirk, PLDActions.ArmsLength, PLDActions.Interject }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _gnb =
    {
        new("Combo GCDs",        l => new[] { GNBActions.KeenEdge, GNBActions.BrutalShell, GNBActions.SolidBarrel }
                                          .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Combo GCDs",    l => new[] { GNBActions.DemonSlice, GNBActions.DemonSlaughter }
                                          .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders",    l => new[] { GNBActions.GnashingFang, GNBActions.SavageClaw, GNBActions.WickedTalon, GNBActions.BurstStrike, GNBActions.FatedCircle, GNBActions.DoubleDown, GNBActions.ReignOfBeasts, GNBActions.NobleBlood, GNBActions.LionHeart }
                                          .Where(a => a.MinLevel <= l).ToArray()),
        new("Continuation",      l => new[] { GNBActions.Continuation, GNBActions.JugularRip, GNBActions.AbdomenTear, GNBActions.EyeGouge, GNBActions.Hypervelocity }
                                          .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",       l => new[] { GNBActions.DangerZone, GNBActions.BlastingZone, GNBActions.BowShock, GNBActions.SonicBreak, GNBActions.RoughDivide, GNBActions.Trajectory }
                                          .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",             l => new[] { GNBActions.NoMercy, GNBActions.Bloodfest }
                                          .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",         l => new[] { GNBActions.Nebula, GNBActions.GreatNebula, GNBActions.HeartOfStone, GNBActions.HeartOfCorundum, GNBActions.Superbolide, GNBActions.Aurora, GNBActions.HeartOfLight, GNBActions.Camouflage }
                                          .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",           l => new[] { GNBActions.LightningShot, GNBActions.RoyalGuard }
                                          .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",      l => new[] { GNBActions.Rampart, GNBActions.LowBlow, GNBActions.Reprisal, GNBActions.Provoke, GNBActions.Shirk, GNBActions.ArmsLength, GNBActions.Interject }
                                          .Where(a => a.MinLevel <= l).ToArray()),
    };

    // ── Melee DPS ─────────────────────────────────────────────────────────

    private static readonly ChecklistGroup[] _drg =
    {
        new("Combo GCDs",   l => new[] { DRGActions.TrueThrust, DRGActions.VorpalThrust, DRGActions.FullThrust, DRGActions.HeavensThrust, DRGActions.Disembowel, DRGActions.ChaosThrust, DRGActions.ChaoticSpring, DRGActions.FangAndClaw, DRGActions.WheelingThrust, DRGActions.Drakesbane }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",     l => new[] { DRGActions.DoomSpike, DRGActions.SonicThrust, DRGActions.CoerthanTorment }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",  l => new[] { DRGActions.Jump, DRGActions.HighJump, DRGActions.MirageDive, DRGActions.SpineshatterDive, DRGActions.DragonfireDive, DRGActions.Geirskogul, DRGActions.Nastrond, DRGActions.Stardiver, DRGActions.WyrmwindThrust, DRGActions.RiseOfTheDragon, DRGActions.Starcross }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",        l => new[] { DRGActions.LanceCharge, DRGActions.BattleLitany, DRGActions.LifeSurge, DRGActions.DragonSight }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",      l => new[] { DRGActions.PiercingTalon, DRGActions.ElusiveJump, DRGActions.WingedGlide }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions", l => new[] { DRGActions.Feint, DRGActions.TrueNorth, DRGActions.Bloodbath, DRGActions.SecondWind, DRGActions.LegSweep, DRGActions.ArmsLength }
                                     .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _mnk =
    {
        new("Opo-opo GCDs", l => new[] { MNKActions.Bootshine, MNKActions.DragonKick, MNKActions.LeapingOpo }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Raptor GCDs",  l => new[] { MNKActions.TrueStrike, MNKActions.TwinSnakes, MNKActions.RisingRaptor }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Coeurl GCDs",  l => new[] { MNKActions.SnapPunch, MNKActions.Demolish, MNKActions.PouncingCoeurl }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",     l => new[] { MNKActions.ArmOfTheDestroyer, MNKActions.ShadowOfTheDestroyer, MNKActions.FourPointFury, MNKActions.Rockbreaker }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Blitz GCDs",   l => new[] { MNKActions.ElixirField, MNKActions.CelestialRevolution, MNKActions.FlintStrike, MNKActions.RisingPhoenix, MNKActions.PhantomRush, MNKActions.ElixirBurst, MNKActions.WindsReply, MNKActions.FiresReply }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Chakra",       l => new[] { MNKActions.SteelPeak, MNKActions.TheForbiddenChakra, MNKActions.HowlingFist, MNKActions.Enlightenment, MNKActions.Meditation }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",        l => new[] { MNKActions.RiddleOfFire, MNKActions.Brotherhood, MNKActions.PerfectBalance, MNKActions.RiddleOfWind, MNKActions.RiddleOfEarth, MNKActions.Mantra }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",      l => new[] { MNKActions.Thunderclap, MNKActions.FormShift, MNKActions.Anatman, MNKActions.SixSidedStar }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions", l => new[] { MNKActions.Feint, MNKActions.TrueNorth, MNKActions.Bloodbath, MNKActions.SecondWind, MNKActions.LegSweep, MNKActions.ArmsLength }
                                     .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _nin =
    {
        new("Combo GCDs",   l => new[] { NINActions.SpinningEdge, NINActions.GustSlash, NINActions.AeolianEdge, NINActions.ArmorCrush }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",     l => new[] { NINActions.DeathBlossom, NINActions.HakkeMujinsatsu }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Mudras",       l => new[] { NINActions.Ten, NINActions.Chi, NINActions.Jin, NINActions.Ninjutsu }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Ninjutsu",     l => new[] { NINActions.FumaShuriken, NINActions.Raiton, NINActions.Katon, NINActions.Hyoton, NINActions.Huton, NINActions.Doton, NINActions.Suiton, NINActions.GokaMekkyaku, NINActions.HyoshoRanryu, NINActions.RabbitMedium }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Ninki",        l => new[] { NINActions.Bhavacakra, NINActions.HellfrogMedium, NINActions.ZeshoMeppo, NINActions.DeathfrogMedium }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",  l => new[] { NINActions.Mug, NINActions.Dokumori, NINActions.KunaisBane, NINActions.TenriJindo, NINActions.ForkedRaiju, NINActions.FleetingRaiju, NINActions.PhantomKamaitachi }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",        l => new[] { NINActions.TrickAttack, NINActions.Kassatsu, NINActions.TenChiJin, NINActions.Bunshin, NINActions.Meisui }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",      l => new[] { NINActions.Shukuchi, NINActions.ShadeShift }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions", l => new[] { NINActions.Feint, NINActions.TrueNorth, NINActions.Bloodbath, NINActions.SecondWind, NINActions.LegSweep, NINActions.ArmsLength }
                                     .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _sam =
    {
        new("Combo GCDs",   l => new[] { SAMActions.Hakaze, SAMActions.Gyofu, SAMActions.Jinpu, SAMActions.Shifu, SAMActions.Yukikaze, SAMActions.Gekko, SAMActions.Kasha }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",     l => new[] { SAMActions.Fuga, SAMActions.Fuko, SAMActions.Mangetsu, SAMActions.Oka }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Iaijutsu",     l => new[] { SAMActions.Higanbana, SAMActions.TenkaGoken, SAMActions.MidareSetsugekka, SAMActions.TsubameGaeshi, SAMActions.KaeshiHiganbana, SAMActions.KaeshiGoken, SAMActions.KaeshiSetsugekka, SAMActions.OgiNamikiri, SAMActions.KaeshiNamikiri }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Kenki",        l => new[] { SAMActions.Shinten, SAMActions.Kyuten, SAMActions.Senei, SAMActions.Guren, SAMActions.Zanshin, SAMActions.Shoha }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",        l => new[] { SAMActions.MeikyoShisui, SAMActions.Ikishoten, SAMActions.Hagakure }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",      l => new[] { SAMActions.Enpi, SAMActions.Gyoten, SAMActions.Yaten, SAMActions.ThirdEye, SAMActions.Tengentsu }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions", l => new[] { SAMActions.Feint, SAMActions.TrueNorth, SAMActions.Bloodbath, SAMActions.SecondWind, SAMActions.LegSweep, SAMActions.ArmsLength }
                                     .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _rpr =
    {
        new("Combo GCDs",     l => new[] { RPRActions.Slice, RPRActions.WaxingSlice, RPRActions.InfernalSlice }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { RPRActions.SpinningScythe, RPRActions.NightmareScythe }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("DoTs",           l => new[] { RPRActions.ShadowOfDeath, RPRActions.WhorlOfDeath }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Shroud GCDs",    l => new[] { RPRActions.Gibbet, RPRActions.Gallows, RPRActions.Guillotine, RPRActions.UnveiledGibbet, RPRActions.UnveiledGallows, RPRActions.VoidReaping, RPRActions.CrossReaping, RPRActions.GrimReaping, RPRActions.Communio, RPRActions.Perfectio, RPRActions.LemuresSlice, RPRActions.LemuresScythe, RPRActions.Sacrificium }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Soul GCDs",      l => new[] { RPRActions.SoulSlice, RPRActions.SoulScythe, RPRActions.Soulsow, RPRActions.HarvestMoon }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { RPRActions.BloodStalk, RPRActions.GrimSwathe, RPRActions.Gluttony, RPRActions.PlentifulHarvest, RPRActions.ArcaneCircle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { RPRActions.Enshroud }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",        l => new[] { RPRActions.Harpe, RPRActions.HellsIngress, RPRActions.HellsEgress, RPRActions.Regress, RPRActions.ArcaneCrest }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { RPRActions.Feint, RPRActions.TrueNorth, RPRActions.Bloodbath, RPRActions.SecondWind, RPRActions.LegSweep, RPRActions.ArmsLength }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _vpr =
    {
        new("GCDs",           l => new[] { VPRActions.SteelFangs, VPRActions.ReavingFangs, VPRActions.HuntersSting, VPRActions.SwiftskinsString, VPRActions.FlankstingStrike, VPRActions.FlanksbaneFang, VPRActions.HindstingStrike, VPRActions.HindsbaneFang }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { VPRActions.SteelMaw, VPRActions.ReavingMaw, VPRActions.HuntersBite, VPRActions.SwiftskinsBite, VPRActions.JaggedMaw, VPRActions.BloodiedMaw }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Venom Combo",    l => new[] { VPRActions.Vicewinder, VPRActions.HuntersCoil, VPRActions.SwiftskinsCoil, VPRActions.Vicepit, VPRActions.HuntersDen, VPRActions.SwiftskinsDen }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { VPRActions.Twinfang, VPRActions.Twinblood, VPRActions.TwinfangBite, VPRActions.TwinbloodBite, VPRActions.TwinfangThresh, VPRActions.TwinbloodThresh, VPRActions.UncoiledFury, VPRActions.UncoiledTwinfang, VPRActions.UncoiledTwinblood, VPRActions.SerpentsIre, VPRActions.SerpentsTail, VPRActions.DeathRattle, VPRActions.LastLash }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Reawaken",       l => new[] { VPRActions.Reawaken, VPRActions.FirstGeneration, VPRActions.SecondGeneration, VPRActions.ThirdGeneration, VPRActions.FourthGeneration, VPRActions.FirstLegacy, VPRActions.SecondLegacy, VPRActions.ThirdLegacy, VPRActions.FourthLegacy, VPRActions.Ouroboros }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",        l => new[] { VPRActions.WrithingSnap }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { VPRActions.Feint, VPRActions.TrueNorth, VPRActions.Bloodbath, VPRActions.SecondWind, VPRActions.LegSweep, VPRActions.ArmsLength }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    // ── Ranged Physical DPS ───────────────────────────────────────────────

    private static readonly ChecklistGroup[] _brd =
    {
        new("GCDs",           l => new[] { BRDActions.HeavyShot, BRDActions.BurstShot, BRDActions.StraightShot, BRDActions.RefulgentArrow }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("DoTs",           l => new[] { BRDActions.VenomousBite, BRDActions.CausticBite, BRDActions.Windbite, BRDActions.Stormbite, BRDActions.IronJaws }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { BRDActions.QuickNock, BRDActions.Ladonsbite, BRDActions.Shadowbite }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Songs",          l => new[] { BRDActions.MagesBallad, BRDActions.ArmysPaeon, BRDActions.WanderersMinuet }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { BRDActions.Bloodletter, BRDActions.HeartbreakShot, BRDActions.RainOfDeath, BRDActions.EmpyrealArrow, BRDActions.Sidewinder, BRDActions.PitchPerfect, BRDActions.ApexArrow, BRDActions.BlastArrow, BRDActions.ResonantArrow, BRDActions.RadiantEncore }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { BRDActions.RagingStrikes, BRDActions.BattleVoice, BRDActions.RadiantFinale, BRDActions.Barrage }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Support",        l => new[] { BRDActions.Troubadour, BRDActions.NaturesMinne, BRDActions.WardensPaean, BRDActions.RepellingShot }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { BRDActions.Peloton, BRDActions.HeadGraze, BRDActions.SecondWind, BRDActions.ArmsLength }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _mch =
    {
        new("Combo GCDs",     l => new[] { MCHActions.SplitShot, MCHActions.HeatedSplitShot, MCHActions.SlugShot, MCHActions.HeatedSlugShot, MCHActions.CleanShot, MCHActions.HeatedCleanShot }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { MCHActions.SpreadShot, MCHActions.Scattergun, MCHActions.Bioblaster }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Drill / Anchor", l => new[] { MCHActions.Drill, MCHActions.HotShot, MCHActions.AirAnchor, MCHActions.ChainSaw, MCHActions.Excavator, MCHActions.FullMetalField }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Overheated GCDs",l => new[] { MCHActions.HeatBlast, MCHActions.BlazingShot, MCHActions.AutoCrossbow }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { MCHActions.GaussRound, MCHActions.DoubleCheck, MCHActions.Ricochet, MCHActions.Checkmate, MCHActions.Wildfire, MCHActions.RookAutoturret, MCHActions.AutomatonQueen, MCHActions.RookOverdrive, MCHActions.QueenOverdrive }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { MCHActions.Reassemble, MCHActions.BarrelStabilizer, MCHActions.Hypercharge }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Support",        l => new[] { MCHActions.Tactician, MCHActions.Dismantle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { MCHActions.Peloton, MCHActions.HeadGraze, MCHActions.SecondWind, MCHActions.ArmsLength }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _dnc =
    {
        new("GCDs",           l => new[] { DNCActions.Cascade, DNCActions.Fountain, DNCActions.ReverseCascade, DNCActions.Fountainfall }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { DNCActions.Windmill, DNCActions.Bladeshower, DNCActions.RisingWindmill, DNCActions.Bloodshower }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Steps",          l => new[] { DNCActions.StandardStep, DNCActions.TechnicalStep, DNCActions.Emboite, DNCActions.Entrechat, DNCActions.Jete, DNCActions.Pirouette, DNCActions.StandardFinish, DNCActions.TechnicalFinish }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders", l => new[] { DNCActions.SaberDance, DNCActions.Tillana, DNCActions.StarfallDance, DNCActions.LastDance, DNCActions.FinishingMove, DNCActions.DanceOfTheDawn }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Fan Dance",      l => new[] { DNCActions.FanDance, DNCActions.FanDanceII, DNCActions.FanDanceIII, DNCActions.FanDanceIV }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { DNCActions.Devilment, DNCActions.Flourish }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Support",        l => new[] { DNCActions.ClosedPosition, DNCActions.Ending, DNCActions.ShieldSamba, DNCActions.CuringWaltz, DNCActions.Improvisation, DNCActions.ImprovisedFinish }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",        l => new[] { DNCActions.EnAvant }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { DNCActions.Peloton, DNCActions.HeadGraze, DNCActions.SecondWind, DNCActions.ArmsLength }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    // ── Caster DPS ────────────────────────────────────────────────────────

    private static readonly ChecklistGroup[] _blm =
    {
        new("Fire GCDs",      l => new[] { BLMActions.Fire, BLMActions.Fire3, BLMActions.Fire4, BLMActions.Despair, BLMActions.Paradox }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Ice GCDs",       l => new[] { BLMActions.Blizzard, BLMActions.Blizzard3, BLMActions.Blizzard4, BLMActions.UmbralSoul }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Thunder",        l => new[] { BLMActions.Thunder, BLMActions.Thunder3, BLMActions.HighThunder }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Polyglot",       l => new[] { BLMActions.Foul, BLMActions.Xenoglossy }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Fire GCDs",  l => new[] { BLMActions.Fire2, BLMActions.HighFire2, BLMActions.Flare, BLMActions.FlareStar }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Ice GCDs",   l => new[] { BLMActions.Blizzard2, BLMActions.HighBlizzard2, BLMActions.Freeze }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Thunder",    l => new[] { BLMActions.Thunder2, BLMActions.Thunder4, BLMActions.HighThunder2 }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { BLMActions.Manafont, BLMActions.Triplecast, BLMActions.LeyLines, BLMActions.Amplifier }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",        l => new[] { BLMActions.Transpose, BLMActions.Scathe, BLMActions.BetweenTheLines, BLMActions.Retrace, BLMActions.Manaward, BLMActions.Sleep }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { BLMActions.Swiftcast, BLMActions.LucidDreaming, BLMActions.Surecast, BLMActions.Addle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _smn =
    {
        new("Ruin GCDs",      l => new[] { SMNActions.Ruin, SMNActions.Ruin2, SMNActions.Ruin3, SMNActions.Ruin4, SMNActions.AstralImpulse, SMNActions.FountainOfFire, SMNActions.UmbralImpulse }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { SMNActions.Outburst, SMNActions.TriDisaster, SMNActions.AstralFlare, SMNActions.BrandOfPurgatory, SMNActions.UmbralFlare }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Primal Summons", l => new[] { SMNActions.SummonIfrit, SMNActions.SummonIfrit2, SMNActions.SummonTitan, SMNActions.SummonTitan2, SMNActions.SummonGaruda, SMNActions.SummonGaruda2 }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Primal GCDs",    l => new[] { SMNActions.RubyRite, SMNActions.RubyCatastrophe, SMNActions.TopazRite, SMNActions.TopazCatastrophe, SMNActions.EmeraldRite, SMNActions.EmeraldCatastrophe, SMNActions.CrimsonCyclone, SMNActions.CrimsonStrike, SMNActions.Slipstream, SMNActions.MountainBuster }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Demi Summons",   l => new[] { SMNActions.SummonBahamut, SMNActions.SummonPhoenix, SMNActions.SummonSolarBahamut, SMNActions.EnkindleBahamut, SMNActions.EnkindlePhoenix, SMNActions.EnkindleSolarBahamut, SMNActions.Deathflare, SMNActions.Rekindle, SMNActions.Sunflare, SMNActions.LuxSolaris }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { SMNActions.EnergyDrain, SMNActions.EnergySiphon, SMNActions.Fester, SMNActions.Necrotize, SMNActions.Painflare, SMNActions.SearingLight, SMNActions.SearingFlash }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",        l => new[] { SMNActions.SummonCarbuncle, SMNActions.RadiantAegis }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { SMNActions.Swiftcast, SMNActions.LucidDreaming, SMNActions.Surecast, SMNActions.Addle, SMNActions.Resurrection }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _rdm =
    {
        new("GCDs",           l => new[] { RDMActions.Jolt, RDMActions.Jolt2, RDMActions.Jolt3, RDMActions.Verthunder, RDMActions.Verthunder3, RDMActions.Veraero, RDMActions.Veraero3, RDMActions.Verfire, RDMActions.Verstone }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { RDMActions.Verthunder2, RDMActions.Veraero2, RDMActions.Impact }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Melee Combo",    l => new[] { RDMActions.Riposte, RDMActions.EnchantedRiposte, RDMActions.Zwerchhau, RDMActions.EnchantedZwerchhau, RDMActions.Redoublement, RDMActions.EnchantedRedoublement, RDMActions.Verflare, RDMActions.Verholy, RDMActions.Scorch, RDMActions.Resolution, RDMActions.GrandImpact }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Melee",      l => new[] { RDMActions.EnchantedMoulinet, RDMActions.EnchantedMoulinetDeux, RDMActions.EnchantedMoulinetTrois }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { RDMActions.Fleche, RDMActions.ContreSixte, RDMActions.CorpsACorps, RDMActions.Engagement, RDMActions.Displacement, RDMActions.ViceOfThorns, RDMActions.Prefulgence }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { RDMActions.Embolden, RDMActions.Manafication, RDMActions.Acceleration }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Support",        l => new[] { RDMActions.Vercure, RDMActions.Verraise, RDMActions.MagickBarrier }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { RDMActions.Swiftcast, RDMActions.LucidDreaming, RDMActions.Surecast, RDMActions.Addle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _pct =
    {
        new("Combo GCDs",       l => new[] { PCTActions.FireInRed, PCTActions.AeroInGreen, PCTActions.WaterInBlue, PCTActions.BlizzardInCyan, PCTActions.StoneInYellow, PCTActions.ThunderInMagenta }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",         l => new[] { PCTActions.Fire2InRed, PCTActions.Aero2InGreen, PCTActions.Water2InBlue, PCTActions.Blizzard2InCyan, PCTActions.Stone2InYellow, PCTActions.Thunder2InMagenta, PCTActions.HolyInWhite, PCTActions.CometInBlack }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("Motifs",           l => new[] { PCTActions.CreatureMotif, PCTActions.PomMotif, PCTActions.WingMotif, PCTActions.ClawMotif, PCTActions.MawMotif, PCTActions.WeaponMotif, PCTActions.HammerMotif, PCTActions.LandscapeMotif, PCTActions.StarrySkyMotif }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("Muses",            l => new[] { PCTActions.LivingMuse, PCTActions.PomMuse, PCTActions.WingedMuse, PCTActions.ClawedMuse, PCTActions.FangedMuse, PCTActions.SteelMuse, PCTActions.StrikingMuse, PCTActions.ScenicMuse, PCTActions.StarryMuse }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("Hammer Combo",     l => new[] { PCTActions.HammerStamp, PCTActions.HammerBrush, PCTActions.PolishingHammer }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD / Finishers", l => new[] { PCTActions.MogOfTheAges, PCTActions.RetributionOfTheMadeen, PCTActions.RainbowDrip, PCTActions.StarPrism }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { PCTActions.SubtractivePalette, PCTActions.StarryMuse }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",        l => new[] { PCTActions.TemperaCoat, PCTActions.TemperaGrassa }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("Utility",          l => new[] { PCTActions.Smudge }
                                         .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { PCTActions.Swiftcast, PCTActions.LucidDreaming, PCTActions.Surecast, PCTActions.Addle }
                                         .Where(a => a.MinLevel <= l).ToArray()),
    };
}
