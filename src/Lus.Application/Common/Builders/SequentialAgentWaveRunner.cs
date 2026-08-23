namespace Lus.Application.Common.Builders
{
    /// <summary>
    /// One sequential-wave agent step's outcome — enough for the runner to decide whether to
    /// apply/save/notify (success) or note-and-continue (failure). <typeparamref name="TPatchOp"/>
    /// is the caller's own patch-op DTO type (kept out of the kernel so it stays decoupled from
    /// any one entity's Contracts).
    /// </summary>
    public readonly record struct WaveAgentOutcome<TPatchOp>(
        bool Ok, IReadOnlyList<TPatchOp>? Patches, string? FailureCode, string? FailureMessage)
    {
        public static WaveAgentOutcome<TPatchOp> Success(IReadOnlyList<TPatchOp> patches) => new(true, patches, null, null);
        public static WaveAgentOutcome<TPatchOp> Failed(string? code, string? message) => new(false, null, code, message);
    }

    /// <summary>
    /// Strictly-sequential "run agent → apply its patches → save → notify" wave executor
    /// (ARCH-1) — extracted bottom-up from
    /// OrgBuilderOrchestrator.RunRulesEnrichmentWaveAsync once the Rules builder needed the
    /// identical degrade-not-die sequencing. Per descriptor: sendStatus(running) → runAgentAsync
    /// (the caller re-reads its OWN mutable draft fresh each iteration — this runner holds no
    /// draft/session type of its own, and is responsible for merging any success-path notes into
    /// its own state before returning the outcome) → on success with patches: applyAndSaveAsync
    /// (apply-batch + save + send-patched are bundled into ONE hook because the original code
    /// wraps all three in a single try/catch — an unapplicable batch degrades that agent, never
    /// the wave, so the caller's hook must swallow/log its own exceptions) → on failure:
    /// addFailureNote, then either way sendStatus(done/failed). One agent's failure never stops
    /// the wave.
    /// </summary>
    public static class SequentialAgentWaveRunner
    {
        public static async Task RunAsync<TDescriptor, TPatchOp>(
            IEnumerable<TDescriptor> wave,
            Func<TDescriptor, CancellationToken, Task<WaveAgentOutcome<TPatchOp>>> runAgentAsync,
            Func<TDescriptor, IReadOnlyList<TPatchOp>, CancellationToken, Task> applyAndSaveAsync,
            Func<TDescriptor, string, string?, CancellationToken, Task> sendStatusAsync,
            Action<TDescriptor, WaveAgentOutcome<TPatchOp>> addFailureNote,
            CancellationToken ct)
        {
            foreach (var descriptor in wave)
            {
                await sendStatusAsync(descriptor, "running", null, ct);

                var outcome = await runAgentAsync(descriptor, ct);

                if (!outcome.Ok)
                {
                    addFailureNote(descriptor, outcome);
                    await sendStatusAsync(descriptor, "failed", outcome.FailureCode, ct);
                    continue;
                }

                if (outcome.Patches is { Count: > 0 })
                    await applyAndSaveAsync(descriptor, outcome.Patches, ct);

                await sendStatusAsync(descriptor, "done", null, ct);
            }
        }
    }
}
