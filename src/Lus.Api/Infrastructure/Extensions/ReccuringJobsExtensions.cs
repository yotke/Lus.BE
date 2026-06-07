using MediatR;

namespace Lus.Infrastructure.Extensions
{
    public static class ReccuringJobsExtensions
    {
        public static void CreateJobs(this IApplicationBuilder app, IConfiguration configuration)
        {
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                //mediator.Send(new SyncSystemTablesCommand()).Wait();
            }
        }
    }
}
