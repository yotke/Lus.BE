using Lus.Infrastructure.Extensions;
using Lus.Infrastructure.SignalRHubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lus.Api.Tests.SignalR
{
    public class DocumentBuilderHubRouteTests
    {
        [Fact]
        public void Path_is_the_canonical_hub_route()
        {
            Assert.Equal("/hub/document-builder", DocumentBuilderHub.Path);
        }

        [Fact]
        public async Task MapSignalRHubs_registers_document_builder_hub()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddSignalR();
            await using var app = builder.Build();
            app.MapSignalRHubs();
            await app.StartAsync();

            IEndpointRouteBuilder routes = app;
            var endpoints = routes.DataSources.SelectMany(d => d.Endpoints).ToList();
            Assert.Contains(
                endpoints,
                e =>
                {
                    var pattern = (e as RouteEndpoint)?.RoutePattern.RawText ?? "";
                    var name = e.DisplayName ?? "";
                    return pattern.Contains("document-builder")
                           || name.Contains("DocumentBuilderHub")
                           || name.Contains("document-builder");
                });
        }
    }
}
