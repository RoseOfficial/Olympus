using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.NikeCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Samurai
/// rotation fires.
/// </summary>
public static class NikeAbilities
{
    // --- Combo starters ---
    public static readonly AbilityBehavior Hakaze = new() { Action = SAMActions.Hakaze };
    public static readonly AbilityBehavior Gyofu = new() { Action = SAMActions.Gyofu };
    public static readonly AbilityBehavior Fuga = new() { Action = SAMActions.Fuga, Toggle = cfg => cfg.Samurai.EnableAoERotation };
    public static readonly AbilityBehavior Fuko = new() { Action = SAMActions.Fuko, Toggle = cfg => cfg.Samurai.EnableAoERotation };

    // --- Combo step 2 ---
    public static readonly AbilityBehavior Jinpu = new() { Action = SAMActions.Jinpu };
    public static readonly AbilityBehavior Shifu = new() { Action = SAMActions.Shifu };
    public static readonly AbilityBehavior Yukikaze = new() { Action = SAMActions.Yukikaze };

    // --- Combo finishers / Meikyo ---
    public static readonly AbilityBehavior Gekko = new() { Action = SAMActions.Gekko };
    public static readonly AbilityBehavior Kasha = new() { Action = SAMActions.Kasha };
    public static readonly AbilityBehavior Mangetsu = new() { Action = SAMActions.Mangetsu, Toggle = cfg => cfg.Samurai.EnableAoERotation };
    public static readonly AbilityBehavior Oka = new() { Action = SAMActions.Oka, Toggle = cfg => cfg.Samurai.EnableAoERotation };

    // --- Iaijutsu ---
    public static readonly AbilityBehavior Higanbana = new() { Action = SAMActions.Higanbana, Toggle = cfg => cfg.Samurai.EnableIaijutsu };
    public static readonly AbilityBehavior TenkaGoken = new() { Action = SAMActions.TenkaGoken, Toggle = cfg => cfg.Samurai.EnableIaijutsu };
    public static readonly AbilityBehavior MidareSetsugekka = new() { Action = SAMActions.MidareSetsugekka, Toggle = cfg => cfg.Samurai.EnableIaijutsu };

    // --- Tsubame-gaeshi ---
    public static readonly AbilityBehavior KaeshiHiganbana = new() { Action = SAMActions.KaeshiHiganbana, Toggle = cfg => cfg.Samurai.EnableTsubamegaeshi };
    public static readonly AbilityBehavior KaeshiGoken = new() { Action = SAMActions.KaeshiGoken, Toggle = cfg => cfg.Samurai.EnableTsubamegaeshi };
    public static readonly AbilityBehavior KaeshiSetsugekka = new() { Action = SAMActions.KaeshiSetsugekka, Toggle = cfg => cfg.Samurai.EnableTsubamegaeshi };

    // --- Ogi Namikiri / Kaeshi: Namikiri / Zanshin ---
    public static readonly AbilityBehavior OgiNamikiri = new() { Action = SAMActions.OgiNamikiri, Toggle = cfg => cfg.Samurai.EnableOgiNamikiri };
    public static readonly AbilityBehavior KaeshiNamikiri = new() { Action = SAMActions.KaeshiNamikiri };
    public static readonly AbilityBehavior Zanshin = new() { Action = SAMActions.Zanshin, Toggle = cfg => cfg.Samurai.EnableZanshin };

    // --- Kenki spenders ---
    public static readonly AbilityBehavior Shinten = new() { Action = SAMActions.Shinten, Toggle = cfg => cfg.Samurai.EnableShinten };
    public static readonly AbilityBehavior Kyuten = new() { Action = SAMActions.Kyuten, Toggle = cfg => cfg.Samurai.EnableKyuten };
    public static readonly AbilityBehavior Senei = new() { Action = SAMActions.Senei, Toggle = cfg => cfg.Samurai.EnableSenei };
    public static readonly AbilityBehavior Guren = new() { Action = SAMActions.Guren, Toggle = cfg => cfg.Samurai.EnableGuren };

    // --- Buffs / Meditation ---
    public static readonly AbilityBehavior Shoha = new() { Action = SAMActions.Shoha, Toggle = cfg => cfg.Samurai.EnableShoha };
    public static readonly AbilityBehavior MeikyoShisui = new() { Action = SAMActions.MeikyoShisui, Toggle = cfg => cfg.Samurai.EnableMeikyoShisui };
    public static readonly AbilityBehavior Ikishoten = new() { Action = SAMActions.Ikishoten, Toggle = cfg => cfg.Samurai.EnableIkishoten };

    // --- Role ---
    public static readonly AbilityBehavior TrueNorth = new() { Action = RoleActions.TrueNorth, Toggle = cfg => cfg.Samurai.EnableTrueNorth };
    public static readonly AbilityBehavior Feint = new() { Action = RoleActions.Feint, Toggle = cfg => cfg.Samurai.EnableFeint };
}
