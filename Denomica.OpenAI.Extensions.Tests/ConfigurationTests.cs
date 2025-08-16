using Denomica.OpenAI.Extensions.Chat;
using Denomica.OpenAI.Extensions.Configuration;
using Denomica.OpenAI.Extensions.Embeddings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OpenAI.Extensions.Tests
{
    [TestClass]
    public class ConfigurationTests
    {

        [TestMethod]
        public void ConfigureServices01()
        {
            var provider = new ServiceCollection()
                .AddOpenAIExtensions()
                .WithEmbeddingModel((opt, sp) =>
                {
                    opt.Endpoint = "https://foo-hub.openai.azure.com";
                    opt.ApiKey = "my-api-key";
                    opt.Name = "embedding-model";
                })
                .Services
                .BuildServiceProvider();

            var embeddingProvider = provider.GetService<EmbeddingProvider>();
            Assert.IsNotNull(embeddingProvider, "The embedding provider must be registered as a non-keyed service.");
        }

        [TestMethod]
        public void ConfigureServices02()
        {
            var provider = new ServiceCollection()
                .AddSingleton<EmbeddingDependencyService>()
                .AddOpenAIExtensions()
                .WithEmbeddingModel((opt, sp) =>
                {
                    opt.Endpoint = "https://foo-hub.openai.azure.com";
                    opt.ApiKey = "my-api-key";
                    opt.Name = "embedding-model";
                })
                .Services
                .BuildServiceProvider();


            var service = provider.GetService<EmbeddingDependencyService>();
            Assert.IsNotNull(service, "The dependency service must not be null");
        }

        [TestMethod]
        public void ConfigureServices03()
        {
            var provider = new ServiceCollection()
                .AddOpenAIExtensions()
                .WithChatModel((opt, sp) =>
                {
                    opt.Endpoint = "https://foo-hub.openai.azure.com";
                    opt.ApiKey = "my-api-key";
                    opt.Name = "chat-model";
                })
                .Services
                .BuildServiceProvider();

            var chat = provider.GetService<ChatProvider>();
            Assert.IsNotNull(chat, "Chat provider service must not be null");
        }

    }


    public class EmbeddingDependencyService
    {
        public EmbeddingDependencyService(EmbeddingProvider provider)
        {
            this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public EmbeddingProvider Provider { get; private set; }
    }
}
