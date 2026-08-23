namespace Lus.Application.Common.Options
{
    /// <summary>AI Document Builder configuration ("AiBuilder" section).</summary>
    public class AiBuilderOptions
    {
        public const string SectionName = "AiBuilder";

        /// <summary>Per python-agent call budget (linked CTS inside the turn budget).</summary>
        public int AgentTimeoutSeconds { get; set; } = 60;

        /// <summary>Whole-turn budget for a builder turn.</summary>
        public int TurnTimeoutSeconds { get; set; } = 300;

        /// <summary>Commit/render wait budget.</summary>
        public int CommitTimeoutSeconds { get; set; } = 600;

        /// <summary>LLM backend for conversational agents — forwarded as AIB_LLM_PROVIDER.</summary>
        public string LlmProvider { get; set; } = "openai";
    }
}
