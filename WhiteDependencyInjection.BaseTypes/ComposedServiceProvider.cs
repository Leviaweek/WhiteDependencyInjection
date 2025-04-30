using System.Runtime.CompilerServices;

namespace WhiteDependencyInjection.BaseTypes;

public sealed class ComposedServiceProvider : IServiceProvider
{
    private readonly List<IServiceProvider> _providers;

    internal ComposedServiceProvider(List<IServiceProvider> providers)
    {
        _providers = providers;
    }

    public T GetRequiredService<T>() where T : class
    {
        foreach (var provider in _providers)
        {
            var service = provider.GetService<T>();
            if (service is not null)
            {
                return Unsafe.As<T>(service);
            }
        }

        throw new InvalidOperationException($"Service of type {typeof(T)} not found.");
    }

    public object? GetService<T>() where T : class
    {
        foreach (var provider in _providers)
        {
            var service = provider.GetService<T>();
            if (service is not null)
            {
                return service;
            }
        }

        return null;
    }

    public IServiceScope CreateScope()
    {
        var scopedProviders = new List<IServiceProvider>();
        foreach (var provider in _providers)
        {
            var scope = provider.CreateScope();
            scopedProviders.Add(scope.ServiceProvider);
        }

        return new ServiceScope(new ComposedServiceProvider(scopedProviders));
    }

    public object? GetService(Type serviceType)
    {
        foreach (var provider in _providers)
        {
            var service = provider.GetService(serviceType);
            if (service is not null)
            {
                return service;
            }
        }

        return null;
    }
    
    public void Dispose()
    {
        foreach (var provider in _providers)
        {
            provider.Dispose();
        }
    }
}