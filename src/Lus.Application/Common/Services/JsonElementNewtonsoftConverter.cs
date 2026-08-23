using System.Text.Json;
using Newtonsoft.Json;

namespace Lus.Application.Common.Services
{
    /// <summary>
    /// Lets Newtonsoft read and write a System.Text.Json <see cref="JsonElement"/>.
    ///
    /// Needed anywhere Newtonsoft touches a type that holds one — which in this app is both
    /// the MVC request/response pipeline (AddNewtonsoftJson) and the cache. Without it
    /// Newtonsoft cannot construct the struct, silently produces `default(JsonElement)` with
    /// ValueKind.Undefined, and the first access throws "Operation is not valid due to the
    /// current state of the object".
    ///
    /// That is not a hypothetical: it is what made every canvas edit return 400, because
    /// <c>DraftPatchOp.Value</c> is a JsonElement and the request body is parsed by Newtonsoft.
    /// </summary>
    public sealed class JsonElementNewtonsoftConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(JsonElement) || objectType == typeof(JsonElement?);

        public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is not JsonElement element || element.ValueKind == JsonValueKind.Undefined)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteRawValue(element.GetRawText());
        }

        public override object? ReadJson(
            JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return objectType == typeof(JsonElement?) ? null : default(JsonElement);

            var token = Newtonsoft.Json.Linq.JToken.Load(reader);
            using var document = JsonDocument.Parse(token.ToString(Newtonsoft.Json.Formatting.None));

            // Clone: the element must outlive the document it was parsed from.
            return document.RootElement.Clone();
        }
    }
}
