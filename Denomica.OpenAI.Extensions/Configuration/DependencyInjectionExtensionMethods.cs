using Azure.AI.OpenAI;
using Denomica.OpenAI.Extensions.Configuration;
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
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring and retrieving OpenAI services in a dependency injection container.
    /// </summary>
    public static class DependencyInjectionExtensionMethods
    {

        /// <summary>
        /// Adds OpenAI extensions to the specified service collection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="OpenAIConfigurationBuilder"/> that is returned specifies the following default services.
        /// </para>
        /// <para>
        /// <list type="bullet">
        /// <item><description><see cref="SemanticChunkingService"/> for text chunking when creating vector embeddings.</description></item>
        /// <item><description><see cref="WeightedAverageAggregationService"/> for aggregating vector embeddings generated for multiple chunks into one.</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the OpenAI extensions will be added. Cannot be <see
        /// langword="null"/>.</param>
        /// <returns>An <see cref="OpenAIConfigurationBuilder"/> that can be used to further configure the OpenAI services.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
        public static OpenAIConfigurationBuilder AddOpenAIExtensions(this IServiceCollection services)
        {
            return new OpenAIConfigurationBuilder(services ?? throw new ArgumentNullException(nameof(services)));
        }

        /// <summary>
        /// Retrieves a <see cref="ChatClient"/> instance from the specified <see cref="IServiceProvider"/>.
        /// </summary>
        /// <remarks>This method resolves the <see cref="ChatClient"/> using a keyed service mechanism, 
        /// where the key is determined by the deployment options configured in the service provider. Ensure that the
        /// required services, including <see cref="IOptions{TOptions}"/> for  <see cref="ChatModelDeploymentOptions"/>,
        /// are registered in the service provider.</remarks>
        /// <param name="serviceProvider">The service provider used to resolve the <see cref="ChatClient"/> instance.</param>
        /// <returns>A <see cref="ChatClient"/> instance associated with the configured deployment options.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
        public static ChatClient GetChatClient(this IServiceProvider serviceProvider)
        {
            if (null == serviceProvider) throw new ArgumentNullException(nameof(serviceProvider));
            var options = serviceProvider.GetRequiredService<IOptions<ChatModelDeploymentOptions>>();
            var chatClient = serviceProvider.GetRequiredKeyedService<ChatClient>(options.Value.Name);
            return chatClient;
        }

        /// <summary>
        /// Retrieves an <see cref="EmbeddingClient"/> instance configured with the specified deployment options.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve the required services.</param>
        /// <returns>An <see cref="EmbeddingClient"/> instance associated with the deployment specified in the options.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
        public static EmbeddingClient GetEmbeddingClient(this IServiceProvider serviceProvider)
        {
            if (null == serviceProvider) throw new ArgumentNullException(nameof(serviceProvider));
            var options = serviceProvider.GetRequiredService<IOptions<EmbeddingModelDeploymentOptions>>();
            var embeddingClient = serviceProvider.GetRequiredKeyedService<EmbeddingClient>(options.Value.Name);
            return embeddingClient;
        }

    }
}
