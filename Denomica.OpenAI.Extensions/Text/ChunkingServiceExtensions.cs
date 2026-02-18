using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Denomica.OpenAI.Extensions.Text
{
    /// <summary>
    /// Extension methods for <see cref="IChunkingService"/>.
    /// </summary>
    public static class ChunkingServiceExtensions
    {
        /// <summary>
        /// Asynchronously retrieves chunks of data from the specified input string.
        /// </summary>
        /// <param name="service">The chunking service to use.</param>
        /// <param name="input">The input string to chunk up. If <see langword="null"/> or empty, no chunks are returned.</param>
        /// <returns>An asynchronous stream of strings, where each string represents a chunk of the input.</returns>
        public static async IAsyncEnumerable<string> GetChunksAsync(this IChunkingService service, string input)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input ?? string.Empty));
            await foreach (var chunk in service.GetChunksAsync(stream))
            {
                yield return chunk;
            }
        }
    }
}
