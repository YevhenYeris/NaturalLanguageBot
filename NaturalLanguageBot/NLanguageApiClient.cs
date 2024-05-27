using Google.Cloud.Language.V1;
using Google.Cloud.Translation.V2;
using Google.Cloud.AIPlatform.V1;
using Google.Api.Gax.Grpc;
using System.Text;

namespace NaturalLanguageBot;

public class NLanguageApiClient
{
    private readonly LanguageServiceClient _languageServiceClient;

    public NLanguageApiClient()
    {
       _languageServiceClient = LanguageServiceClient.Create();
    }

    public async Task<string> GenerateContent(
        string prompt,
        string projectId = "springproject-423216",
        string location = "us-central1",
        string publisher = "google",
        string model = "gemini-1.5-flash-preview-0514"
    )
    {
        // Create client
        var predictionServiceClient = new PredictionServiceClientBuilder
        {
            Endpoint = $"{location}-aiplatform.googleapis.com"
        }.Build();

        // Initialize content request
        var generateContentRequest = new GenerateContentRequest
        {
            Model = $"projects/{projectId}/locations/{location}/publishers/{publisher}/models/{model}",
            GenerationConfig = new GenerationConfig
            {
                Temperature = 0.4f,
                TopP = 1,
                TopK = 32,
                MaxOutputTokens = 2048
            },
            Contents =
            {
                new Content
                {
                    Role = "USER",
                    Parts =
                    {
                        new Part { Text = prompt }
                    }
                }
            }
        };

        // Make the request, returning a streaming response
        using PredictionServiceClient.StreamGenerateContentStream response = predictionServiceClient.StreamGenerateContent(generateContentRequest);

        StringBuilder fullText = new();

        // Read streaming responses from server until complete
        AsyncResponseStream<GenerateContentResponse> responseStream = response.GetResponseStream();
        await foreach (GenerateContentResponse responseItem in responseStream)
        {
            fullText.Append(responseItem.Candidates[0].Content.Parts[0].Text);
        }

        return fullText.ToString();
    }

    public async Task<IEnumerable<Entity>> GetOrderedEntities(string text, int entityCount)
    {
        var document = Document.FromPlainText(text);

        var response = await _languageServiceClient.AnalyzeEntitySentimentAsync(document);

        var topEntities = response.Entities.GroupBy(e => e.Type).OrderByDescending(g => g.Count()).Select(g => g.OrderByDescending(e => e.Mentions.Count).First()).Take(entityCount).ToList();

        if (topEntities.Count() < entityCount)
        {
            topEntities.AddRange(response.Entities.Except(topEntities).OrderByDescending(e => e.Mentions.Count).Take(entityCount - topEntities.Count()));
        }

        return topEntities;
    }

    public Task<string> AnalyzeTextSentimentAsync(string text)
    {
        var document = Document.FromPlainText(text);

        return GetSentimentResponse(document);
    }

    public Task<string> AnalyzeTextEntityAsync(string text)
    {
        var document = Document.FromPlainText(text);

        return GetEntityResponse(document);
    }

    private async Task<string> GetEntityResponse(Document document)
    {
        var response = await _languageServiceClient.AnalyzeEntitySentimentAsync(document);

        return string.Join("-----\n", response.Entities.Select(e => $"Name: {e.Name}\n{e.Type}\n{e.Sentiment}\n"));
    }

    private async Task<string> GetSentimentResponse(Document document)
    {
        var response = await _languageServiceClient.AnalyzeSentimentAsync(document);

        return $"Detected language: {response.Language}\n" + 
               $"Sentiment score: {response.DocumentSentiment.Score}\n" + 
               $"Sentiment magnitude: {response.DocumentSentiment.Magnitude}\n";
    }
}