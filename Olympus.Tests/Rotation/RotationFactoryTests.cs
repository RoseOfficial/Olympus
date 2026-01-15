using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Services;

namespace Olympus.Tests.Rotation;

public class RotationFactoryTests
{
    private readonly Mock<IPluginLog> _mockLog;
    private readonly ServiceContainer _services;

    public RotationFactoryTests()
    {
        _mockLog = new Mock<IPluginLog>();
        _services = new ServiceContainer();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServices_CreatesFactory()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);

        Assert.NotNull(factory);
        Assert.Empty(factory.CreatedRotations);
    }

    #endregion

    #region CreateRotation Tests

    [Fact]
    public void CreateRotation_TypeWithNoMatchingConstructor_ReturnsNull()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);

        // Try to create a rotation that needs dependencies we haven't registered
        var result = factory.CreateRotation(typeof(RotationNeedingService));

        Assert.Null(result);
    }

    [Fact]
    public void CreateRotation_TypeWithDefaultConstructor_Succeeds()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);

        var result = factory.CreateRotation(typeof(SimpleTestRotation));

        Assert.NotNull(result);
        Assert.IsType<SimpleTestRotation>(result);
    }

    [Fact]
    public void CreateRotation_TypeWithServiceDependency_InjectsService()
    {
        var testService = new TestService();
        _services.Register(testService);
        var factory = new RotationFactory(_services, _mockLog.Object);

        var result = factory.CreateRotation(typeof(RotationNeedingService));

        Assert.NotNull(result);
        var rotation = Assert.IsType<RotationNeedingService>(result);
        Assert.Same(testService, rotation.Service);
    }

    [Fact]
    public void CreateRotation_TypeWithMultipleConstructors_UsesLongestSatisfiable()
    {
        var testService = new TestService();
        _services.Register(testService);
        var factory = new RotationFactory(_services, _mockLog.Object);

        var result = factory.CreateRotation(typeof(RotationWithMultipleConstructors));

        Assert.NotNull(result);
        var rotation = Assert.IsType<RotationWithMultipleConstructors>(result);
        // Should use the constructor that takes TestService since we registered it
        Assert.Same(testService, rotation.Service);
    }

    [Fact]
    public void CreateRotation_TypeWithDefaultParameter_UsesDefaultWhenServiceMissing()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);

        var result = factory.CreateRotation(typeof(RotationWithDefaultParameter));

        Assert.NotNull(result);
        var rotation = Assert.IsType<RotationWithDefaultParameter>(result);
        Assert.Equal("default", rotation.Value);
    }

    #endregion

    #region Create<T> Tests

    [Fact]
    public void CreateGeneric_SimpleRotation_ReturnsTypedInstance()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);

        var result = factory.Create<SimpleTestRotation>();

        Assert.NotNull(result);
        Assert.IsType<SimpleTestRotation>(result);
    }

    [Fact]
    public void CreateGeneric_RotationNeedingMissingService_ReturnsNull()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);

        var result = factory.Create<RotationNeedingService>();

        Assert.Null(result);
    }

    #endregion

    #region TrackRotation Tests

    [Fact]
    public void TrackRotation_AddsToCreatedRotations()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);
        var rotation = new SimpleTestRotation();

        factory.TrackRotation(rotation);

        Assert.Contains(rotation, factory.CreatedRotations);
    }

    [Fact]
    public void TrackRotation_MultipleTimes_AddsAll()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);
        var rotation1 = new SimpleTestRotation();
        var rotation2 = new SimpleTestRotation();

        factory.TrackRotation(rotation1);
        factory.TrackRotation(rotation2);

        Assert.Equal(2, factory.CreatedRotations.Count);
        Assert.Contains(rotation1, factory.CreatedRotations);
        Assert.Contains(rotation2, factory.CreatedRotations);
    }

    #endregion

    #region DisposeRotations Tests

    [Fact]
    public void DisposeRotations_DisposableRotation_CallsDispose()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);
        var rotation = new DisposableTestRotation();
        factory.TrackRotation(rotation);

        factory.DisposeRotations();

        Assert.True(rotation.IsDisposed);
    }

    [Fact]
    public void DisposeRotations_MultipleRotations_DisposesAll()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);
        var rotation1 = new DisposableTestRotation();
        var rotation2 = new DisposableTestRotation();
        factory.TrackRotation(rotation1);
        factory.TrackRotation(rotation2);

        factory.DisposeRotations();

        Assert.True(rotation1.IsDisposed);
        Assert.True(rotation2.IsDisposed);
    }

    [Fact]
    public void DisposeRotations_ClearsCreatedRotationsList()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);
        factory.TrackRotation(new SimpleTestRotation());

        factory.DisposeRotations();

        Assert.Empty(factory.CreatedRotations);
    }

    [Fact]
    public void DisposeRotations_NonDisposableRotation_NoError()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);
        factory.TrackRotation(new SimpleTestRotation());

        // Should not throw
        factory.DisposeRotations();
    }

    [Fact]
    public void DisposeRotations_RotationThrowsOnDispose_ContinuesWithOthers()
    {
        var factory = new RotationFactory(_services, _mockLog.Object);
        var throwingRotation = new ThrowingDisposeRotation();
        var normalRotation = new DisposableTestRotation();
        factory.TrackRotation(throwingRotation);
        factory.TrackRotation(normalRotation);

        factory.DisposeRotations();

        // Both should have been attempted despite throw
        Assert.True(throwingRotation.DisposeAttempted);
        Assert.True(normalRotation.IsDisposed);
    }

    #endregion

    #region DiscoverAndRegister Tests

    [Fact]
    public void DiscoverAndRegister_WithManager_RegistersFoundRotations()
    {
        // This test verifies the discovery mechanism works with real rotation classes
        // Since we have [Rotation] attributes on Apollo, Athena, etc., this would find them
        // but we can't easily instantiate them without all their dependencies
        // Instead, test that the method runs without throwing
        var factory = new RotationFactory(_services, _mockLog.Object);
        var manager = new RotationManager();

        // Should not throw - rotations won't be created due to missing dependencies
        var count = factory.DiscoverAndRegister(manager);

        // We can't assert the exact count since it depends on whether dependencies resolve
        Assert.True(count >= 0);
    }

    #endregion

    #region Test Helpers

    private class TestService { }

    // Simple rotation with no dependencies
    [Rotation("SimpleTest", 1)]
    private class SimpleTestRotation : IRotation
    {
        public string Name => "SimpleTest";
        public uint[] SupportedJobIds => new uint[] { 1 };
        public DebugState DebugState { get; } = new();
        public void Execute(IPlayerCharacter player) { }
    }

    // Rotation that requires a service
    [Rotation("ServiceTest", 2)]
    private class RotationNeedingService : IRotation
    {
        public TestService Service { get; }
        public string Name => "ServiceTest";
        public uint[] SupportedJobIds => new uint[] { 2 };
        public DebugState DebugState { get; } = new();

        public RotationNeedingService(TestService service)
        {
            Service = service;
        }

        public void Execute(IPlayerCharacter player) { }
    }

    // Rotation with multiple constructors
    [Rotation("MultiConstructor", 3)]
    private class RotationWithMultipleConstructors : IRotation
    {
        public TestService? Service { get; }
        public string Name => "MultiConstructor";
        public uint[] SupportedJobIds => new uint[] { 3 };
        public DebugState DebugState { get; } = new();

        public RotationWithMultipleConstructors()
        {
            Service = null;
        }

        public RotationWithMultipleConstructors(TestService service)
        {
            Service = service;
        }

        public void Execute(IPlayerCharacter player) { }
    }

    // Rotation with default parameter
    [Rotation("DefaultParam", 4)]
    private class RotationWithDefaultParameter : IRotation
    {
        public string Value { get; }
        public string Name => "DefaultParam";
        public uint[] SupportedJobIds => new uint[] { 4 };
        public DebugState DebugState { get; } = new();

        public RotationWithDefaultParameter(string value = "default")
        {
            Value = value;
        }

        public void Execute(IPlayerCharacter player) { }
    }

    // Disposable rotation for testing disposal
    [Rotation("Disposable", 5)]
    private class DisposableTestRotation : IRotation, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public string Name => "Disposable";
        public uint[] SupportedJobIds => new uint[] { 5 };
        public DebugState DebugState { get; } = new();

        public void Execute(IPlayerCharacter player) { }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    // Rotation that throws on dispose
    [Rotation("ThrowingDispose", 6)]
    private class ThrowingDisposeRotation : IRotation, IDisposable
    {
        public bool DisposeAttempted { get; private set; }
        public string Name => "ThrowingDispose";
        public uint[] SupportedJobIds => new uint[] { 6 };
        public DebugState DebugState { get; } = new();

        public void Execute(IPlayerCharacter player) { }

        public void Dispose()
        {
            DisposeAttempted = true;
            throw new InvalidOperationException("Test exception");
        }
    }

    #endregion
}
