namespace Lus.Application.Common.Builders
{
    /// <summary>
    /// Outcome of a single subprocess agent call — extracted bottom-up (ARCH-1) from
    /// Organizations/Builder/Agents/IBuilderAgentClient once a second builder needed the
    /// identical success/failure shape. Entity-agnostic: <see cref="BuilderAgentClientCore"/>
    /// returns this; the org-specific <c>IBuilderAgentClient</c> still exposes its OWN
    /// <c>AgentResult&lt;T&gt;</c> (same shape) so the org interface/result types are
    /// unchanged for every existing caller and test — the thin org wrapper converts.
    /// </summary>
    public class AgentResult<T> where T : class
    {
        public bool Ok { get; init; }
        public T? Value { get; init; }
        public string? FailureCode { get; init; }
        public string? FailureMessage { get; init; }

        public static AgentResult<T> Success(T value) => new() { Ok = true, Value = value };

        public static AgentResult<T> Failed(string code, string message)
            => new() { Ok = false, FailureCode = code, FailureMessage = message };
    }
}
