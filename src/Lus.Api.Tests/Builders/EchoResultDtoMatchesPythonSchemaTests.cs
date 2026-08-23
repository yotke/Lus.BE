using System.Reflection;
using System.Text.Json;
using Lus.Contracts.Documents.Builder;
using Xunit;

namespace Lus.Api.Tests.Builders
{
    /// <summary>
    /// Hand-synced contracts drift. The runner validates every agent's output against
    /// <c>PythonScripts/agents/schemas/&lt;agent&gt;.result.schema.json</c> BEFORE emitting, so that
    /// schema — not the C# class — is the source of truth. This test reads the real schema file and
    /// fails the moment the DTO and the schema disagree.
    /// </summary>
    public class EchoResultDtoMatchesPythonSchemaTests
    {
        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "PythonScripts")))
                dir = dir.Parent;

            Assert.True(dir is not null, "PythonScripts/ must exist above the test binary");
            return dir!.FullName;
        }

        private static JsonElement EchoSchema()
        {
            var path = Path.Combine(RepoRoot(), "PythonScripts", "agents", "schemas",
                                    "echo.result.schema.json");
            Assert.True(File.Exists(path), $"schema not found at {path}");

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }

        private static string[] DtoPropertyNames() =>
            typeof(EchoResultDto)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

        [Fact]
        public void Dto_properties_match_the_schema_properties_exactly()
        {
            var schemaProps = EchoSchema().GetProperty("properties")
                .EnumerateObject()
                .Select(p => p.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(schemaProps, DtoPropertyNames());
        }

        [Fact]
        public void Every_schema_required_field_is_non_nullable_on_the_dto()
        {
            var required = EchoSchema().GetProperty("required")
                .EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();

            foreach (var name in required)
            {
                var prop = typeof(EchoResultDto).GetProperty(name);
                Assert.True(prop is not null, $"schema requires '{name}' but the DTO has no such property");

                var context = new NullabilityInfoContext().Create(prop!);
                Assert.True(
                    context.ReadState == NullabilityState.NotNull,
                    $"'{name}' is required by the schema, so the DTO property must be non-nullable");
            }
        }

        [Fact]
        public void Schema_forbids_extra_properties_so_the_dto_cannot_silently_lag()
        {
            // additionalProperties:false is what makes the property-set assertion above meaningful —
            // without it the agent could return fields the DTO never sees.
            Assert.False(EchoSchema().GetProperty("additionalProperties").GetBoolean());
        }

        [Fact]
        public void A_payload_shaped_by_the_schema_deserializes_into_the_dto()
        {
            const string payload = """{"Echo":"שלום עולם","Lang":"he"}""";

            var dto = JsonSerializer.Deserialize<EchoResultDto>(
                payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.NotNull(dto);
            Assert.Equal("שלום עולם", dto!.Echo);
            Assert.Equal("he", dto.Lang);
        }
    }
}
