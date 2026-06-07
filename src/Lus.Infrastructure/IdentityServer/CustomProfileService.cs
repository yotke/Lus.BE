using IdentityModel;
using MediatR;
using System.Security.Claims;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Lus.Application.Users.Queries.GetUserData;
using Lus.Contracts.Users;

namespace Lus.Infrastructure.IdentityServer
{
    public class CustomProfileService : IProfileService
    {
        private readonly IMediator mediator;

        public CustomProfileService(IMediator mediator)
            => this.mediator = mediator;

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var userId = ToIntKey(context.Subject.GetSubjectId());
            var user = await this.mediator.Send(new GetUserDataQuery(userId));

            var claims = CreateUserClaims(user);
            claims.AddRange(context.Subject.Claims);

            context.IssuedClaims = claims;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;
            return Task.CompletedTask;
        }

        private int ToIntKey(string userIdString)
        {
            if (!int.TryParse(userIdString, out var userId))
            {
                throw new InvalidOperationException($"Not supported cast from '{userIdString}' string to int");
            }

            return userId;
        }

        private static List<Claim> CreateUserClaims(UserDataDto user)
        {
            var claims = new List<Claim>();

            foreach (var claim in user.Claims)
            {
                claims.Add(new(claim.Key, claim.Value));
            }

            claims.Add(new(JwtClaimTypes.Subject, user.Id.ToString()));
            claims.Add(new(JwtClaimTypes.Email, user.UserName));
            claims.Add(new(JwtClaimTypes.Name, user.UserName));

            return claims;
        }
    }
}
