using System.Collections.Generic;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Modules.Healing;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Orchestrates healing sub-handlers for the WHM rotation.
/// Delegates to specialized handlers in priority order.
/// </summary>
public sealed class HealingModule : IApolloModule
{
    private readonly List<IHealingHandler> _handlers;

    public int Priority => 10; // High priority for healing
    public string Name => "Healing";

    public HealingModule()
    {
        _handlers = new List<IHealingHandler>
        {
            new BenedictionHandler(),
            new AssizeHealingHandler(),
            new EsunaHandler(),
            new PreemptiveHealingHandler(),
            new AoEHealingHandler(),
            new SingleTargetHealingHandler(),
            new RegenHandler(),
            new TetragrammatonHandler(),
            new LilyCapPreventionHandler()
        };

        // Sort by priority (already in order, but explicit for clarity)
        _handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        foreach (var handler in _handlers)
        {
            // Apply execution constraints based on handler type
            if (!CanExecuteHandler(handler, context))
                continue;

            if (handler.TryExecute(context, isMoving))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(ApolloContext context)
    {
        // Debug state is updated during handler execution
    }

    /// <summary>
    /// Determines if a handler can execute based on current context.
    /// oGCD-only handlers require CanExecuteOgcd, combat-only handlers require InCombat.
    /// </summary>
    private static bool CanExecuteHandler(IHealingHandler handler, ApolloContext context)
    {
        return handler.Priority switch
        {
            // oGCD-only handlers
            HealingPriority.Benediction => context.CanExecuteOgcd,
            HealingPriority.AssizeHealing => context.CanExecuteOgcd && context.InCombat,
            HealingPriority.Tetragrammaton => context.CanExecuteOgcd,

            // Combat-only handlers
            HealingPriority.Esuna => context.InCombat,
            HealingPriority.PreemptiveHeal => context.InCombat,
            HealingPriority.Regen => context.InCombat,
            HealingPriority.LilyCapPrevention => context.InCombat,

            // Always available handlers
            HealingPriority.AoEHeal => true,
            HealingPriority.SingleHeal => true,

            _ => true
        };
    }
}
