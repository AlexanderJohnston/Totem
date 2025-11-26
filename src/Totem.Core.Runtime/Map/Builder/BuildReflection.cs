namespace Totem.Map.Builder;

internal static class BuildReflection
{
    static readonly Assembly _totemRuntime = Assembly.GetExecutingAssembly();

    internal static IEnumerable<MethodInfo> GetPossibleTimelineMethods(this Type declaredType) =>
        from method in declaredType.GetRuntimeMethods()
        where method.DeclaringType != typeof(object)
        where method.DeclaringType?.Assembly != _totemRuntime
        select method;
}
