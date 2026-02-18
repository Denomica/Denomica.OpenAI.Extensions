using Denomica.OpenAI.Extensions.Text;

namespace Denomica.OpenAI.Extensions.Configuration
{
    /// <summary>
    /// Configuration options for <see cref="SemanticChunkingService"/>.
    /// </summary>
    public class SemanticChunkingServiceOptions
    {
        /// <summary>Maximum tokens per emitted chunk (approximate; uses 4 chars ≈ 1 token heuristic).</summary>
        public int MaxChunkSize { get; set; } = 6000;

        /// <summary>Number of tokens to repeat at the start of each chunk from the end of the previous one.</summary>
        public int OverlapTokens { get; set; } = 60;

        /// <summary>Attempt to keep consecutive bullet/numbered-list paragraphs together in one block.</summary>
        public bool KeepListsTogether { get; set; } = true;
    }
}
