namespace WhiteDependencyInjection.BaseTypes.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
public sealed class ScopedServiceAttribute<T>() : ServiceAttribute(typeof(T));

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
public sealed class ScopedServiceAttribute() : ServiceAttribute(null);
