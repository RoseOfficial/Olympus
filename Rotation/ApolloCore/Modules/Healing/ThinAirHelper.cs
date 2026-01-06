using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Shared helper for Thin Air optimization in healing handlers.
/// </summary>
internal static class ThinAirHelper
{
    /// <summary>
    /// Checks if we should wait for Thin Air before casting an expensive spell.
    /// </summary>
    /// <param name="context">The Apollo context.</param>
    /// <returns>True if Thin Air is available and we should wait for it.</returns>
    public static bool ShouldWaitForThinAir(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir || player.Level < WHMActions.ThinAir.MinLevel)
            return false;

        if (context.HasThinAir)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId))
            return false;

        return true;
    }
}
