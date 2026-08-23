namespace Lus.Application.Common.Ports
{
    /// <summary>
    /// Generic agent runner over PythonScripts/agents/runner.py.
    /// Payload goes over STDIN (one JSON doc {"Draft":…, "Input":…}).
    /// Returns the raw single-line stdout envelope. Agents exit 0 even on handled
    /// failures; a non-zero exit means the process crashed → PythonScriptException.
    /// </summary>
    public interface IPythonScriptsAdapter
    {
        Task<string> RunAgentAsync(
            string agentName,
            string draftJson,
            string inputJson,
            string langCode,
            CancellationToken cancellationToken = default);
    }
}
