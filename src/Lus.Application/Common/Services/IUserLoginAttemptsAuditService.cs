using Lus.Application.Users.Models;
using Lus.Contracts.Users.Types;

namespace Lus.Application.Common.Services
{
    public interface IUserLoginAttemptsAuditService
    {
        Task<LoginAudit> GetUserLoginFailureEventAsync(
            string userName,
            LoginFailReasonType failReason,
            int? userId);

        public Task<LoginAudit> GetUserLoginAfterFailureEventAsync(
            string userName,
            int userId);
    }
}
