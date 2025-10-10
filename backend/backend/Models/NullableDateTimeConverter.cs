using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataMatchBackend.JsonConverters // ตรวจสอบ namespace นี้ให้ถูกต้อง
{
    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        private static readonly string[] _supportedFormats = {
            "yyyy-MM-ddTHH:mm:ss.ffffffZ", // ISO 8601 with Z (UTC)
            "yyyy-MM-ddTHH:mm:ss.fffZ",    // ISO 8601 with Z (UTC)
            "yyyy-MM-ddTHH:mm:ssZ",        // ISO 8601 with Z (UTC)
            "yyyy-MM-ddTHH:mm:ss.ffffff",  // ISO 8601 no Z
            "yyyy-MM-ddTHH:mm:ss.fff",     // ISO 8601 no Z
            "yyyy-MM-ddTHH:mm:ss",         // ISO 8601 no Z
            "yyyy-MM-dd",                  // Date only
        };

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? dateString = reader.GetString();

                if (string.IsNullOrWhiteSpace(dateString))
                {
                    return null; // Handle empty string or whitespace as null
                }

                // Try parsing with exact formats first
                foreach (var format in _supportedFormats)
                {
                    if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture,
                                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                                out DateTime parsedDate))
                    {
                        return parsedDate;
                    }
                }

                // Fallback to general parsing if exact formats fail
                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                        out DateTime generalParsedDate))
                {
                    return generalParsedDate;
                }

                throw new JsonException($"'{dateString}' is not a valid date or unsupported format for DateTime?");
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} for DateTime?");
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                // Always write out as ISO 8601 UTC with 'Z' suffix
                writer.WriteStringValue(value.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}