using System.Text.Json;
using Lus.Contracts.Common.Builders;

namespace Lus.Application.Common.Builders
{
    /// <summary>
    /// Turns the raw single-line stdout envelope from
    /// <see cref="Lus.Application.Common.Ports.IPythonScriptsAdapter.RunAgentAsync"/> into a
    /// strongly-typed <see cref="AgentEnvelopeDto{TResult}"/>.
    ///
    /// Exists so no caller has to forward the raw string to its client. Parsing NEVER throws —
    /// unreadable stdout degrades to a handled failure envelope, because "an agent misbehaved"
    /// must not become "the request 500'd".
    /// </summary>
    public static class AgentEnvelopeParser
    {
        /// <summary>
        /// Matches the runner's output: PascalCase property names, so the default
        /// (case-insensitive) web options are used rather than a camelCase policy.
        /// </summary>
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        /// <summary>Emitted when stdout could not be read as an envelope at all.</summary>
        public const string UnparseableCode = "envelope_unparseable";

        /// <summary>Emitted when the envelope claims success but carries no result.</summary>
        public const string EmptyResultCode = "empty_result";

        /// <summary>
        /// Parses <paramref name="rawStdout"/>. Returns a failure envelope rather than throwing
        /// when the payload is unreadable, so a stray <c>print()</c> in an agent degrades one turn
        /// instead of crashing the request.
        /// </summary>
        public static AgentEnvelopeDto<TResult> Parse<TResult>(string? rawStdout, string requestedAgent)
            where TResult : class
        {
            if (string.IsNullOrWhiteSpace(rawStdout))
            {
                return Failure<TResult>(requestedAgent, UnparseableCode,
                    "הסוכן לא החזיר תוצאה.", "The agent returned nothing.");
            }

            AgentEnvelopeDto<TResult>? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<AgentEnvelopeDto<TResult>>(rawStdout.Trim(), Options);
            }
            catch (JsonException)
            {
                // Anything printed to stdout beside the envelope lands here. Stdout carries the
                // envelope and NOTHING else — diagnostics belong on stderr.
                envelope = null;
            }

            if (envelope is null)
            {
                return Failure<TResult>(requestedAgent, UnparseableCode,
                    "הסוכן החזיר תוצאה שלא ניתן לקרוא.", "The agent returned an unreadable result.");
            }

            if (envelope.Ok && envelope.Result is null)
            {
                // Ok:true with no Result is a contract violation on the Python side; surface it as
                // a handled failure rather than handing the caller a null Result behind an Ok flag.
                return Failure<TResult>(requestedAgent, EmptyResultCode,
                    "הסוכן לא החזיר תוצאה.", "The agent returned nothing.");
            }

            return envelope;
        }

        private static AgentEnvelopeDto<TResult> Failure<TResult>(
            string agent, string code, string messageHe, string messageEn)
            where TResult : class
            => new()
            {
                Ok = false,
                Agent = agent,
                SchemaVersion = 1,
                Result = null,
                ErrorInfo = new AgentErrorInfoDto
                {
                    Code = code,
                    UserMessageHe = messageHe,
                    UserMessageEn = messageEn,
                },
            };
    }
}
