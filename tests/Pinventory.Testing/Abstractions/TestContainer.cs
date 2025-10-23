using DotNet.Testcontainers.Containers;

using TUnit.Core.Interfaces;

namespace Pinventory.Testing.Abstractions;

public abstract class TestContainer<TContainer> : IAsyncInitializer, IAsyncDisposable
    where TContainer : IContainer
{
    protected readonly TContainer _container;

    public TestContainer()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        _container = Build();
    }

    protected abstract TContainer Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}