using Azure.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.OpenAI.Extensions.Configuration
{
    /// <summary>
    /// An options class for defining how to use a model deployment in Azure AI Foundry.
    /// </summary>
    public class ModelDeploymentOptions
    {
        /// <summary>
        /// The endpoint URL.
        /// </summary>
        /// <remarks>
        /// This is typically the URL of the Azure AI Foundry instance where the model is deployed,
        /// for instance https://{resource-name}.services.ai.azure.com/
        /// </remarks>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// The network timeout to apply when communicating with the endpoint. If not set, the default timeout of the underlying client will be used.
        /// </summary>
        public TimeSpan? NetworkTimeout { get; set; }

        /// <summary>
        /// The API key to use when accessing the endpoint.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// The token credential to use when an API key is not configured.
        /// </summary>
        public TokenCredential? TokenCredential { get; set; }

        /// <summary>
        /// The name of the deployed model.
        /// </summary>
        public string? Name { get; set; }
    }
}
