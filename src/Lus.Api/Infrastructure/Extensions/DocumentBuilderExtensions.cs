using Lus.Api.Infrastructure.SignalRHubs.Services;
using Lus.Application.Common.Builders;
using Lus.Application.Documents.Builder;
using Lus.Application.Documents.Builder.Agents;
using Lus.Application.Documents.Builder.Orchestration;
using Lus.Application.Documents.Builder.Services;

namespace Lus.Infrastructure.Extensions
{
    public static class DocumentBuilderExtensions
    {
        public static IServiceCollection AddDocumentBuilder(this IServiceCollection services)
        {
            services.AddSingleton<IBuilderAgentCatalog, DocumentBuilderAgentCatalog>();
            services.AddScoped<DocumentBuilderAgentClient>();
            services.AddScoped<DocumentBuildSessionStore>();
            services.AddScoped<DocumentBuilderOrchestrator>();
            services.AddScoped<IDocumentBuilderEventSender, DocumentBuilderEventSender>();
            return services;
        }
    }
}
