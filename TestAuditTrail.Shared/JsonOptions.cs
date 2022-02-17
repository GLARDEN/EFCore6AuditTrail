using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using TestAuditTrail.Shared.Json.Serialization;

namespace TestAuditTrail.JsonUtilities;

public class JsonOptions
{
    public static JsonSerializerOptions Default { get; }

    static JsonOptions()
    {
        Default = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        Default.Converters.Add(new DateOnlyJsonConverter());
        Default.Converters.Add(new TimeOnlyJsonConverter());
    }
}
