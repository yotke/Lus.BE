using Microsoft.AspNetCore.Mvc;
using Lus.Infrastructure.Common;
using Newtonsoft.Json;

namespace Lus.Infrastructure
{
    public static class SerializationSettings
    {
        public static JsonSerializerSettings JsonSettings { get; private set; }

        public static void DefaultApiJsonOptions(MvcNewtonsoftJsonOptions options)
        {
            options.SerializerSettings.ApplyDefault();

            JsonSettings = options.SerializerSettings;
        }
    }
}
