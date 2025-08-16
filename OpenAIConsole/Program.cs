using Denomica.OpenAI.Extensions.Chat;
using Denomica.OpenAI.Extensions.Embeddings;
using Denomica.OpenAI.Extensions.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

var provider = new ServiceCollection()
    .AddOpenAIExtensions()
    //.WithChunkingService<WordChunker>()
    .WithChunkingService<LineChunkingService>()
    .WithChatModel((opt, sp) =>
    {
        opt.Endpoint = $"https://{args[0]}.openai.azure.com";
        opt.ApiKey = args[1];
        opt.Name = args[2];
    })
    .WithEmbeddingModel((opt, sp) =>
    {
        opt.Endpoint = $"https://{args[0]}.openai.azure.com";
        opt.ApiKey = args[1];
        opt.Name = args[3];
    })
    .Services

    .BuildServiceProvider();

var chatProvider = provider.GetRequiredKeyedService<ChatProvider>(args[2]);
var chatResult = await chatProvider.Client.CompleteChatAsync(
    new SystemChatMessage("You are the underboss to the user writing to you. You must always address them as boss. Also, use typical lingo that was used by mafia members in the 60s."),
    new UserChatMessage("Hi! Can I call you Kevin?")
);
var chatContent = chatResult.GetContent().ToList();
chatContent.ForEach(x => Console.WriteLine(x));
var chatResponse = chatResult.GetRawResponse();
var chatJson = chatResponse.Content.ToString();
Console.WriteLine(chatJson);

var embeddingProvider = provider.GetRequiredKeyedService<EmbeddingProvider>(args[3]);
var embedding = await embeddingProvider.GenerateEmbeddingAsync("Hello World! This is a test of the embedding generation service. It should return an embedding vector for the given text input.");
var tokens = embedding.Usage.TotalTokens;
//var chunker = provider.GetChunkingService();
//var embeddingClient = provider.GetKeyedService<EmbeddingClient>(args[3]);
//var embeddingResult = await embeddingClient.GenerateEmbeddingAsync("Hello World!", chunker);
//var embeddingResult = await embeddingClient.GenerateEmbeddingAsync("Hello World!", options: new EmbeddingGenerationOptions { });
//var embedding = embeddingResult.GetEmbedding();
//var embeddingResponse = embeddingResult.GetRawResponse();
//var embeddingJson = embeddingResponse.Content.ToString();
//Console.WriteLine(embeddingJson);
//var dimensions = embeddingResult?.Embedding?.Length;