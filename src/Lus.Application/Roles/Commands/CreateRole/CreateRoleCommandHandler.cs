using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.Common.Exceptions;
using Lus.Application.Roles.Entities;
using Lus.Application.Roles.Repositories;
using Lus.Authorization;

namespace Lus.Application.Roles.Commands.CreateRole
{
    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Unit>
    {
        private readonly IRolesRepository rolesRepository;
        private readonly IMapper mapper;
        private readonly IEasyCachingProvider provider;
        private readonly IUserAccessor userAccessor;

        public CreateRoleCommandHandler(IRolesRepository rolesRepository, IMapper mapper, IEasyCachingProvider provider, IUserAccessor userAccessor)
        {
            this.userAccessor = userAccessor;
            this.provider = provider;
            this.rolesRepository = rolesRepository;
            this.mapper = mapper;
        }

        public async Task<Unit> Handle(CreateRoleCommand createCommand, CancellationToken cancellationToken)
        {
            var roles = await this.rolesRepository.GetAllListAsync(
                r => r.Name.ToLower() == createCommand.Name.ToLower(), cancellationToken);
            if (roles.Any())
            {
                var organizationId = await this.provider.GetAsync<int>(
                     $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}", cancellationToken);

                var rolesFind = roles.Where(r =>
                    !organizationId.HasValue || r.OrganizationId == organizationId.Value);

                if (rolesFind.Any())
                {
                    throw new EntityValidationException(nameof(CreateRoleCommand.Name), $"Role with name {createCommand.Name} already exists", 17);
                }
            }

            var roleToCreate = this.mapper.Map<Role>(createCommand);

            await this.rolesRepository.AddAsync(roleToCreate, cancellationToken);

            await this.rolesRepository.RunSpCreateUpdateMuniRoles();

            return Unit.Value;
        }
    }
}
