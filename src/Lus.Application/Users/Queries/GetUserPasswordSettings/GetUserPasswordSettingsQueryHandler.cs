using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Options;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUserPasswordSettings
{
    public class GetUserPasswordSettingsQueryHandler : IRequestHandler<GetUserPasswordSettingsQuery, UserPasswordSettingsDto>
    {
        private readonly PasswordConfigOptions passwordConfigOptions;
        private readonly IMapper mapper;

        public GetUserPasswordSettingsQueryHandler(IOptions<PasswordConfigOptions> options, IMapper mapper)
        {
            this.passwordConfigOptions = options.Value;
            this.mapper = mapper;
        }

        public async Task<UserPasswordSettingsDto> Handle(GetUserPasswordSettingsQuery request, CancellationToken cancellationToken)
        {
            return new UserPasswordSettingsDto
            {
                SmallLetter = passwordConfigOptions.MinRequiredSmallLetter,
                CapitalLetter = passwordConfigOptions.MinRequiredCapitalLetter,
                NonAlphanumericCharacters = passwordConfigOptions.MinRequiredNonAlphanumericCharacters,
                NumericCharacters = passwordConfigOptions.MinRequiredNumericCharacters,
                PasswordLength = passwordConfigOptions.MinRequiredPasswordLength
            };
        }
    }
}
