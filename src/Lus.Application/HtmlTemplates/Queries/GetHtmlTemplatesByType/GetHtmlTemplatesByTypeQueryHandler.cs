using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.HtmlTemplates.Repositories;
using Lus.Authorization;
using Lus.Contracts.HtmlTemplates;

namespace Lus.Application.HtmlTemplates.Queries.GetHtmlTemplatesByType
{
    public class GetHtmlTemplatesByTypeQueryHandler : IRequestHandler<GetHtmlTemplatesByTypeQuery, ICollection<HtmlTemplateDto>>
    {
        private readonly IHtmlTemplatesRepository htmlTemplatesRepository;
        private readonly IMapper mapper;
        private readonly IEasyCachingProvider provider;
        private readonly IUserAccessor userAccessor;

        public GetHtmlTemplatesByTypeQueryHandler(IUserAccessor userAccessor, IHtmlTemplatesRepository htmlTemplatesRepository, IMapper mapper, IEasyCachingProvider provider)
        {
            this.userAccessor = userAccessor;
            this.provider = provider;
            this.htmlTemplatesRepository = htmlTemplatesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<HtmlTemplateDto>> Handle(GetHtmlTemplatesByTypeQuery request, CancellationToken cancellationToken)
        {
            var organizationId = await this.provider.GetAsync<int>(
                $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}", cancellationToken);


            var htmlTemplates = await this.htmlTemplatesRepository.GetAllListAsync(html => html.HtmlType == request.HtmlType && (!organizationId.HasValue || html.OrganizationId == organizationId.Value), cancellationToken, htmlTemp => htmlTemp.Organization);

            var htmlTemplatesDto = mapper.Map<ICollection<HtmlTemplateDto>>(htmlTemplates);

            return htmlTemplatesDto;
        }
    }
}
