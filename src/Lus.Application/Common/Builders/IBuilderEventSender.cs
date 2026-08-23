namespace Lus.Application.Common.Builders
{
    /// <summary>
    /// Entity-agnostic builder SignalR event contract (ARCH-1) — extracted bottom-up from
    /// Organizations/Builder/IOrgBuilderEventSender once the Rules builder needed the identical
    /// canvas events. Event names/payloads/groups are FROZEN; this extraction changes only which
    /// interface(s) an implementation declares. Generic over the entity's own patch-op/question/
    /// warning DTOs (kept OUT of the kernel — Common/Builders must never reference
    /// Organizations/Rules Contracts types, see BuilderArchitectureGuardTests) so an entity
    /// interface simply closes the generics over its own Contracts types (see
    /// <c>IOrgBuilderEventSender</c>). Entity-specific events (e.g. the org's
    /// OrganizationMaterialized) stay on the entity's own interface. All sends are fire-safe: an
    /// outage must never fail a turn.
    /// </summary>
    public interface IBuilderEventSender<TPatchOp, TQuestion, TWarning>
    {
        /// <summary>"DraftPatched" — the canvas applies ops and animates the change.</summary>
        Task SendDraftPatchedAsync(string jobId, string userId, string sessionId, int version,
            IReadOnlyList<TPatchOp> ops, CancellationToken ct = default);

        /// <summary>"AgentStatus" — the ticker line ("question_planner is thinking…"). state: queued|running|done|failed.</summary>
        Task SendAgentStatusAsync(string jobId, string userId, string agent, string state,
            string? detail = null, CancellationToken ct = default);

        /// <summary>"QuestionAsked" — quick-reply chips (↑/↓ + Enter).</summary>
        Task SendQuestionAskedAsync(string jobId, string userId, TQuestion question,
            CancellationToken ct = default);

        /// <summary>
        /// "BuilderMessage" — a chat-rail message (role: assistant|system). The optional
        /// suggestions carry advisor growth chips (additive payload field; null for every
        /// existing caller, so the event shape is unchanged for non-advisor messages).
        /// (ct stays ahead of suggestions to keep existing positional callers source-compatible.)
        /// </summary>
        Task SendBuilderMessageAsync(string jobId, string userId, string role, string text,
            CancellationToken ct = default, IReadOnlyList<string>? suggestions = null);

        /// <summary>"BuilderCommitCompleted" — truthful terminal event carrying the validator warnings.</summary>
        Task SendCommitCompletedAsync(string jobId, string userId, string sessionId, int organizationId,
            IReadOnlyDictionary<string, int> counts, IReadOnlyList<TWarning> warnings,
            CancellationToken ct = default);

        /// <summary>Existing "OrganizationCreationError" event — reused for builder job failures.</summary>
        Task SendErrorAsync(string jobId, string userId, string errorCode, string userSafeMessage,
            CancellationToken ct = default);
    }
}
