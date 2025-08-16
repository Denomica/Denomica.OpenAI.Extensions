using Denomica.OpenAI.Extensions.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OpenAI.Extensions.Embeddings
{
    public class WeightedAverageAggregationService : IEmbeddingAggregationService
    {
        public Task<EmbeddingResponse?> AggregateAsync(IEnumerable<EmbeddingResponse>? embeddings)
        {
            EmbeddingResponse? response = null;
            if (embeddings?.Any() == true)
            {
                var first = embeddings.First();
                var length = first.Vector.Length;
                var result = new float[length];
                foreach (var embedding in embeddings)
                {
                    if (embedding.Vector.Length != length)
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
                for (int i = 0; i < length; i++)
                {
                    result[i] /= totalTokens;
                }

                response = new EmbeddingResponse
                {
                    Model = first.Model,
                    Vector = result,
                    Usage = new Usage
                    {
                        PromptTokens = embeddings.Sum(x => x.Usage.PromptTokens),
                        CompletionTokens = embeddings.Sum(x => x.Usage.CompletionTokens),
                        TotalTokens = totalTokens
                    }
                };
            }

            return Task.FromResult(response);
        }
    }
}
