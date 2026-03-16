using System;
using Moq;
using Olympus.Services;
using Olympus.Services.Prediction;
using Xunit;

namespace Olympus.Tests.Services.Prediction;

public class DoTTrackingServiceTests
{
    private static (DoTTrackingService service,
                    Mock<ICombatEventService> mockCes,
                    Mock<IDamageIntakeService> mockDis,
                    Action<uint, int, uint> handler)
        Build()
    {
        var mockCes = new Mock<ICombatEventService>();
        var mockDis = new Mock<IDamageIntakeService>();
        Action<uint, int, uint>? captured = null;

        mockCes
            .SetupAdd(x => x.OnLocalPlayerDamageDealt += It.IsAny<Action<uint, int, uint>>())
            .Callback<Action<uint, int, uint>>(h => captured = h);

        var svc = new DoTTrackingService(mockCes.Object, mockDis.Object);
        return (svc, mockCes, mockDis, captured!);
    }

    [Fact]
    public void KnownDotAction_CallsRegisterActiveDoT()
    {
        var (_, _, mockDis, handler) = Build();
        handler.Invoke(42u, 500, 16532u); // WHM Dia
        mockDis.Verify(d => d.RegisterActiveDoT(42u, It.IsAny<int>(), It.IsAny<float>()), Times.Once);
    }

    [Fact]
    public void UnknownAction_DoesNotCallRegisterActiveDoT()
    {
        var (_, _, mockDis, handler) = Build();
        handler.Invoke(1u, 300, 99999u);
        mockDis.Verify(d => d.RegisterActiveDoT(It.IsAny<uint>(), It.IsAny<int>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void KnownDotAction_PassesCorrectTargetEntityId()
    {
        var (_, _, mockDis, handler) = Build();
        handler.Invoke(77u, 100, 7407u); // BRD Stormbite
        mockDis.Verify(d => d.RegisterActiveDoT(77u, It.IsAny<int>(), It.IsAny<float>()), Times.Once);
    }

    [Fact]
    public void Dia_PassesCorrect30sDuration()
    {
        var (_, _, mockDis, handler) = Build();
        handler.Invoke(1u, 500, 16532u); // Dia = 30s
        mockDis.Verify(d => d.RegisterActiveDoT(1u, It.IsAny<int>(), 30f), Times.Once);
    }

    [Fact]
    public void KnownDotAction_DamagePerTickIsPositive()
    {
        var (_, _, mockDis, handler) = Build();
        handler.Invoke(1u, 500, 121u); // WHM Aero
        mockDis.Verify(d => d.RegisterActiveDoT(1u, It.Is<int>(v => v > 0), It.IsAny<float>()), Times.Once);
    }

    [Fact]
    public void TwoDistinctTargets_RegistersEachSeparately()
    {
        var (_, _, mockDis, handler) = Build();
        handler.Invoke(10u, 500, 16532u);
        handler.Invoke(20u, 500, 16532u);
        mockDis.Verify(d => d.RegisterActiveDoT(10u, It.IsAny<int>(), 30f), Times.Once);
        mockDis.Verify(d => d.RegisterActiveDoT(20u, It.IsAny<int>(), 30f), Times.Once);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvent()
    {
        var (svc, mockCes, _, _) = Build();
        svc.Dispose();
        mockCes.VerifyRemove(
            x => x.OnLocalPlayerDamageDealt -= It.IsAny<Action<uint, int, uint>>(),
            Times.Once);
    }

    [Fact]
    public void ZeroDamageForKnownDot_StillRegisters()
    {
        var (_, _, mockDis, handler) = Build();
        handler.Invoke(5u, 0, 132u); // WHM Aero II
        mockDis.Verify(d => d.RegisterActiveDoT(5u, It.IsAny<int>(), It.IsAny<float>()), Times.Once);
    }
}
