namespace WhiteDependencyInjection.BaseTypes.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
public sealed class SingletonServiceAttribute<T>() : ServiceAttribute(typeof(T));

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
public sealed class SingletonServiceAttribute() : ServiceAttribute(null);

