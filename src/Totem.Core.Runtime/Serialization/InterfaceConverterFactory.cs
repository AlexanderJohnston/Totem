using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Totem.Serialization
{

    public class InterfaceConverterFactory : JsonConverterFactory
    {
        private readonly Type _concreteType;
        private readonly Type _interfaceType;

        public InterfaceConverterFactory(Type concrete, Type interfaceType)
        {
            _concreteType = concrete ?? throw new ArgumentNullException(nameof(concrete));
            _interfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == _interfaceType;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(InterfaceConverter<,>).MakeGenericType(_concreteType, _interfaceType);
            return Activator.CreateInstance(converterType) as JsonConverter;
        }
    }
}
