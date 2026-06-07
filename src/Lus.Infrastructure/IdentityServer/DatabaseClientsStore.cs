using AutoMapper;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;

namespace Lus.Infrastructure.IdentityServer
{
    public class DatabaseClientsStore : IClientStore
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;

        public DatabaseClientsStore(IUsersRepository usersRepository, IMapper mapper)
        {
            this.usersRepository = usersRepository;
            this.mapper = mapper;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var user = await this.usersRepository.FindByUserNameAsync(clientId);
            var client = PrepareClient(user);

            return client;
        }

        private Client PrepareClient(User user)
        {
            var client = this.mapper.Map<Client>(user);
            client?.Claims.Add(new ClientClaim("sub", user.Id.ToString()));

            return client;
        }
    }
}
