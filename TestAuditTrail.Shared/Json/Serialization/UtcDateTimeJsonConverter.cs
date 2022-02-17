using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestAuditTrail.Shared.Json.Serialization
{
    public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        private readonly string serializationFormat;

        public UtcDateTimeJsonConverter() : this(null)
        {
        }

        public UtcDateTimeJsonConverter(string? serializationFormat)
        {
            this.serializationFormat = serializationFormat ?? "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return DateTime.Parse(value!).ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue((value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value)
                .ToString(serializationFormat));
    }
}