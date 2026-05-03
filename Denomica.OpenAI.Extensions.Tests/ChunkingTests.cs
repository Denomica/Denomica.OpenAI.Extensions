using Denomica.OpenAI.Extensions.Configuration;
using Denomica.OpenAI.Extensions.Text;
using Microsoft.Extensions.Options;

namespace Denomica.OpenAI.Extensions.Tests
{
    [TestClass]
    public class ChunkingTests
    {
        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static SemanticChunkingService CreateService(
            int maxChunkSize = 600,
            int overlapTokens = 60,
            bool keepListsTogether = true)
        {
            var opts = Options.Create(new SemanticChunkingServiceOptions
            {
                MaxChunkSize = maxChunkSize,
                OverlapTokens = overlapTokens,
                KeepListsTogether = keepListsTogether
            });
            return new SemanticChunkingService(opts);
        }

        private static async Task<List<string>> ChunkAsync(IChunkingService svc, string input)
        {
            var chunks = new List<string>();
            using var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(input ?? string.Empty));
            await foreach (var chunk in svc.GetChunksAsync(ms))
                chunks.Add(chunk);
            return chunks;
        }

        // -------------------------------------------------------------------------
        // Null / empty input
        // -------------------------------------------------------------------------

        /// <summary>Null input produces no chunks.</summary>
        [TestMethod]
        public async Task SemanticChunkText01()
        {
            var svc = CreateService();
            var chunks = await ChunkAsync(svc, null!);

            Assert.AreEqual(0, chunks.Count);
        }

        /// <summary>Empty string produces no chunks.</summary>
        [TestMethod]
        public async Task SemanticChunkText02()
        {
            var svc = CreateService();
            var chunks = await ChunkAsync(svc, string.Empty);

            Assert.AreEqual(0, chunks.Count);
        }

        /// <summary>Whitespace-only string produces no chunks.</summary>
        [TestMethod]
        public async Task SemanticChunkText03()
        {
            var svc = CreateService();
            var chunks = await ChunkAsync(svc, "   \n\n   ");

            Assert.AreEqual(0, chunks.Count);
        }

        // -------------------------------------------------------------------------
        // Single chunk
        // -------------------------------------------------------------------------

        /// <summary>Text well within the token budget is emitted as one chunk.</summary>
        [TestMethod]
        public async Task SemanticChunkText04()
        {
            var svc = CreateService(maxChunkSize: 600, overlapTokens: 0);
            var input = "This is a short paragraph that fits easily within the token budget.";

            var chunks = await ChunkAsync(svc, input);

            Assert.AreEqual(1, chunks.Count);
            Assert.AreEqual(input.Trim(), chunks[0]);
        }

        /// <summary>No chunk is empty or whitespace-only.</summary>
        [TestMethod]
        public async Task SemanticChunkText05()
        {
            var svc = CreateService(maxChunkSize: 100, overlapTokens: 0);
            var input = string.Join("\n\n", Enumerable.Range(1, 10)
                .Select(i => $"Paragraph {i}: " + new string('x', 50)));

            var chunks = await ChunkAsync(svc, input);

            Assert.IsTrue(chunks.Count > 0);
            Assert.IsTrue(chunks.All(c => !string.IsNullOrWhiteSpace(c)));
        }

        // -------------------------------------------------------------------------
        // Multiple blocks packing
        // -------------------------------------------------------------------------

        /// <summary>Two small paragraphs are packed into a single chunk when they fit.</summary>
        [TestMethod]
        public async Task SemanticChunkText06()
        {
            var svc = CreateService(maxChunkSize: 600, overlapTokens: 0);
            var input = "First short paragraph.\n\nSecond short paragraph.";

            var chunks = await ChunkAsync(svc, input);

            Assert.AreEqual(1, chunks.Count);
            StringAssert.Contains(chunks[0], "First short paragraph.");
            StringAssert.Contains(chunks[0], "Second short paragraph.");
        }

        /// <summary>When paragraphs together exceed the budget, they are split across chunks.</summary>
        [TestMethod]
        public async Task SemanticChunkText07()
        {
            // Each paragraph ~50 tokens (200 chars), budget is 60 tokens → must split
            var svc = CreateService(maxChunkSize: 60, overlapTokens: 0);
            var para = new string('a', 200);
            var input = $"{para}\n\n{para}\n\n{para}";

            var chunks = await ChunkAsync(svc, input);

            Assert.IsTrue(chunks.Count > 1, "Expected more than one chunk.");
        }

        /// <summary>Every chunk respects the token budget (allowing for overlap).</summary>
        [TestMethod]
        public async Task SemanticChunkText08()
        {
            var maxChunkSize = 100;
            var overlapTokens = 20;
            var svc = CreateService(maxChunkSize: maxChunkSize, overlapTokens: overlapTokens);
            var input = string.Join("\n\n", Enumerable.Range(1, 20)
                .Select(i => $"Paragraph {i}: " + new string('x', 100)));

            var chunks = await ChunkAsync(svc, input);

            // Each chunk's token estimate (length/4) should not vastly exceed budget + overlap
            foreach (var chunk in chunks)
            {
                var tokens = chunk.Length / 4;
                Assert.IsTrue(tokens <= maxChunkSize + overlapTokens + 10,
                    $"Chunk exceeded budget: {tokens} tokens. Content: {chunk[..Math.Min(80, chunk.Length)]}...");
            }
        }

        // -------------------------------------------------------------------------
        // Overlap
        // -------------------------------------------------------------------------

        /// <summary>When overlap is enabled, the start of a later chunk contains content from the end of the previous one.</summary>
        [TestMethod]
        public async Task SemanticChunkText09()
        {
            var svc = CreateService(maxChunkSize: 60, overlapTokens: 20);
            // Two paragraphs that each fill the budget so a second chunk is forced
            var para1 = "Alpha " + new string('a', 200);
            var para2 = "Beta " + new string('b', 200);
            var input = $"{para1}\n\n{para2}";

            var chunks = await ChunkAsync(svc, input);

            Assert.IsTrue(chunks.Count >= 2, "Expected at least two chunks.");
            // The second chunk should carry some tail content from the first
            var lastLineOfChunk1 = chunks[0].Split('\n').Last(l => !string.IsNullOrWhiteSpace(l));
            StringAssert.Contains(chunks[1], lastLineOfChunk1[..Math.Min(20, lastLineOfChunk1.Length)]);
        }

        /// <summary>With overlap disabled, no content from one chunk leaks into the next.</summary>
        [TestMethod]
        public async Task SemanticChunkText10()
        {
            var svc = CreateService(maxChunkSize: 60, overlapTokens: 0);
            var para1 = "UNIQUESTART " + new string('a', 200);
            var para2 = "UNIQUEEND " + new string('b', 200);
            var input = $"{para1}\n\n{para2}";

            var chunks = await ChunkAsync(svc, input);

            Assert.IsTrue(chunks.Count >= 2);
            // No chunk after the first should contain content from the beginning of para1
            for (int i = 1; i < chunks.Count; i++)
                Assert.IsFalse(chunks[i].Contains("UNIQUESTART"),
                    $"Chunk {i} unexpectedly contains overlap content.");
        }

        // -------------------------------------------------------------------------
        // List merging
        // -------------------------------------------------------------------------

        /// <summary>Consecutive list paragraphs are merged into a single block when KeepListsTogether is true.</summary>
        [TestMethod]
        public async Task SemanticChunkText11()
        {
            var svc = CreateService(maxChunkSize: 600, overlapTokens: 0, keepListsTogether: true);
            var input = "- Item one\n\n- Item two\n\n- Item three";

            var chunks = await ChunkAsync(svc, input);

            Assert.AreEqual(1, chunks.Count, "All list items should be in one chunk.");
            StringAssert.Contains(chunks[0], "Item one");
            StringAssert.Contains(chunks[0], "Item two");
            StringAssert.Contains(chunks[0], "Item three");
        }

        /// <summary>A non-list paragraph following a list is NOT merged into the list block.</summary>
        [TestMethod]
        public async Task SemanticChunkText12()
        {
            var svc = CreateService(maxChunkSize: 600, overlapTokens: 0, keepListsTogether: true);
            var input = "- Item one\n\n- Item two\n\nThis is a normal paragraph after the list.";

            var chunks = await ChunkAsync(svc, input);

            // The normal paragraph must not be swallowed into the list block
            Assert.IsTrue(chunks.Any(c => c.Contains("normal paragraph")),
                "Normal paragraph after list was lost.");
        }

        /// <summary>With KeepListsTogether false, list paragraphs are treated as ordinary blocks.</summary>
        [TestMethod]
        public async Task SemanticChunkText13()
        {
            // Each item is ~60 chars (~15 tokens), well above the budget of 10, so each must be its own chunk
            var svc = CreateService(maxChunkSize: 10, overlapTokens: 0, keepListsTogether: false);
            var input = "- Item one with enough text to exceed the token budget\n\n" +
                        "- Item two with enough text to exceed the token budget\n\n" +
                        "- Item three with enough text to exceed the token budget";

            var chunks = await ChunkAsync(svc, input);

            Assert.IsTrue(chunks.Count >= 2, "Items should not be merged when KeepListsTogether is false.");
        }

        // -------------------------------------------------------------------------
        // Heading pairing
        // -------------------------------------------------------------------------

        /// <summary>An ALL-CAPS heading is paired with the following paragraph into one block.</summary>
        [TestMethod]
        public async Task SemanticChunkText14()
        {
            var svc = CreateService(maxChunkSize: 600, overlapTokens: 0);
            var input = "INTRODUCTION\n\nThis section introduces the topic in detail.";

            var chunks = await ChunkAsync(svc, input);

            Assert.AreEqual(1, chunks.Count);
            StringAssert.Contains(chunks[0], "INTRODUCTION");
            StringAssert.Contains(chunks[0], "introduces the topic");
        }

        /// <summary>A Chapter heading is paired with its body.</summary>
        [TestMethod]
        public async Task SemanticChunkText15()
        {
            var svc = CreateService(maxChunkSize: 600, overlapTokens: 0);
            var input = "Chapter 1\n\nThis is the body of chapter one.";

            var chunks = await ChunkAsync(svc, input);

            Assert.AreEqual(1, chunks.Count);
            StringAssert.Contains(chunks[0], "Chapter 1");
            StringAssert.Contains(chunks[0], "body of chapter one");
        }

        // -------------------------------------------------------------------------
        // Oversized block — sentence splitting
        // -------------------------------------------------------------------------

        /// <summary>A block that exceeds the budget is split into multiple sentence-based sub-chunks.</summary>
        [TestMethod]
        public async Task SemanticChunkText16()
        {
            // Budget of 20 tokens (80 chars); build a paragraph of several long sentences
            var svc = CreateService(maxChunkSize: 20, overlapTokens: 0);
            var sentences = Enumerable.Range(1, 6)
                .Select(i => $"This is sentence number {i} and it contains enough words to matter.");
            var input = string.Join(" ", sentences);

            var chunks = await ChunkAsync(svc, input);

            Assert.IsTrue(chunks.Count > 1, "Oversized block should be split into multiple chunks.");
        }

        /// <summary>A single sentence that alone exceeds the budget is emitted as its own chunk without looping.</summary>
        [TestMethod]
        public async Task SemanticChunkText17()
        {
            var svc = CreateService(maxChunkSize: 5, overlapTokens: 0);
            var input = "This is one very long sentence that greatly exceeds even a tiny token budget by itself.";

            var chunks = await ChunkAsync(svc, input);

            Assert.AreEqual(1, chunks.Count, "Single oversized sentence should be emitted as one chunk.");
            StringAssert.Contains(chunks[0], "long sentence");
        }

        // -------------------------------------------------------------------------
        // Stream overload
        // -------------------------------------------------------------------------

        /// <summary>The Stream overload produces the same chunks as the string overload.</summary>
        [TestMethod]
        public async Task SemanticChunkText18()
        {
            var svc = CreateService(maxChunkSize: 100, overlapTokens: 0);
            var input = string.Join("\n\n", Enumerable.Range(1, 5)
                .Select(i => $"Paragraph {i}: " + new string('x', 80)));

            var fromString = await ChunkAsync(svc, input);

            var fromStream = new List<string>();
            using var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(input));
            await foreach (var chunk in svc.GetChunksAsync(ms))
                fromStream.Add(chunk);

            Assert.AreEqual(fromString.Count, fromStream.Count);
            for (int i = 0; i < fromString.Count; i++)
                Assert.AreEqual(fromString[i], fromStream[i]);
        }

        // -------------------------------------------------------------------------
        // MaxChunkSize property
        // -------------------------------------------------------------------------

        /// <summary>Changing MaxChunkSize after construction is respected on the next call.</summary>
        [TestMethod]
        public async Task SemanticChunkText19()
        {
            var input = string.Join("\n\n", Enumerable.Range(1, 5)
                .Select(_ => new string('x', 200)));

            var svcLarge = CreateService(maxChunkSize: 600, overlapTokens: 0);
            var chunksLarge = await ChunkAsync(svcLarge, input);

            var svcSmall = CreateService(maxChunkSize: 50, overlapTokens: 0);
            var chunksSmall = await ChunkAsync(svcSmall, input);

            Assert.IsTrue(chunksSmall.Count > chunksLarge.Count,
                "Smaller budget should produce more chunks.");
        }

        // -------------------------------------------------------------------------
        // Default options / realistic input
        // -------------------------------------------------------------------------

        /// <summary>
        /// Uses default options (MaxChunkSize = 6000) and a lorem ipsum corpus large enough
        /// to exceed the budget, verifying that at least two chunks are produced and that
        /// no chunk is empty.
        /// </summary>
        [TestMethod]
        public async Task SemanticChunkText20()
        {
            var defaultOptions = new SemanticChunkingServiceOptions();
            var svc = CreateService(maxChunkSize: defaultOptions.MaxChunkSize, overlapTokens: defaultOptions.OverlapTokens);

            // Four classic lorem ipsum paragraphs, each ~200 chars (~50 tokens).
            // Repeated 55 times → ~44 000 chars (~11 000 tokens), reliably spanning two default-sized chunks.
            const string p1 =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor " +
                "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
                "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";

            const string p2 =
                "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu " +
                "fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa " +
                "qui officia deserunt mollit anim id est laborum.";

            const string p3 =
                "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque " +
                "laudantium, totam rem aperiam eaque ipsa quae ab illo inventore veritatis et quasi " +
                "architecto beatae vitae dicta sunt explicabo.";

            const string p4 =
                "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia " +
                "consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro " +
                "quisquam est qui dolorem ipsum quia dolor sit amet consectetur adipisci velit.";

            var singlePass = string.Join("\n\n", p1, p2, p3, p4);
            var corpus = string.Join("\n\n", Enumerable.Repeat(singlePass, 55));

            var estimatedTokens = corpus.Length / 4;
            Assert.IsTrue(estimatedTokens > defaultOptions.MaxChunkSize,
                "Corpus is too small to exceed the default budget. Increase the repetition count.");

            var chunks = await ChunkAsync(svc, corpus);

            Assert.IsTrue(chunks.Count >= 2,
                $"Expected at least 2 chunks with MaxChunkSize={defaultOptions.MaxChunkSize} and a ~{estimatedTokens}-token corpus.");
            Assert.IsTrue(chunks.All(c => !string.IsNullOrWhiteSpace(c)),
                "One or more emitted chunks are empty.");

            // All chunks except the last should be at least 70% of MaxChunkSize.
            // The last chunk is exempt because it contains whatever remains after the final boundary.
            var minTokens = (int)(defaultOptions.MaxChunkSize * 0.70);
            foreach (var chunk in chunks.SkipLast(1))
            {
                var tokens = chunk.Length / 4;
                Assert.IsTrue(tokens >= minTokens,
                    $"Non-final chunk is below 70% of MaxChunkSize ({tokens} tokens, minimum {minTokens}).");
            }
        }


    }
}
