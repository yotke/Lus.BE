using AutoMapper;
using IdentityServer4.Models;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Lus.Application.Common.Exceptions;
using Lus.Application.Common.Ports;
using Lus.Application.Notifications.Commands.SendEmail;
using Lus.Application.Users.Commands.AddOrganizationToUser;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;
using Lus.Infrastructure.Exceptions;

namespace Lus.Application.Users.Commands.CreateUser
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly IRecaptchaAdapter recaptchaAdapter;
        private readonly IMediator mediator;
        private readonly IUserAccessor userAccessor;
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly IUserPasswordHistoriesRepository userPasswordHistoriesRepository;

        public CreateUserCommandHandler(IUserAccessor userAccessor, IUserPasswordHistoriesRepository userPasswordHistoriesRepository, IDataProtectionProvider dataProtectionProvider, IMediator mediator, IUsersRepository usersRepository, IMapper mapper, IPasswordHasher<User> passwordHasher, IRecaptchaAdapter recaptchaAdapter)
        {
            this.userAccessor = userAccessor;
            this.userPasswordHistoriesRepository = userPasswordHistoriesRepository;
            this.dataProtectionProvider = dataProtectionProvider;
            this.usersRepository = usersRepository;
            this.recaptchaAdapter = recaptchaAdapter;
            this.mapper = mapper;
            this.passwordHasher = passwordHasher;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(CreateUserCommand createCommand, CancellationToken cancellationToken)
        {
            await ThrowOnUserExistsAsync(createCommand);

            var user = CreateUser(createCommand);

            user.ConfirmationToken = this.dataProtectionProvider.CreateProtector("Lus.co.il").ToTimeLimitedDataProtector().Protect(user.PasswordHash, TimeSpan.FromMinutes(30));
            user.VerificationTokenExpiration = DateTime.UtcNow.AddMinutes(30);

            user = await this.usersRepository.AddAsync(user, cancellationToken);

            await this.userPasswordHistoriesRepository.AddAsync(new UserPasswordHistory
            {
                PasswordHash = user.PasswordHash,
                UserId = user.Id
            }, cancellationToken);

            if (this.userAccessor.ProjectUser?.OrganizationId.HasValue ?? false)
            {
                await this.mediator.Send(new AddOrganizationToUserCommand(new List<int>{this.userAccessor.ProjectUser.OrganizationId.Value}, user.Id), cancellationToken);
            }

            var userDto = this.mapper.Map<UserDto>(user);

            await this.mediator.Send(new SendEmailCommand(userDto, MailType.SiteRegistration), cancellationToken);

            return Unit.Value;
        }

        private User CreateUser(CreateUserCommand createCommand)
        {
            var user = this.mapper.Map<User>(createCommand);
            user.Email = user.Email.ToLower();
            user.UserName = user.Email.ToLower();
            user.PasswordHash = this.passwordHasher.HashPassword(user, createCommand.Password);
            user.PasswordChangedDate = DateTime.UtcNow;
            user.AllowedScopes = new List<string>
                {
                    ApplicationConstants.Scopes.PublicApiScope,
                    ApplicationConstants.Scopes.InternalApi
                };
            user.ClientSecrets = new List<string> { createCommand.Password.Sha256() };
            user.AllowedGrantTypes = new List<string> { ApplicationConstants.AllowedGrandTypes.Password };
            var claims = new List<KeyValuePair<string, string>>
            {
                new(ApplicationConstants.ClaimsTypes.Allowance, "secure_data_read"),
                new(ApplicationConstants.ClaimsTypes.Scope, ApplicationConstants.Scopes.InternalApi)
            };

            user.Claims = claims;

            return user;
        }

        private async Task ThrowOnUserExistsAsync(CreateUserCommand request)
        {
            var result = await this.recaptchaAdapter.CheckRecaptcha();
            if (!result)
            {
                throw new MembershipException("Recaptcha not valid", 41);
            }

            var user = await this.usersRepository.GetAsync(u => u.UserName == request.Email);
            if (user != null)
            {
                throw new EntityValidationException(nameof(CreateUserCommand.Email), $"User with name {request.Email} already exists", 17);
            }
        }
    }
}
