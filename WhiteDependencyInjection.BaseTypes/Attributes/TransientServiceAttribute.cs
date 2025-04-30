namespace WhiteDependencyInjection.BaseTypes.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
public sealed class TransientServiceAttribute<T>() : ServiceAttribute(typeof(T));

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
public sealed class TransientServiceAttribute() : ServiceAttribute(null);