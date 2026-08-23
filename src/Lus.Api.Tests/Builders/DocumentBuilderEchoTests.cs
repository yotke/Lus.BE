using Lus.Application.Common.Builders;
using Lus.Application.Common.Ports;
using Lus.Application.Documents.Builder.Agents;
using Lus.Contracts.Common.Builders;
using Lus.Contracts.Documents.Builder;
using Lus.Infrastructure.Adapters.PythonScriptsWS;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Lus.Api.Tests.Builders
{
    public class PythonScriptsAdapterTests
    {
        [Fact]
        public void Static_ctor_sets_PYTHONUTF8()
        {
            _ = typeof(PythonScriptsAdapter);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(PythonScriptsAdapter).TypeHandle);

            Assert.Equal("1", Environment.GetEnvironmentVariable("PYTHONUTF8"));
            Assert.Equal("utf-8", Environment.GetEnvironmentVariable("PYTHONIOENCODING"));
        }
    }

    public class DocumentBuilderEchoTests
    {
        [Fact]
        public async Task Echo_action_returns_typed_hebrew_envelope()
        {
            var python = new FakePython();
            var controller = new Lus.Controllers.DocumentBuilderController(
                python, MockMediator(), new DocumentBuilderAgentCatalog());

            var result = await controller.Echo(
                new EchoRequestDto { Text = "שלום" },
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var envelope = Assert.IsType<AgentEnvelopeDto<EchoResultDto>>(ok.Value);
            Assert.True(envelope.Ok);
            Assert.Equal("שלום", envelope.Result!.Echo);
            Assert.Equal("doc.echo", python.LastAgent);
        }

        private static IMediator MockMediator() => new Moq.Mock<IMediator>().Object;

        private sealed class FakePython : IPythonScriptsAdapter
        {
            public string? LastAgent { get; private set; }

            public Task<string> RunAgentAsync(
                string agentName, string draftJson, string inputJson,
                string langCode, CancellationToken cancellationToken = default)
            {
                LastAgent = agentName;
                return Task.FromResult(
                    "{\"Ok\":true,\"Agent\":\"doc.echo\",\"SchemaVersion\":1,\"Result\":{\"Echo\":\"שלום\",\"Lang\":\"he\"},\"ErrorInfo\":null}");
            }
        }
    }
}
