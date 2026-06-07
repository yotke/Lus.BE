using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Lus.Application.Common.Exceptions;
using Lus.Application.Common.Extensions;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Application.HtmlTemplates.Queries.GetHtmlTemplate;
using Lus.Application.Images.Queries.GetImageById;
using Lus.Application.Images.Repositories;
using Lus.Application.Roles.Entities;
using Lus.Application.Roles.Repositories;
using Lus.Contracts.Images;
using Lus.Contracts.Roles;
using System.Drawing;

namespace Lus.Application.Roles.Commands.ModifyImage
{
    public class ModifyImageStatusCommandHandler : IRequestHandler<ModifyImageStatusCommand,Unit>
    {
        private readonly IImagesRepository imagesRepository;
        private readonly IMapper mapper;

        private readonly List<string> ImageOfPropertiesToIgnore =
            new List<string> { "OrganizationId", "Organization", "UniqueId" };

        public ModifyImageStatusCommandHandler(IImagesRepository imagesRepository, IMapper mapper)
        {
            this.imagesRepository = imagesRepository;
            this.mapper = mapper;
        }

        public async Task<Unit> Handle(ModifyImageStatusCommand modifyCommand, CancellationToken cancellationToken)
        {
            var savedImage = await this.imagesRepository.GetAsync(img=>img.UniqueId== modifyCommand.Id, cancellationToken);
            if (savedImage == null)
            {
                throw new EntityNotFoundException(nameof(Image), -1);
            }
            savedImage.Status = modifyCommand.Status;
           var image= await this.imagesRepository.UpdateAsync(savedImage, cancellationToken);
            return Unit.Value;


        }
    }
}