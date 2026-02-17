using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Totem.Reflection;

namespace Totem.Runtime.Json
{
  /// <summary>
  /// Resolves JSON type info for Totem's durable type system, replacing
  /// JsonFormatContractResolver and JsonFormatSerializationBinder
  /// </summary>
  public class TotemJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
  {
    readonly IDurableTypeSet _durableTypes;

    public TotemJsonTypeInfoResolver(IDurableTypeSet durableTypes)
    {
      _durableTypes = durableTypes;
      Modifiers.Add(ModifyTypeInfo);
    }

    void ModifyTypeInfo(JsonTypeInfo typeInfo)
    {
      ModifyArrayType(typeInfo);
      ModifyObjectType(typeInfo);
    }

    void ModifyArrayType(JsonTypeInfo typeInfo)
    {
      if(typeInfo.Kind != JsonTypeInfoKind.Enumerable) return;
      if(!typeof(Many<>).IsAssignableFromGeneric(typeInfo.Type)) return;

      var elementType = typeInfo.Type.GetGenericArguments()[0];
      var callOf = Expression.Call(typeof(Many), "Of", new[] { elementType });
      var lambda = Expression.Lambda<Func<object>>(callOf).Compile();

      typeInfo.CreateObject = lambda;
    }

    void ModifyObjectType(JsonTypeInfo typeInfo)
    {
      if(typeInfo.Kind != JsonTypeInfoKind.Object) return;
      if(!_durableTypes.TryGetOrAdd(typeInfo.Type, out var durableType)) return;

      typeInfo.CreateObject = durableType.Create;

      const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

      var durableMembers = Enumerable.Empty<MemberInfo>()
        .Concat(typeInfo.Type.GetFields(flags))
        .Concat(typeInfo.Type.GetProperties(flags))
        .Where(IsDurableProperty)
        .ToList();

      typeInfo.Properties.Clear();

      foreach(var member in durableMembers)
      {
        var property = typeInfo.CreateJsonPropertyInfo(GetMemberType(member), GetPropertyName(member, typeInfo.Options));

        if(member is FieldInfo fieldInfo)
        {
          property.Get = obj => fieldInfo.GetValue(obj);
          property.Set = (obj, val) => fieldInfo.SetValue(obj, val);
        }
        else if(member is PropertyInfo propInfo)
        {
          var effectiveProp = member.ReflectedType != member.DeclaringType
            ? member.DeclaringType.GetProperty(member.Name, flags)
            : propInfo;

          if(effectiveProp == null) continue;

          var getter = effectiveProp.GetGetMethod(nonPublic: true);
          var setter = effectiveProp.GetSetMethod(nonPublic: true);

          var isWriteOnly = effectiveProp.IsDefined(typeof(WriteOnlyAttribute), inherit: true);

          if(getter != null)
          {
            property.Get = obj => effectiveProp.GetValue(obj);
          }

          if(!isWriteOnly && setter != null)
          {
            property.Set = (obj, val) => effectiveProp.SetValue(obj, val);
          }
        }

        typeInfo.Properties.Add(property);
      }
    }

    static bool IsDurableProperty(MemberInfo member) =>
      !member.IsDefined(typeof(TransientAttribute))
      && !member.IsDefined(typeof(CompilerGeneratedAttribute))
      && member.DeclaringType != typeof(Notion);

    static Type GetMemberType(MemberInfo member) =>
      member is FieldInfo f ? f.FieldType : ((PropertyInfo)member).PropertyType;

    static string GetPropertyName(MemberInfo member, JsonSerializerOptions options) =>
      options.PropertyNamingPolicy?.ConvertName(member.Name) ?? member.Name;
  }
}
