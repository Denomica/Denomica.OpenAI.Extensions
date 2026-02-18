using Denomica.OpenAI.Extensions.Text;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenAIConsole
{
    public class WordChunker : IChunkingService
    {
        public async IAsyncEnumerable<string> GetChunksAsync(Stream input)
        {
            string content;
            using (var reader = new StreamReader(input, Encoding.UTF8))
            {
                content = await reader.ReadToEndAsync();
            }

            foreach (var chunk in content.Split(' '))
            {
                yield return chunk;
            }
        }
    }
}
