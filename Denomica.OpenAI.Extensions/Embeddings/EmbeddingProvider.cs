using Denomica.OpenAI.Extensions.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OpenAI.Extensions.Embeddings
{
    /// <summary>
    /// Provides functionality for generating embeddings from input text using a specified embedding client.
    /// </summary>
    /// <remarks>This class utilizes an <see cref="EmbeddingClient"/> to generate embeddings for input text. 
    /// It supports chunking of input text through a chunking service, which can be resolved from the  provided <see
    /// cref="IServiceProvider"/> or defaults to a line-based chunking service if none is available.</remarks>
    public class EmbeddingProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddingProvider"/> class with the specified client and
        /// service provider.
        /// </summary>
        /// <param name="client">The <see cref="EmbeddingClient"/> instance used to interact with the embedding service. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="sp">The <see cref="IServiceProvider"/> instance used to resolve dependencies. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> or <paramref name="sp"/> is <see langword="null"/>.</exception>
        public EmbeddingProvider(EmbeddingClient client, IServiceProvider sp)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
            this.Provider = sp ?? throw new ArgumentNullException(nameof(sp));
        }

        private readonly IServiceProvider Provider;

        /// <summary>
        /// Gets the <see cref="EmbeddingClient"/> instance used to interact with the embedding service.
        /// </summary>
        public EmbeddingClient Client { get; private set; }


        /// <summary>
        /// Generates an embedding for the specified input text by processing it in chunks.
        /// </summary>
        /// <remarks>The input text is divided into chunks using a chunking service, and an embedding is
        /// generated for each chunk. The individual embeddings are then combined into a single result. If no chunking
        /// service is provided, a default line-based chunking service is used.</remarks>
        /// <param name="input">The input text to generate an embedding for. Cannot be null or empty.</param>
        /// <returns>An <see cref="EmbeddingResponse"/> object representing the combined embedding for the input text. If no
        /// embedding could be generated, an empty <see cref="EmbeddingResponse"/> is returned.</returns>
        public async Task<EmbeddingResponse> GenerateEmbeddingAsync(string input)
        {
            var results = new List<EmbeddingResponse>();
            IChunkingService chunker = this.Provider.GetService<IChunkingService>() ?? new LineChunkingService();
            await foreach (var chunk in chunker.GetChunksAsync(input))
            {
                var result = await this.Client.GenerateEmbeddingAsync(chunk);
                var embedding = result.GetEmbedding();
                results.Add(embedding);
            }

            return results.Combine() ?? new EmbeddingResponse();
        }
    }
}
