using AutoMapper;
using IdentityModel;
using MediatR;
using Lus.Application.Common.Exceptions;
using Lus.Application.Common.Extensions;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Application.HtmlTemplates.Queries.GetHtmlTemplate;
using Lus.Application.HtmlTemplates.Repositories;
using Lus.Contracts.HtmlTemplates;

namespace Lus.Application.HtmlTemplates.Commands.ModifyHtmlTemplate
{
    public class ModifyHtmlTemplateCommandHandler : IRequestHandler<ModifyHtmlTemplateCommand, HtmlTemplateDto>
    {
        private readonly IHtmlTemplatesRepository htmlTemplatesRepository;
        private readonly IMediator mediator;
        private readonly List<string> htmlTemplateListOfPropertiesToIgnore = new List<string> { "OrganizationId", "Organization", "HtmlType" };

        public ModifyHtmlTemplateCommandHandler(IHtmlTemplatesRepository htmlTemplatesRepository, IMediator mediator)
        {
            this.htmlTemplatesRepository = htmlTemplatesRepository;
            this.mediator = mediator;
        }

        public async Task<HtmlTemplateDto> Handle(ModifyHtmlTemplateCommand modifyCommand, CancellationToken cancellationToken)
        {
            var savedhtmlTemplate = await this.htmlTemplatesRepository.GetWithIncludeAsync(modifyCommand.Id, cancellationToken, html => html.Organization);
            if (savedhtmlTemplate == null)
            {
                throw new EntityNotFoundException(nameof(HtmlTemplate), modifyCommand.Id);
            }
            savedhtmlTemplate.CopyIfDifferent(modifyCommand, htmlTemplateListOfPropertiesToIgnore);

            await this.htmlTemplatesRepository.UpdateAsync(savedhtmlTemplate, cancellationToken);

            return await this.mediator.Send(new GetHtmlTemplateQuery(savedhtmlTemplate.Id), cancellationToken);
        }
    }
}
