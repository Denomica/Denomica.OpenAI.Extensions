using Denomica.OpenAI.Extensions.Embeddings;
using Denomica.OpenAI.Extensions.Model;

namespace Denomica.OpenAI.Extensions.Tests;

[TestClass]
public class EmbeddingTests
{
    [TestMethod]
    public void CombineEmbeddings01()
    {
        var embeddings = new List<EmbeddingResponse>();
        var result = embeddings.Combine();
        Assert.IsNull(result);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
    public void CombineEmbeddings02()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Embedding = new float[] { 1, 2, 3 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = "model1",
                Embedding = new float[] { 4, 5, 6, 7 },
                Usage = new Usage { TotalTokens = 2 }
            }
        };

        var result = embeddings.Combine();
    }

    [TestMethod]
    [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
    public void CombineEmbeddings03()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Embedding = new float[] { 1, 2, 3 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = "model2",
                Embedding = new float[] { 4, 5, 6 },
                Usage = new Usage { TotalTokens = 2 }
            }
        };
        var result = embeddings.Combine();
    }

    [TestMethod]
    public void CombineEmbeddings04()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Embedding = new float[] { 1, 3, 5 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = "model1",
                Embedding = new float[] { 4, 6, 8 },
                Usage = new Usage { TotalTokens = 2 }
            }
        };

        var result = embeddings.Combine();
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Embedding[0]);
        Assert.AreEqual(5, result.Embedding[1]);
        Assert.AreEqual(7, result.Embedding[2]);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
    public void CombineEmbeddings05()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Embedding = new float[] { 1, 2, 3 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = string.Empty,
                Embedding = new float[] { 4, 5, 6 },
                Usage = new Usage { TotalTokens = 1 } // TotalTokens is null
            }
        };
        var result = embeddings.Combine();
    }

    [TestMethod]
    [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
    public void CombineEmbeddings06()
    {
        var embeddings = new List<EmbeddingResponse>
        {
            new EmbeddingResponse
            {
                Model = "model1",
                Embedding = new float[] { 1, 2, 3 },
                Usage = new Usage { TotalTokens = 1 }
            },
            new EmbeddingResponse
            {
                Model = "model1",
                Embedding = new float[] { 4, 5, 6 },
                Usage = new Usage { TotalTokens = null }
            }
        };
        var result = embeddings.Combine();
    }
}
