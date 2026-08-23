namespace Lus.Application.Common.Builders
{
    /// <summary>What an agent DOES — drives how the orchestrator invokes it (its strategy).</summary>
    public enum BuilderAgentKind
    {
        /// <summary>Emits draft patches in a dependency wave (roles/crews/shifts/people/rules).</summary>
        Content,
        /// <summary>Coherence lint + deterministic auto-fixes (validator).</summary>
        Validator,
        /// <summary>Picks the single next interview question (question_planner).</summary>
        Planner,
        /// <summary>Free-text business advice grounded in the org (copilot).</summary>
        Advisor,
        /// <summary>Sharpens a raw request before it runs.</summary>
        Refiner,
        /// <summary>Ingests an uploaded file into draft patches (Excel/CSV import).</summary>
        Importer,
    }

    /// <summary>The kind of input an agent consumes beyond the draft.</summary>
    public enum BuilderAgentInputKind
    {
        None,
        Text,
        File,
    }

    /// <summary>
    /// Double-dispatch visitor over the agent-descriptor hierarchy: `descriptor.Accept(visitor)`
    /// routes to the method for the descriptor's concrete kind, so invocation strategy lives in
    /// ONE place per visitor and adding a kind is a compiler-enforced new Visit method — no
    /// scattered `switch (Kind)`. The canonical strategy is <see cref="AgentInvocationVisitor"/>.
    /// </summary>
    public interface IBuilderAgentVisitor<out TResult>
    {
        TResult VisitContent(ContentAgentDescriptor descriptor);
        TResult VisitValidator(ValidatorAgentDescriptor descriptor);
        TResult VisitPlanner(PlannerAgentDescriptor descriptor);
        TResult VisitAdvisor(AdvisorAgentDescriptor descriptor);
        TResult VisitRefiner(RefinerAgentDescriptor descriptor);
        TResult VisitImporter(ImporterAgentDescriptor descriptor);
    }

    /// <summary>
    /// One agent in the catalog — the single source of truth for which agents exist and what
    /// they do. A polymorphic hierarchy (one subtype per kind) so invocation is a double dispatch
    /// via <see cref="Accept{TResult}"/>, not a kind switch. Name MUST match the Python module.
    /// </summary>
    public abstract record BuilderAgentDescriptor
    {
        public required string Name { get; init; }
        public bool ProducesPatches { get; init; }
        public BuilderAgentInputKind InputKind { get; init; } = BuilderAgentInputKind.None;

        /// <summary>i18n keys for the UI agent picker (localized he/en client-side).</summary>
        public string DisplayNameKey { get; init; } = "";
        public string DescriptionKey { get; init; } = "";

        /// <summary>Material icon name for the agent chip.</summary>
        public string Icon { get; init; } = "";

        /// <summary>A disabled descriptor is listed but never dispatched (staged rollout).</summary>
        public bool Enabled { get; init; } = true;

        public abstract BuilderAgentKind Kind { get; }

        /// <summary>Double-dispatch entry point — calls the visitor method for this concrete kind.</summary>
        public abstract TResult Accept<TResult>(IBuilderAgentVisitor<TResult> visitor);
    }

    /// <summary>Content agent — emits draft patches in dependency wave order.</summary>
    public sealed record ContentAgentDescriptor : BuilderAgentDescriptor
    {
        /// <summary>Dependency wave (1-based): roles/crews=1, shifts/people=2, rules=3.</summary>
        public required int Wave { get; init; }

        /// <summary>
        /// A rules-ENRICHMENT agent: part of the sequential wave that runs AFTER the baseline
        /// content waves (rules_text → slots_demand → day_variant → crew_rules → rule_dependency
        /// → rules_reconciler). Unlike the baseline content waves (parallel within a wave), the
        /// enrichment wave is strictly sequential — each agent sees the PRIOR agents' patches
        /// applied to the draft, which the reconciler's dangling-ruleKeys guarantee depends on.
        /// Kept OUT of <see cref="IBuilderAgentCatalog.Content"/> so the baseline pass is unchanged.
        /// </summary>
        public bool RulesEnrichment { get; init; }

        public override BuilderAgentKind Kind => BuilderAgentKind.Content;
        public override TResult Accept<TResult>(IBuilderAgentVisitor<TResult> visitor) => visitor.VisitContent(this);
    }

    public sealed record ValidatorAgentDescriptor : BuilderAgentDescriptor
    {
        public override BuilderAgentKind Kind => BuilderAgentKind.Validator;
        public override TResult Accept<TResult>(IBuilderAgentVisitor<TResult> visitor) => visitor.VisitValidator(this);
    }

    public sealed record PlannerAgentDescriptor : BuilderAgentDescriptor
    {
        public override BuilderAgentKind Kind => BuilderAgentKind.Planner;
        public override TResult Accept<TResult>(IBuilderAgentVisitor<TResult> visitor) => visitor.VisitPlanner(this);
    }

    public sealed record AdvisorAgentDescriptor : BuilderAgentDescriptor
    {
        public override BuilderAgentKind Kind => BuilderAgentKind.Advisor;
        public override TResult Accept<TResult>(IBuilderAgentVisitor<TResult> visitor) => visitor.VisitAdvisor(this);
    }

    public sealed record RefinerAgentDescriptor : BuilderAgentDescriptor
    {
        public override BuilderAgentKind Kind => BuilderAgentKind.Refiner;
        public override TResult Accept<TResult>(IBuilderAgentVisitor<TResult> visitor) => visitor.VisitRefiner(this);
    }

    public sealed record ImporterAgentDescriptor : BuilderAgentDescriptor
    {
        public override BuilderAgentKind Kind => BuilderAgentKind.Importer;
        public override TResult Accept<TResult>(IBuilderAgentVisitor<TResult> visitor) => visitor.VisitImporter(this);
    }

    /// <summary>
    /// Registry of builder agents. `Content` is the wave-ordered, enabled content set the
    /// orchestrator iterates; `Find` resolves a descriptor by Python module name.
    /// </summary>
    public interface IBuilderAgentCatalog
    {
        IReadOnlyList<BuilderAgentDescriptor> All { get; }

        /// <summary>Enabled BASELINE Content agents, ordered by wave then declaration order.</summary>
        IReadOnlyList<ContentAgentDescriptor> Content { get; }

        /// <summary>
        /// The enabled rules-ENRICHMENT agents, in strict sequential order (wave then declaration).
        /// Runs after <see cref="Content"/>; each agent must see prior agents' patches applied and
        /// the reconciler runs LAST — see <see cref="ContentAgentDescriptor.RulesEnrichment"/>.
        /// </summary>
        IReadOnlyList<ContentAgentDescriptor> RulesWave { get; }

        BuilderAgentDescriptor? Find(string name);
    }
}
