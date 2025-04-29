// See https://aka.ms/new-console-template for more information

using WhiteDependencyInjection;

var provider = new ServiceProvider();
using var scope = provider.CreateScope();
var singletonService = scope.ServiceProvider.GetRequiredService<SingletonService>();
Console.WriteLine(singletonService.GetType().Name);

[SingletonService]
public sealed class SingletonService
{
    public SingletonService()
    {
        Console.WriteLine("SingletonService created");
    }
}

[TransientService]
public sealed class TransientService
{
    public TransientService()
    {
        Console.WriteLine("TransientService created");
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