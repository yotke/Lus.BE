using Lus.Application.Documents.Builder;
using Lus.Contracts.Documents.Builder;
using Lus.Infrastructure.SignalRHubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Lus.Api.Infrastructure.SignalRHubs.Services
{
    /// <summary>
    /// Fire-and-forget Document Builder events. A SignalR outage must never fail a turn.
    /// Writes are detached from the caller's token: a turn-budget cancel must not abort
    /// the hub connection (ArmyLuz scar — do not "clean up").
    /// </summary>
    public sealed class DocumentBuilderEventSender : IDocumentBuilderEventSender
    {
        private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(10);

        private readonly IHubContext<DocumentBuilderHub> hubContext;
        private readonly ILogger<DocumentBuilderEventSender> logger;

        public DocumentBuilderEventSender(
            IHubContext<DocumentBuilderHub> hubContext,
            ILogger<DocumentBuilderEventSender> logger)
        {
            this.hubContext = hubContext;
            this.logger = logger;
        }

        public Task SendDraftPatchedAsync(string jobId, string userId, string sessionId, int version,
            IReadOnlyList<DraftPatchOp> ops, CancellationToken ct = default)
            => SendAsync(jobId, userId, sessionId, "DraftPatched", new
            {
                SessionId = sessionId,
                Version = version,
                Ops = ops,
                Timestamp = DateTime.UtcNow,
            });

        public Task SendAgentStatusAsync(string jobId, string userId, string agent, string state,
            string? detail = null, CancellationToken ct = default)
            => SendAsync(jobId, userId, sessionId: userId, "AgentStatus", new
            {
                Agent = agent,
                State = state,
                Detail = detail,
            });

        public Task SendQuestionAskedAsync(string jobId, string userId, DocumentQuestionDto question,
            CancellationToken ct = default)
            => SendAsync(jobId, userId, sessionId: userId, "QuestionAsked", new
            {
                Question = question,
            });

        public Task SendBuilderMessageAsync(string jobId, string userId, string role, string text,
            CancellationToken ct = default, IReadOnlyList<string>? suggestions = null)
            => SendAsync(jobId, userId, sessionId: userId, "BuilderMessage", new
            {
                Role = role,
                Text = text,
                Suggestions = suggestions,
            });

        public Task SendCommitCompletedAsync(string jobId, string userId, string sessionId, int organizationId,
            IReadOnlyDictionary<string, int> counts, IReadOnlyList<DocumentWarningDto> warnings,
            CancellationToken ct = default)
            => SendAsync(jobId, userId, sessionId, "BuilderCommitCompleted", new
            {
                JobId = jobId,
                SessionId = sessionId,
                OrganizationId = organizationId,
                Counts = counts,
                Warnings = warnings,
                Timestamp = DateTime.UtcNow,
            });

        public Task SendErrorAsync(string jobId, string userId, string errorCode, string userSafeMessage,
            CancellationToken ct = default)
            => SendAsync(jobId, userId, sessionId: userId, "BuilderError", new
            {
                JobId = jobId,
                ErrorCode = errorCode,
                Error = userSafeMessage,
                Timestamp = DateTime.UtcNow,
            });

        private static CancellationTokenSource DetachedWriteToken() => new(SendTimeout);

        private async Task SendAsync(string jobId, string userId, string sessionId, string eventName, object payload)
        {
            using var write = DetachedWriteToken();
            try
            {
                await this.hubContext.Clients.Group($"user_{userId}").SendAsync(eventName, payload, write.Token);
                if (!string.IsNullOrEmpty(sessionId) && sessionId != userId)
                    await this.hubContext.Clients.Group($"session_{sessionId}").SendAsync(eventName, payload, write.Token);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex,
                    "Failed to send {EventName} for job {JobId} — clients fall back to GET session.",
                    eventName, jobId);
            }
        }
    }
}
