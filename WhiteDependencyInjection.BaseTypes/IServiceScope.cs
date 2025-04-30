namespace WhiteDependencyInjection.BaseTypes;

public interface IServiceScope: IDisposable
{
    public IServiceProvider ServiceProvider { get; }
}