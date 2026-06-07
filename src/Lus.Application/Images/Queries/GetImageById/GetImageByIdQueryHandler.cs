using AutoMapper;
using MediatR;
using Lus.Application.Common.Exceptions;
using Lus.Application.Images.Entities;
using Lus.Application.Images.Repositories;
using Lus.Contracts.Images;

namespace Lus.Application.Images.Queries.GetImageById
{
    public class GetImageByIdQueryHandler : IRequestHandler<GetImageByIdQuery, ImageDto>
    {
        private readonly IImagesRepository imagesRepository;
        private readonly IMapper mapper;

        public GetImageByIdQueryHandler(IImagesRepository imagesRepository, IMapper mapper)
        {
            this.imagesRepository = imagesRepository;
            this.mapper = mapper;
        }

        public async Task<ImageDto> Handle(GetImageByIdQuery request, CancellationToken cancellationToken)
        {
            var image = await this.imagesRepository.GetAsync(im => im.UniqueId == request.Id, cancellationToken);
            if (image == null)
            {
                throw new EntityNotFoundException(nameof(Image));
            }

            var imageDto = this.mapper.Map<ImageDto>(image);

            return imageDto;
        }
    }
}
