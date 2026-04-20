using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Plugin.Services;

namespace Olympus.Services.JobGauge;

public sealed class GnbGaugeReader : IGnbGaugeReader
{
    private readonly IJobGauges _gauges;
    private readonly IErrorMetricsService? _errorMetrics;

    public GnbGaugeReader(IJobGauges gauges, IErrorMetricsService? errorMetrics = null)
    {
        _gauges = gauges;
        _errorMetrics = errorMetrics;
    }

    public byte AmmoComboStep
    {
        get
        {
            try { return _gauges.Get<GNBGauge>().AmmoComboStep; }
            catch { _errorMetrics?.RecordError("GnbGaugeReader", "Failed to read GNB AmmoComboStep"); return 0; }
        }
    }

    public byte Cartridges
    {
        get
        {
            try { return _gauges.Get<GNBGauge>().Ammo; }
            catch { _errorMetrics?.RecordError("GnbGaugeReader", "Failed to read GNB Cartridges"); return 0; }
        }
    }
}
