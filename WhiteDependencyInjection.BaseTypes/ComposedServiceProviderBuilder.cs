namespace WhiteDependencyInjection.BaseTypes;

public sealed class ComposedServiceProviderBuilder
{
    private readonly List<IServiceProvider> _providers = [];

    public ComposedServiceProviderBuilder AddProvider(IServiceProvider provider)
    {
        _providers.Add(provider);
        return this;
    }
    
    public ComposedServiceProvider Build() => new(_providers);
}