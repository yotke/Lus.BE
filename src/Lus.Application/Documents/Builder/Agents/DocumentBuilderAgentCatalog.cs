using Lus.Application.Common.Builders;

namespace Lus.Application.Documents.Builder.Agents
{
    /// <summary>
    /// Single source of truth for Document Builder agents. Names MUST match
    /// PythonScripts/agents/runner.py aliases (<c>doc.*</c>).
    /// </summary>
    public sealed class DocumentBuilderAgentCatalog : IBuilderAgentCatalog
    {
        private static readonly IReadOnlyList<BuilderAgentDescriptor> Descriptors = new List<BuilderAgentDescriptor>
        {
            new ImporterAgentDescriptor
            {
                Name = "doc.template_reader", ProducesPatches = true, Enabled = true,
                InputKind = BuilderAgentInputKind.File,
                DisplayNameKey = "docBuilder.agents.templateReader.name",
                DescriptionKey = "docBuilder.agents.templateReader.desc",
                Icon = "upload_file"
            },
            new ContentAgentDescriptor
            {
                Name = "doc.carry_forward", Wave = 0, ProducesPatches = true, Enabled = false,
                InputKind = BuilderAgentInputKind.None,
                DisplayNameKey = "docBuilder.agents.carryForward.name",
                DescriptionKey = "docBuilder.agents.carryForward.desc",
                Icon = "redo"
            },
            new ContentAgentDescriptor
            {
                Name = "doc.echo", Wave = 1, ProducesPatches = true, Enabled = false,
                InputKind = BuilderAgentInputKind.Text,
                DisplayNameKey = "docBuilder.agents.echo.name",
                DescriptionKey = "docBuilder.agents.echo.desc",
                Icon = "record_voice_over"
            },
            new ContentAgentDescriptor
            {
                Name = "doc.schema_planner", Wave = 1, ProducesPatches = false, Enabled = true,
                InputKind = BuilderAgentInputKind.Text,
                DisplayNameKey = "docBuilder.agents.schemaPlanner.name",
                DescriptionKey = "docBuilder.agents.schemaPlanner.desc",
                Icon = "schema"
            },
            new ContentAgentDescriptor
            {
                Name = "doc.row_extractor", Wave = 2, ProducesPatches = true, Enabled = true,
                InputKind = BuilderAgentInputKind.Text,
                DisplayNameKey = "docBuilder.agents.rowExtractor.name",
                DescriptionKey = "docBuilder.agents.rowExtractor.desc",
                Icon = "table_rows"
            },
            new ContentAgentDescriptor
            {
                Name = "doc.formatter", Wave = 3, ProducesPatches = true, Enabled = true,
                InputKind = BuilderAgentInputKind.None,
                DisplayNameKey = "docBuilder.agents.formatter.name",
                DescriptionKey = "docBuilder.agents.formatter.desc",
                Icon = "calculate"
            },
            new ContentAgentDescriptor
            {
                Name = "doc.reviewer", Wave = 4, ProducesPatches = false, Enabled = true,
                InputKind = BuilderAgentInputKind.Text,
                DisplayNameKey = "docBuilder.agents.reviewer.name",
                DescriptionKey = "docBuilder.agents.reviewer.desc",
                Icon = "rate_review"
            },
            new ValidatorAgentDescriptor
            {
                Name = "doc.validator", ProducesPatches = true, Enabled = true,
                InputKind = BuilderAgentInputKind.None,
                DisplayNameKey = "docBuilder.agents.validator.name",
                DescriptionKey = "docBuilder.agents.validator.desc",
                Icon = "verified"
            },
            new PlannerAgentDescriptor
            {
                Name = "doc.question_planner", ProducesPatches = false, Enabled = true,
                InputKind = BuilderAgentInputKind.None,
                DisplayNameKey = "docBuilder.agents.questionPlanner.name",
                DescriptionKey = "docBuilder.agents.questionPlanner.desc",
                Icon = "help"
            },
            new AdvisorAgentDescriptor
            {
                Name = "doc.advisor", ProducesPatches = false, Enabled = true,
                InputKind = BuilderAgentInputKind.Text,
                DisplayNameKey = "docBuilder.agents.advisor.name",
                DescriptionKey = "docBuilder.agents.advisor.desc",
                Icon = "support_agent"
            },
            new AdvisorAgentDescriptor
            {
                Name = "doc.router", ProducesPatches = false, Enabled = false,
                InputKind = BuilderAgentInputKind.Text,
                DisplayNameKey = "docBuilder.agents.router.name",
                DescriptionKey = "docBuilder.agents.router.desc",
                Icon = "alt_route"
            }
        };

        public IReadOnlyList<BuilderAgentDescriptor> All => Descriptors;

        public IReadOnlyList<ContentAgentDescriptor> Content =>
            Descriptors.OfType<ContentAgentDescriptor>()
                .Where(c => c.Enabled && !c.RulesEnrichment)
                .OrderBy(c => c.Wave)
                .ToList();

        public IReadOnlyList<ContentAgentDescriptor> RulesWave =>
            Array.Empty<ContentAgentDescriptor>();

        public BuilderAgentDescriptor? Find(string name) =>
            Descriptors.FirstOrDefault(d => d.Name == name);
    }
}
