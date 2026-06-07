using MediatR;

namespace Lus.Application.Images.Commands.DeleteImageById
{
    public record DeleteImageByIdCommand(string ImageId) : IRequest<Unit>;
}
