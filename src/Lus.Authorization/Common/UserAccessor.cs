using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Lus.Authorization.Models;
using System.Net;
using System.Security.Claims;
using Lus.Authorization.Extensions;

namespace Lus.Authorization.Common
{
    public class UserAccessor : IUserAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProjectUser projectUser;
        private readonly List<string> allowedClaims = new List<string> { ClaimTypes.Email };

        private readonly AsyncLocal<UserScope> userScope = new AsyncLocal<UserScope>();

        public UserAccessor(IHttpContextAccessor httpContextAccessor,
            IProjectUser projectUser,
            IOptions<AllowedClaimTypeOptions> allowedClaimsOptions = null)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.projectUser = projectUser;
            TryExtendDefaultClaims(allowedClaimsOptions?.Value?.GetClaims());
        }

        public IProjectUser ProjectUser
        {
            get
            {
                if (this.userScope.Value != null)
                {
                    return this.userScope.Value.User;
                }

                this.userScope.Value = new UserScope(this, GetCurrentUser(), null);
                return this.userScope.Value.User;
            }
        }

        public IDisposable CreateUserScope(IProjectUser user)
        {
            return this.userScope.Value = (user?.Id ?? 0) == 0
                ? new UserScope(this, this.projectUser, this.userScope.Value)
                : new UserScope(this, user, this.userScope.Value);
        }

        protected virtual IProjectUser GetCurrentUser()
        {
            if ((this.httpContextAccessor.HttpContext?.User?.Identity?.GetUserId<int>() ?? 0) == 0)
            {
                return this.projectUser;
            }

            var user = new ProjectUser
            {
                Roles = this.httpContextAccessor.HttpContext.User.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "userrole")
                    .Select(c => c.Value)
                    .ToList()
            };

            if (this.httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Forwarded-Identity-UserId", out var userIdHeaderValue)
                && int.TryParse(userIdHeaderValue, out var userId))
            {
                user.Id = userId;
            }
            else
            {
                user.Id = this.httpContextAccessor.HttpContext.User.Identity.GetUserId<int>();
            }

            if (this.httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Forwarded-Identity-UserName", out var userNameHeaderValue))
            {
                user.Name = WebUtility.UrlDecode(userNameHeaderValue);
            }
            else
            {
                user.Name = this.httpContextAccessor.HttpContext.User.GetUserName<string>();
            }

            user.Claims = this.httpContextAccessor.HttpContext.User.Claims
                .Where(c => this.allowedClaims.Contains(c.Type))
                .Select(c => (c.Type, c.Value))
                .ToList();

            return user;
        }

        private void TryExtendDefaultClaims(IReadOnlyCollection<string> claimsToExtend)
        {
            if (claimsToExtend is { Count: > 0 })
            {
                this.allowedClaims.AddRange(claimsToExtend);
            }
        }

        private class UserScope : IDisposable
        {
            private readonly UserAccessor userAccessor;
            private readonly UserScope parentUserScope;

            private bool isDisposed;

            public UserScope(UserAccessor userAccessor, IProjectUser projectUser, UserScope parent)
            {
                this.userAccessor = userAccessor;
                this.parentUserScope = parent;

                User = projectUser;
            }

            public IProjectUser User { get; }

            public void Dispose()
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.userAccessor.userScope.Value = this.parentUserScope;
                this.isDisposed = true;
            }
        }
    }
}
