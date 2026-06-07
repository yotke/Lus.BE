using EasyCaching.Core;
using MediatR;
using Lus.Application.Common.Ports;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Exceptions;

namespace Lus.Application.Organizations.Commands.ChangeCurrentOrganization
{
    public class ChangeCurrentOrganizationCommandHandler : IRequestHandler<ChangeCurrentOrganizationCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IEasyCachingProvider provider;
        private readonly IUserAccessor userAccessor;
        private readonly IRecaptchaAdapter recaptchaAdapter;

        public ChangeCurrentOrganizationCommandHandler(IRecaptchaAdapter recaptchaAdapter, IUsersRepository usersRepository, IUserAccessor userAccessor, IEasyCachingProvider provider)
        {
            this.recaptchaAdapter = recaptchaAdapter;
            this.userAccessor = userAccessor;
            this.usersRepository = usersRepository;
            this.provider = provider;
        }

        public async Task<Unit> Handle(ChangeCurrentOrganizationCommand command, CancellationToken cancellationToken)
        {
            var result = await this.recaptchaAdapter.CheckRecaptcha(cancellationToken);
            if (!result)
            {
                throw new MembershipException(41);
            }

            var user = await this.usersRepository.GetWithIncludeAsync(this.userAccessor.ProjectUser.Id,
                cancellationToken, u => u.UserOrganizations);

            if (user == null)
            {
                throw new MembershipException(10);
            }

            if (user.UserOrganizations.All(uo => uo.OrganizationId != command.OrganizationId))
            {
                throw new MembershipException(11);
            }

            if (await this.provider.ExistsAsync($"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{user.Id}", cancellationToken))
            {
                await this.provider.RemoveAsync(
                    $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{user.Id}", cancellationToken);
            }

            await this.provider.SetAsync<int>(
                $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{user.Id}", command.OrganizationId,
                TimeSpan.FromDays(365), cancellationToken);

            return Unit.Value;
        }
    }
}
