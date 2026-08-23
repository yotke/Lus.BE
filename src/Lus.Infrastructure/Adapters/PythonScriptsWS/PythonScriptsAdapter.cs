using System.Diagnostics;
using System.Text;
using Lus.Application.Common.Ports;
using Microsoft.Extensions.Logging;

namespace Lus.Infrastructure.Adapters.PythonScriptsWS
{
    /// <summary>
    /// SECURITY: Exception type for Python script errors.
    /// Message is safe to show to users (no internal details).
    /// </summary>
    public class PythonScriptException : Exception
    {
        public PythonScriptException(string userSafeMessage) : base(userSafeMessage) { }
        public PythonScriptException(string userSafeMessage, Exception innerException)
            : base(userSafeMessage, innerException) { }
    }

    public class PythonScriptsAdapter : IPythonScriptsAdapter
    {
        private readonly ILogger<PythonScriptsAdapter> logger;
        private readonly string scriptsPath;
        private readonly string pythonExePath;
        private readonly string? apiKey;

        static PythonScriptsAdapter()
        {
            // WINDOWS ENCODING FIX (process-wide). Windows console stdio defaults to
            // cp1252, so Hebrew json.dumps(..., ensure_ascii=False) raises
            // UnicodeEncodeError. PYTHONUTF8=1 forces UTF-8 Mode. No-op on Linux.
            Environment.SetEnvironmentVariable("PYTHONUTF8", "1");
            Environment.SetEnvironmentVariable("PYTHONIOENCODING", "utf-8");
        }

        public PythonScriptsAdapter(
            string scriptsPath,
            string pythonExePath,
            string? apiKey,
            ILogger<PythonScriptsAdapter> logger)
        {
            this.scriptsPath = scriptsPath;
            this.pythonExePath = pythonExePath;
            this.apiKey = apiKey;
            this.logger = logger;
        }

        public async Task<string> RunAgentAsync(
            string agentName,
            string draftJson,
            string inputJson,
            string langCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentName)) throw new ArgumentNullException(nameof(agentName));
            if (string.IsNullOrWhiteSpace(scriptsPath)) throw new InvalidOperationException("Missing scriptsPath");
            if (string.IsNullOrWhiteSpace(pythonExePath)) throw new InvalidOperationException("Missing pythonExePath");

            var scriptPath = Path.Combine(scriptsPath, "agents", "runner.py");
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Agent runner script not found", scriptPath);

            var psi = new ProcessStartInfo
            {
                FileName = pythonExePath,
                WorkingDirectory = scriptsPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            psi.ArgumentList.Add(scriptPath);
            psi.ArgumentList.Add("--agent");
            psi.ArgumentList.Add(agentName);
            psi.ArgumentList.Add("--lang");
            psi.ArgumentList.Add(langCode);
            psi.ArgumentList.Add("--non-interactive");
            psi.ArgumentList.Add("--payload-stdin");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                psi.ArgumentList.Add("--api-key");
                psi.ArgumentList.Add(apiKey);
            }

            cancellationToken.ThrowIfCancellationRequested();

            using var process = Process.Start(psi)
                                ?? throw new InvalidOperationException("Failed to start python");

            await using var killRegistration = cancellationToken.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
            });

            var payload = $"{{\"Draft\":{(string.IsNullOrWhiteSpace(draftJson) ? "null" : draftJson)}," +
                          $"\"Input\":{(string.IsNullOrWhiteSpace(inputJson) ? "null" : inputJson)}}}";

            await process.StandardInput.WriteAsync(payload.AsMemory(), cancellationToken);
            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                this.logger.LogError(
                    "[RunAgentAsync] agent={Agent} exited {ExitCode}\nSTDERR:\n{Stderr}\nSTDOUT:\n{Stdout}",
                    agentName, process.ExitCode, stderr, stdout);
                throw new PythonScriptException($"Agent '{agentName}' failed to run.");
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                this.logger.LogError(
                    "[RunAgentAsync] agent={Agent} produced no output\nSTDERR:\n{Stderr}",
                    agentName, stderr);
                throw new PythonScriptException($"Agent '{agentName}' returned no result.");
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                var trimmed = stderr.Length > 8000 ? stderr[..8000] + "…(truncated)" : stderr;
                this.logger.LogInformation("[RunAgentAsync] agent={Agent} stderr:\n{Stderr}", agentName, trimmed);
            }

            return stdout.Trim();
        }
    }
}
