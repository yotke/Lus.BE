using Lus.Contracts.Common;

namespace Lus.Application.Common.Builders
{
    /// <summary>
    /// Identity + job context for one builder turn (captured in the HTTP request, carried into the
    /// background job) — entity-agnostic (UserId/JobId/Language only), so it belongs in the kernel.
    /// The org builder already has its OWN identical record at
    /// <c>Organizations.Builder.Orchestration.BuilderTurnContext</c>, predating this kernel; it is
    /// left untouched (a rename there would ripple through every org builder file/test) — this is a
    /// deliberate, harmless duplication until a future task unifies them. RulesBuilderOrchestrator
    /// uses THIS one, since Rules/Builder must never reference Organizations.Builder
    /// (BuilderArchitectureGuardTests).
    /// </summary>
    public record BuilderTurnContext(int UserId, string UserIdString, string JobId, LanguageType Language);
}
