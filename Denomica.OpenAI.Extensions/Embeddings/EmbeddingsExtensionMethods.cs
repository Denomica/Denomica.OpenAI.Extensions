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
