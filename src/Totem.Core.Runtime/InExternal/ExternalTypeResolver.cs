using System;

namespace Totem.InExternal;

internal static class ExternalTypeResolver
{
    internal static Type? Resolve(string? typeName)
    {
        if(string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        var type = Type.GetType(typeName, throwOnError: false, ignoreCase: false);

        if(type is not null)
        {
            return type;
        }

        foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);

            if(type is not null)
            {
                return type;
            }
        }

        return null;
    }
}
