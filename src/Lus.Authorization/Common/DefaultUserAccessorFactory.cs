using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lus.Authorization.Common
{
    public class DefaultUserAccessorFactory : IUserAccessorFactory
    {
        public IUserAccessor CreateUserAccessor(IServiceProvider serviceProvider, IProjectUser projectUser) =>
            new UserAccessor(serviceProvider.GetRequiredService<IHttpContextAccessor>(), projectUser,
                serviceProvider.GetService<IOptions<AllowedClaimTypeOptions>>());
    }
}
