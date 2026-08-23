using Lus.Application.Common.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Net.Mime;

namespace Lus.Infrastructure.Common
{
    public static class JsonSerializerSettingsExtensions
    {
        public static async Task WriteJsonAsync(this HttpContext context, object jsonObject)
        {
            var json = JsonConvert.SerializeObject(jsonObject);
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(json);
        }

        public static void ApplyDefault(this JsonSerializerSettings settings)
        {
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.DefaultValueHandling = DefaultValueHandling.Include;
            settings.DateParseHandling = DateParseHandling.DateTimeOffset;
            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            settings.DateFormatString = "O"; // ISO 8601
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            settings.ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new OriginalCaseNamingStrategy()
            };

            // Request bodies are parsed by Newtonsoft (AddNewtonsoftJson). Types that carry a
            // System.Text.Json JsonElement — DraftPatchOp.Value — cannot survive that without
            // this converter: Newtonsoft yields ValueKind.Undefined and the first read throws.
            settings.Converters.Add(new JsonElementNewtonsoftConverter());
        }
    }
}
