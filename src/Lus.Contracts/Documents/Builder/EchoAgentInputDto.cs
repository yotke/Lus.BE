namespace Lus.Contracts.Documents.Builder
{
    /// <summary>
    /// The <c>Input</c> half of the stdin payload for <c>doc.echo</c>
    /// (<c>{"Draft": …, "Input": …}</c>). Property names ARE the wire contract the Python agent
    /// reads (<c>agent_input.get("Text")</c>), so they live in a DTO the compiler can check rather
    /// than in an anonymous object.
    /// </summary>
    public sealed class EchoAgentInputDto
    {
        public required string Text { get; init; }
    }
}
