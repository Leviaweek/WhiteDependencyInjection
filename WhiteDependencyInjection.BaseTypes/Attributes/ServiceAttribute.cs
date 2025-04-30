namespace WhiteDependencyInjection.BaseTypes.Attributes;

public abstract class ServiceAttribute(Type? baseType) : Attribute
{
    internal readonly Type? BaseType = baseType;
}