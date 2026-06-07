using AutoMapper;
using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUserData
{
    public class GetUserDataQueryHandler : IRequestHandler<GetUserDataQuery, UserDataDto>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;

        public GetUserDataQueryHandler(IUsersRepository usersRepository, IMapper mapper)
        {
            this.usersRepository = usersRepository;
            this.mapper = mapper;
        }

        public async Task<UserDataDto> Handle(GetUserDataQuery request, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetSingleEntityAsync(request.UserId, cancellationToken: cancellationToken);

            var userDto = this.mapper.Map<UserDataDto>(user);

            return userDto;
        }
    }
}
