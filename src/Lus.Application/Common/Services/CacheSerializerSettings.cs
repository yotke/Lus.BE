using System.Text.Json;
using Newtonsoft.Json;

namespace Lus.Application.Common.Services
{
    /// <summary>
    /// Newtonsoft settings for anything persisted to the cache.
    ///
    /// Builder sessions carry patch ops whose Value is a System.Text.Json
    /// <see cref="JsonElement"/>. Newtonsoft has no idea what that is: it reflects over the
    /// struct's internals and hands back an element with ValueKind.Undefined, after which
    /// every access throws "Operation is not valid due to the current state of the object" —
    /// which surfaced as a 400 on the next canvas edit once a session had been round-tripped.
    /// </summary>
    public static class CacheSerializerSettings
    {
        public static JsonSerializerSettings Create() => Apply(new JsonSerializerSettings());

        public static JsonSerializerSettings Apply(JsonSerializerSettings settings)
        {
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            settings.Converters.Add(new JsonElementNewtonsoftConverter());
            return settings;
        }
    }
}
