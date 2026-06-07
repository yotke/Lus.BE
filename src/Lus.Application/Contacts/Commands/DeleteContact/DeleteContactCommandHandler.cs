using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Contacts.Repositories;

namespace Lus.Application.Contacts.Commands.DeleteContact
{
    public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand, Unit>
    {
        private readonly IContactsRepository contactsRepository;

        public DeleteContactCommandHandler(IContactsRepository contactsRepository) => this.contactsRepository = contactsRepository;

        public async Task<Unit> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
        {
            var contact = await this.contactsRepository.GetSingleEntityAsync(request.Id, cancellationToken);
            await this.contactsRepository.DeleteAsync(contact, cancellationToken);

            return Unit.Value;
        }
    }
}
