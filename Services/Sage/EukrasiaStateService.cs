using Dalamud.Game.ClientState.Objects.SubKinds;
using Olympus.Data;

namespace Olympus.Services.Sage;

/// <summary>
/// Tracks Sage's Eukrasia state.
/// Eukrasia modifies the next Diagnosis, Dosis, Prognosis, or Dyskrasia spell:
/// - Diagnosis → Eukrasian Diagnosis (shield)
/// - Dosis → Eukrasian Dosis (DoT)
/// - Prognosis → Eukrasian Prognosis (AoE shield)
/// - Dyskrasia → Eukrasian Dyskrasia (AoE DoT)
///
/// Eukrasian spells have a fixed 2.5s GCD regardless of spell speed.
/// </summary>
public sealed class EukrasiaStateService : IEukrasiaStateService
{
    /// <summary>
    /// Fixed GCD time for Eukrasian spells (not affected by spell speed).
    /// </summary>
    public const float EukrasianGcd = 2.5f;

    /// <summary>
    /// Checks if Eukrasia is currently active on the player.
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    public bool IsEukrasiaActive(IPlayerCharacter player)
    {
        if (player == null)
            return false;

        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SGEActions.EukrasiaStatusId)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the remaining duration of Eukrasia buff.
    /// Returns 0 if not active.
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    public float GetEukrasiaRemaining(IPlayerCharacter player)
    {
        if (player == null)
            return 0f;

        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SGEActions.EukrasiaStatusId)
                return status.RemainingTime;
        }

        return 0f;
    }

    /// <summary>
    /// Determines the predicted spell that Eukrasia will modify.
    /// This is context-dependent based on what spell is about to be cast.
    /// </summary>
    /// <param name="intendedSpell">The spell the player intends to cast.</param>
    /// <returns>The Eukrasian version of the spell, or null if not applicable.</returns>
    public EukrasianSpellType? GetPredictedEukrasianSpell(EukrasianSpellType intendedSpell)
    {
        return intendedSpell switch
        {
            EukrasianSpellType.Diagnosis => EukrasianSpellType.EukrasianDiagnosis,
            EukrasianSpellType.Dosis => EukrasianSpellType.EukrasianDosis,
            EukrasianSpellType.Prognosis => EukrasianSpellType.EukrasianPrognosis,
            EukrasianSpellType.Dyskrasia => EukrasianSpellType.EukrasianDyskrasia,
            _ => null
        };
    }

    /// <summary>
    /// Returns true if we should use Eukrasia before the next spell.
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    /// <param name="intendedSpell">The Eukrasian spell we want to cast.</param>
    public bool ShouldUseEukrasia(IPlayerCharacter player, EukrasianSpellType intendedSpell)
    {
        // Only need Eukrasia for the Eukrasian versions
        if (intendedSpell is not (EukrasianSpellType.EukrasianDiagnosis or
            EukrasianSpellType.EukrasianDosis or
            EukrasianSpellType.EukrasianPrognosis or
            EukrasianSpellType.EukrasianDyskrasia))
        {
            return false;
        }

        // Check if Eukrasia is already active
        return !IsEukrasiaActive(player);
    }

    /// <summary>
    /// Checks if Zoe is active (+50% next GCD heal potency).
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    public bool IsZoeActive(IPlayerCharacter player)
    {
        if (player == null)
            return false;

        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SGEActions.ZoeStatusId)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the remaining duration of Zoe buff.
    /// </summary>
    /// <param name="player">The Sage player character.</param>
    public float GetZoeRemaining(IPlayerCharacter player)
    {
        if (player == null)
            return 0f;

        foreach (var status in player.StatusList)
        {
            if (status.StatusId == SGEActions.ZoeStatusId)
                return status.RemainingTime;
        }

        return 0f;
    }
}

/// <summary>
/// Types of spells that can be modified by Eukrasia.
/// </summary>
public enum EukrasianSpellType
{
    /// <summary>
    /// Base Diagnosis spell.
    /// </summary>
    Diagnosis,

    /// <summary>
    /// Eukrasian Diagnosis (shield heal).
    /// </summary>
    EukrasianDiagnosis,

    /// <summary>
    /// Base Dosis spell.
    /// </summary>
    Dosis,

    /// <summary>
    /// Eukrasian Dosis (DoT).
    /// </summary>
    EukrasianDosis,

    /// <summary>
    /// Base Prognosis spell.
    /// </summary>
    Prognosis,

    /// <summary>
    /// Eukrasian Prognosis (AoE shield).
    /// </summary>
    EukrasianPrognosis,

    /// <summary>
    /// Base Dyskrasia spell.
    /// </summary>
    Dyskrasia,

    /// <summary>
    /// Eukrasian Dyskrasia (AoE DoT).
    /// </summary>
    EukrasianDyskrasia
}

/// <summary>
/// Interface for Eukrasia state service.
/// </summary>
public interface IEukrasiaStateService
{
    /// <summary>
    /// Checks if Eukrasia is currently active.
    /// </summary>
    bool IsEukrasiaActive(IPlayerCharacter player);

    /// <summary>
    /// Gets the remaining duration of Eukrasia buff.
    /// </summary>
    float GetEukrasiaRemaining(IPlayerCharacter player);

    /// <summary>
    /// Determines the predicted spell that Eukrasia will modify.
    /// </summary>
    EukrasianSpellType? GetPredictedEukrasianSpell(EukrasianSpellType intendedSpell);

    /// <summary>
    /// Returns true if we should use Eukrasia before the next spell.
    /// </summary>
    bool ShouldUseEukrasia(IPlayerCharacter player, EukrasianSpellType intendedSpell);

    /// <summary>
    /// Checks if Zoe is active.
    /// </summary>
    bool IsZoeActive(IPlayerCharacter player);

    /// <summary>
    /// Gets the remaining duration of Zoe buff.
    /// </summary>
    float GetZoeRemaining(IPlayerCharacter player);
}
