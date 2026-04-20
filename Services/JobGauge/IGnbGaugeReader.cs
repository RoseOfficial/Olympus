namespace Olympus.Services.JobGauge;

/// <summary>
/// Mockable abstraction over <c>IJobGauges.Get&lt;GNBGauge&gt;()</c>.
/// Exists because Dalamud's <c>GNBGauge</c> is a sealed struct-like type
/// whose fields can't be forged in tests. Production reads from
/// <c>IJobGauges</c>; test mocks return fixed values.
/// </summary>
public interface IGnbGaugeReader
{
    /// <summary>
    /// Raw <c>GNBGauge.AmmoComboStep</c> byte:
    ///   0 = no combo, 1 = Savage Claw next, 2 = Wicked Talon next,
    ///   3 = Noble Blood next, 4 = Lion Heart next.
    /// Returns 0 on any read failure.
    /// </summary>
    byte AmmoComboStep { get; }

    /// <summary>Current cartridge count (0-3).</summary>
    byte Cartridges { get; }
}
