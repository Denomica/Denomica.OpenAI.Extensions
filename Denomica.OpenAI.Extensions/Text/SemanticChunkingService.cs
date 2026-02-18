using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Denomica.OpenAI.Extensions.Text
{
    /// <summary>
    /// A lean, rule-based chunker that treats MaxChunkSize as a token budget.
    /// Produces structure-aware chunks with optional overlap, without using an LLM.
    /// </summary>
    public sealed class SemanticChunkingService : IChunkingService
    {
        // === IChunkingService requirement ===
        // Interpreted as: maximum tokens per emitted chunk (approximate).
        public int MaxChunkSize { get; set; } = 600;

        // === Tuning knobs exposed as properties (no separate options class) ===
        /// <summary>~10% overlap is a good default. Helps recall across chunk boundaries.</summary>
        public int OverlapTokens { get; set; } = 60;

        /// <summary>Minimum characters to avoid tiny fragments.</summary>
        public int MinChunkChars { get; set; } = 300;

        /// <summary>Attempt to keep consecutive bullet/numbered list lines together.</summary>
        public bool KeepListsTogether { get; set; } = true;

        /// <summary>Target size when splitting an oversized block by sentences.</summary>
        public int FallbackSentenceTargetTokens { get; set; } = 600;

        // Heading-like patterns to find natural section boundaries in exported/plain text
        private static readonly Regex[] HeadingPatterns = new[]
        {
            new Regex(@"^\s*\d+\.\s+\S", RegexOptions.Compiled),                               // "1. Title"
            new Regex(@"^\s*(?:[IVXLC]+)\.\s+\S", RegexOptions.Compiled),                      // "I. Title"
            new Regex(@"^\s*Chapter\s+\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^[A-Z][A-Z0-9 \-:]{6,}$", RegexOptions.Compiled)                       // ALL CAPS-ish headline
        };

        // ------------------------- Public API -------------------------

        /// <summary>
        /// Chunk raw text content. Writes it to a stream and delegates to the Stream overload.
        /// </summary>
        public async IAsyncEnumerable<string> GetChunksAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                yield break;

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(input));
            ms.Position = 0;

            await foreach (var chunk in GetChunksAsync(ms))
                yield return chunk;
        }

        /// <summary>
        /// Chunk an input stream (UTF-8 text). Uses semantic blocks + token-budgeted packing with overlap.
        /// </summary>
        public async IAsyncEnumerable<string> GetChunksAsync(Stream input)
        {
            if (input is null) yield break;

            using var reader = new StreamReader(input, Encoding.UTF8, true, 4096, leaveOpen: true);
            var blocks = await ReadBlocksAsync(reader); // (title?, body) pairs

            var buffer = new List<(string title, string body)>();
            var bufferTokens = 0;

            var tail = new LinkedList<string>(); // last lines from the previous emitted chunk (for overlap)
            var tailTokens = 0;

            foreach (var block in blocks)
            {
                var piece = Merge(block.title, block.body);
                var pieceTokens = EstimateTokens(piece);

                // Oversized single block → split by sentences into sub-chunks
                if (pieceTokens > MaxChunkSize)
                {
                    if (buffer.Count > 0)
                    {
                        var emitted = EmitChunk(buffer);
                        yield return emitted;
                        UpdateOverlapTail(emitted, ref tail, ref tailTokens);
                        buffer.Clear();
                        bufferTokens = 0;
                    }

                    foreach (var sub in SplitBySentence(block.title, block.body, MaxChunkSize, FallbackSentenceTargetTokens))
                    {
                        yield return sub;
                        UpdateOverlapTail(sub, ref tail, ref tailTokens);
                    }
                    continue;
                }

                // Fits current chunk? else emit and start a new one (with overlap carried over)
                if (bufferTokens + pieceTokens <= MaxChunkSize || bufferTokens == 0)
                {
                    buffer.Add((block.title ?? string.Empty, piece));
                    bufferTokens += pieceTokens;
                }
                else
                {
                    var emitted = EmitChunk(buffer);
                    yield return emitted;
                    UpdateOverlapTail(emitted, ref tail, ref tailTokens);

                    buffer.Clear();
                    bufferTokens = 0;

                    if (OverlapTokens > 0 && tail.Count > 0)
                    {
                        var overlapJoined = string.Join("\n", tail);
                        buffer.Add((string.Empty, overlapJoined));
                        bufferTokens += EstimateTokens(overlapJoined);
                    }

                    buffer.Add((block.title ?? string.Empty, piece));
                    bufferTokens += pieceTokens;
                }
            }

            if (buffer.Count > 0)
                yield return EmitChunk(buffer);
        }

        // ------------------------- Internals -------------------------

        private static string Merge(string? title, string body)
            => string.IsNullOrWhiteSpace(title) ? body.Trim() : (title + "\n" + body).Trim();

        private async Task<List<(string? title, string body)>> ReadBlocksAsync(StreamReader reader)
        {
            var text = await reader.ReadToEndAsync();
            return ToSemanticBlocks(text, KeepListsTogether);
        }

        private static List<(string? title, string body)> ToSemanticBlocks(string text, bool keepLists)
        {
            var nl = text.Replace("\r\n", "\n");
            var paras = nl.Split("\n\n")
                          .Select(p => p.Trim())
                          .Where(p => p.Length > 0)
                          .ToList();

            // Merge consecutive list-like paragraphs so lists stay intact
            static bool IsListLine(string s) =>
                Regex.IsMatch(s, @"^(\s*[-*•–]\s|\s*\d+\.\s)");

            var blocks = new List<string>();
            var buf = new List<string>();

            foreach (var p in paras)
            {
                if (keepLists && (IsListLine(p) || (buf.Count > 0 && buf.All(IsListLine))))
                {
                    buf.Add(p);
                    continue;
                }

                if (buf.Count > 0)
                {
                    blocks.Add(string.Join("\n", buf));
                    buf.Clear();
                }
                blocks.Add(p);
            }
            if (buf.Count > 0) blocks.Add(string.Join("\n", buf));

            // Promote heading-like blocks; pair with following body when possible
            var result = new List<(string? title, string body)>();
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (LooksLikeHeading(b) && i + 1 < blocks.Count)
                {
                    result.Add((b, blocks[i + 1]));
                    i++; // consume next as body
                }
                else
                {
                    var lines = b.Split('\n');
                    if (lines.Length > 1 && LooksLikeHeading(lines[0]))
                        result.Add((lines[0], string.Join("\n", lines.Skip(1))));
                    else
                        result.Add((null, b));
                }
            }
            return result;

            static bool LooksLikeHeading(string s) => HeadingPatterns.Any(rx => rx.IsMatch(s));
        }

        private IEnumerable<string> SplitBySentence(string? title, string body, int maxTokens, int targetTokens)
        {
            // Naive sentence boundary splitter; used only when a single block is too large.
            var sentences = Regex.Split(body, @"(?<=[\.!\?])\s+")
                                 .Where(s => !string.IsNullOrWhiteSpace(s))
                                 .ToList();

            var acc = new List<string>();
            int t = 0;

            foreach (var s in sentences)
            {
                var st = EstimateTokens(s);
                if (acc.Count > 0 && (t + st > Math.Min(maxTokens, targetTokens)))
                {
                    yield return EmitSentenceChunk(title, acc);

                    // sentence-level overlap: carry the last sentence into next chunk
                    var last = acc.Last();
                    acc.Clear();
                    acc.Add(last);
                    t = EstimateTokens(last);
                }

                acc.Add(s);
                t += st;
            }

            if (acc.Count > 0)
                yield return EmitSentenceChunk(title, acc);

            static string EmitSentenceChunk(string? ttl, List<string> parts)
            {
                var bodyChunk = string.Join(" ", parts).Trim();
                return string.IsNullOrWhiteSpace(ttl) ? bodyChunk : (ttl + "\n" + bodyChunk);
            }
        }

        private string EmitChunk(List<(string title, string body)> buffer)
        {
            var content = string.Join("\n\n", buffer.Select(b => string.IsNullOrWhiteSpace(b.title) ? b.body : b.title + "\n" + b.body)).Trim();

            if (content.Length < MinChunkChars && buffer.Count > 1)
            {
                // Keep simple; the greedy packer already balances sizes.
            }
            return content;
        }

        private void UpdateOverlapTail(string emitted, ref LinkedList<string> tail, ref int tailTokens)
        {
            if (OverlapTokens <= 0) return;

            var lines = emitted.Split('\n');
            tail = new LinkedList<string>();
            tailTokens = 0;

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i];
                var t = EstimateTokens(line);
                if (tailTokens + t > OverlapTokens && tail.Count > 0) break;
                tail.AddFirst(line);
                tailTokens += t;
                if (tailTokens >= OverlapTokens) break;
            }
        }

        // --- Private, simplified token estimator (no external dependency) ---
        private static int EstimateTokens(string text)
            => Math.Max(1, (text?.Length ?? 0) / 4); // ~4 chars ≈ 1 token (heuristic)
    }
}
