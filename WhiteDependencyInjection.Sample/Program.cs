namespace WhiteDependencyInjection.Sample;

using BaseTypes.Attributes;


internal static class Program
{
    private static void Main(string[] args)
    {
        var provider = new ServiceProvider();
        using var scope = provider.CreateScope();
        var singletonService = scope.ServiceProvider.GetRequiredService<SingletonService>();
        Console.WriteLine(singletonService.GetType().Name);

        var transientService = scope.ServiceProvider.GetRequiredService<TransientService>();
        Console.WriteLine(transientService.GetType().Name);

        var scopedService = scope.ServiceProvider.GetRequiredService<ScopedService>();
        Console.WriteLine(scopedService.GetType().Name);

        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        Console.WriteLine(testService.GetType().Name);
        
        var controller = scope.ServiceProvider.GetRequiredService<Controller>();
        Console.WriteLine(controller.GetType().Name);
    }
}

[SingletonService]
public sealed class SingletonService: IDisposable
{
    public SingletonService()
    {
        Console.WriteLine("SingletonService created");
    }

    public void Dispose()
    {
        Console.WriteLine("SingletonService disposed");
    }
}

[TransientService]
public sealed class TransientService: IDisposable
{
    public TransientService()
    {
        Console.WriteLine("TransientService created");
    }

    public void Dispose()
    {
        Console.WriteLine("TransientService disposed");
    }
}

[ScopedService]
public sealed class ScopedService
{
    public ScopedService()
    {
        Console.WriteLine("ScopedService created");
    }
}

[SingletonService]
public sealed class TestService
{
    public TestService()
    {
        Console.WriteLine("TestService created");
    }
}

[ScopedService]
public abstract class ControllerBase;

public sealed class Controller: ControllerBase;