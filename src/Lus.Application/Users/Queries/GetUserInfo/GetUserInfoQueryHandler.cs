using AutoMapper;
using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUserInfo
{
    public class GetUserInfoQueryHandler : IRequestHandler<GetUserInfoQuery, UserInfoDto>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;
        private readonly IUserAccessor userAccessor;

        public GetUserInfoQueryHandler(IUserAccessor userAccessor, IUsersRepository usersRepository, IMapper mapper)
        {
            this.userAccessor = userAccessor;
            this.usersRepository = usersRepository;
            this.mapper = mapper;
        }

        public async Task<UserInfoDto> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetSingleEntityAsync(this.userAccessor.ProjectUser.Id, cancellationToken);

            var userDto = this.mapper.Map<UserInfoDto>(user);

            return userDto;
        }
    }
}
