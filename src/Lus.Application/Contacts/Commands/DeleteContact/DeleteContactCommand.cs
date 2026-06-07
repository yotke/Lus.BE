using MediatR;

namespace Lus.Application.Contacts.Commands.DeleteContact
{
    public record DeleteContactCommand(int Id) : IRequest<Unit>;
}
