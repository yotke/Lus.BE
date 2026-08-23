using System.Text.Json.Serialization;

namespace Lus.Contracts.Common.Builders
{
    /// <summary>
    /// The strongly-typed C# face of the agent envelope contract
    /// (docs/PYTHON_AGENTS_BRIDGE.md): every agent writes exactly one line of PascalCase JSON to
    /// stdout, shaped
    /// <c>{"Ok",…,"Agent",…,"SchemaVersion",…,"Result",…,"ErrorInfo",…}</c>.
    ///
    /// Generic over the agent's own result type so an endpoint returns a real contract instead of
    /// forwarding the raw stdout string — the API surface, Swagger schema and client types all
    /// follow from <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">
    /// The agent's result shape. MUST mirror
    /// <c>PythonScripts/agents/schemas/&lt;agent&gt;.result.schema.json</c>; the runner validates
    /// against that schema before emitting, so a mismatch here is a C#-side bug.
    /// </typeparam>
    public sealed class AgentEnvelopeDto<TResult> where TResult : class
    {
        /// <summary>False for a HANDLED agent failure — never an exception, never a non-zero exit.</summary>
        public required bool Ok { get; init; }

        /// <summary>The agent name as REQUESTED (the runner echoes the caller's spelling back).</summary>
        public required string Agent { get; init; }

        public required int SchemaVersion { get; init; }

        /// <summary>Populated when <see cref="Ok"/>; null otherwise.</summary>
        public TResult? Result { get; init; }

        /// <summary>Populated when <see cref="Ok"/> is false; null otherwise.</summary>
        public AgentErrorInfoDto? ErrorInfo { get; init; }
    }

    /// <summary>
    /// A handled agent failure. <see cref="Code"/> is the localization contract — the frontend
    /// renders <c>agentErrors.&lt;code&gt;</c> from its own locale files, which is why the envelope
    /// only ever carries two message strings. They are a FALLBACK for a code the client does not
    /// yet know, not the primary display path.
    /// </summary>
    public sealed class AgentErrorInfoDto
    {
        public required string Code { get; init; }

        [JsonPropertyName("UserMessage")]
        public string? UserMessageHe { get; init; }

        public string? UserMessageEn { get; init; }
    }
}
