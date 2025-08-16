using Denomica.OpenAI.Extensions.Embeddings;
using Denomica.OpenAI.Extensions.Model;

namespace Denomica.OpenAI.Extensions.Tests;

[TestClass]
public class EmbeddingTests
{
    [TestMethod]
    public async Task CombineEmbeddings01()
    {
        var embeddings = new List<EmbeddingResponse>();
        var result = await this.CombineAsync(embeddings);
        Assert.IsNull(result);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
    public async Task CombineEmbeddings02()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Vector = new float[] { 1, 2, 3 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = "model1",
                Vector = new float[] { 4, 5, 6, 7 },
                Usage = new Usage { TotalTokens = 2 }
            }
        };

        var result = await this.CombineAsync(embeddings);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
    public async Task CombineEmbeddings03()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Vector = new float[] { 1, 2, 3 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = "model2",
                Vector = new float[] { 4, 5, 6 },
                Usage = new Usage { TotalTokens = 2 }
            }
        };
        var result = await this.CombineAsync(embeddings);
    }

    [TestMethod]
    public async Task CombineEmbeddings04()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Vector = new float[] { 1, 3, 5 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = "model1",
                Vector = new float[] { 4, 6, 8 },
                Usage = new Usage { TotalTokens = 2 }
            }
        };

        var result = await this.CombineAsync(embeddings);
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Vector[0]);
        Assert.AreEqual(5, result.Vector[1]);
        Assert.AreEqual(7, result.Vector[2]);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
    public async Task CombineEmbeddings05()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Vector = new float[] { 1, 2, 3 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = string.Empty,
                Vector = new float[] { 4, 5, 6 },
                Usage = new Usage { TotalTokens = 1 } // TotalTokens is null
            }
        };
        var result = await this.CombineAsync(embeddings);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
    public async Task CombineEmbeddings06()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Vector = new float[] { 1, 2, 3 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = "model1",
                Vector = new float[] { 4, 5, 6 },
                Usage = new Usage { TotalTokens = null }
            }
        };
        var result = await this.CombineAsync(embeddings);
    }



    private Task<EmbeddingResponse?> CombineAsync(IEnumerable<EmbeddingResponse>? embeddings)
    {
        var aggregator = new WeightedAverageAggregationService();
        return aggregator.AggregateAsync(embeddings);
    }
}
