using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Totem.Map;

namespace Totem.External
{
    public class SystemTypeConverter : JsonConverter<Type>
    {
        RuntimeMap _map;

        public SystemTypeConverter(RuntimeMap map)
        {
            _map = map;
        }

        public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (_map.Events.TypeKeys.Contains(typeToConvert))
            {
                var qualifiedName = reader.GetString();
                return Type.GetType(qualifiedName);
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            if (_map.Events.TypeKeys.Contains(value))
            {
                writer.WriteStringValue(value.FullName);
            }
        }
    }

    public class EventConverter : JsonConverter<IEvent>
    {
        public override IEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null;
        }

        public override void Write(Utf8JsonWriter writer, IEvent value, JsonSerializerOptions options)
        {
            var text = JsonSerializer.Serialize((object)value);
            writer.WriteStringValue(text);
        }
    }
    public class ListConverter<M> : JsonConverter<IList<M>>
    {
        public override IList<M> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<List<M>>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, IList<M> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class IListInterfaceConverterFactory : JsonConverterFactory
    {
        public IListInterfaceConverterFactory(Type interfaceType)
        {
            InterfaceType = interfaceType;
        }

        public Type InterfaceType { get; }

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert.Equals(typeof(IList<>).MakeGenericType(this.InterfaceType))
             && typeToConvert.GenericTypeArguments[0].Equals(this.InterfaceType))
            {
                return true;
            }

            return false;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(ListConverter<>).MakeGenericType(InterfaceType));
        }
    }

    public class InterfaceConverter<M, I> : JsonConverter<I> where M : class, I
    {
        public override I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<M>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, I value, JsonSerializerOptions options) 
        {
            JsonSerializer.Serialize(writer, value, typeof(M), options);
        }
    }

    public class InterfaceConverterFactory : JsonConverterFactory
    {
        readonly Type ConcreteType;
        readonly Type InterfaceType;

        public InterfaceConverterFactory(Type concrete, Type interfaceType)
        {
            ConcreteType = concrete;
            InterfaceType = interfaceType;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == InterfaceType;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            // can do this in the constructor!
            var converterType = typeof(InterfaceConverter<,>).MakeGenericType(ConcreteType, InterfaceType);

            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }

    //public sealed class SerializationBinder
    //{
    //    readonly RuntimeMap _map;

    //    public SerializationBinder(RuntimeMap map)
    //    {
    //        _map = map;
    //    }
    //}

    //public class EventConverter<M> : JsonConverter<IEvent>
    //{
    //    public override IEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {

    //    }
    //    public override void Write(Utf8JsonWriter writer, IEvent value, JsonSerializerOptions options)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
