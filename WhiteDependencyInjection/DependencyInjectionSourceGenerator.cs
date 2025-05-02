using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace WhiteDependencyInjection;

[Generator]
public class DependencyInjectionSourceGenerator : IIncrementalGenerator
{
    private const string AttributesNamespace = "WhiteDependencyInjection.BaseTypes.Attributes";
    private const string ServiceProviderNamespace = "WhiteDependencyInjection.BaseTypes";
    private const string ServiceProviderClassName = "ServiceProvider";
    private const string ScopedServiceProviderClassName = "ScopedServiceProvider";
    private const string ServiceScopeClassName = "ServiceScope";
    private const string IServiceScopeClassName = "IServiceScope";
    private const string IServiceProviderClassName = "IServiceProvider";
    private const string ServiceAttributeClassName = "ServiceAttribute";
    private const string FactoryMethodAttributeClassName = "FactoryMethodAttribute";
    private const string ScopedServiceAttributeClassName = "ScopedServiceAttribute";
    private const string TransientServiceAttributeClassName = "TransientServiceAttribute";
    private const string SingletonServiceAttributeClassName = "SingletonServiceAttribute";
    private const string SingletonServiceGenericAttributeClassName = "SingletonServiceAttribute<T>";
    private const string TransientServiceGenericAttributeClassName = "TransientServiceAttribute<T>";
    private const string ScopedServiceGenericAttributeClassName = "ScopedServiceAttribute<T>";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Debugger.Launch();
        
        var currentAssemblyServices = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: GetNamedTypeSymbolWithAttribute)
            .Where(symbol => symbol is not null)
            .Select((symbol, _) => CreateServiceDescriptor(symbol))
            .Where(serviceDescriptor => serviceDescriptor is not null)
            .Collect();

        var globalNamespaceProviderValue = context.AnalyzerConfigOptionsProvider
            .Select((x, _) =>
            {
                x.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                return rootNamespace;
            });
        
        context.RegisterSourceOutput(currentAssemblyServices.Combine(globalNamespaceProviderValue), (spc, tuple) =>
        {
            if (tuple is not (var services,  { } globalNamespace))
                return;
            
            var source = HandleServiceDescriptors(services!, globalNamespace);
            source = CSharpSyntaxTree.ParseText(source)
                .GetRoot()
                .NormalizeWhitespace()
                .ToFullString();
            spc.AddSource("ServiceProvider.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static ServiceDescriptor? CreateServiceDescriptor(INamedTypeSymbol? classSyntax)
    {
        if (classSyntax is null) return null;

        var classAttributes = classSyntax.GetAttributes();
        var baseClassAttributes = classSyntax.BaseType?.OriginalDefinition.GetAttributes();
        var interfacesAttributes = classSyntax.Interfaces
            .SelectMany(i => i.GetAttributes())
            .ToImmutableArray();
        
        var serviceAttribute = classAttributes
            .FirstOrDefault(attr => attr.AttributeClass?.BaseType?.Name == ServiceAttributeClassName) 
            ?? baseClassAttributes
                ?.FirstOrDefault(attr => attr.AttributeClass?.BaseType?.Name == ServiceAttributeClassName)
                ?? interfacesAttributes
                    .FirstOrDefault(attr => attr.AttributeClass?.BaseType?.Name == ServiceAttributeClassName);

        if (serviceAttribute is null)
            return null;

        INamedTypeSymbol? serviceType;

        if (serviceAttribute.AttributeClass?.IsGenericType == true)
        {
            var genericArguments = serviceAttribute.AttributeClass.TypeArguments;
            if (genericArguments.Length == 0)
                return null;

            serviceType = genericArguments[0] as INamedTypeSymbol;
        }
        else
        {
            serviceType = classSyntax;
        }

        if (serviceType is null)
            return null;

        var lifetimeAttribute =
            classAttributes.FirstOrDefault(attr => attr.AttributeClass?.BaseType?.Name == ServiceAttributeClassName)
            ?? baseClassAttributes
                ?.FirstOrDefault(attr => attr.AttributeClass?.BaseType?.Name == ServiceAttributeClassName)
                ?? interfacesAttributes
                .FirstOrDefault(attr => attr.AttributeClass?.BaseType?.Name == ServiceAttributeClassName);

        var lifetime = lifetimeAttribute?.AttributeClass?.Name switch
        {
            SingletonServiceGenericAttributeClassName => ServiceLifetime.Singleton,
            SingletonServiceAttributeClassName => ServiceLifetime.Singleton,
            TransientServiceGenericAttributeClassName => ServiceLifetime.Transient,
            TransientServiceAttributeClassName => ServiceLifetime.Transient,
            ScopedServiceGenericAttributeClassName => ServiceLifetime.Scoped,
            ScopedServiceAttributeClassName => ServiceLifetime.Scoped,
            _ => throw new ArgumentOutOfRangeException()
        };

        var serviceDescriptor = new ServiceDescriptor(
            serviceType: serviceType,
            implementationType: classSyntax,
            lifetime);

        return serviceDescriptor;
    }

    private static INamedTypeSymbol? GetNamedTypeSymbolWithAttribute(GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var symbol = ModelExtensions.GetDeclaredSymbol(semanticModel, classDeclarationSyntax, cancellationToken);

        if (symbol is not INamedTypeSymbol classSymbol)
            return null;

        if (symbol.IsAbstract)
            return null;
        
        var attributes = classSymbol.GetAttributes().AddRange(
            classSymbol.BaseType?.GetAttributes() ?? ImmutableArray<AttributeData>.Empty)
            .AddRange(classSymbol.Interfaces.SelectMany(x => x.GetAttributes()));

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.BaseType?.ToDisplayString() !=
                $"{AttributesNamespace}.{ServiceAttributeClassName}")
                continue;

            return classSymbol;
        }

        return null;
    }

    private static string HandleServiceDescriptors(ImmutableArray<ServiceDescriptor> services, string globalNamespace)
    {
        var builder = new SourceGeneratorBuilder();
        builder.Append("#nullable enable\n");
        builder.Append($"namespace {globalNamespace};");
        builder.Append("using System.Runtime.CompilerServices;");

        builder.AppendClass(ServiceProviderClassName, $"{ServiceProviderNamespace}.{IServiceProviderClassName}", sb =>
        {
            var providerServices = services
                .Where(s => s.Lifetime is not ServiceLifetime.Scoped)
                .ToImmutableArray();
            AddGetMethods(providerServices, sb);

            AddGenericGetService(providerServices, sb);
            
            AddGetService(providerServices, sb);

            AddGetRequiredService(builder);
            
            sb.AppendMethod("public", "CreateScope", $"{ServiceProviderNamespace}.{IServiceScopeClassName}", sb2 =>
            {
                sb2.Append($"return new {ServiceProviderNamespace}.{ServiceScopeClassName}(new ScopedServiceProvider(this));");
            });
            
            AddDispose(providerServices
                .Where(s => s.Lifetime is ServiceLifetime.Singleton)
                .Where(s
                => s.ServiceType.Interfaces.Any(x => x.Name == "IDisposable")).ToImmutableArray(), builder);
        });
        builder.AppendClass(ScopedServiceProviderClassName, $"{ServiceProviderNamespace}.{IServiceProviderClassName}", sb =>
        {
            var scopedServices = services
                .Where(s => s.Lifetime is ServiceLifetime.Scoped)
                .ToImmutableArray();
            sb.Append($"private readonly {ServiceProviderNamespace}.{IServiceProviderClassName} _serviceProvider;");
            sb.Append($"public ScopedServiceProvider({ServiceProviderNamespace}.{IServiceProviderClassName} serviceProvider) {{ _serviceProvider = serviceProvider; }}");
            AddScopedGetMethods(scopedServices, sb);
            
            AddScopedGetService(scopedServices, sb);
            
            AddGenericScopedGetService(scopedServices, sb);
            
            AddScopedGetRequiredService(sb);
            
            sb.AppendMethod("public", "CreateScope", $"{ServiceProviderNamespace}.{IServiceScopeClassName}", sb2 =>
            {
                sb2.Append($"return new {ServiceProviderNamespace}.{ServiceScopeClassName}(this);");
            });
            
            AddDispose(scopedServices
                    .Where(s => s.Lifetime is ServiceLifetime.Scoped)
                    .Where(s
                => s.ServiceType.Interfaces.Any(x => x.Name == "IDisposable"))
                    .ToImmutableArray(),
                builder);
        });
        return builder.Build();
    }

    private static void AddScopedGetRequiredService(SourceGeneratorBuilder sb)
    {
        sb.AppendMethod("public", "GetRequiredService<T>", "class", "T", sb2 =>
        {
            sb2.Append("var service = GetService<T>();");
            sb2.Append("if (service is null)");
            sb2.AppendOpenBracket();
            sb2.Append("throw new InvalidOperationException($" +
                       "\"Service of type {typeof(T)} not registered.\");");
            sb2.AppendCloseBracket();
            sb2.Append("return Unsafe.As<T>(service);");
        });
    }

    private static void AddGenericScopedGetService(ImmutableArray<ServiceDescriptor> services, SourceGeneratorBuilder sb)
    {
        sb.AppendMethod("public", "GetService<T>", "class", "object?", sb2 =>
        {
            sb2.Append("switch (typeof(T))");
            sb2.AppendOpenBracket();
            foreach (var serviceDescriptor in services)
            {
                var genericArguments = string.Join("__", serviceDescriptor.ServiceType.TypeArguments
                    .Select(arg => arg.Name));
                if (genericArguments != string.Empty)
                {
                    genericArguments = $"__{genericArguments}";
                }

                sb2.Append($"case {{}} t when t == typeof({serviceDescriptor.ServiceType.ToDisplayString()}):");

                sb2.Append($"return Get{serviceDescriptor.ServiceType.Name}{genericArguments}();");
                
            }

            sb2.Append("default:");
            sb2.Append("return _serviceProvider.GetService<T>();");
            sb2.AppendCloseBracket();
        });
    }
    
    private static void AddScopedGetService(ImmutableArray<ServiceDescriptor> services, SourceGeneratorBuilder sb)
    {
        sb.AppendMethod("public", "GetService", "object?", sb2 =>
        {
            sb2.Append("switch (serviceType)");
            sb2.AppendOpenBracket();
            foreach (var serviceDescriptor in services)
            {
                var genericArguments = string.Join("__", serviceDescriptor.ServiceType.TypeArguments
                    .Select(arg => arg.Name));
                if (genericArguments != string.Empty)
                {
                    genericArguments = $"__{genericArguments}";
                }

                sb2.Append($"case {{}} t when t == typeof({serviceDescriptor.ServiceType.ToDisplayString()}):");
                sb2.Append($"return Get{serviceDescriptor.ServiceType.Name}{genericArguments}();");
            }

            sb2.Append("default:");
            sb2.Append("return _serviceProvider.GetService(serviceType);");
            sb2.AppendCloseBracket();
        }, "Type serviceType");
    }

    private static void AddScopedGetMethods(ImmutableArray<ServiceDescriptor> services, SourceGeneratorBuilder sb)
    {
        foreach (var serviceDescriptor in services)
        {
            var genericArguments = string.Join("__", serviceDescriptor.ServiceType.TypeArguments
                .Select(arg => arg.Name));
            if (genericArguments != string.Empty)
            {
                genericArguments = $"__{genericArguments}";
            }
            var constructorArguments = string.Join(", ",
                serviceDescriptor.ServiceType.Constructors.First().Parameters
                    .Select(arg => $"GetRequiredService<{arg.Type.ToDisplayString()}>()"));
            
            var fabricMethod = GetFabricMethod(serviceDescriptor);
            var creationString = $"new {serviceDescriptor.ImplementationType}({constructorArguments})";

            if (fabricMethod is not null)
            {
                creationString = $"{serviceDescriptor.ImplementationType}.{fabricMethod.Name}(this)";
            }

            var fieldName = $"_{serviceDescriptor.ServiceType.Name}{genericArguments}";
            
            sb.Append(
                $"private {serviceDescriptor.ServiceType.ToDisplayString()}? {fieldName};");
            sb.AppendMethod("private", $"Get{serviceDescriptor.ServiceType.Name}{genericArguments}",
                serviceDescriptor.ServiceType.ToDisplayString(), sb2 =>
                {
                    sb2.Append($"if ({fieldName} is not null)");
                    sb2.AppendOpenBracket();
                    sb2.Append(
                        $"return {fieldName};");
                    sb2.AppendCloseBracket();
                    sb2.Append(
                        $"{fieldName} = " +
                        $"({serviceDescriptor.ServiceType.ToDisplayString()})" +
                        $"{creationString};");
                    sb2.Append(
                        $"return {fieldName};");
                });
        }
    }

    private static IMethodSymbol? GetFabricMethod(ServiceDescriptor serviceDescriptor)
    {
        var fabricMethod = serviceDescriptor.ImplementationType
            .GetMembers()
            .Where(m => m.Kind == SymbolKind.Method && m.IsStatic)
            .FirstOrDefault(m => m.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == FactoryMethodAttributeClassName));

        if (fabricMethod is not IMethodSymbol methodSymbol) return null;
        
        if (methodSymbol.Parameters.Length != 1)
            return null;
                
        if (methodSymbol.Parameters[0].Type.ToDisplayString() != $"{typeof(IServiceProvider).Namespace}.{nameof(IServiceProvider)}")
            return null;


        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, serviceDescriptor.ImplementationType))
            return null;
        
        return methodSymbol;
    }

    private static void AddGetMethods(ImmutableArray<ServiceDescriptor> services, SourceGeneratorBuilder sb)
    {
        foreach (var serviceDescriptor in services.Where(s => s.Lifetime is not ServiceLifetime.Scoped))
        {
            
            var genericArguments = string.Join("__", serviceDescriptor.ServiceType.TypeArguments
                .Select(arg => arg.Name));
            if (genericArguments != string.Empty)
            {
                genericArguments = $"__{genericArguments}";
            }

            var constructorArguments = string.Join(", ",
                serviceDescriptor.ServiceType.Constructors.First().Parameters
                    .Select(arg => $"GetRequiredService<{arg.Type.ToDisplayString()}>()"));
            
            var fabricMethod = GetFabricMethod(serviceDescriptor);
            var creationString = $"new {serviceDescriptor.ImplementationType}({constructorArguments})";

            if (fabricMethod is not null)
            {
                creationString = $"{serviceDescriptor.ImplementationType}.{fabricMethod.Name}(this)";
            }

            switch (serviceDescriptor.Lifetime)
            {
                case ServiceLifetime.Singleton:
                {
                    var fieldName = $"_{serviceDescriptor.ServiceType.Name}{genericArguments}";
                    sb.Append(
                        $"private {serviceDescriptor.ServiceType.ToDisplayString()}? " +
                        $"{fieldName};");

                    sb.AppendMethod("private", $"Get{serviceDescriptor.ServiceType.Name}{genericArguments}",
                        serviceDescriptor.ServiceType.ToDisplayString(), sb2 =>
                        {
                            sb2.Append($"if ({fieldName} is not null)");
                            sb2.AppendOpenBracket();
                            sb2.Append(
                                $"return {fieldName};");
                            sb2.AppendCloseBracket();
                            sb2.Append(
                                $"{fieldName} = " +
                                $"({serviceDescriptor.ServiceType.ToDisplayString()})" +
                                $"{creationString};");
                            sb2.Append(
                                $"return {fieldName};");
                        });
                    break;
                }
                case ServiceLifetime.Transient:
                    sb.AppendMethod("private", $"Get{serviceDescriptor.ServiceType.Name}{genericArguments}",
                        serviceDescriptor.ServiceType.ToDisplayString(), sb2 =>
                        {
                            sb2.Append(
                                $"return ({serviceDescriptor.ServiceType.ToDisplayString()})" +
                                $"{creationString};");
                        });
                    break;
                case ServiceLifetime.Scoped:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void AddGetService(ImmutableArray<ServiceDescriptor> services, SourceGeneratorBuilder sb)
    {
        sb.AppendMethod("public", "GetService", "object?", sb2 =>
        {
            sb2.Append("switch (serviceType)");
            sb2.AppendOpenBracket();
            foreach (var serviceDescriptor in services)
            {
                var genericArguments = string.Join("__", serviceDescriptor.ServiceType.TypeArguments
                    .Select(arg => arg.Name));
                if (genericArguments != string.Empty)
                {
                    genericArguments = $"__{genericArguments}";
                }

                sb2.Append($"case {{}} t when t == typeof({serviceDescriptor.ServiceType.ToDisplayString()}):");
                sb2.Append($"return Get{serviceDescriptor.ServiceType.Name}{genericArguments}();");
            }

            sb2.Append("default:");
            sb2.Append("return null;");
            sb2.AppendCloseBracket();
        }, "Type serviceType");
    }
    
    private static void AddGenericGetService(ImmutableArray<ServiceDescriptor> services, SourceGeneratorBuilder sb)
    {
        sb.AppendMethod("public", "GetService<T>", "class", "object?", sb2 =>
        {
            sb2.Append("switch (typeof(T))");
            sb2.AppendOpenBracket();
            foreach (var serviceDescriptor in services)
            {
                var genericArguments = string.Join("__", serviceDescriptor.ServiceType.TypeArguments
                    .Select(arg => arg.Name));
                if (genericArguments != string.Empty)
                {
                    genericArguments = $"__{genericArguments}";
                }

                sb2.Append($"case {{}} t when t == typeof({serviceDescriptor.ServiceType.ToDisplayString()}):");
                sb2.Append($"return Get{serviceDescriptor.ServiceType.Name}{genericArguments}();");
            }

            sb2.Append("default:");
            sb2.Append("return null;");
            sb2.AppendCloseBracket();
        });
    }

    private static void AddGetRequiredService(SourceGeneratorBuilder builder)
    {
        builder.AppendMethod("public", "GetRequiredService<T>", "class", "T", sb =>
        {
            sb.Append("var service = GetService<T>();");
            sb.Append("if (service is null)");
            sb.AppendOpenBracket();
            sb.Append("throw new InvalidOperationException($" +
                      "\"Service of type {typeof(T)} not registered.\");");
            sb.AppendCloseBracket();
            sb.Append("return Unsafe.As<T>(service);");
        });
    }

    private static void AddDispose(ImmutableArray<ServiceDescriptor> services, SourceGeneratorBuilder builder)
    {
        builder.AppendMethod("public", "Dispose", "void", sb2 =>
        {
            foreach (var serviceDescriptor in services)
            {
                var genericArguments = string.Join("__", serviceDescriptor.ServiceType.TypeArguments
                    .Select(arg => arg.Name));
                    
                if (genericArguments != string.Empty)
                {
                    genericArguments = $"__{genericArguments}";
                }

                var fieldName = $"_{serviceDescriptor.ServiceType.Name}{genericArguments}";
                sb2.Append($"{fieldName}?.Dispose();");
            }
        });
    }
}