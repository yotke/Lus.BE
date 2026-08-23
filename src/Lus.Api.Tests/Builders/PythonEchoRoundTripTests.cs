using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Lus.Infrastructure.Adapters.PythonScriptsWS;
using Lus.Infrastructure.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lus.Api.Tests.Builders
{
    public class PythonEchoRoundTripTests
    {
        [Fact]
        public async Task RunAgentAsync_echoes_hebrew_without_bom()
        {
            var python = FindPython();
            var scripts = PythonAdapterExtensions.ResolveScriptsFolder(null);
            if (python is null || string.IsNullOrWhiteSpace(scripts)
                || !File.Exists(Path.Combine(scripts, "agents", "runner.py")))
            {
                return; // no local python / scripts — CI without the runtime
            }

            var adapter = new PythonScriptsAdapter(scripts, python, apiKey: null, NullLogger<PythonScriptsAdapter>.Instance);
            var raw = await adapter.RunAgentAsync(
                "doc.echo",
                "{}",
                JsonSerializer.Serialize(new { Text = "שלום עולם" }),
                "he");

            using var doc = JsonDocument.Parse(raw);
            Assert.True(doc.RootElement.GetProperty("Ok").GetBoolean());
            Assert.Equal("שלום עולם", doc.RootElement.GetProperty("Result").GetProperty("Echo").GetString());
        }

        private static string? FindPython()
        {
            foreach (var name in new[] { "python3", "python" })
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = name,
                        ArgumentList = { "--version" },
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var process = Process.Start(psi);
                    if (process is null) continue;
                    if (!process.WaitForExit(3000)) continue;
                    if (process.ExitCode == 0) return name;
                }
                catch (Exception)
                {
                    // not on PATH
                }
            }

            return null;
        }
    }
}
