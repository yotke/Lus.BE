using MediatR;
using Microsoft.AspNetCore.Mvc;
using Lus.Contracts.Images;
using Lus.Contracts.Roles;

namespace Lus.Application.Roles.Commands.ModifyImage
{
    public record ModifyImageStatusCommand : IRequest<Unit>
    {
        public string Id { get; set; }
        public int? Status { get; set; }
    }
}
