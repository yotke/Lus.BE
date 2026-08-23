using Lus.Application.Common.Options;
using Lus.Application.Common.Ports;
using Lus.Application.Common.Services;
using Lus.Infrastructure.Adapters.PythonScriptsWS;

namespace Lus.Infrastructure.Extensions
{
    public static class PythonAdapterExtensions
    {
        public static IServiceCollection AddPythonScriptsAdapter(this IServiceCollection services, IConfiguration configuration)
        {
            var pythonProviderPath = configuration.GetValue<string>("PythonSetting:PythonProviderPath");
            var pythonScriptFolder = ResolveScriptsFolder(
                configuration.GetValue<string>("PythonSetting:PythonScriptFolder"));
            var apiKey = configuration.GetValue<string>("OpenAI:ApiKey");
            var openAiModel = configuration.GetValue<string>("OpenAI:Model");
            var llmProvider = configuration.GetValue<string>("AiBuilder:LlmProvider");

            if (string.IsNullOrWhiteSpace(pythonProviderPath))
                throw new ArgumentNullException("PythonSetting:PythonProviderPath", "The path to the local Python executable is not configured.");

            if (string.IsNullOrWhiteSpace(pythonScriptFolder))
                throw new ArgumentNullException("PythonSetting:PythonScriptFolder", "The path to the folder containing Python scripts is not configured.");

            if (!string.IsNullOrWhiteSpace(openAiModel))
                Environment.SetEnvironmentVariable("AIB_OPENAI_MODEL", openAiModel);

            ExportIfSet(configuration, "OpenAI:ModelLite", "AIB_MODEL_LITE");
            ExportIfSet(configuration, "OpenAI:ModelChat", "AIB_MODEL_CHAT");
            ExportIfSet(configuration, "OpenAI:ModelContent", "AIB_MODEL_CONTENT");
            ExportIfSet(configuration, "OpenAI:ModelDeep", "AIB_MODEL_DEEP");
            ExportIfSet(configuration, "OpenAI:ScoreContent", "AIB_SCORE_CONTENT");
            ExportIfSet(configuration, "OpenAI:ScoreDeep", "AIB_SCORE_DEEP");

            if (!string.IsNullOrWhiteSpace(llmProvider))
                Environment.SetEnvironmentVariable("AIB_LLM_PROVIDER", llmProvider);

            services.AddSingleton<IPythonScriptsAdapter>(sp =>
                new PythonScriptsAdapter(
                    pythonScriptFolder,
                    pythonProviderPath,
                    apiKey,
                    sp.GetRequiredService<ILogger<PythonScriptsAdapter>>()));

            services.AddSingleton<ISelfHealingStore, SelfHealingStore>();

            return services;
        }

        /// <summary>
        /// Docker sets an absolute folder. Local `dotnet run` walks up from
        /// BaseDirectory until PythonScripts/agents/runner.py is found.
        /// </summary>
        public static string ResolveScriptsFolder(string? configured)
        {
            if (!string.IsNullOrWhiteSpace(configured)
                && File.Exists(Path.Combine(configured, "agents", "runner.py")))
                return configured!;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PythonScripts");
                if (File.Exists(Path.Combine(candidate, "agents", "runner.py")))
                    return candidate;
                dir = dir.Parent;
            }

            return configured ?? "";
        }

        private static void ExportIfSet(IConfiguration configuration, string key, string envVar)
        {
            var value = configuration.GetValue<string>(key);
            if (!string.IsNullOrWhiteSpace(value))
                Environment.SetEnvironmentVariable(envVar, value);
        }
    }
}
