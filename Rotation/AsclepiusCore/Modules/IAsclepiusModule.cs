using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Interface for Asclepius (Sage) rotation modules.
/// Inherits from IHealerRotationModule for consistent module patterns across healers.
/// Standard priorities:
///   3 - Kardia (ensure Kardia is placed)
///   5 - Resurrection (raise dead party members)
///  10 - Healing (Addersgall heals, Pneuma, GCD heals)
///  15 - Shields (Haima, Panhaima, E.Diagnosis)
///  20 - Defensive (Kerachole, Taurochole mitigation)
///  30 - Buffs (Physis II, Krasis, Zoe, Soteria)
///  50 - Damage (Dosis, DoT, Phlegma, Psyche)
/// </summary>
public interface IAsclepiusModule : IHealerRotationModule<IAsclepiusContext>
{
}
