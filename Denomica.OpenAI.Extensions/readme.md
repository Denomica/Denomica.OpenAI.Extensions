# Denomica.OpenAI.Extensions

Denomica.OpenAI.Extensions is a library that provides extension methods for working with types in [`Azure.AI.OpenAI`](https://www.nuget.org/packages/Azure.AI.OpenAI).

## Getting Started

The following sample code illustrates how to quickly get started with the library.

```csharp
using Denomica.OpenAI.Extensions.Chat;
using Denomica.OpenAI.Extensions.Embeddings;
using Denomica.OpenAI.Extensions.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

var provider = new ServiceCollection()
    .AddOpenAIExtensions()
    .WithChunkingService<LineChunkingService>()
    .WithChatModel((opt, sp) =>
    {
        // Add your chat model configuration here.
    })
    .WithEmbeddingModel((opt, sp) =>
    {
        // Add your embedding model configuration here.
    })
    .Services
    .BuildServiceProvider();

var chatProvider = provider
    .GetRequiredKeyedService<ChatProvider>([name of your chat model deployment]);
var chatResult = await chatProvider.Client.CompleteChatAsync(
    new UserChatMessage("Hi! Can I call you Kevin?")
);
var chatContent = chatResult.GetContent().ToList();

var embeddingProvider = provider.GetRequiredKeyedService<EmbeddingProvider>([name of your embedding model deployment]);
var embedding = await embeddingProvider.GenerateEmbeddingAsync("Hello World!");

```

## Version Highlights

The main hihglights in the published versions are outlined below.

### v1.0.0-beta.3

- Added `DeploymentName` property to the `EmbeddingProvider` and `ChatProvider` classes, which returns the name of the model deployment used by the provider.

### v1.0.0-beta.2

- Fixed a typo in the readme file.

### v1.0.0-beta.1

The initial version of the library includes the following features.

- A text chunking service that chunks text into smaller pieces.
- An `EmbeddingProvider` service class for working with embeddings.
- The `EmbeddingProvider` service uses a configured text chunking service to break up text into smaller chunks.
- Combines embeddings generated from the text chunks into a single embedding using a weighted average approach where the total number of tokens consumed by each chunk is used as the weight.
- A ChatProvider service class for working with chat completions.
- Dependency injection support for easy configuration of both embedding models and chat completion models.