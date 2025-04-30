namespace WhiteDependencyInjection.BaseTypes;

public interface IServiceScope: IDisposable
{
    IServiceProvider ServiceProvider { get; }
}