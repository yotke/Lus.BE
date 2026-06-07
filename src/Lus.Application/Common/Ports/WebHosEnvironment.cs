using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Lus.Application.Common.Ports
{
    public sealed class WebHosEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = default!;
        public IFileProvider ContentRootFileProvider { get; set; } = default!;
        public string ContentRootPath { get; set; } = default!;
        public string EnvironmentName { get; set; } = default!;
        public string WebRootPath { get; set; } = default!;
        public IFileProvider WebRootFileProvider { get; set; } = default!;
    }
}
