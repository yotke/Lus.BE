using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Microsoft.AspNetCore.Http;
using Lus.Application.Notifications.Commands.SendEmail;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Notifications.Types;

namespace Lus.Application.Users.Commands.AddUserLoginInfo
{
    public class AddUserLoginInfoCommandHandler : IRequestHandler<AddUserLoginInfoCommand, Unit>
    {
        private readonly IUserLoginInfoRepository userLoginInfoRepository;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMediator mediator;

        public AddUserLoginInfoCommandHandler(IMediator mediator, IHttpContextAccessor httpContextAccessor, IUserLoginInfoRepository userLoginInfoRepository)
        {
            this.mediator = mediator;
            this.httpContextAccessor = httpContextAccessor;
            this.userLoginInfoRepository = userLoginInfoRepository;
        }

        public async Task<Unit> Handle(AddUserLoginInfoCommand request, CancellationToken cancellationToken)
        {
            var userAgent = this.httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            var ipAddress = GetIPAddresses();

            var userLoginInfos = await this.userLoginInfoRepository.GetAllListAsync(uli => uli.UserId == request.User.Id, cancellationToken);

            if (!userLoginInfos.Any(uli => uli.UserAgent.Equals(userAgent, StringComparison.InvariantCultureIgnoreCase) && uli.IpAddress.Equals(ipAddress, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (userLoginInfos.Any())
                {
                    await this.mediator.Send(new SendEmailCommand(request.User, MailType.UserLoginInfoNotification), cancellationToken);
                }

                await this.userLoginInfoRepository.AddAsync(new UserLoginInfo
                {
                    UserId = request.User.Id,
                    UserAgent = userAgent,
                    IpAddress = ipAddress
                }, cancellationToken);
            }

            return Unit.Value;
        }

        private string GetIPAddresses()
        {
            var httpContext = this.httpContextAccessor.HttpContext;

            var clientIp = httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(clientIp))
            {
                clientIp = clientIp.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();
            }

            if (string.IsNullOrEmpty(clientIp))
            {
                clientIp = httpContext?.Connection?.RemoteIpAddress?.ToString();
            }

            if (string.IsNullOrEmpty(clientIp))
            {
                clientIp = "::1";
            }
            return clientIp;
        }
    }
}
