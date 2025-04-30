namespace WhiteDependencyInjection.BaseTypes;

public interface IServiceProvider: IDisposable
{
    T GetRequiredService<T>() where T : class;
    object? GetService<T>() where T : class;
    IServiceScope CreateScope();
    object? GetService(Type serviceType);
}