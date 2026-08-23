using Lus.Application.Common.Builders;
using Lus.Contracts.Documents.Builder;
using Microsoft.Extensions.Logging;

namespace Lus.Application.Documents.Builder.Services
{
    public sealed class NullDocumentBuilderEventSender : IDocumentBuilderEventSender
    {
        private readonly ILogger<NullDocumentBuilderEventSender> logger;

        public NullDocumentBuilderEventSender(ILogger<NullDocumentBuilderEventSender> logger)
            => this.logger = logger;

        public Task SendDraftPatchedAsync(string jobId, string userId, string sessionId, int version,
            IReadOnlyList<DraftPatchOp> ops, CancellationToken ct = default) => Task.CompletedTask;

        public Task SendAgentStatusAsync(string jobId, string userId, string agent, string state,
            string? detail = null, CancellationToken ct = default) => Task.CompletedTask;

        public Task SendQuestionAskedAsync(string jobId, string userId, DocumentQuestionDto question,
            CancellationToken ct = default) => Task.CompletedTask;

        public Task SendBuilderMessageAsync(string jobId, string userId, string role, string text,
            CancellationToken ct = default, IReadOnlyList<string>? suggestions = null) => Task.CompletedTask;

        public Task SendCommitCompletedAsync(string jobId, string userId, string sessionId, int organizationId,
            IReadOnlyDictionary<string, int> counts, IReadOnlyList<DocumentWarningDto> warnings,
            CancellationToken ct = default) => Task.CompletedTask;

        public Task SendErrorAsync(string jobId, string userId, string errorCode, string userSafeMessage,
            CancellationToken ct = default)
        {
            this.logger.LogWarning("Builder error {Code}: {Message}", errorCode, userSafeMessage);
            return Task.CompletedTask;
        }
    }
}
