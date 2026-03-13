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
        new("GCD Damage",       l => new[] { WHMActions.GetDamageGcdForLevel(l) }),
        new("GCD DoT",          l => new[] { WHMActions.GetDotForLevel(l) }),
        new("GCD AoE Damage",   l => WHMActions.AoEDamageGcds.Where(a => a.MinLevel <= l).Take(1).ToArray()),
        new("GCD Single Heals", l => WHMActions.SingleHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Heals",    l => WHMActions.AoEHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Heals",       l => new[] { WHMActions.Tetragrammaton, WHMActions.Benediction, WHMActions.DivineBenison }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD AoE Heals",   l => WHMActions.AoEHealOgcds.Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { WHMActions.PresenceOfMind, WHMActions.ThinAir, WHMActions.Temperance, WHMActions.PlenaryIndulgence, WHMActions.Aquaveil }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { WHMActions.Swiftcast, WHMActions.LucidDreaming }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _sch =
    {
        // Exclude RuinII — it's a movement filler, not the primary damage GCD
        new("GCD Damage",       l => SCHActions.DamageGcds.Where(a => a != SCHActions.RuinII && a.MinLevel <= l).Take(1).ToArray()),
        new("GCD DoT",          l => new[] { SCHActions.GetDotForLevel(l) }),
        new("GCD AoE Damage",   l => SCHActions.AoEDamageGcds.Where(a => a.MinLevel <= l).Take(1).ToArray()),
        new("GCD Single Heals", l => SCHActions.SingleHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Heals",    l => SCHActions.AoEHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Heals",       l => new[] { SCHActions.Lustrate, SCHActions.Excogitation, SCHActions.Protraction }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD AoE Heals",   l => new[] { SCHActions.Indomitability, SCHActions.SacredSoil, SCHActions.FeyBlessing }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { SCHActions.ChainStratagem, SCHActions.Dissipation, SCHActions.Recitation, SCHActions.Expedient }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { SCHActions.Swiftcast, SCHActions.LucidDreaming }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _sge =
    {
        new("GCD Damage",       l => new[] { SGEActions.GetDamageGcdForLevel(l) }),
        new("GCD DoT",          l => new[] { SGEActions.GetDotForLevel(l) }),
        new("GCD AoE Damage",   l => SGEActions.AoEDamageGcds.Where(a => a.MinLevel <= l).Take(1).ToArray()),
        new("GCD Single Heals", l => SGEActions.SingleHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Heals",    l => SGEActions.AoEHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Heals",       l => new[] { SGEActions.Druochole, SGEActions.Taurochole, SGEActions.Haima }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD AoE Heals",   l => new[] { SGEActions.Ixochole, SGEActions.Kerachole, SGEActions.PhysisII, SGEActions.Panhaima, SGEActions.Holos }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { SGEActions.Soteria, SGEActions.Zoe, SGEActions.Krasis, SGEActions.Pneuma, SGEActions.Psyche, SGEActions.Philosophia }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { SGEActions.Swiftcast, SGEActions.LucidDreaming }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _ast =
    {
        new("GCD Damage",       l => new[] { ASTActions.GetDamageGcdForLevel(l) }),
        new("GCD DoT",          l => new[] { ASTActions.GetDotForLevel(l) }),
        new("GCD AoE Damage",   l => ASTActions.AoEDamageGcds.Where(a => a.MinLevel <= l).Take(1).ToArray()),
        new("GCD Single Heals", l => ASTActions.SingleHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("GCD AoE Heals",    l => ASTActions.AoEHealGcds.Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Heals",       l => new[] { ASTActions.EssentialDignity, ASTActions.CelestialIntersection, ASTActions.Exaltation }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD AoE Heals",   l => new[] { ASTActions.CelestialOpposition, ASTActions.EarthlyStar, ASTActions.Horoscope, ASTActions.Macrocosmos }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Card Actions",     l => new[] { ASTActions.PlayI, ASTActions.PlayII, ASTActions.PlayIII, ASTActions.MinorArcana }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",            l => new[] { ASTActions.Divination, ASTActions.Lightspeed, ASTActions.Synastry, ASTActions.NeutralSect, ASTActions.Oracle, ASTActions.SunSign }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",     l => new[] { ASTActions.Swiftcast, ASTActions.LucidDreaming }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    // ── Tanks ─────────────────────────────────────────────────────────────

    private static readonly ChecklistGroup[] _war =
    {
        new("Combo GCDs",     l => new[] { WARActions.HeavySwing, WARActions.Maim, WARActions.StormsPath, WARActions.StormsEye }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Combo GCDs", l => new[] { WARActions.Overpower, WARActions.MythrilTempest }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders", l =>
        {
            var spenders = new List<ActionDefinition>();
            if (l >= WARActions.InnerBeast.MinLevel)       spenders.Add(WARActions.GetFellCleaveAction(l));
            if (l >= WARActions.SteelCyclone.MinLevel)     spenders.Add(WARActions.GetDecimateAction(l));
            if (l >= WARActions.PrimalRend.MinLevel)       spenders.Add(WARActions.PrimalRend);
            if (l >= WARActions.PrimalRuination.MinLevel)  spenders.Add(WARActions.PrimalRuination);
            return spenders.ToArray();
        }),
        new("oGCD Damage",    l => new[] { WARActions.Upheaval, WARActions.Orogeny, WARActions.Onslaught }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { WARActions.GetDamageBuffAction(l), WARActions.Infuriate }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",      l => new[] { WARActions.GetVengeanceAction(l), WARActions.GetBloodwhettingAction(l), WARActions.ThrillOfBattle, WARActions.Equilibrium, WARActions.ShakeItOff, WARActions.NascentFlash }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Role Actions",   l => new[] { WARActions.Rampart, WARActions.LowBlow, WARActions.Reprisal, WARActions.Provoke, WARActions.Shirk }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _drk =
    {
        new("Combo GCDs",     l => new[] { DRKActions.HardSlash, DRKActions.SyphonStrike, DRKActions.Souleater }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Combo GCDs", l => new[] { DRKActions.Unleash, DRKActions.StalwartSoul }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders", l =>
        {
            var spenders = new List<ActionDefinition>();
            spenders.Add(DRKActions.GetEdgeAction(l));   // EdgeOfDarkness → EdgeOfShadow
            if (l >= DRKActions.Bloodspiller.MinLevel) spenders.Add(DRKActions.Bloodspiller);
            if (l >= DRKActions.Quietus.MinLevel)      spenders.Add(DRKActions.Quietus);
            if (l >= DRKActions.GetFloodAction(l).MinLevel) spenders.Add(DRKActions.GetFloodAction(l));
            if (l >= DRKActions.Shadowbringer.MinLevel) spenders.Add(DRKActions.Shadowbringer);
            if (l >= DRKActions.ScarletDelirium.MinLevel) spenders.Add(DRKActions.ScarletDelirium);
            if (l >= DRKActions.Disesteem.MinLevel)     spenders.Add(DRKActions.Disesteem);
            return spenders.ToArray();
        }),
        new("oGCD Damage",    l => new[] { DRKActions.CarveAndSpit, DRKActions.AbyssalDrain, DRKActions.SaltedEarth, DRKActions.SaltAndDarkness, DRKActions.Plunge }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { DRKActions.BloodWeapon, DRKActions.Delirium, DRKActions.LivingShadow }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",      l => new[] { DRKActions.GetShadowWallAction(l), DRKActions.LivingDead, DRKActions.DarkMind, DRKActions.Oblation, DRKActions.DarkMissionary }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Role Actions",   l => new[] { DRKActions.Rampart, DRKActions.LowBlow, DRKActions.Reprisal, DRKActions.Provoke, DRKActions.Shirk }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _pld =
    {
        new("Combo GCDs",      l => new[] { PLDActions.FastBlade, PLDActions.RiotBlade, PLDActions.GetComboFinisher(l) }
                                        .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("AoE Combo GCDs",  l => new[] { PLDActions.TotalEclipse, PLDActions.Prominence }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders",  l => new[] { PLDActions.HolySpirit, PLDActions.HolyCircle, PLDActions.Confiteor, PLDActions.BladeOfFaith, PLDActions.BladeOfTruth, PLDActions.BladeOfValor, PLDActions.BladeOfHonor, PLDActions.Atonement, PLDActions.Supplication, PLDActions.Sepulchre }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",     l => new[] { PLDActions.CircleOfScorn, PLDActions.Expiacion, PLDActions.SpiritsWithin, PLDActions.Intervene }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",           l => new[] { PLDActions.FightOrFlight, PLDActions.Requiescat }
                                        .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",       l => new[] { PLDActions.GetSheltronAction(l), PLDActions.GetSentinelAction(l), PLDActions.Bulwark, PLDActions.HallowedGround, PLDActions.DivineVeil, PLDActions.PassageOfArms, PLDActions.Clemency }
                                        .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Role Actions",    l => new[] { PLDActions.Rampart, PLDActions.LowBlow, PLDActions.Reprisal, PLDActions.Provoke, PLDActions.Shirk }
                                        .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _gnb =
    {
        new("Combo GCDs",     l => new[] { GNBActions.KeenEdge, GNBActions.BrutalShell, GNBActions.SolidBarrel }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Combo GCDs", l => new[] { GNBActions.DemonSlice, GNBActions.DemonSlaughter }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders", l => new[] { GNBActions.GnashingFang, GNBActions.SavageClaw, GNBActions.WickedTalon, GNBActions.BurstStrike, GNBActions.FatedCircle, GNBActions.DoubleDown, GNBActions.ReignOfBeasts, GNBActions.NobleBlood, GNBActions.LionHeart }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { GNBActions.GetBlastingZoneAction(l), GNBActions.BowShock, GNBActions.Continuation, GNBActions.SonicBreak }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Buffs",          l => new[] { GNBActions.NoMercy, GNBActions.Bloodfest }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Defensive",      l => new[] { GNBActions.GetNebulaAction(l), GNBActions.GetHeartAction(l), GNBActions.Superbolide, GNBActions.Aurora, GNBActions.HeartOfLight, GNBActions.Camouflage }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Role Actions",   l => new[] { GNBActions.Rampart, GNBActions.LowBlow, GNBActions.Reprisal, GNBActions.Provoke, GNBActions.Shirk }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    // ── Melee DPS ─────────────────────────────────────────────────────────

    private static readonly ChecklistGroup[] _drg =
    {
        new("Combo GCDs",   l => new[] { DRGActions.TrueThrust, DRGActions.VorpalThrust, DRGActions.Disembowel, DRGActions.GetVorpalFinisher(l), DRGActions.GetDisembowelFinisher(l), DRGActions.FangAndClaw, DRGActions.WheelingThrust, DRGActions.GetPositionalFinisher(l) }
                                     .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("AoE GCDs",     l => new[] { DRGActions.DoomSpike, DRGActions.SonicThrust, DRGActions.GetAoeFinisher(l) }
                                     .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("oGCD Damage",  l => new[] { DRGActions.GetJumpAction(l), DRGActions.MirageDive, DRGActions.SpineshatterDive, DRGActions.DragonfireDive, DRGActions.Geirskogul, DRGActions.Nastrond, DRGActions.Stardiver, DRGActions.WyrmwindThrust, DRGActions.RiseOfTheDragon, DRGActions.Starcross }
                                     .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Buffs",        l => new[] { DRGActions.LanceCharge, DRGActions.BattleLitany, DRGActions.LifeSurge }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions", l => new[] { DRGActions.Feint, DRGActions.TrueNorth, DRGActions.Bloodbath, DRGActions.SecondWind }
                                     .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _mnk =
    {
        new("GCDs",         l => new[] { MNKActions.Bootshine, MNKActions.DragonKick, MNKActions.LeapingOpo, MNKActions.TrueStrike, MNKActions.TwinSnakes, MNKActions.RisingRaptor, MNKActions.SnapPunch, MNKActions.Demolish, MNKActions.PouncingCoeurl }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",     l => new[] { MNKActions.ArmOfTheDestroyer, MNKActions.FourPointFury, MNKActions.Rockbreaker }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Blitz GCDs",   l => new[] { MNKActions.ElixirField, MNKActions.CelestialRevolution, MNKActions.FlintStrike, MNKActions.RisingPhoenix, MNKActions.PhantomRush, MNKActions.ElixirBurst, MNKActions.WindsReply, MNKActions.FiresReply }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",  l =>
        {
            var ogcds = new List<ActionDefinition>();
            if (l >= MNKActions.SteelPeak.MinLevel) ogcds.Add(MNKActions.GetChakraSpender(l));
            if (l >= MNKActions.HowlingFist.MinLevel) ogcds.Add(MNKActions.GetAoeChakraSpender(l));
            return ogcds.Distinct().ToArray();
        }),
        new("Buffs",        l => new[] { MNKActions.RiddleOfFire, MNKActions.Brotherhood, MNKActions.PerfectBalance, MNKActions.RiddleOfWind }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions", l => new[] { MNKActions.Feint, MNKActions.TrueNorth, MNKActions.Bloodbath, MNKActions.SecondWind }
                                     .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _nin =
    {
        new("Combo GCDs",   l => new[] { NINActions.SpinningEdge, NINActions.GustSlash, NINActions.AeolianEdge, NINActions.ArmorCrush }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",     l => new[] { NINActions.DeathBlossom, NINActions.HakkeMujinsatsu }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Ninjutsu",     l => new[] { NINActions.FumaShuriken, NINActions.Raiton, NINActions.Katon, NINActions.Hyoton, NINActions.Doton, NINActions.Suiton, NINActions.GokaMekkyaku, NINActions.HyoshoRanryu }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",  l =>
        {
            var ogcds = new List<ActionDefinition>();
            ogcds.Add(NINActions.GetMugAction(l)); // Mug → Dokumori
            if (l >= NINActions.Bhavacakra.MinLevel) ogcds.Add(NINActions.Bhavacakra);
            if (l >= NINActions.HellfrogMedium.MinLevel) ogcds.Add(NINActions.HellfrogMedium);
            if (l >= NINActions.KunaisBane.MinLevel) ogcds.Add(NINActions.KunaisBane);
            if (l >= NINActions.TenriJindo.MinLevel) ogcds.Add(NINActions.TenriJindo);
            if (l >= NINActions.ForkedRaiju.MinLevel) ogcds.Add(NINActions.ForkedRaiju);
            return ogcds.ToArray();
        }),
        new("Buffs",        l => new[] { NINActions.TrickAttack, NINActions.Kassatsu, NINActions.TenChiJin, NINActions.Bunshin, NINActions.Meisui }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions", l => new[] { NINActions.Feint, NINActions.TrueNorth, NINActions.Bloodbath, NINActions.SecondWind }
                                     .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _sam =
    {
        new("Combo GCDs",   l => new[] { SAMActions.GetComboStarter(l), SAMActions.Jinpu, SAMActions.Shifu, SAMActions.Yukikaze, SAMActions.Gekko, SAMActions.Kasha }
                                     .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("AoE GCDs",     l => new[] { SAMActions.GetAoeComboStarter(l), SAMActions.Mangetsu, SAMActions.Oka }
                                     .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Iaijutsu",     l => new[] { SAMActions.Higanbana, SAMActions.TenkaGoken, SAMActions.MidareSetsugekka, SAMActions.TsubameGaeshi, SAMActions.OgiNamikiri, SAMActions.KaeshiNamikiri }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",  l =>
        {
            var ogcds = new List<ActionDefinition>();
            if (l >= SAMActions.Shinten.MinLevel) ogcds.Add(SAMActions.Shinten);
            if (l >= SAMActions.Kyuten.MinLevel)  ogcds.Add(SAMActions.Kyuten);
            if (l >= SAMActions.Senei.MinLevel)   ogcds.Add(SAMActions.Senei);
            if (l >= SAMActions.Guren.MinLevel)   ogcds.Add(SAMActions.Guren);
            if (l >= SAMActions.Zanshin.MinLevel) ogcds.Add(SAMActions.Zanshin);
            if (l >= SAMActions.Shoha.MinLevel)   ogcds.Add(SAMActions.Shoha);
            return ogcds.ToArray();
        }),
        new("Buffs",        l => new[] { SAMActions.MeikyoShisui, SAMActions.Ikishoten, SAMActions.Hagakure }
                                     .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions", l => new[] { SAMActions.Feint, SAMActions.TrueNorth, SAMActions.Bloodbath, SAMActions.SecondWind }
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
        new("Shroud GCDs",    l => new[] { RPRActions.Gibbet, RPRActions.Gallows, RPRActions.Guillotine, RPRActions.VoidReaping, RPRActions.CrossReaping, RPRActions.GrimReaping, RPRActions.Communio, RPRActions.Perfectio, RPRActions.LemuresSlice, RPRActions.LemuresScythe, RPRActions.Sacrificium }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { RPRActions.BloodStalk, RPRActions.GrimSwathe, RPRActions.Gluttony, RPRActions.PlentifulHarvest, RPRActions.ArcaneCircle, RPRActions.SoulSlice, RPRActions.SoulScythe }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { RPRActions.Enshroud, RPRActions.ArcaneCircle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { RPRActions.Feint, RPRActions.TrueNorth, RPRActions.Bloodbath, RPRActions.SecondWind }
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
        new("oGCD Damage",    l => new[] { VPRActions.Twinfang, VPRActions.Twinblood, VPRActions.TwinfangBite, VPRActions.TwinbloodBite, VPRActions.TwinfangThresh, VPRActions.TwinbloodThresh, VPRActions.UncoiledFury, VPRActions.UncoiledTwinfang, VPRActions.UncoiledTwinblood, VPRActions.SerpentsIre, VPRActions.DeathRattle, VPRActions.LastLash }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Reawaken",       l => new[] { VPRActions.Reawaken, VPRActions.FirstGeneration, VPRActions.SecondGeneration, VPRActions.ThirdGeneration, VPRActions.FourthGeneration, VPRActions.Ouroboros }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { VPRActions.Feint, VPRActions.TrueNorth, VPRActions.Bloodbath, VPRActions.SecondWind }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    // ── Ranged Physical DPS ───────────────────────────────────────────────

    private static readonly ChecklistGroup[] _brd =
    {
        new("GCDs",           l => new[] { BRDActions.GetFiller(l), BRDActions.GetProcAction(l), BRDActions.GetCausticBite(l), BRDActions.GetStormbite(l) }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("AoE GCDs",       l => new[] { BRDActions.GetAoeFiller(l), BRDActions.Shadowbite }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Songs",          l => new[] { BRDActions.MagesBallad, BRDActions.ArmysPaeon, BRDActions.WanderersMinuet }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { BRDActions.GetBloodletter(l), BRDActions.RainOfDeath, BRDActions.EmpyrealArrow, BRDActions.Sidewinder, BRDActions.PitchPerfect, BRDActions.ApexArrow, BRDActions.BlastArrow, BRDActions.ResonantArrow, BRDActions.RadiantEncore }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Buffs",          l => new[] { BRDActions.RagingStrikes, BRDActions.BattleVoice, BRDActions.RadiantFinale, BRDActions.Barrage }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { BRDActions.Peloton, BRDActions.HeadGraze, BRDActions.SecondWind, BRDActions.ArmsLength }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _mch =
    {
        new("Combo GCDs",     l => new[] { MCHActions.GetComboStarter(l), MCHActions.GetComboSecond(l), MCHActions.GetComboFinisher(l) }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("AoE GCDs",       l => new[] { MCHActions.GetAoeAction(l) }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Drill / Anchor", l => new[] { MCHActions.Drill, MCHActions.GetAirAnchor(l), MCHActions.ChainSaw, MCHActions.Excavator, MCHActions.FullMetalField }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Overheated GCDs",l =>
        {
            var result = new List<ActionDefinition>();
            if (l >= MCHActions.HeatBlast.MinLevel) result.Add(MCHActions.GetOverheatedGcd(l, false));
            if (l >= MCHActions.AutoCrossbow.MinLevel) result.Add(MCHActions.GetOverheatedGcd(l, true));
            return result.Distinct().ToArray();
        }),
        new("oGCD Damage",    l => new[] { MCHActions.GetGaussRound(l), MCHActions.GetRicochet(l), MCHActions.Wildfire, MCHActions.GetPetSummon(l), MCHActions.GetPetOverdrive(l) }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("Buffs",          l => new[] { MCHActions.Reassemble, MCHActions.BarrelStabilizer, MCHActions.Hypercharge }
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
        new("Steps",          l => new[] { DNCActions.StandardStep, DNCActions.TechnicalStep }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Gauge Spenders", l => new[] { DNCActions.SaberDance, DNCActions.Tillana, DNCActions.StarfallDance, DNCActions.LastDance, DNCActions.FinishingMove, DNCActions.DanceOfTheDawn }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Fan Dance",      l => new[] { DNCActions.FanDance, DNCActions.FanDanceII, DNCActions.FanDanceIII, DNCActions.FanDanceIV }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { DNCActions.Devilment, DNCActions.Flourish }
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
                                       .Where(a => a.MinLevel <= l).Take(1).ToArray()),
        new("Polyglot",       l => new[] { BLMActions.Foul, BLMActions.Xenoglossy }
                                       .Where(a => a.MinLevel <= l).Take(1).ToArray()),
        new("AoE GCDs",       l => new[] { BLMActions.Fire2, BLMActions.HighFire2, BLMActions.Flare, BLMActions.FlareStar, BLMActions.Blizzard2, BLMActions.HighBlizzard2, BLMActions.Freeze, BLMActions.Thunder2, BLMActions.Thunder4, BLMActions.HighThunder2 }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { BLMActions.Manafont, BLMActions.Triplecast, BLMActions.LeyLines, BLMActions.Amplifier }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { BLMActions.Swiftcast, BLMActions.LucidDreaming, BLMActions.Addle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _smn =
    {
        new("Ruin GCDs",      l => new[] { SMNActions.Ruin, SMNActions.Ruin3, SMNActions.AstralImpulse, SMNActions.FountainOfFire, SMNActions.UmbralImpulse }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { SMNActions.Outburst, SMNActions.TriDisaster, SMNActions.AstralFlare, SMNActions.BrandOfPurgatory, SMNActions.UmbralFlare }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Primal GCDs",    l => new[] { SMNActions.RubyRite, SMNActions.TopazRite, SMNActions.EmeraldRite, SMNActions.CrimsonCyclone, SMNActions.CrimsonStrike, SMNActions.Slipstream }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Demi Summons",   l => new[] { SMNActions.SummonBahamut, SMNActions.SummonPhoenix, SMNActions.SummonSolarBahamut }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { SMNActions.Fester, SMNActions.Necrotize, SMNActions.Painflare, SMNActions.EnergySiphon, SMNActions.SearingLight, SMNActions.SearingFlash }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { SMNActions.Swiftcast, SMNActions.LucidDreaming, SMNActions.Addle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _rdm =
    {
        new("GCDs",           l => new[] { RDMActions.GetJoltSpell(l), RDMActions.GetVerthunderSpell(l), RDMActions.GetVeraeroSpell(l), RDMActions.Verfire, RDMActions.Verstone }
                                       .Where(a => a.MinLevel <= l).Distinct().ToArray()),
        new("AoE GCDs",       l => new[] { RDMActions.Verthunder2, RDMActions.Veraero2, RDMActions.Impact }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Melee Combo",    l => new[] { RDMActions.EnchantedRiposte, RDMActions.EnchantedZwerchhau, RDMActions.EnchantedRedoublement, RDMActions.Verflare, RDMActions.Verholy, RDMActions.Scorch, RDMActions.Resolution, RDMActions.GrandImpact }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE Melee",      l => new[] { RDMActions.EnchantedMoulinet, RDMActions.EnchantedMoulinetDeux, RDMActions.EnchantedMoulinetTrois }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD Damage",    l => new[] { RDMActions.Fleche, RDMActions.ContreSixte, RDMActions.CorpsACorps, RDMActions.Engagement, RDMActions.ViceOfThorns, RDMActions.Prefulgence }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { RDMActions.Embolden, RDMActions.Manafication, RDMActions.Acceleration }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { RDMActions.Swiftcast, RDMActions.LucidDreaming, RDMActions.Addle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };

    private static readonly ChecklistGroup[] _pct =
    {
        new("Combo GCDs",     l => new[] { PCTActions.FireInRed, PCTActions.AeroInGreen, PCTActions.WaterInBlue, PCTActions.BlizzardInCyan, PCTActions.StoneInYellow, PCTActions.ThunderInMagenta }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("AoE GCDs",       l => new[] { PCTActions.Fire2InRed, PCTActions.Aero2InGreen, PCTActions.Water2InBlue, PCTActions.Blizzard2InCyan, PCTActions.Stone2InYellow, PCTActions.Thunder2InMagenta, PCTActions.HolyInWhite, PCTActions.CometInBlack }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Motif / Muse",   l => new[] { PCTActions.CreatureMotif, PCTActions.WeaponMotif, PCTActions.LandscapeMotif, PCTActions.LivingMuse, PCTActions.StrikingMuse, PCTActions.ScenicMuse }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Hammer Combo",   l => new[] { PCTActions.HammerStamp, PCTActions.HammerBrush, PCTActions.PolishingHammer }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("oGCD / Finishers",l => new[] { PCTActions.MogOfTheAges, PCTActions.RetributionOfTheMadeen, PCTActions.RainbowDrip, PCTActions.StarPrism }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Buffs",          l => new[] { PCTActions.SubtractivePalette, PCTActions.StarryMuse }
                                       .Where(a => a.MinLevel <= l).ToArray()),
        new("Role Actions",   l => new[] { PCTActions.Swiftcast, PCTActions.LucidDreaming, PCTActions.Addle }
                                       .Where(a => a.MinLevel <= l).ToArray()),
    };
}
