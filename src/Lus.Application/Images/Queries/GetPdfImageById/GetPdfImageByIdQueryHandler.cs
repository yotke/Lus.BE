using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Lus.Application.Common.Exceptions;
using Lus.Application.Common.Services.RazorViewRenderService;
using Lus.Application.Images.Entities;
using Lus.Application.Images.Repositories;
using Lus.Contracts.Images.Types;

namespace Lus.Application.Images.Queries.GetPdfImageById
{
    public class GetPdfImageByIdQueryHandler : IRequestHandler<GetPdfImageByIdQuery, IActionResult>
    {
        private readonly IImagesRepository imagesRepository;
        private readonly IMapper mapper;

        public GetPdfImageByIdQueryHandler(IImagesRepository imagesRepository, IMapper mapper)
        {
            this.imagesRepository = imagesRepository;
            this.mapper = mapper;
        }

        public async Task<IActionResult> Handle(GetPdfImageByIdQuery request, CancellationToken cancellationToken)
        {
            var image = await this.imagesRepository.GetAsync(im => im.UniqueId == request.Id, cancellationToken);
            if (image == null)
            {
                throw new EntityNotFoundException(nameof(Image));
            }

            MemoryStream ms = new MemoryStream(image.FileContent);
            return new FileStreamResult(ms, "application/pdf");
        }
    }
}