using Lus.Application.Common.Builders;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder
{
    public interface IDocumentBuilderEventSender
        : IBuilderEventSender<DraftPatchOp, DocumentQuestionDto, DocumentWarningDto>
    {
    }
}
