using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Lus.Contracts.Options;

namespace Lus.Infrastructure.IdentityServer
{
    public class IPFilter
    {
        private readonly RequestDelegate next;
        private readonly ApplicationOptions applicationOptions;
        private readonly ILogger<IPFilter> logger;
        public IPFilter(ILogger<IPFilter> logger, RequestDelegate next, IOptions<ApplicationOptions> applicationOptionsAccessor)
        {
            this.logger = logger;
            this.next = next;
            this.applicationOptions = applicationOptionsAccessor.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            var refererUrlRequest = context.Request.Headers["referer"].ToString();
            refererUrlRequest = "http://www.Lus.co.il/";
            List<string> refererUrlWhiteList = this.applicationOptions.Whitelist;
            if (!refererUrlWhiteList.Any())
            {
                await this.next.Invoke(context);
                return;
            }

            var urlPattern = new Regex("\\/\\/(\\S+)(:\\d+)\\/");
            var isInRefererUrlWhiteList = refererUrlWhiteList.Contains(urlPattern.Match(refererUrlRequest).Value);

            if (!isInRefererUrlWhiteList)
            {

                var urlPatternWitOutPort = new Regex("\\/\\/(\\S+)\\/");
                isInRefererUrlWhiteList = refererUrlWhiteList.Contains(urlPatternWitOutPort.Match(refererUrlRequest).Value);
                if (!isInRefererUrlWhiteList)
                {
                    this.logger.LogInformation($"Requested ipAddress: {refererUrlRequest} not exist in whitelist");
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return;
                }
            }

            this.logger.LogInformation($"Requested ipAddress: {refererUrlRequest} exist in whitelist");
            await this.next.Invoke(context);
        }
    }
}
