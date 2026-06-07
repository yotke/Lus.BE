using AutoMapper;
using MediatR;
using Lus.Application.Common.Exceptions;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Application.HtmlTemplates.Repositories;
using Lus.Contracts.HtmlTemplates;

namespace Lus.Application.HtmlTemplates.Queries.GetHtmlTemplate
{
    public class GetHtmlTemplateQueryHandler : IRequestHandler<GetHtmlTemplateQuery, HtmlTemplateDto>
    {
        private readonly IHtmlTemplatesRepository htmlTemplatesRepository;
        private readonly IMapper mapper;

        public GetHtmlTemplateQueryHandler(IHtmlTemplatesRepository htmlTemplatesRepository, IMapper mapper)
        {
            this.htmlTemplatesRepository = htmlTemplatesRepository;
            this.mapper = mapper;
        }

        public async Task<HtmlTemplateDto> Handle(GetHtmlTemplateQuery request, CancellationToken cancellationToken)
        {
            var htmlTemplate = await this.htmlTemplatesRepository.GetWithIncludeAsync(request.HtmlTemplateId, cancellationToken, html => html.Organization);
            if (htmlTemplate == null)
            {
                throw new EntityNotFoundException(nameof(HtmlTemplate), request.HtmlTemplateId);
            }

            var htmlTemplateDto = mapper.Map<HtmlTemplateDto>(htmlTemplate);

            return htmlTemplateDto;
        }
    }
}
