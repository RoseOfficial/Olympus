using Olympus.Services;

namespace Olympus.Tests.Services;

public class ServiceContainerTests
{
    #region Registration Tests

    [Fact]
    public void Register_SingleService_CanBeRetrieved()
    {
        var container = new ServiceContainer();
        var service = new TestService();

        container.Register(service);

        var retrieved = container.Get<TestService>();
        Assert.Same(service, retrieved);
    }

    [Fact]
    public void Register_WithInterface_CanRetrieveByBothTypes()
    {
        var container = new ServiceContainer();
        var service = new TestService();

        container.Register<ITestService, TestService>(service);

        var byInterface = container.Get<ITestService>();
        var byClass = container.Get<TestService>();

        Assert.Same(service, byInterface);
        Assert.Same(service, byClass);
    }

    [Fact]
    public void Register_FluentChaining_Works()
    {
        var service1 = new TestService();
        var service2 = new AnotherService();

        var container = new ServiceContainer()
            .Register(service1)
            .Register(service2);

        Assert.Same(service1, container.Get<TestService>());
        Assert.Same(service2, container.Get<AnotherService>());
    }

    [Fact]
    public void Register_NullService_ThrowsArgumentNullException()
    {
        var container = new ServiceContainer();

        Assert.Throws<ArgumentNullException>(() => container.Register<TestService>(null!));
    }

    #endregion

    #region Retrieval Tests

    [Fact]
    public void Get_UnregisteredService_ThrowsInvalidOperationException()
    {
        var container = new ServiceContainer();

        var ex = Assert.Throws<InvalidOperationException>(() => container.Get<TestService>());
        Assert.Contains("TestService", ex.Message);
    }

    [Fact]
    public void TryGet_RegisteredService_ReturnsTrue()
    {
        var container = new ServiceContainer();
        container.Register(new TestService());

        var result = container.TryGet<TestService>(out var service);

        Assert.True(result);
        Assert.NotNull(service);
    }

    [Fact]
    public void TryGet_UnregisteredService_ReturnsFalse()
    {
        var container = new ServiceContainer();

        var result = container.TryGet<TestService>(out var service);

        Assert.False(result);
        Assert.Null(service);
    }

    [Fact]
    public void IsRegistered_RegisteredService_ReturnsTrue()
    {
        var container = new ServiceContainer();
        container.Register(new TestService());

        Assert.True(container.IsRegistered<TestService>());
    }

    [Fact]
    public void IsRegistered_UnregisteredService_ReturnsFalse()
    {
        var container = new ServiceContainer();

        Assert.False(container.IsRegistered<TestService>());
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_DisposesRegisteredDisposables()
    {
        var container = new ServiceContainer();
        var disposable = new DisposableService();

        container.Register(disposable);
        container.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void Dispose_DisposesInReverseOrder()
    {
        var container = new ServiceContainer();
        var order = new List<int>();
        var disposable1 = new OrderedDisposable(1, order);
        var disposable2 = new OrderedDisposable(2, order);
        var disposable3 = new OrderedDisposable(3, order);

        container.Register(disposable1);
        container.Register(disposable2);
        container.Register(disposable3);
        container.Dispose();

        Assert.Equal(new[] { 3, 2, 1 }, order);
    }

    [Fact]
    public void Dispose_DoesNotDisposeNonDisposables()
    {
        var container = new ServiceContainer();
        container.Register(new TestService());

        // Should not throw
        container.Dispose();
    }

    [Fact]
    public void Dispose_CalledTwice_OnlyDisposesOnce()
    {
        var container = new ServiceContainer();
        var disposable = new DisposableService();

        container.Register(disposable);
        container.Dispose();
        container.Dispose(); // Second call

        Assert.Equal(1, disposable.DisposeCount);
    }

    [Fact]
    public void Dispose_WithInterfaceRegistration_DisposesOnlyOnce()
    {
        var container = new ServiceContainer();
        var service = new DisposableService();

        container.Register<ITestService, DisposableService>(service);
        container.Dispose();

        Assert.Equal(1, service.DisposeCount);
    }

    #endregion

    #region Test Helpers

    private interface ITestService { }

    private class TestService : ITestService { }

    private class AnotherService { }

    private class DisposableService : ITestService, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            DisposeCount++;
        }
    }

    private class OrderedDisposable : IDisposable
    {
        private readonly int _order;
        private readonly List<int> _disposeOrder;

        public OrderedDisposable(int order, List<int> disposeOrder)
        {
            _order = order;
            _disposeOrder = disposeOrder;
        }

        public void Dispose() => _disposeOrder.Add(_order);
    }

    #endregion
}
