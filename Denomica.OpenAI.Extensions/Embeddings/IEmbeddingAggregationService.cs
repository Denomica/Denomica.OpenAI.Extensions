using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OpenAI.Extensions.Embeddings
{
    /// <summary>
    /// An embedding aggregation service that provides functionality to combine multiple embedding responses into a single aggregated result.
    /// </summary>
    public interface IEmbeddingAggregationService
    {
        /// <summary>
        /// Aggregates a collection of <see cref="EmbeddingResponse"/> objects into a single <see cref="EmbeddingResponse"/> that represents the combined embedding.
        /// </summary>
        /// <param name="embeddings">A collection of embeddings to aggregate.</param>
        /// <returns>Returns the aggregated <see cref="EmbeddingResponse"/>.</returns>
        Task<EmbeddingResponse?> AggregateAsync(IEnumerable<EmbeddingResponse>? embeddings);
    }
}
