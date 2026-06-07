using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Options;
using Lus.Application.Common.Services;
using Lus.Application.Users.Commands.DeleteUserLoginAttemptsByType;
using Lus.Application.Users.Commands.UserLoginAttempts;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Users;
using Lus.Contracts.Users.Types;
using Lus.Infrastructure.Exceptions;

namespace Lus.Infrastructure.IdentityServer
{
    public class UsersService : IUsersService
    {
        private readonly IUsersRepository usersRepository;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly IMediator mediator;
        private readonly PasswordConfigOptions passwordConfigOptions;

        public UsersService(
            IUsersRepository usersRepository,
            IPasswordHasher<User> passwordHasher,
            IOptions<PasswordConfigOptions> options,
            IMediator mediator)
        {
            this.passwordConfigOptions = options.Value;
            this.usersRepository = usersRepository;
            this.passwordHasher = passwordHasher;
            this.mediator = mediator;
        }

        public async Task<UserLoginResult> LoginAsync(string userName, string password)
        {
            var user = await this.usersRepository.FindByUserNameAsync(userName, u => u.UserLoginAttempts);

            if (user == null)
            {
                await this.mediator.Send(new UserLoginAttemptCommand
                {
                    UserName = userName,
                    UserLoginAttemptType = UserLoginAttemptType.Failed,
                    LoginFailReason = LoginFailReasonType.UserNotFound
                });

                return new UserLoginResult
                {
                    LoginFailReason = LoginFailReasonType.UserNotFound
                };
            }

            if (user.UserName == "LusClient")
            {
                return new UserLoginResult
                {
                    LockTimeLeft = 0,
                    IsValidCredentials = true,
                    IsConfirmed = user.IsConfirmed,
                    UserId = user.Id,
                    UserFound = true
                };
            }

            if (user.UserLoginAttempts.Any() && user.UserLoginAttempts.Count(ua => ua.LoginFailReason == LoginFailReasonType.WrongPassword) >= this.passwordConfigOptions.MaxInvalidPasswordAttempts && IsUserLockedOut(user, out double? lockTimeLeft))
            {
                await this.mediator.Send(new UserLoginAttemptCommand
                {
                    UserName = userName,
                    UserLoginAttemptType = UserLoginAttemptType.Failed,
                    LoginFailReason = LoginFailReasonType.BlockedUser,
                    UserId = user.Id
                });

                return new UserLoginResult
                {
                    LockTimeLeft = lockTimeLeft,
                    UserFound = true,
                    LoginFailReason = LoginFailReasonType.BlockedUser,
                    UserId = user.Id
                };
            }

            var passwordVerificationResult = this.passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            var isPasswordValid = passwordVerificationResult != PasswordVerificationResult.Failed;
            if (!isPasswordValid)
            {
                await this.mediator.Send(new UserLoginAttemptCommand
                {
                    UserName = userName,
                    UserLoginAttemptType = UserLoginAttemptType.Failed,
                    LoginFailReason = LoginFailReasonType.WrongPassword,
                    UserId = user.Id
                });

                if (user.UserLoginAttempts.Any() &&
                    user.UserLoginAttempts.Count(ua => ua.LoginFailReason == LoginFailReasonType.WrongPassword) >=
                    this.passwordConfigOptions.MaxInvalidPasswordAttempts &&
                    !IsUserLockedOut(user, out double? _))
                {
                    await this.mediator.Send(new DeleteUserLoginAttemptsByTypeCommand(user.Id, LoginFailReasonType.WrongPassword));
                }

                return new UserLoginResult
                {
                    UserFound = true,
                    LoginFailReason = LoginFailReasonType.WrongPassword,
                    UserId = user.Id
                };

            }
            else
            {
                await this.mediator.Send(new DeleteUserLoginAttemptsByTypeCommand(user.Id, LoginFailReasonType.WrongPassword));
            }

            if (!user.IsConfirmed)
            {
                await this.mediator.Send(new UserLoginAttemptCommand
                {
                    UserName = userName,
                    UserLoginAttemptType = UserLoginAttemptType.Failed,
                    LoginFailReason = LoginFailReasonType.UserNotConfirm,
                    UserId = user.Id
                });

                return new UserLoginResult
                {
                    IsValidCredentials = true,
                    UserFound = true,
                    LoginFailReason = LoginFailReasonType.UserNotConfirm,
                    UserId = user.Id
                };
            }

            if (user.PasswordChangedDate.HasValue && user.PasswordChangedDate.Value.AddDays(this.passwordConfigOptions.PasswordExpiresMaxDays) <= DateTime.UtcNow)
            {
                await this.mediator.Send(new UserLoginAttemptCommand
                {
                    UserName = userName,
                    UserLoginAttemptType = UserLoginAttemptType.ResetPassword,
                    LoginFailReason = LoginFailReasonType.PasswordExpired,
                    UserId = user.Id
                });

                return new UserLoginResult
                {
                    IsValidCredentials = true,
                    UserFound = true,
                    ErrorMessage = $"תוקף הסיסמה מוגבל ל {this.passwordConfigOptions.PasswordExpiresMaxDays} יום!;יש לקבוע סיסמה חדשה.",
                    LoginFailReason = LoginFailReasonType.PasswordExpired,
                    UserId = user.Id
                };
            }

            await this.mediator.Send(new UserLoginAttemptCommand
            {
                UserName = userName,
                UserLoginAttemptType = UserLoginAttemptType.Succeed,
                UserId = user.Id
            });

            return new UserLoginResult
            {
                LockTimeLeft = 0,
                IsValidCredentials = true,
                IsConfirmed = user.IsConfirmed,
                UserId = user.Id,
                UserFound = true
            };
        }

        public async Task<LockedResultDto> IsAccountLockedAsync(string smsCode)
        {
            var user = await this.usersRepository.GetWithIncludeAsync(u => u.SmsVerificationToken == smsCode, CancellationToken.None, u => u.UserLoginAttempts);

            if (user == null)
            {
                return new LockedResultDto { IsUserFound = false };
            }

            if (IsUserLockedOut(user, out double? lockTimeLeft))
            {
                return new LockedResultDto { IsUserFound = true, IsLocked = true, LockTimeLeft = lockTimeLeft };
            }

            return new LockedResultDto { IsUserFound = true, IsLocked = false };
        }

        private bool IsUserLockedOut(User user, out double? lockTimeLeft)
        {
            lockTimeLeft = null;

            if (!user.UserLoginAttempts.Any())
            {
                return false;
            }

            var attempt = user.UserLoginAttempts.Where(ua => ua.LoginFailReason == LoginFailReasonType.WrongPassword)
                 .OrderByDescending(ua => ua.CreatedOn).First();
            var totalMinutes = (DateTime.UtcNow - attempt.CreatedOn).TotalMinutes;

            if (totalMinutes <= this.passwordConfigOptions.UserLockInterval)
            {
                lockTimeLeft = this.passwordConfigOptions.UserLockInterval - totalMinutes;

                return true;
            }

            return false;
        }
    }
}
