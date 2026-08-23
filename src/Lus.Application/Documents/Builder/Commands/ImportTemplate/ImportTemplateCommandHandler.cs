using MediatR;
using Lus.Application.Documents.Builder.Orchestration;
using Lus.Authorization;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.ImportTemplate
{
    public sealed class ImportTemplateCommandHandler
        : IRequestHandler<ImportTemplateCommand, TemplateImportResponseDto>
    {
        private readonly DocumentBuilderOrchestrator orchestrator;
        private readonly IUserAccessor users;

        public ImportTemplateCommandHandler(
            DocumentBuilderOrchestrator orchestrator,
            IUserAccessor users)
        {
            this.orchestrator = orchestrator;
            this.users = users;
        }

        public async Task<TemplateImportResponseDto> Handle(
            ImportTemplateCommand request, CancellationToken cancellationToken)
        {
            var result = await this.orchestrator.ImportTemplateAsync(
                this.users.ProjectUser.Id, request.FilePath, cancellationToken);

            var template = result.Draft.Template;
            return new TemplateImportResponseDto
            {
                Version = result.Version,
                Ops = result.Ops.ToList(),
                SheetName = template?.SheetName ?? "",
                Rtl = template?.Rtl ?? true,
                MergeCount = template?.MergeCount ?? 0,
                DataBandStartRow = template?.DataBandStartRow ?? 0,
            };
        }
    }
}
