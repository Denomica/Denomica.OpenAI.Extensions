using Denomica.OpenAI.Extensions.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAIConsole
{
    public class WordChunker : IChunkingService
    {
        public int MaxChunkLength { get; set; }

        public async IAsyncEnumerable<string> GetChunksAsync(Stream input)
        {
            string content;
            using(var reader = new StreamReader(input))
            {
                content = await reader.ReadToEndAsync();
            }

            await foreach(var chunk in GetChunksAsync(content))
            {
                yield return chunk;
            }
        }

        public async IAsyncEnumerable<string> GetChunksAsync(string input)
        {
            foreach (var chunk in input.Split(' '))
            {
                yield return await Task.FromResult(chunk);
            }
        }
    }
}
