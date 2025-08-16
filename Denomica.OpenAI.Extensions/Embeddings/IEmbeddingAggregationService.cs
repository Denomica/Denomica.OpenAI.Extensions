using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OpenAI.Extensions.Embeddings
{
    public interface IEmbeddingAggregationService
    {
        Task<EmbeddingResponse?> AggregateAsync(IEnumerable<EmbeddingResponse>? embeddings);
    }
}
