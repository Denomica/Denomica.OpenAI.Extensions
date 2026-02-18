using Azure.AI.OpenAI;
using Denomica.OpenAI.Extensions.Chat;
using Denomica.OpenAI.Extensions.Embeddings;
using Denomica.OpenAI.Extensions.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Denomica.OpenAI.Extensions.Configuration
{
    /// <summary>
    /// Provides a builder for configuring OpenAI-related services, including chat models, embedding models, and
    /// chunking services, within a dependency injection container.
    /// </summary>
    /// <remarks>This class is designed to simplify the registration and configuration of OpenAI services in
    /// an application using dependency injection. It allows for the customization of chat and embedding model
    /// deployments, as well as the addition of custom chunking services.</remarks>
    public class OpenAIConfigurationBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIConfigurationBuilder"/> class  with the specified service
        /// collection.
        /// </summary>
        /// <param name="services">The collection of services to configure. This parameter cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
        public OpenAIConfigurationBuilder(IServiceCollection services)
        {
            this.Services = services ?? throw new ArgumentNullException(nameof(services));
            this.WithChunkingService<SemanticChunkingService>();
            this.WithEmbeddingAggregationService<WeightedAverageAggregationService>();
        }

        /// <summary>
        /// Gets the collection of service descriptors used to configure dependency injection.
        /// </summary>
        /// <remarks>This property provides access to the application's service collection, which is used
        /// to register and configure services for dependency injection. Modifications to this collection will affect
        /// the services available in the application's dependency injection container.</remarks>
        public IServiceCollection Services { get; private set; }



        /// <summary>
        /// Configures the chat model deployment options and registers the necessary services for the chat model.
        /// </summary>
        /// <param name="configureOptions">A delegate that configures the <see cref="ChatModelDeploymentOptions"/> using the provided options instance
        /// and service provider. This delegate is invoked during service registration.</param>
        /// <returns>A new instance of <see cref="OpenAIConfigurationBuilder"/> with the chat model services configured.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <see cref="Services"/> is <see langword="null"/> or if <paramref name="configureOptions"/> is <see
        /// langword="null"/>.</exception>
        public OpenAIConfigurationBuilder WithChatModel(Action<ChatModelDeploymentOptions, IServiceProvider> configureOptions)
        {
            if (null == this.Services)
            {
                throw new ArgumentNullException(nameof(this.Services));
            }
            if (null == configureOptions)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return new OpenAIConfigurationBuilder(
                this.Services
                    .AddOptions<ChatModelDeploymentOptions>()
                    .Configure<IServiceProvider>((opt, sp) =>
                    {
                        configureOptions(opt, sp);
                    }).Services
                    .AddSingleton<ChatClient>(sp =>
                    {
                        var opt = sp.GetRequiredService<IOptions<ChatModelDeploymentOptions>>().Value;
                        var client = this.CreateOpenAIClient(opt);
                        return client.GetChatClient(opt.Name);
                    })
                    .AddSingleton<ChatProvider>(sp =>
                    {
                        var opt = sp.GetRequiredService<IOptions<ChatModelDeploymentOptions>>().Value;
                        var client = sp.GetRequiredService<ChatClient>();
                        return new ChatProvider(client, sp, Options.Create(opt));
                    })
            );
        }

        /// <summary>
        /// Configures the builder to use the specified chunking service implementation.
        /// </summary>
        /// <remarks>This method registers the specified chunking service type with a scoped lifetime in
        /// the dependency injection container.</remarks>
        /// <typeparam name="TChunkingService">The type of the chunking service to register. Must implement <see cref="IChunkingService"/>.</typeparam>
        /// <returns>The current instance of <see cref="OpenAIConfigurationBuilder"/> to allow for method chaining.</returns>
        public OpenAIConfigurationBuilder WithChunkingService<TChunkingService>() where TChunkingService : class, IChunkingService
        {
            this.Services.AddSingleton<IChunkingService, TChunkingService>();
            return this;
        }

        /// <summary>
        /// Configures the builder to use a custom implementation of <see cref="IChunkingService"/>.
        /// </summary>
        /// <param name="factory">A factory function that provides an instance of <see cref="IChunkingService"/>. The function receives an
        /// <see cref="IServiceProvider"/> to resolve dependencies.</param>
        /// <returns>The current instance of <see cref="OpenAIConfigurationBuilder"/> to allow method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <see langword="null"/> or if the <c>Services</c> property of the
        /// builder is <see langword="null"/>.</exception>
        public OpenAIConfigurationBuilder WithChunkingService(Func<IServiceProvider, IChunkingService> factory)
        {
            if (null == this.Services)
            {
                throw new ArgumentNullException(nameof(this.Services));
            }
            if (null == factory)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            this.Services.AddSingleton<IChunkingService>(factory);

            return this;
        }

        /// <summary>
        /// Configures the builder to use the specified embedding aggregation service implementation.
        /// </summary>
        /// <remarks>This method registers the specified implementation of <see
        /// cref="IEmbeddingAggregationService"/>  in the dependency injection container with a scoped lifetime. Use
        /// this method to customize the  behavior of embedding aggregation in your application.</remarks>
        /// <typeparam name="TAggregationService">The type of the embedding aggregation service to register. This type must implement  <see
        /// cref="IEmbeddingAggregationService"/> and have a parameterless constructor or be resolvable  through
        /// dependency injection.</typeparam>
        /// <returns>The current instance of <see cref="OpenAIConfigurationBuilder"/> to allow for method chaining.</returns>
        public OpenAIConfigurationBuilder WithEmbeddingAggregationService<TAggregationService>() where TAggregationService : class, IEmbeddingAggregationService
        {
            this.Services.AddSingleton<IEmbeddingAggregationService, TAggregationService>();
            return this;
        }

        /// <summary>
        /// Configures the builder to use a custom factory for creating instances of <see
        /// cref="IEmbeddingAggregationService"/>.
        /// </summary>
        /// <param name="factory">A factory function that takes an <see cref="IServiceProvider"/> and returns an instance of <see
        /// cref="IEmbeddingAggregationService"/>.</param>
        /// <returns>The current <see cref="OpenAIConfigurationBuilder"/> instance, allowing for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <see langword="null"/> or if the <c>Services</c> property of the
        /// builder is <see langword="null"/>.</exception>
        public OpenAIConfigurationBuilder WithEmbeddingAggregationService(Func<IServiceProvider, IEmbeddingAggregationService> factory)
        {
            if (null == this.Services)
            {
                throw new ArgumentNullException(nameof(this.Services));
            }
            if (null == factory)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            this.Services.AddSingleton<IEmbeddingAggregationService>(factory);
            return this;
        }

        /// <summary>
        /// Configures the embedding model deployment options and registers the necessary services for embedding
        /// functionality.
        /// </summary>
        /// <remarks>This method registers the necessary services for embedding functionality, including
        /// the embedding client and provider. The <paramref name="configureOptions"/> delegate is used to configure the
        /// deployment options, such as the endpoint URI and API key. These services are added to the dependency
        /// injection container and can be resolved as needed.</remarks>
        /// <param name="configureOptions">A delegate that configures the <see cref="EmbeddingModelDeploymentOptions"/> using the provided options
        /// instance and <see cref="IServiceProvider"/>. This delegate is used to specify the endpoint, API key, and
        /// other settings required for the embedding model.</param>
        /// <returns>A new instance of <see cref="OpenAIConfigurationBuilder"/> with the embedding model services configured.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <see cref="Services"/> is <c>null</c> or if <paramref name="configureOptions"/> is <c>null</c>.</exception>
        public OpenAIConfigurationBuilder WithEmbeddingModel(Action<EmbeddingModelDeploymentOptions, IServiceProvider> configureOptions)
        {
            if (null == this.Services)
            {
                throw new ArgumentNullException(nameof(this.Services));
            }
            if (null == configureOptions)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return new OpenAIConfigurationBuilder(
                this.Services
                    .AddOptions<EmbeddingModelDeploymentOptions>()
                    .Configure<IServiceProvider>((opt, sp) =>
                    {
                        configureOptions(opt, sp);
                    }).Services
                    .AddSingleton<EmbeddingClient>(sp =>
                    {
                        var opt = sp.GetRequiredService<IOptions<EmbeddingModelDeploymentOptions>>().Value;
                        var client = new AzureOpenAIClient(new Uri(opt.Endpoint), new ApiKeyCredential(opt.ApiKey ?? ""));
                        return client.GetEmbeddingClient(opt.Name);
                    })
                    .AddSingleton<EmbeddingProvider>(sp =>
                    {
                        var opt = sp.GetRequiredService<IOptions<EmbeddingModelDeploymentOptions>>().Value;
                        var client = sp.GetRequiredService<EmbeddingClient>();
                        return new EmbeddingProvider(client, sp, Options.Create(opt));
                    })
            );
        }



        private OpenAIClient CreateOpenAIClient(ModelDeploymentOptions options)
        {
            return new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey ?? ""));
        }

    }
}
