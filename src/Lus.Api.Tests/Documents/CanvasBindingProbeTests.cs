using System.Text.Json;
using Lus.Application.Documents.Builder.Services;
using Lus.Contracts.Documents.Builder;
using Lus.Infrastructure.Common;
using Newtonsoft.Json;
using Xunit;

namespace Lus.Api.Tests.Documents
{
    /// <summary>
    /// Exercises the exact bytes the browser sent when "Add row" returned 400, through the
    /// same deserialization the model binder performs.
    /// </summary>
    public class CanvasBindingProbeTests
    {
        /// <summary>Exactly the settings MVC binds request bodies with.</summary>
        private static JsonSerializerSettings ApiSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.ApplyDefault();
            return settings;
        }

        [Fact]
        public void The_browser_payload_binds_through_the_API_serializer()
        {
            // The reported 400: the body is parsed by Newtonsoft (AddNewtonsoftJson), which
            // cannot build a JsonElement and used to hand back ValueKind.Undefined.
            var body = JsonConvert.DeserializeObject<CanvasEditRequestDto>(BrowserBody, ApiSettings())!;

            var value = body.Ops[0].Value;
            Assert.NotNull(value);
            Assert.Equal(JsonValueKind.Object, value!.Value.ValueKind);
        }

        [Fact]
        public void An_API_bound_payload_survives_author_stamping()
        {
            var body = JsonConvert.DeserializeObject<CanvasEditRequestDto>(BrowserBody, ApiSettings())!;

            // This is the exact call that threw: PatchAuthorStamp.Stamp -> GetRawText().
            var stamped = PatchAuthorStamp.Stamp(body.Ops, PatchAuthorStamp.User);

            Assert.Equal("user", stamped[0].Value!.Value.GetProperty("Source").GetString());
        }

        [Fact]
        public void An_API_bound_rate_edit_binds_its_number()
        {
            const string rateBody =
                """{"Version":3,"Ops":[{"Op":"SetField","Path":"totals.hourlyRate","Value":225}]}""";

            var body = JsonConvert.DeserializeObject<CanvasEditRequestDto>(rateBody, ApiSettings())!;

            Assert.Equal(JsonValueKind.Number, body.Ops[0].Value!.Value.ValueKind);
            Assert.Equal(225m, body.Ops[0].Value!.Value.GetDecimal());
        }

        [Fact]
        public void An_API_bound_payload_applies_end_to_end()
        {
            var body = JsonConvert.DeserializeObject<CanvasEditRequestDto>(BrowserBody, ApiSettings())!;
            var stamped = PatchAuthorStamp.Stamp(body.Ops, PatchAuthorStamp.User);

            var (next, _) = DraftPatcher.Apply(new DocumentDraftDto { Version = 1 }, 1, stamped);

            Assert.Single(next.Rows);
            Assert.Equal("user", next.Rows[0].Source);
        }

        private const string BrowserBody =
            """{"Version":1,"Ops":[{"Op":"AddRow","Path":"rows","Value":{}}]}""";

        [Fact]
        public void The_browser_payload_deserializes()
        {
            var options = new System.Text.Json.JsonSerializerOptions(JsonSerializerDefaults.Web);
            var body = System.Text.Json.JsonSerializer.Deserialize<CanvasEditRequestDto>(BrowserBody, options);

            Assert.NotNull(body);
            Assert.Single(body!.Ops);
            Assert.Equal("AddRow", body.Ops[0].Op);
        }

        [Fact]
        public void The_browser_payload_survives_author_stamping()
        {
            var options = new System.Text.Json.JsonSerializerOptions(JsonSerializerDefaults.Web);
            var body = System.Text.Json.JsonSerializer.Deserialize<CanvasEditRequestDto>(BrowserBody, options)!;

            var stamped = PatchAuthorStamp.Stamp(body.Ops, PatchAuthorStamp.User);

            Assert.Equal("user", stamped[0].Value!.Value.GetProperty("Source").GetString());
        }

        [Fact]
        public void The_browser_payload_applies_to_an_empty_draft()
        {
            var options = new System.Text.Json.JsonSerializerOptions(JsonSerializerDefaults.Web);
            var body = System.Text.Json.JsonSerializer.Deserialize<CanvasEditRequestDto>(BrowserBody, options)!;
            var draft = new DocumentDraftDto { Version = 1 };

            var stamped = PatchAuthorStamp.Stamp(body.Ops, PatchAuthorStamp.User);
            var (next, _) = DraftPatcher.Apply(draft, 1, stamped);

            Assert.Single(next.Rows);
        }
    }
}
