using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Denomica.OpenAI.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Denomica.OpenAI.Extensions.Text
{
    /// <summary>
    /// A rule-based chunking service that treats <see cref="MaxChunkSize"/> as an approximate token budget.
    /// Produces structure-aware chunks with optional overlap, without using an LLM.
    /// </summary>
    /// <remarks>
    /// Pipeline: read → paragraph split → block classification → greedy pack → overlap injection → yield.
    /// </remarks>
    public sealed class SemanticChunkingService : IChunkingService
    {
        /// <summary>
        /// Initializes a new instance with default options.
        /// </summary>
        public SemanticChunkingService()
        {
            this.Options = new SemanticChunkingServiceOptions();
        }

        /// <summary>
        /// Initializes a new instance using the provided options.
        /// </summary>
        public SemanticChunkingService(IOptions<SemanticChunkingServiceOptions> options)
        {
           this.Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        private readonly SemanticChunkingServiceOptions Options;

        /// <inheritdoc/>
        public int MaxChunkSize
        {
            get => this.Options.MaxChunkSize;
            set => this.Options.MaxChunkSize = value;
        }


        // -------------------------------------------------------------------------
        // Heading detection
        // -------------------------------------------------------------------------

        private static readonly Regex[] HeadingPatterns =
        {
            new Regex(@"^\s*Chapter\s+\d+\b",              RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^\s*(?:[IVXLC]+)\.\s+\S",          RegexOptions.Compiled),   // "IV. Title"
            new Regex(@"^[A-Z][A-Z0-9 \-:]{6,}$",          RegexOptions.Compiled),   // ALL-CAPS headline
        };

        private static bool LooksLikeHeading(string s) =>
            !string.IsNullOrWhiteSpace(s) && HeadingPatterns.Any(rx => rx.IsMatch(s));

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <inheritdoc/>
        public async IAsyncEnumerable<string> GetChunksAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) yield break;

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(input));
            await foreach (var chunk in GetChunksAsync(ms))
                yield return chunk;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<string> GetChunksAsync(Stream input)
        {
            if (input is null) yield break;

            string text;
            using (var reader = new StreamReader(input, Encoding.UTF8, true, 4096, leaveOpen: true))
                text = await reader.ReadToEndAsync();

            var blocks = Segment(text);

            // Greedy packer state
            var buffer = new List<Block>();
            var bufferTokens = 0;
            var overlapText = string.Empty; // tail of the last emitted chunk

            foreach (var block in blocks)
            {
                // Oversized block: flush buffer, inject overlap, split by sentence
                if (block.Tokens > MaxChunkSize)
                {
                    if (buffer.Count > 0)
                    {
                        var flushed = Pack(buffer);
                        overlapText = ExtractOverlapTail(flushed);
                        yield return flushed;
                        buffer.Clear();
                        bufferTokens = 0;
                    }

                    foreach (var sub in SplitBySentences(block, overlapText))
                    {
                        overlapText = ExtractOverlapTail(sub);
                        yield return sub;
                    }
                    continue;
                }

                // Block fits in current buffer
                if (bufferTokens + block.Tokens <= MaxChunkSize)
                {
                    buffer.Add(block);
                    bufferTokens += block.Tokens;
                }
                else
                {
                    // Emit current buffer, start a new one with overlap prepended
                    var emitted = Pack(buffer);
                    overlapText = ExtractOverlapTail(emitted);
                    yield return emitted;

                    buffer.Clear();
                    bufferTokens = 0;

                    if (overlapText.Length > 0)
                    {
                        var overlapBlock = new Block(null, overlapText);
                        buffer.Add(overlapBlock);
                        bufferTokens += overlapBlock.Tokens;
                    }

                    buffer.Add(block);
                    bufferTokens += block.Tokens;
                }
            }

            if (buffer.Count > 0)
                yield return Pack(buffer);
        }

        // -------------------------------------------------------------------------
        // Stage 1 – Segmentation: raw text → classified Block list
        // -------------------------------------------------------------------------

        private List<Block> Segment(string text)
        {
            var paragraphs = SplitIntoParagraphs(text);
            var merged = this.Options.KeepListsTogether ? MergeListParagraphs(paragraphs) : paragraphs;
            return ClassifyBlocks(merged);
        }

        /// <summary>Splits on blank lines, trimming each paragraph.</summary>
        private static List<string> SplitIntoParagraphs(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Split(new[] { "\n\n" }, StringSplitOptions.None)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
        }

        /// <summary>
        /// Merges consecutive list-only paragraphs into a single paragraph so a list
        /// is not split across blocks.
        /// </summary>
        private static List<string> MergeListParagraphs(List<string> paragraphs)
        {
            var result = new List<string>();
            var listBuf = new List<string>();

            foreach (var p in paragraphs)
            {
                if (IsListParagraph(p))
                {
                    listBuf.Add(p);
                }
                else
                {
                    if (listBuf.Count > 0)
                    {
                        result.Add(string.Join("\n", listBuf));
                        listBuf.Clear();
                    }
                    result.Add(p);
                }
            }

            if (listBuf.Count > 0)
                result.Add(string.Join("\n", listBuf));

            return result;
        }

        /// <summary>Returns true when every non-empty line in the paragraph looks like a list item.</summary>
        private static bool IsListParagraph(string p)
        {
            var lines = p.Split('\n').Where(l => l.Trim().Length > 0).ToList();
            return lines.Count > 0 && lines.All(IsListLine);
        }

        private static readonly Regex ListLineRx =
            new Regex(@"^(\s*[-*•–]\s|\s*\d+\.\s)", RegexOptions.Compiled);

        private static bool IsListLine(string line) => ListLineRx.IsMatch(line);

        /// <summary>
        /// Pairs heading paragraphs with their following body paragraph, and splits
        /// paragraphs whose first line looks like a heading from their body.
        /// </summary>
        private static List<Block> ClassifyBlocks(List<string> paragraphs)
        {
            var result = new List<Block>();

            for (int i = 0; i < paragraphs.Count; i++)
            {
                var p = paragraphs[i];

                // Standalone heading paragraph: pair it with the next paragraph as its body
                if (LooksLikeHeading(p) && i + 1 < paragraphs.Count)
                {
                    result.Add(new Block(p, paragraphs[i + 1]));
                    i++; // consumed as body
                    continue;
                }

                // Paragraph whose first line is a heading
                var lines = p.Split('\n');
                if (lines.Length > 1 && LooksLikeHeading(lines[0]))
                {
                    result.Add(new Block(lines[0], string.Join("\n", lines.Skip(1))));
                    continue;
                }

                result.Add(new Block(null, p));
            }

            return result;
        }

        // -------------------------------------------------------------------------
        // Stage 2 – Packing: Block list → single chunk string
        // -------------------------------------------------------------------------

        private static string Pack(List<Block> blocks)
        {
            return string.Join("\n\n", blocks.Select(b => b.ToText())).Trim();
        }

        // -------------------------------------------------------------------------
        // Stage 3 – Sentence splitting (fallback for oversized blocks)
        // -------------------------------------------------------------------------

        private IEnumerable<string> SplitBySentences(Block block, string overlapText)
        {
            var sentences = TokenizeSentences(block.Body);

            var acc = new List<string>();
            var accTokens = 0;

            // Prepend overlap from previous chunk if available
            if (overlapText.Length > 0)
            {
                acc.Add(overlapText);
                accTokens += EstimateTokens(overlapText);
            }

            foreach (var sentence in sentences)
            {
                var st = EstimateTokens(sentence);

                // Single sentence too large to fit: emit it alone to avoid an infinite loop
                if (acc.Count == 0 && st > MaxChunkSize)
                {
                    yield return BuildSentenceChunk(block.Heading, sentence);
                    continue;
                }

                if (accTokens + st > MaxChunkSize && acc.Count > 0)
                {
                    yield return BuildSentenceChunk(block.Heading, string.Join(" ", acc));

                    // Carry last sentence as overlap into the next sub-chunk
                    var lastSentence = acc.Last();
                    acc.Clear();
                    acc.Add(lastSentence);
                    accTokens = EstimateTokens(lastSentence);
                }

                acc.Add(sentence);
                accTokens += st;
            }

            if (acc.Count > 0)
                yield return BuildSentenceChunk(block.Heading, string.Join(" ", acc));
        }

        private static List<string> TokenizeSentences(string text)
        {
            return Regex
                .Split(text, @"(?<=[\.!\?])\s+")
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        private static string BuildSentenceChunk(string? heading, string body)
        {
            body = body.Trim();
            return string.IsNullOrWhiteSpace(heading) ? body : heading + "\n" + body;
        }

        // -------------------------------------------------------------------------
        // Stage 4 – Overlap extraction
        // -------------------------------------------------------------------------

        /// <summary>
        /// Walks backwards through the lines of <paramref name="chunk"/>, accumulating
        /// non-blank lines until the token budget (<see cref="OverlapTokens"/>) is met.
        /// </summary>
        private string ExtractOverlapTail(string chunk)
        {
            if (this.Options.OverlapTokens <= 0 || string.IsNullOrWhiteSpace(chunk))
                return string.Empty;

            var lines = chunk.Split('\n');
            var tail = new List<string>();
            var tailTokens = 0;

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue; // skip blank separator lines

                var t = EstimateTokens(line);
                tail.Insert(0, line);
                tailTokens += t;

                if (tailTokens >= this.Options.OverlapTokens) break;
            }

            return string.Join("\n", tail);
        }

        // -------------------------------------------------------------------------
        // Token estimator
        // -------------------------------------------------------------------------

        private static int EstimateTokens(string text) =>
            Math.Max(1, (text?.Length ?? 0) / 4); // ~4 chars ≈ 1 token

        // -------------------------------------------------------------------------
        // Block: a heading + body pair produced by the segmentation stage
        // -------------------------------------------------------------------------

        private sealed class Block
        {
            public string? Heading { get; }
            public string Body { get; }
            public int Tokens { get; }

            public Block(string? heading, string body)
            {
                Heading = string.IsNullOrWhiteSpace(heading) ? null : heading.Trim();
                Body = body?.Trim() ?? string.Empty;
                Tokens = EstimateTokens(ToText());
            }

            public string ToText() =>
                Heading is null ? Body : Heading + "\n" + Body;

            private static int EstimateTokens(string text) =>
                Math.Max(1, (text?.Length ?? 0) / 4);
        }
    }
}
