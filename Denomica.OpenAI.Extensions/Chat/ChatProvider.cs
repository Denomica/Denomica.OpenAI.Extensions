using Denomica.OpenAI.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.OpenAI.Extensions.Chat
{
    /// <summary>
    /// Provides functionality for interacting with a chat service using a specified client and deployment
    /// configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is initialized with a <see cref="ChatClient"/>, a service provider, and deployment
    /// options. It exposes the chat client and deployment name for use in chat-related operations.
    /// </para>
    /// <para>
    /// Currently, this class does not implement any methods for sending or receiving messages. It merely
    /// serves as a wrapper around the <see cref="ChatClient"/> and configuration options.
    /// </para>
    /// </remarks>
    public class ChatProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChatProvider"/> class with the specified client, service
        /// provider, and deployment options.
        /// </summary>
        /// <param name="client">The <see cref="ChatClient"/> instance used to interact with the chat service. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve dependencies. Cannot be <see langword="null"/>.</param>
        /// <param name="options">The deployment options for the chat model. Cannot be <see langword="null"/> and must contain a valid value.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="sp"/>, or <paramref name="options"/> is <see
        /// langword="null"/>.</exception>
        public ChatProvider(ChatClient client, IServiceProvider sp, IOptions<ChatModelDeploymentOptions> options)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
            this.Provider = sp ?? throw new ArgumentNullException(nameof(sp));
            this.Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }
        private readonly IServiceProvider Provider;
        private readonly ChatModelDeploymentOptions Options;

        /// <summary>
        /// Gets the instance of the <see cref="ChatClient"/> used for managing chat operations.
        /// </summary>
        public ChatClient Client { get; private set; }

        /// <summary>
        /// Gets the name of the chat model deployment associated with the current options.
        /// </summary>
        public string? DeploymentName => this.Options.Name;
    }
}
