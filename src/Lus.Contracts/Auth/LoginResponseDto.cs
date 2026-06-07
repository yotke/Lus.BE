using Lus.Contracts.Organizations;
using Lus.Contracts.Roles;

namespace Lus.Contracts.Auth
{
    /// <summary>
    /// Result of a cookie login attempt. On failure, <see cref="ExceptionId"/> preserves the
    /// numeric error codes the existing frontend already handles (10 = user not found,
    /// 11 = not confirmed, 12 = wrong password, 13 = password expired, 41 = recaptcha,
    /// 101 = blocked, 20 = invalid token, 22 = login-by-token failed).
    /// </summary>
    public class LoginResponseDto
    {
        public bool IsSuccess { get; set; }

        public int? ExceptionId { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsLocked { get; set; }

        public double? LockTimeLeft { get; set; }

        public AuthenticatedUserDto User { get; set; }

        public static LoginResponseDto Failure(int exceptionId, string message = null) =>
            new() { IsSuccess = false, ExceptionId = exceptionId, ErrorMessage = message };

        public static LoginResponseDto Locked(int exceptionId, double? lockTimeLeft) =>
            new() { IsSuccess = false, ExceptionId = exceptionId, IsLocked = true, LockTimeLeft = lockTimeLeft };
    }

    public class AuthenticatedUserDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string IdNumber { get; set; }

        public ICollection<AuthRoleDto> Roles { get; set; }

        public ICollection<AuthOrganizationDto> Organizations { get; set; }
    }
}

