using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Lus.Authorization.Authentication;

namespace Lus.Infrastructure.SignalRHubs
{
    [Authorize(AuthenticationSchemes = CookieAuthSchemes.Api)]
    public class CitiesStreetsHub : Hub
    {
        private readonly IMediator mediator;

        public CitiesStreetsHub(IMediator mediator)
        {
            this.mediator = mediator;
        }
        
        public async Task GetCities()
        {
            //var citiesDto = await this.mediator.Send(new GetCitiesQuery());

            //await Clients.Caller.SendAsync("onCitiesStreets", citiesDto);
        }
    }
}
