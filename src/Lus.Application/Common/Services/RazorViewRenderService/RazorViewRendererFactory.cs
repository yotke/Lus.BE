using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Lus.Application.Common.Ports;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;

namespace Lus.Application.Common.Services.RazorViewRenderService
{
    public static class RazorViewRendererFactory
    {
        public static RazorViewRenderer New(string inputDirectory)
        {
            if (string.IsNullOrWhiteSpace(inputDirectory))
                throw new ArgumentNullException(nameof(inputDirectory));

            ServiceCollection services = new();

            WebHosEnvironment environment = new()
            {
                ApplicationName = "Lus",
                ContentRootPath = inputDirectory,
                ContentRootFileProvider = new PhysicalFileProvider(inputDirectory),
                WebRootPath = inputDirectory,
                WebRootFileProvider = new PhysicalFileProvider(inputDirectory)
            };
            services.AddSingleton<IWebHostEnvironment>(environment);

            services.Configure<MvcRazorRuntimeCompilationOptions>(mrrco =>
            {
                mrrco.FileProviders.Clear();
                mrrco.FileProviders.Add(new PhysicalFileProvider(inputDirectory));
            }
            );
            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.TryAddSingleton(new DiagnosticListener("Microsoft.AspNetCore"));
            services.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.TryAddSingleton<ConsolidatedAssemblyApplicationPartFactory>();
            services.AddLogging()
                .AddHttpContextAccessor()
                .AddMvcCore()
                .AddRazorPages()
                .AddRazorRuntimeCompilation();
            services.AddSingleton<RazorViewRenderer>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetRequiredService<RazorViewRenderer>();
        }
    };
}
