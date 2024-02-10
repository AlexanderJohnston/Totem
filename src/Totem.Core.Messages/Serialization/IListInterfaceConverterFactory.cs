using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Totem.Serialization
{
    public class IListInterfaceConverterFactory : JsonConverterFactory
    {
        public Type InterfaceType { get; }

        public IListInterfaceConverterFactory(Type interfaceType)
        {
            InterfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.Equals(typeof(IList<>).MakeGenericType(InterfaceType))
                && typeToConvert.GenericTypeArguments[0].Equals(InterfaceType);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(ListConverter<>).MakeGenericType(InterfaceType);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }
}
