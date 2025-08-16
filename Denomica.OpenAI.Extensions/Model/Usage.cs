using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Denomica.OpenAI.Extensions.Model
{
    /// <summary>
    /// Represents the token usage details for a specific operation, including prompt, completion, and total tokens.
    /// </summary>
    /// <remarks>This class is typically used to track and report the number of tokens consumed during an
    /// operation,  such as in natural language processing or API interactions that involve token-based billing or
    /// limits.</remarks>
    public class Usage
    {
        /// <summary>
        /// Gets or sets the number of tokens used to generate the completion.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens consumed by the prompt in a request.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }

        /// <summary>
        /// Gets or sets the total number of tokens processed in the operation.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }



        /// <inheritdoc />
        public override string ToString()
        {
            return $"Total Tokens: {this.TotalTokens}";
        }
    }
}
