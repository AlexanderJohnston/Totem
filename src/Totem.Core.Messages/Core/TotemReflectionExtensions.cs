namespace Totem.Core;

public static class TotemReflectionExtensions
{
    public static bool IsConcreteClass(this Type type) =>
        type.IsPublic && type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters;

    public static bool IsGenericTypeDefinition(this Type type, Type definition) =>
        type.IsGenericType
        && !type.IsGenericTypeDefinition
        && type.GetGenericTypeDefinition() == definition;

    public static bool ImplementsGenericInterface(this Type type, Type interfaceDefinition) =>
        type.GetInterfaces().Any(i => i.IsGenericTypeDefinition(interfaceDefinition));

    public static IEnumerable<Type> GetInterfaceGenericArguments(this Type type, Type interfaceDefinition) =>
        type.IsGenericTypeDefinition(interfaceDefinition) ? type.GetGenericArguments() : Enumerable.Empty<Type>();

    public static IEnumerable<Type> GetImplementedInterfaceGenericArguments(this Type type, Type interfaceDefinition)
    {
        foreach(var typeInterface in type.GetInterfaces().Prepend(type))
        {
            var foundArguments = false;

            foreach(var argument in typeInterface.GetInterfaceGenericArguments(interfaceDefinition))
            {
                yield return argument;

                foundArguments = true;
            }

            if(foundArguments)
            {
                yield break;
            }
        }
    }
}
