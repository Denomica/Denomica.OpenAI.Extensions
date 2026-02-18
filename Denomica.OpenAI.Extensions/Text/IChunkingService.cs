using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Denomica.OpenAI.Extensions.Text
{
    /// <summary>
    /// The interface implemented by a service that provides chunks for creating vector embeddings from.
    /// </summary>
    public interface IChunkingService
    {
        /// <summary>
        /// Asynchronously retrieves chunks of data from the specified input stream.
        /// </summary>
        /// <param name="input">A stream containing the input to chunk up.</param>
        /// <returns>An asynchronous stream of strings, where each string represents a chunk of the input.</returns>
        IAsyncEnumerable<string> GetChunksAsync(Stream input);
    }
}
