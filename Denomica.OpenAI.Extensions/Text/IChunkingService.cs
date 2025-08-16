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
        /// Sets or returns the maximum chunk length returned by the chunking service.
        /// </summary>
        int MaxChunkLength { get; set; }

        /// <summary>
        /// Returns a collection of strings representing
        /// </summary>
        /// <param name="input">A stream containing the input string to chunk up.</param>
        IAsyncEnumerable<string> GetChunksAsync(Stream input);

        /// <summary>
        /// Asynchronously retrieves chunks of data from the specified input string.
        /// </summary>
        /// <remarks>This method processes the input string and yields chunks of data asynchronously. It
        /// is suitable for scenarios where the input is large or when processing needs to be performed
        /// incrementally.</remarks>
        /// <param name="input">The input string to process. Cannot be null or empty.</param>
        /// <returns>An asynchronous stream of strings, where each string represents a chunk of the input. The sequence will be
        /// empty if the input is empty.</returns>
        IAsyncEnumerable<string> GetChunksAsync(string input);
    }
}
