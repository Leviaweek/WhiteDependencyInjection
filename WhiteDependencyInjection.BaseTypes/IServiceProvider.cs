namespace WhiteDependencyInjection.BaseTypes;

public interface IServiceProvider: IDisposable
{
    public T GetRequiredService<T>() where T : class;
    public object? GetService<T>() where T : class;
    public IServiceScope CreateScope();
    public object? GetService(Type serviceType);
}