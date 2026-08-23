using System.Text.Json;
using Lus.Application.Common.Services;
using Lus.Application.Documents.Builder.Services;
using Lus.Contracts.Documents.Builder;
using Newtonsoft.Json;
using Xunit;

namespace Lus.Api.Tests.Documents
{
    /// <summary>
    /// The session is persisted through EasyCaching's Newtonsoft serializer, but
    /// <see cref="DraftPatchOp.Value"/> is a System.Text.Json <see cref="JsonElement"/>.
    /// These pin the round trip: a mangled element comes back as ValueKind.Undefined and
    /// every later access throws "Operation is not valid due to the current state of the
    /// object" — the 400 seen when adding a row after a few turns.
    /// </summary>
    public class SessionRoundTripTests
    {
        private static DocumentBuildSession SessionWithHistory()
        {
            var op = new DraftPatchOp
            {
                Op = "AddRow",
                Path = "rows",
                Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Hours = 3m, Subject = "עבודה" })
            };

            return new DocumentBuildSession
            {
                UserId = 42,
                Draft = new DocumentDraftDto { Version = 1 },
                UndoForwards = { new List<DraftPatchOp> { op } },
                UndoInverses = { new List<DraftPatchOp> { op } },
            };
        }

        [Fact]
        public void A_session_survives_the_cache_serializer()
        {
            var settings = CacheSerializerSettings.Create();

            var json = JsonConvert.SerializeObject(SessionWithHistory(), settings);
            var restored = JsonConvert.DeserializeObject<DocumentBuildSession>(json, settings);

            Assert.NotNull(restored);
            var value = restored!.UndoForwards[0][0].Value;
            Assert.NotNull(value);
            Assert.Equal(JsonValueKind.Object, value!.Value.ValueKind);
            Assert.Equal(3m, value.Value.GetProperty("Hours").GetDecimal());
        }

        [Fact]
        public void A_restored_op_can_still_be_applied()
        {
            var settings = CacheSerializerSettings.Create();
            var json = JsonConvert.SerializeObject(SessionWithHistory(), settings);
            var restored = JsonConvert.DeserializeObject<DocumentBuildSession>(json, settings)!;

            // Redo re-applies a stored forward batch: it must not throw.
            var (next, _) = DraftPatcher.Apply(
                new DocumentDraftDto { Version = 0 }, 0, restored.UndoForwards[0]);

            Assert.Single(next.Rows);
            Assert.Equal(3m, next.Rows[0].Hours);
        }

        [Fact]
        public void A_restored_session_can_be_saved_again()
        {
            var settings = CacheSerializerSettings.Create();
            var once = JsonConvert.SerializeObject(SessionWithHistory(), settings);
            var restored = JsonConvert.DeserializeObject<DocumentBuildSession>(once, settings)!;

            // The second save is where the mangled element bites: every later request
            // re-persists the history it just loaded.
            var twice = JsonConvert.SerializeObject(restored, settings);

            Assert.Contains("Hours", twice);
        }

        [Fact]
        public void Without_the_converter_the_element_is_destroyed()
        {
            // Pins WHY the converter exists. Plain Newtonsoft reflects over JsonElement's
            // internals and returns an Undefined element; touching it throws the exact
            // exception the API returned as 400.
            var plain = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

            var json = JsonConvert.SerializeObject(SessionWithHistory(), plain);
            var restored = JsonConvert.DeserializeObject<DocumentBuildSession>(json, plain);

            var value = restored!.UndoForwards[0][0].Value;
            var kind = value?.ValueKind ?? JsonValueKind.Undefined;
            Assert.Equal(JsonValueKind.Undefined, kind);

            if (value is { } element)
                Assert.Throws<InvalidOperationException>(() => element.GetRawText());
        }

        [Fact]
        public void A_null_value_round_trips_as_null()
        {
            var settings = CacheSerializerSettings.Create();
            var session = new DocumentBuildSession
            {
                UndoForwards = { new List<DraftPatchOp> { new() { Op = "RemoveRow", Path = "rows[0]" } } },
            };

            var json = JsonConvert.SerializeObject(session, settings);
            var restored = JsonConvert.DeserializeObject<DocumentBuildSession>(json, settings)!;

            Assert.Null(restored.UndoForwards[0][0].Value);
        }
    }
}
