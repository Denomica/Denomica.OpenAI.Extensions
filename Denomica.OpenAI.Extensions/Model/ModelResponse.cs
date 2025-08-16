using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.OpenAI.Extensions.Model
{
    /// <summary>
    /// Represents the response from a model, including the model identifier and usage details.
    /// </summary>
    /// <remarks>This class is typically used to encapsulate the output of a model operation, providing
    /// information about the specific model used and its associated usage metrics.</remarks>
    public class ModelResponse
    {
        /// <summary>
        /// Gets or sets the model name associated with the object.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the usage details of the model, including token counts.
        /// </summary>
        public Usage Usage { get; set; } = new Usage();
    }
}
