using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.PrometheusCore;

/// <summary>
/// Pure-switch tests for Prometheus.ComputeComboStep. Machinist's basic
/// combo: Split Shot → Slug Shot → Clean Shot, with action-replacement to
/// Heated Split Shot / Heated Slug Shot / Heated Clean Shot at Lv.54.
/// </summary>
public class PrometheusComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]        // No combo
    [InlineData(2866u, 0f, 0)]      // Split Shot - timer expired
    [InlineData(2866u, 30f, 1)]     // Split Shot → step 1
    [InlineData(7411u, 30f, 1)]     // Heated Split Shot (Lv.54 replacement) → step 1
    [InlineData(2868u, 30f, 2)]     // Slug Shot → step 2
    [InlineData(7412u, 30f, 2)]     // Heated Slug Shot (Lv.60 replacement) → step 2
    [InlineData(99999u, 30f, 0)]    // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Prometheus.ComputeComboStep(comboAction, comboTimer));
    }
}
