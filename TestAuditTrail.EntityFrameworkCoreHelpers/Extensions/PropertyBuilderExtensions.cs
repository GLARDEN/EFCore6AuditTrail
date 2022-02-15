using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestAuditTrail.EntityFrameworkCore.Shared.Comparers;
using TestAuditTrail.EntityFrameworkCore.Shared.Converters;
using TestAuditTrail.Shared.Json.Serialization;



namespace TestAuditTrail.EntityFrameworkCore.Shared.Extensions;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<DateOnly> HasDateOnlyConversion<DateOnly>(this PropertyBuilder<DateOnly> propertyBuilder)
        => propertyBuilder.HasConversion<DateOnlyConverter, DateOnlyComparer>();

    public static PropertyBuilder<TimeOnly> HasTimeOnlyConversion<TimeOnly>(this PropertyBuilder<TimeOnly> propertyBuilder)
        => propertyBuilder.HasConversion<TimeOnlyConverter, TimeOnlyComparer>();

    public static PropertyBuilder<T?> HasJsonConversion<T>(this PropertyBuilder<T?> propertyBuilder, JsonSerializerOptions? jsonSerializerOptions = null, bool useUtcDate = false, bool serializeEnumAsString = false)
    {
        jsonSerializerOptions ??= new(JsonOptions.Default);

        if (useUtcDate)
        {
            jsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        }

        if (serializeEnumAsString)
        {
            jsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        }

        var converter = new JsonStringConverter<T>(jsonSerializerOptions);
        var comparer = new JsonStringComparer<T>(jsonSerializerOptions);

        propertyBuilder.HasConversion(converter, comparer);

        return propertyBuilder;
    }

}
