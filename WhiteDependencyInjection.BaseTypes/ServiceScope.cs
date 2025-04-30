namespace WhiteDependencyInjection.BaseTypes;

public sealed class ServiceScope(IServiceProvider serviceProvider) : IServiceScope
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public void Dispose() => ServiceProvider.Dispose();
}