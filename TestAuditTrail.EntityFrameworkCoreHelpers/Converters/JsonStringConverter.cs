using System.Text.Json;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TestAuditTrail.JsonUtilities;

namespace TestAuditTrail.EntityFrameworkCore.Shared.Converters;

public class JsonStringConverter<T> : ValueConverter<T?, string>
{
    public JsonStringConverter(JsonSerializerOptions? jsonSerializerOptions = null) : base(
            value => JsonSerializer.Serialize(value, jsonSerializerOptions ?? JsonOptions.Default),
            json => JsonSerializer.Deserialize<T>(json, jsonSerializerOptions ?? JsonOptions.Default))
    {
    }
}