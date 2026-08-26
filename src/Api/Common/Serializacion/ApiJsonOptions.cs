using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Common.Serializacion;

public static class ApiJsonOptions
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions();
        Configure(options);
        return options;
    }

    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    }
}