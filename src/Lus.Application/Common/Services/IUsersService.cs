using Lus.Contracts.Users;

namespace Lus.Application.Common.Services
{
    public interface IUsersService
    {
        Task<UserLoginResult> LoginAsync(string userEmail, string password);

        Task<LockedResultDto> IsAccountLockedAsync(string smsCode);
    }
}
