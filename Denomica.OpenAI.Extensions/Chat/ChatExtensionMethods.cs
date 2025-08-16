using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Denomica.OpenAI.Extensions.Chat
{
    /// <summary>
    /// Extension methods for working with chat completions.
    /// </summary>
    public static class ChatExtensionMethods
    {
        /// <summary>
        /// Extracts and returns the content text from a <see cref="ClientResult{T}"/> of <see cref="ChatCompletion"/>.
        /// </summary>
        /// <param name="chatResult">The result containing the <see cref="ChatCompletion"/> object to extract content from. Cannot be null.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of strings representing the text content of the chat completion. The
        /// collection will be empty if no content is available.</returns>
        public static IEnumerable<string> GetContent(this ClientResult<ChatCompletion> chatResult)
        {
            return chatResult.Value.Content.ToList().Select(x => x.Text);
        }
    }
}
