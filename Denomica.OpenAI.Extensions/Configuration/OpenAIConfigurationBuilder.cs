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
        }

        /// <summary>
        /// Gets the collection of service descriptors used to configure dependency injection.
        /// </summary>
        /// <remarks>This property provides access to the application's service collection, which is used
        /// to register and configure services for dependency injection. Modifications to this collection will affect
        /// the services available in the application's dependency injection container.</remarks>
        public IServiceCollection Services { get; private set; }



        /// <summary>
        /// Configures the deployment options for a chat model and registers the necessary services.
        /// </summary>
        /// <remarks>This method allows you to configure a chat model deployment by specifying options
        /// such as the model name and other deployment-specific settings. It also registers the necessary services,
        /// including a keyed <see cref="ChatClient"/> and <see cref="ChatProvider"/>, for interacting with the
        /// configured chat model.</remarks>
        /// <param name="configureOptions">A delegate that configures the <see cref="ChatModelDeploymentOptions"/> instance. The delegate receives the
        /// options to configure and an <see cref="IServiceProvider"/> for resolving additional dependencies.</param>
        /// <returns>An updated <see cref="OpenAIConfigurationBuilder"/> instance with the configured chat model deployment
        /// options and associated services registered.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configureOptions"/> is <see langword="null"/> or if the <c>Services</c> property
        /// of the current <see cref="OpenAIConfigurationBuilder"/> instance is <see langword="null"/>.</exception>
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

            var options = new ChatModelDeploymentOptions();
            configureOptions(options, this.Services.BuildServiceProvider());

            return new OpenAIConfigurationBuilder(
                this.Services
                    .AddSingleton<IOptions<ChatModelDeploymentOptions>>(Options.Create(options))
                    .AddKeyedScoped<ChatClient>(options.Name, (sp, key) =>
                    {
                        var opt = sp.GetRequiredService<IOptions<ChatModelDeploymentOptions>>().Value;
                        var client = this.CreateOpenAIClient(opt);
                        return client.GetChatClient($"{key}");
                    })
                    .AddKeyedScoped<ChatProvider>(options.Name, (sp, key) =>
                    {
                        var client = sp.GetRequiredKeyedService<ChatClient>(key);
                        return new ChatProvider(client, sp);
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
            this.Services.AddScoped<IChunkingService, TChunkingService>();
            return this;
        }

        /// <summary>
        /// Configures the embedding model deployment options and registers the necessary services for embedding
        /// functionality.
        /// </summary>
        /// <remarks>This method allows you to configure and register an embedding model deployment by
        /// providing a custom configuration delegate. The configured options are registered as a singleton, and the
        /// method also sets up scoped services for <see cref="EmbeddingClient"/> and <see cref="EmbeddingProvider"/>
        /// keyed by the deployment name.</remarks>
        /// <param name="configureOptions">A delegate that configures the <see cref="EmbeddingModelDeploymentOptions"/> using the provided options
        /// instance and the <see cref="IServiceProvider"/> for resolving dependencies.</param>
        /// <returns>A new instance of <see cref="OpenAIConfigurationBuilder"/> with the configured embedding model services.</returns>
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

            EmbeddingModelDeploymentOptions options = new EmbeddingModelDeploymentOptions();
            configureOptions(options, this.Services.BuildServiceProvider());

            return new OpenAIConfigurationBuilder(
                this.Services
                    .AddSingleton<IOptions<EmbeddingModelDeploymentOptions>>(Options.Create(options))
                    .AddKeyedScoped<EmbeddingClient>(options.Name, (sp, key) =>
                    {
                        var opt = sp.GetRequiredService<IOptions<EmbeddingModelDeploymentOptions>>().Value;
                        var client = new AzureOpenAIClient(new Uri(opt.Endpoint), new ApiKeyCredential(opt.ApiKey ?? ""));
                        return client.GetEmbeddingClient($"{key}");
                    })
                    .AddKeyedScoped<EmbeddingProvider>(options.Name, (sp, key) =>
                    {
                        var client = sp.GetRequiredKeyedService<EmbeddingClient>(key);
                        return new EmbeddingProvider(client, sp);
                    })
            );
        }



        private OpenAIClient CreateOpenAIClient(ModelDeploymentOptions options)
        {
            return new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey ?? ""));
        }

    }
}
