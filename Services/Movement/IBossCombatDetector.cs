namespace Olympus.Services.Movement;

/// <summary>
/// Reports whether the local player is currently engaged with a boss-class enemy.
/// While true, the trash AoE avoidance system goes dormant.
/// </summary>
public interface IBossCombatDetector
{
    bool IsBossEngaged { get; }
    void Update();
}
