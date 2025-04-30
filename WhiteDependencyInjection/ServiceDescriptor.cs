using Microsoft.CodeAnalysis;

namespace WhiteDependencyInjection;

internal sealed class ServiceDescriptor
{
    public INamedTypeSymbol ServiceType { get; }
    public INamedTypeSymbol ImplementationType { get; }
    public ServiceLifetime Lifetime { get; }

    public ServiceDescriptor(INamedTypeSymbol serviceType, INamedTypeSymbol implementationType,
        ServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
    }
}