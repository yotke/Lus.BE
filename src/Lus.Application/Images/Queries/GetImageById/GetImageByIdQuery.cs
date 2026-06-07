using MediatR;
using Lus.Contracts.Images;

namespace Lus.Application.Images.Queries.GetImageById
{
    public record GetImageByIdQuery(string Id) : IRequest<ImageDto>;
}
