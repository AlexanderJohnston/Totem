namespace Totem;

[JsonConverter(typeof(IdJsonConverter))]
[TypeConverter(typeof(IdTypeConverter))]
public sealed class Id : IEquatable<Id>, IComparable<Id>
{
    readonly Guid _value;

    Id(Guid value) => _value = value;

    public override string ToString() => _value.ToString();
    public string ToShortString() => _value.ToString()[..4];
    public Guid ToGuid() => _value;

    public override int GetHashCode() =>
        _value.GetHashCode();

    public override bool Equals(object? obj) =>
      obj switch
      {
          Id other => _value == other._value,
          Guid other => _value == other,
          _ => false
      };

    public bool Equals(Id? other) =>
        _value == other?._value;

    public int CompareTo(Id? other) =>
        other is null ? 1 : _value.CompareTo(other._value);

    public Id DeriveId(string nameInThisNamespace) =>
      DeriveId(_value, nameInThisNamespace);

    public Id DeriveId<T>(T nameInThisNamespace) =>
      DeriveId(nameInThisNamespace?.ToString() ?? "");

    public int ComputeShardKey()
    {
        unchecked
        {
            var data = ToString();
            var hash1 = (5381 << 16) + 5381;
            var hash2 = hash1;

            for(var i = 0; i < data.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ data[i];

                if(i == data.Length - 1)
                {
                    break;
                }

                hash2 = ((hash2 << 5) + hash2) ^ data[i + 1];
            }

            return Math.Abs(hash1 + hash2 * 1566083941);
        }
    }

    //
    // Factory
    //

    public static bool TryFrom(Guid? value, [NotNullWhen(true)] out Id? id) =>
        (id = value is not null && value != Guid.Empty ? new Id(value.Value) : null) is not null;

    public static bool TryFrom(string? value, [NotNullWhen(true)] out Id? id) =>
        (id = Guid.TryParse(value, out var guid) && guid != Guid.Empty ? new Id(guid) : null) is not null;

    public static bool TryFrom(ulong value, [NotNullWhen(true)] out Id? id) =>
        (id = ToGuid(value, out var guid) && guid != Guid.Empty ? new Id(guid) : null) is not null;

    public static bool TryFrom(ushort value, [NotNullWhen(true)] out Id? id) =>
        (id = ToGuid(value, out var guid) && guid != Guid.Empty ? new Id(guid) : null) is not null;

    public static bool TryFromAny(string anything, [NotNullWhen(true)] out Id? id) =>
        (id = ToGuidFromBytes(anything, out var guid) && guid != Guid.Empty ? new Id(guid) : null) is not null;

    public static bool ToGuidFromBytes(string anything, out Guid guid)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(anything));
        var clipped = bytes[..16];
        try
        {
            guid = new Guid(clipped);
        }
        catch (Exception ex)
        {
            guid = Guid.Empty;
            return false;
        }
        return true;
    }

    public static bool ToGuid(ushort value, out Guid guid)
    {
        byte[] bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        guid = new Guid(bytes);
        return true;
    }
    public static bool ToGuid(ulong value, out Guid guid)
    {
        byte[] bytes = new byte[64];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        guid = new Guid(bytes);
        return true;
    }

    public static Id NewId() =>
        new(Guid.NewGuid());

    public static Id From(Guid value) =>
        TryFrom(value, out var id) ? id : throw new ArgumentOutOfRangeException(nameof(value));

    public static Id From(string value) =>
        TryFrom(value, out var id) ? id : throw new ArgumentOutOfRangeException(nameof(value));

    public static Id From(ulong value) =>
        TryFrom(value, out var id) ? id : throw new ArgumentOutOfRangeException(nameof(value));

    public static Id From(ushort value) =>
        TryFrom(value, out var id) ? id : throw new ArgumentOutOfRangeException(nameof(value));

    public static bool operator ==(Id? x, Id? y) => EqualityComparer<Id?>.Default.Equals(x, y);
    public static bool operator !=(Id? x, Id? y) => !(x == y);
    public static bool operator <(Id? x, Id? y) => Comparer<Id?>.Default.Compare(x, y) < 0;
    public static bool operator >(Id? x, Id? y) => Comparer<Id?>.Default.Compare(x, y) > 0;
    public static bool operator <=(Id? x, Id? y) => Comparer<Id?>.Default.Compare(x, y) <= 0;
    public static bool operator >=(Id? x, Id? y) => Comparer<Id?>.Default.Compare(x, y) >= 0;

    public static explicit operator Id(Guid value) => From(value);
    public static explicit operator Id(string value) => From(value);
    public static explicit operator Id(ulong value) => From(value);
    public static explicit operator Id(ushort value) => From(value);
    public static implicit operator Guid(Id value) => value.ToGuid();

    static Id DeriveId(Guid value, string name)
    {
        // Adapted from https://github.com/LogosBible/Logos.Utility/commit/c681eb1993f3ada3fc4c66d1cdb93491e56805aa

        var version = 5;
        var namespaceBytes = value.ToByteArray();
        var nameBytes = Encoding.UTF8.GetBytes(name.ToString() ?? "");
        var derivedBytes = new byte[16];

        SwapByteOrder(namespaceBytes);

        using var sha1 = SHA1.Create();
        sha1.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
        sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);

        Array.Copy(sha1.Hash!, 0, derivedBytes, 0, 16);

        derivedBytes[6] = (byte) ((derivedBytes[6] & 0x0F) | (version << 4));
        derivedBytes[8] = (byte) ((derivedBytes[8] & 0x3F) | 0x80);

        SwapByteOrder(derivedBytes);

        return From(new Guid(derivedBytes));
    }

    static void SwapByteOrder(byte[] bytes)
    {
        SwapBytes(0, 3);
        SwapBytes(1, 2);
        SwapBytes(4, 5);
        SwapBytes(6, 7);

        void SwapBytes(int left, int right) =>
            (bytes[right], bytes[left]) = (bytes[left], bytes[right]);
    }

    //
    // Converters
    //

    sealed class IdJsonConverter : JsonConverter<Id>
    {
        public override Id? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            TryFrom(reader.GetString(), out var id) ? id : null;

        public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }

    sealed class IdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(string) || sourceType == typeof(Guid);

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
            destinationType == typeof(string) || destinationType == typeof(Guid);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            value switch
            {
                string s => From(s),
                Guid g => From(g),
                _ => base.ConvertFrom(context, culture, value)
            };

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
            (destinationType == typeof(string) || destinationType == typeof(Guid)) && value is Id id
                ? id.ToString()
                : base.ConvertTo(context, culture, value, destinationType);
    }
}
