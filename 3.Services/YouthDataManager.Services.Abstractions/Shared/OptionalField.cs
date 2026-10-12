using System.Text.Json;
using System.Text.Json.Serialization;

namespace YouthDataManager.Shared.Service.Abstractions;

[JsonConverter(typeof(OptionalFieldConverterFactory))]
public readonly record struct OptionalField<T>(T? Value, bool IsPresent = true)
{
    public static implicit operator OptionalField<T>(T? value) => new(value);
}
public sealed class OptionalFieldConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(OptionalField<>);
    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(OptionalFieldConverter<>).MakeGenericType(type.GetGenericArguments()))!;
    private sealed class OptionalFieldConverter<T> : JsonConverter<OptionalField<T>>
    {
        public override bool HandleNull => true;
        public override OptionalField<T> Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => new(JsonSerializer.Deserialize<T>(ref reader, options));
        public override void Write(Utf8JsonWriter writer, OptionalField<T> value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value.Value, options);
    }
}
