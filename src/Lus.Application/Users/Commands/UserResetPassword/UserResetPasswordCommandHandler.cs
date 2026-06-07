using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Options;
using Lus.Application.Common.Ports;
using Lus.Application.Notifications.Commands.SendEmail;
using Lus.Application.Notifications.Commands.SendSms;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;
using Lus.Contracts.Users.Types;
using Lus.Infrastructure.Exceptions;
using Lus.NotificationCenter.SmsServices;

namespace Lus.Application.Users.Commands.UserResetPassword
{
    public class UserResetPasswordCommandHandler : IRequestHandler<UserResetPasswordCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IRecaptchaAdapter recaptchaAdapter;
        private readonly PasswordConfigOptions passwordConfigOptions;
        private readonly IMediator mediator;
        private readonly IMapper mapper;
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly ISmsService smsService;

        public UserResetPasswordCommandHandler(ISmsService smsService, IDataProtectionProvider dataProtectionProvider, IUsersRepository usersRepository, IOptions<PasswordConfigOptions> options, IMediator mediator, IMapper mapper, IRecaptchaAdapter recaptchaAdapter)
        {
            this.dataProtectionProvider = dataProtectionProvider;
            this.usersRepository = usersRepository;
            this.smsService = smsService;
            this.mediator = mediator;
            this.mapper = mapper;
            this.recaptchaAdapter = recaptchaAdapter;
            this.passwordConfigOptions = options.Value;
        }

        public async Task<Unit> Handle(UserResetPasswordCommand command, CancellationToken cancellationToken)
        {
            var user = await ValidateUser(command, cancellationToken);

            if (command.ResetPasswordType == ResetPasswordType.Email)
            {
                user.PasswordVerificationToken = this.dataProtectionProvider.CreateProtector("Lus.co.il").ToTimeLimitedDataProtector().Protect(user.PasswordHash, TimeSpan.FromMinutes(30));
                user.VerificationTokenExpiration = DateTime.UtcNow.AddMinutes(30);

                await this.usersRepository.UpdateUserAsync(user);

                var userDto = this.mapper.Map<UserDto>(user);

                await this.mediator.Send(new SendEmailCommand(userDto, MailType.PasswordReset));
            }

            if (command.ResetPasswordType == ResetPasswordType.Sms)
            {
                if (string.IsNullOrWhiteSpace(user.Phone))
                {
                    throw new MembershipException(30);
                }

                user.SmsVerificationToken = this.smsService.GenerateStringCode();
                user.SmsTokenExpiration = DateTime.UtcNow.AddMinutes(30);

                await this.usersRepository.UpdateUserAsync(user);

                var userDto = this.mapper.Map<UserDto>(user);

                await this.mediator.Send(new SendSmsCommand(userDto, SmsType.ResetPassword));
            }

            return Unit.Value;
        }

        private async Task<User> ValidateUser(UserResetPasswordCommand command, CancellationToken cancellationToken)
        {
            var result = await this.recaptchaAdapter.CheckRecaptcha(cancellationToken);
            if (!result)
            {
                throw new MembershipException("Recaptcha not valid", 41);
            }

            var user = await this.usersRepository.GetWithIncludeAsync(u => u.Email == command.Email, cancellationToken, u => u.UserLoginAttempts);
            if (user == null)
            {
                throw new MembershipException(18);
            }

            if (user.UserLoginAttempts.Any() && user.UserLoginAttempts.Count(ua => ua.LoginFailReason == LoginFailReasonType.WrongPassword) >= this.passwordConfigOptions.MaxInvalidPasswordAttempts && IsUserLockedOut(user, out double? lockTimeLeft))
            {
                throw new MembershipException(101, lockTimeLeft);
            }

            return user;
        }

        private bool IsUserLockedOut(User user, out double? lockTimeLeft)
        {
            lockTimeLeft = null;

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