using MediatR;
using Lus.Application.Common.Exceptions;
using Lus.Application.Images.Entities;
using Lus.Application.Images.Repositories;

namespace Lus.Application.Images.Commands.DeleteImageById
{
    public class DeleteImageByIdCommandHandler : IRequestHandler<DeleteImageByIdCommand, Unit>
    {
        private readonly IImagesRepository imagesRepository;

        public DeleteImageByIdCommandHandler(IImagesRepository imagesRepository)
        {
            this.imagesRepository = imagesRepository;
        }

        public async Task<Unit> Handle(DeleteImageByIdCommand request, CancellationToken cancellationToken)
        {
            var image = await this.imagesRepository.GetAsync(im => im.UniqueId == request.ImageId, cancellationToken);
            if (image == null)
            {
                throw new EntityNotFoundException(nameof(Image));
            }
            await this.imagesRepository.DeleteAsync(image, cancellationToken);

            return Unit.Value;
        }
    }
}
