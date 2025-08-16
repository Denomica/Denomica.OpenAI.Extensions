using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OpenAI.Extensions.Text
{
    /// <summary>
    /// A base class for chunking services that provides a default implementation of the <see cref="IChunkingService"/> interface.
    /// </summary>
    public abstract class ChunkingServiceBase : IChunkingService
    {
        /// <inheritdoc/>
        public virtual int MaxChunkSize { get; set; } = 25000;

        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<string> GetChunksAsync(Stream input)
        {
            using (var reader = new StreamReader(input, Encoding.UTF8, true, 4096, true))
            {
                var chunkBuilder = new StringBuilder();
                string? nextChunk = null;
                do
                {
                    nextChunk = await GetNextChunkAsync(reader);
                    if (null != nextChunk)
                    {
                        if (chunkBuilder.Length + nextChunk.Length <= MaxChunkSize)
                        {
                            chunkBuilder.Append(nextChunk);
                        }
                        else
                        {
                            yield return chunkBuilder.ToString();
                            
                            // Since the next chunk did not fit into the current chunk, we start a
                            // new chunk with the current chunk as the first chunk.
                            chunkBuilder.Clear();
                            chunkBuilder.Append(nextChunk);
                        }
                    }
                }
                while (null != nextChunk);

                if (chunkBuilder.Length > 0)
                {
                    yield return chunkBuilder.ToString();
                    chunkBuilder.Clear();
                }
            }

            yield break;
        }

        /// <summary>
        /// Returns a collection of strings representing chunks of the input string.
        /// </summary>
        /// <param name="input">The input string to chunk up.</param>
        public async IAsyncEnumerable<string> GetChunksAsync(string input)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                await foreach (var chunk in GetChunksAsync(stream))
                {
                    yield return chunk;
                }
            }
        }


        /// <summary>
        /// Asynchronously retrieves the next chunk of data from the provided <see cref="StreamReader"/>.
        /// </summary>
        /// <param name="chunkReader">The <see cref="StreamReader"/> used to read the data stream. Must not be <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is a string containing the next chunk of
        /// data,  or <see langword="null"/> if no more data is available.</returns>
        protected abstract Task<string?> GetNextChunkAsync(StreamReader chunkReader);
    }
}
