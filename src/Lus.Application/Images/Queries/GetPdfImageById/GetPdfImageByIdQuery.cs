using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lus.Application.Images.Queries.GetPdfImageById
{
    public record GetPdfImageByIdQuery(string Id) : IRequest<IActionResult>;
}
