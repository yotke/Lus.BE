using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Application.HtmlTemplates.Queries.GetHtmlTemplate;
using Lus.Application.HtmlTemplates.Repositories;
using Lus.Authorization;
using Lus.Contracts.HtmlTemplates;

namespace Lus.Application.HtmlTemplates.Commands.CreateHtmlTemplate
{
    public class CreateHtmlTemplateCommandHandler : IRequestHandler<CreateHtmlTemplateCommand, HtmlTemplateDto>
    {
        private readonly IHtmlTemplatesRepository htmlTemplatesRepository;
        private readonly IUserAccessor userAccessor;
        private readonly IMapper mapper;
        private readonly IEasyCachingProvider provider;
        private readonly IMediator mediator;

        public CreateHtmlTemplateCommandHandler(IMediator mediator, IHtmlTemplatesRepository htmlTemplateRepository, IUserAccessor userAccessor, IMapper mapper, IEasyCachingProvider provider)
        {

            this.htmlTemplatesRepository = htmlTemplateRepository;
            this.mapper = mapper;
            this.mediator = mediator;
            this.userAccessor = userAccessor;
            this.provider = provider;
        }

        public async Task<HtmlTemplateDto> Handle(CreateHtmlTemplateCommand createCommand, CancellationToken cancellationToken)
        {
            var organizationId = await this.provider.GetAsync<int>(
            $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}", cancellationToken);

            createCommand.OrganizationId = organizationId.HasValue ? organizationId.Value : null;
            var htmlTemplate = this.mapper.Map<HtmlTemplate>(createCommand);

            htmlTemplate = await this.htmlTemplatesRepository.AddAsync(htmlTemplate, cancellationToken);

            return await this.mediator.Send(new GetHtmlTemplateQuery(htmlTemplate.Id), cancellationToken);
        }
    }
}
