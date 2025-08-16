using Denomica.OpenAI.Extensions.Model;
using Denomica.OpenAI.Extensions.Text;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Denomica.OpenAI.Extensions.Embeddings
{
    /// <summary>
    /// Provides extension methods for working with embeddings.
    /// </summary>
    public static class EmbeddingsExtensionMethods
    {
        /// <summary>
        /// Combines the given embeddings into one single <see cref="EmbeddingResponse"/> instance using
        /// weighted averaging. The total number of tokens used for each embedding is used as weight.
        /// </summary>
        /// <param name="embeddings">A collection of embeddings to combine.</param>
        /// <exception cref="Exception">
        /// The exception that is thrown the embeddings in the given collection have different number of dimensions or
        /// if they were generated with different embedding models.
        /// </exception>
        public static EmbeddingResponse? Combine(this IEnumerable<EmbeddingResponse>? embeddings)
        {
            if(embeddings?.Any() == true)
            {
                var first = embeddings.First();
                var length = first.Vector.Length;
                var result = new float[length];
                foreach(var embedding in embeddings)
                {
                    if(embedding.Vector.Length != length)
                    {
                        throw new Exception("All embeddings in the given list must have the same number of dimensions.");
                    }

                    if (embedding.Model != first.Model)
                    {
                        throw new Exception("Only embeddings from the same model can be combined.");
                    }

                    for (int i = 0; i < length; i++)
                    {
                        result[i] += embedding.Vector[i] * embedding.Usage.TotalTokens ?? throw new Exception("TotalTokens must not be null.");
                    }
                }

                var totalTokens = embeddings.Sum(x => x.Usage.TotalTokens ?? 0);
                for(int i = 0; i < length; i++)
                {
                    result[i] /= totalTokens;
                }

                return new EmbeddingResponse
                {
                    Model = first.Model,
                    Vector = result,
                    Usage = new Usage
                    {
                        TotalTokens = totalTokens
                    }
                };
            }

            return null;
        }

        /// <summary>
        /// Generates an embedding for the specified input text using the provided chunking service and options.
        /// </summary>
        /// <remarks>
        /// This method processes the input text by splitting it into chunks using the specified <paramref name="chunkingService"/>.
        /// Each chunk is processed individually to generate embeddings, which are then aggregated into a single response using a 
        /// weighted average where the number of consumed tokens for each individual embedding is used as weight.
        /// </remarks>
        /// <param name="client">The <see cref="EmbeddingClient"/> used to generate embeddings.</param>
        /// <param name="input">The input text to generate embeddings for. Cannot be null or empty.</param>
        /// <param name="chunkingService">The service used to split the input text into smaller chunks for processing. Cannot be null.</param>
        /// <param name="options">
        /// Optional configuration for embedding generation, such as model selection or additional parameters. Can be null.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>An <see cref="EmbeddingResponse"/> containing the generated embedding for the input text.</returns>
        public static async Task<EmbeddingResponse?> GenerateEmbeddingAsync(this EmbeddingClient client, string input, IChunkingService chunkingService, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            var embeddings = new List<EmbeddingResponse>();
            await foreach(var chunk in chunkingService.GetChunksAsync(input))
            {
                var response = await client.GenerateEmbeddingAsync(chunk, options, cancellationToken);
                var embedding = response.GetEmbedding();
                embeddings.Add(embedding);
            }

            return embeddings.Combine();
        }

        /// <summary>
        /// Converts the result of an OpenAI embedding operation into a strongly-typed <see cref="EmbeddingResponse"/>
        /// object.
        /// </summary>
        /// <remarks>This method processes the raw response content from the <paramref
        /// name="embeddingResult"/>, deserializes it into an <see cref="EmbeddingResponse"/> object, and populates the
        /// embedding data as a float array. Ensure that the <paramref name="embeddingResult"/> contains valid data
        /// before calling this method.</remarks>
        /// <param name="embeddingResult">The result of the embedding operation, containing the raw response and the embedding data.</param>
        /// <returns>An <see cref="EmbeddingResponse"/> object containing the deserialized response and the embedding data as a
        /// float array.</returns>
        /// <exception cref="Exception">Thrown if the raw response cannot be deserialized into an <see cref="EmbeddingResponse"/> object.</exception>
        public static EmbeddingResponse GetEmbedding(this ClientResult<OpenAIEmbedding> embeddingResult)
        {
            var json = embeddingResult.GetRawResponse().Content.ToString();
            var embedding = JsonSerializer.Deserialize<EmbeddingResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if(null != embedding)
            {
                embedding.Vector = embeddingResult.Value.ToFloats().ToArray();
                return embedding;
            }

            throw new Exception("Failed to deserialize raw response to embedding result.");
        }

    }
}
