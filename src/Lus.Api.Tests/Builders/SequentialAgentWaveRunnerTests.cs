using Lus.Application.Common.Builders;
using Xunit;

namespace Lus.Api.Tests.Builders
{
    public class SequentialAgentWaveRunnerTests
    {
        [Fact]
        public async Task Failed_agent_does_not_stop_the_wave()
        {
            var applied = new List<string>();
            var notes = new List<string>();
            var wave = new[]
            {
                new ContentAgentDescriptor { Name = "a", Wave = 1, ProducesPatches = true },
                new ContentAgentDescriptor { Name = "b", Wave = 1, ProducesPatches = true }
            };

            await SequentialAgentWaveRunner.RunAsync(
                wave,
                runAgentAsync: (descriptor, _) =>
                {
                    if (descriptor.Name == "a")
                        return Task.FromResult(WaveAgentOutcome<int>.Failed("boom", "no"));
                    return Task.FromResult(WaveAgentOutcome<int>.Success(new[] { 1 }));
                },
                applyAndSaveAsync: (descriptor, _, _) =>
                {
                    applied.Add(descriptor.Name);
                    return Task.CompletedTask;
                },
                sendStatusAsync: (_, _, _, _) => Task.CompletedTask,
                addFailureNote: (descriptor, _) => notes.Add(descriptor.Name),
                CancellationToken.None);

            Assert.Equal(new[] { "a" }, notes);
            Assert.Equal(new[] { "b" }, applied);
        }
    }
}
