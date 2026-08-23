using MediatR;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.ImportTemplate
{
    /// <summary>
    /// The uploaded workbook has already been written to <see cref="FilePath"/> by the API
    /// layer: the Python importer reads the file itself, so the bytes never cross MediatR.
    /// </summary>
    public sealed record ImportTemplateCommand : IRequest<TemplateImportResponseDto>
    {
        public required string FilePath { get; init; }
    }
}
