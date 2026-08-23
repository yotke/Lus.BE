using System.ComponentModel.DataAnnotations;

namespace Lus.Contracts.Documents.Builder
{
    /// <summary>
    /// Input to the <c>doc.echo</c> bridge smoke endpoint.
    /// </summary>
    public sealed class EchoRequestDto
    {
        /// <summary>
        /// The text to round-trip. Required — an absent body used to reach the agent as an empty
        /// string and return a misleading Ok:true, so the contract rejects it at the model binder
        /// instead. Hebrew is the expected content; the whole point of the endpoint is proving it
        /// survives the C# → stdin → Python → stdout journey.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(4000)]
        public required string Text { get; init; }
    }
}
