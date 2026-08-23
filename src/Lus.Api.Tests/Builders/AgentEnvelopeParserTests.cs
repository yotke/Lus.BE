using Lus.Application.Common.Builders;
using Lus.Contracts.Documents.Builder;
using Xunit;

namespace Lus.Api.Tests.Builders
{
    /// <summary>
    /// The envelope contract, typed. Parsing must NEVER throw — a misbehaving agent degrades one
    /// turn, it does not 500 the request. See docs/PYTHON_AGENTS_BRIDGE.md.
    /// </summary>
    public class AgentEnvelopeParserTests
    {
        private const string Agent = "doc.echo";

        [Fact]
        public void Parses_a_success_envelope_into_a_typed_result()
        {
            const string raw =
                """{"Ok":true,"Agent":"doc.echo","SchemaVersion":1,"Result":{"Echo":"שלום","Lang":"he"},"ErrorInfo":null}""";

            var envelope = AgentEnvelopeParser.Parse<EchoResultDto>(raw, Agent);

            Assert.True(envelope.Ok);
            Assert.Equal("doc.echo", envelope.Agent);
            Assert.Equal(1, envelope.SchemaVersion);
            Assert.Equal("שלום", envelope.Result!.Echo);
            Assert.Equal("he", envelope.Result.Lang);
            Assert.Null(envelope.ErrorInfo);
        }

        [Fact]
        public void Parses_a_handled_failure_envelope_and_keeps_both_fallback_messages()
        {
            const string raw =
                """{"Ok":false,"Agent":"doc.echo","SchemaVersion":1,"Result":null,"ErrorInfo":{"Code":"agent_error","UserMessage":"שגיאה","UserMessageEn":"Error"}}""";

            var envelope = AgentEnvelopeParser.Parse<EchoResultDto>(raw, Agent);

            Assert.False(envelope.Ok);
            Assert.Null(envelope.Result);
            // Code is the localization contract; the strings are fallback only.
            Assert.Equal("agent_error", envelope.ErrorInfo!.Code);
            Assert.Equal("שגיאה", envelope.ErrorInfo.UserMessageHe);
            Assert.Equal("Error", envelope.ErrorInfo.UserMessageEn);
        }

        [Theory]
        [InlineData("this is not json")]
        [InlineData("{\"Ok\": ")]
        [InlineData("<html>500</html>")]
        public void Unreadable_stdout_degrades_to_a_failure_envelope_instead_of_throwing(string raw)
        {
            var envelope = AgentEnvelopeParser.Parse<EchoResultDto>(raw, Agent);

            Assert.False(envelope.Ok);
            Assert.Equal(AgentEnvelopeParser.UnparseableCode, envelope.ErrorInfo!.Code);
            Assert.Equal(Agent, envelope.Agent);
            Assert.Null(envelope.Result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Empty_stdout_degrades_to_a_failure_envelope(string? raw)
        {
            var envelope = AgentEnvelopeParser.Parse<EchoResultDto>(raw, Agent);

            Assert.False(envelope.Ok);
            Assert.Equal(AgentEnvelopeParser.UnparseableCode, envelope.ErrorInfo!.Code);
        }

        [Fact]
        public void Ok_true_with_no_result_is_treated_as_a_handled_failure()
        {
            // A Python-side contract violation. Better to surface it than to hand the caller a
            // null Result sitting behind an Ok flag.
            const string raw =
                """{"Ok":true,"Agent":"doc.echo","SchemaVersion":1,"Result":null,"ErrorInfo":null}""";

            var envelope = AgentEnvelopeParser.Parse<EchoResultDto>(raw, Agent);

            Assert.False(envelope.Ok);
            Assert.Equal(AgentEnvelopeParser.EmptyResultCode, envelope.ErrorInfo!.Code);
        }

        [Fact]
        public void Surrounding_whitespace_and_a_trailing_newline_are_tolerated()
        {
            // The runner prints with a newline; the adapter trims, but the parser must not depend
            // on that having happened.
            const string raw =
                "\n  {\"Ok\":true,\"Agent\":\"doc.echo\",\"SchemaVersion\":1,\"Result\":{\"Echo\":\"א\",\"Lang\":\"he\"},\"ErrorInfo\":null}  \n";

            var envelope = AgentEnvelopeParser.Parse<EchoResultDto>(raw, Agent);

            Assert.True(envelope.Ok);
            Assert.Equal("א", envelope.Result!.Echo);
        }

        [Fact]
        public void Envelope_property_names_are_matched_case_insensitively()
        {
            // Defence in depth: the contract is PascalCase, but a casing slip in an agent must not
            // silently null out the result.
            const string raw =
                """{"ok":true,"agent":"doc.echo","schemaVersion":1,"result":{"echo":"ב","lang":"he"},"errorInfo":null}""";

            var envelope = AgentEnvelopeParser.Parse<EchoResultDto>(raw, Agent);

            Assert.True(envelope.Ok);
            Assert.Equal("ב", envelope.Result!.Echo);
        }
    }
}
