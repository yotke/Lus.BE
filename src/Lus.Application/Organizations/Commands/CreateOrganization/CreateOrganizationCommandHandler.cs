using AutoMapper;
using MediatR;
using Lus.Application.Organizations.Entities;
using Lus.Application.Organizations.Repositories;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Commands.CreateOrganization
{
    public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, OrganizationDto>
    {
        private readonly IOrganizationsRepository organizationsRepository;
        private readonly IMapper mapper;

        public CreateOrganizationCommandHandler(IOrganizationsRepository organizationsRepository, IMapper mapper)
        {
            this.organizationsRepository = organizationsRepository;
            this.mapper = mapper;
        }

        public async Task<OrganizationDto> Handle(CreateOrganizationCommand createCommand, CancellationToken cancellationToken)
        {
            var organization = this.mapper.Map<Organization>(createCommand);

            organization = await this.organizationsRepository.AddAsync(organization, cancellationToken);

            return this.mapper.Map<OrganizationDto>(organization);
        }
    }
}
