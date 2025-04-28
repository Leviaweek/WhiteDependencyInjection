# White Dependency Injection - Source Generator

## Overview

White Dependency Injection is a Roslyn source generator that automatically generates a dependency injection container based on attributes you apply to your classes. It eliminates the need for manual service registration while providing compile-time safety and performance benefits.

## Key Features

- Attribute-based registration - Simply decorate your classes with service attributes

- Compile-time DI container generation - No runtime reflection overhead

- Three lifetimes supported:

  - Singleton

  - Transient

  - Scoped

- Factory method support - Designate static factory methods for service creation

- Interface-based - Clean contract with IServiceProvider and IServiceScope

- IDisposable support - Automatic disposal of services

## Installation
```xml
<PackageReference Include="WhiteDependencyInjection" Version="1.0.0" />
```

## Usage

#### 1. Decorate your usages

Use one of the provided attributes to register your services:

```csharp
[SingletonService] // Basic singleton
public class MySingletonService { }

[SingletonService<IMyService>] // Singleton implementing interface
public class MyInterfaceService : IMyService { }

[TransientService] // Transient service
public class MyTransientService { }

[ScopedService] // Scoped service
public class MyScopedService { }
```

#### 2.Use factory methods (optional)

For complex initialization, you can use a factory method:

```csharp
[SingletonService]
public class ComplexService 
{
    [FactoryMethod]
    public static ComplexService Create(IServiceProvider provider) 
    {
        // Custom creation logic
    }
}
```

#### 3. Use the generated container

```csharp
var services = new ServiceProvider();

// Get services
var singleton = services.GetRequiredService<MySingletonService>();
var scopedService = services.CreateScope().ServiceProvider.GetService<MyScopedService>();
```

## Service Lifetimes

The source generator supports three service lifetimes:

| Attribute | Lifetime | Description |
|:---:|:---:|:---:|
| [SingletonService] | Singleton | One instance for the entire application |
| [SingletonService<T>] | Singleton | Singleton implementing interface T |
| [TransientService] | Transient | New instance each time requested |
| [TransientService<T>] | Transient | Transient implementing interface T |
| [ScopedService] | Scoped | One instance per scope |
| [ScopedService<T>] | Scoped | Scoped implementing interface T |

## Advanced Features

- **Generic services**: Works with generic service types
- **Constructor injection**: Automatically injects dependencies into constructors
- **Disposable support**: Automatically disposes of services when the container is disposed
- **Nullable reference types**: Fully supports nullable reference types

## Performance Benefits

Since everything is generated at compile time:

- No runtime reflection overhead
- No dynamic code generation
- Optimized service resolution
- Compile-time validation of service registrations

## Limitations

- Requires C# 9.0 or later
- All services must be concrete classes
- Constructor parameters must be resolvable by the DI container

## Example Project Structure

```
MyApp/
├── Services/
│   ├── MySingletonService.cs
│   ├── MyScopedService.cs
│   └── MyTransientService.cs
├── Program.cs
└── MyApp.csproj
```

The source generator will automatically create the ServiceProvider class and all necessary infrastructure code.