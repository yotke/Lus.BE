namespace Lus.Contracts.Documents.Builder
{
    /// <summary>
    /// Result of the <c>doc.echo</c> bridge smoke agent.
    /// MUST mirror <c>PythonScripts/agents/schemas/echo.result.schema.json</c>
    /// (<c>{"Echo": string, "Lang": string}</c>, additionalProperties false) — the runner validates
    /// the agent's output against that schema before emitting, so any drift here is a C#-side bug.
    /// Pinned by <c>EchoResultDtoMatchesPythonSchemaTests</c>.
    /// </summary>
    public sealed class EchoResultDto
    {
        /// <summary>The text sent in, round-tripped through the subprocess. Hebrew must survive intact.</summary>
        public required string Echo { get; init; }

        /// <summary>The language code the agent ran under ("he" | "en").</summary>
        public required string Lang { get; init; }
    }
}
