using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScimServiceProvider.Converters
{
    public class BooleanJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String:
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                        return false;
                    
                    // Handle various string representations of boolean values
                    return stringValue.ToLowerInvariant() switch
                    {
                        "true" => true,
                        "false" => false,
                        "1" => true,
                        "0" => false,
                        "yes" => true,
                        "no" => false,
                        _ => throw new JsonException($"Unable to convert \"{stringValue}\" to boolean.")
                    };
                case JsonTokenType.Number:
                    int numValue = reader.GetInt32();
                    return numValue != 0;
                default:
                    throw new JsonException($"Unexpected token type: {reader.TokenType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    public class NullableBooleanJsonConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String:
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                        return null;
                    
                    return stringValue.ToLowerInvariant() switch
                    {
                        "true" => true,
                        "false" => false,
                        "1" => true,
                        "0" => false,
                        "yes" => true,
                        "no" => false,
                        _ => throw new JsonException($"Unable to convert \"{stringValue}\" to boolean.")
                    };
                case JsonTokenType.Number:
                    int numValue = reader.GetInt32();
                    return numValue != 0;
                default:
                    throw new JsonException($"Unexpected token type: {reader.TokenType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteBooleanValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
