using Google.Cloud.Language.V1;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text;
using System.Text.RegularExpressions;

namespace NaturalLanguageBot;

public class EmojiTranslator
{
    private readonly NLanguageApiClient _apiClient;
    private readonly string _csvPath;

    private const string PromptTemplate = "I have a list of phrases, and I need you to map each phrase to" +
        "the most fitting emoji based on its meaning. Below are the lists of phrases and emojis with their descriptions. " +
        "Your task is to strictly assign each phrase to a single emoji that best matches its meaning. The result should be " +
        "a list of emojis separated by colons." +
        "\n\n**List of Phrases:**\n{0}\n**List of Emojis:**\n{1}\nPlease return the result as a list of emojis separated by colons and enclosed in square braces (e.g., [😊:😢:🎉:🚀:🏆]).";

    public EmojiTranslator(NLanguageApiClient apiClient, string cvsPath)
    {
        _apiClient = apiClient;
        _csvPath = cvsPath;
    }

    public async Task<IEnumerable<Entity>> TextToEntities(string text, int emojiCount)
    {
        var entities = await _apiClient.GetOrderedEntities(text, emojiCount);

        return entities.DistinctBy(e => e.Name);
    }

    public async Task<IEnumerable<string>> EntitiesToEmojis(IEnumerable<Entity> entities)
    {
        var strBuilder = new StringBuilder();

        for (var i = 0; i < entities.Count(); ++i)
        {
            var e = entities.ElementAt(i);
            strBuilder.AppendLine($"{i + 1}. {e.Name} ({e.Type})\n");
        }

        var joinedEntities = strBuilder.ToString();

        var csvData = ReadCsv().Split("\n");

        strBuilder.Clear();

        for (var i = 0; i < csvData.Count(); ++i)
        {
            var code = csvData.ElementAt(i).Split(",").First();
            var description = csvData.ElementAt(i).Split(":").Last();

            strBuilder.AppendLine($"{i + 1}. {code} - {description}");
        }

        var apiResponse = await _apiClient.GenerateContent(string.Format(PromptTemplate, joinedEntities, strBuilder.ToString()));

        var regexPattern = @"\[(.*?)\]";

        var match = Regex.Match(apiResponse, regexPattern);

        if (match.Success)
        {
            var content = match.Groups[1].Value;

            return content.Split(new[] { ":" }, StringSplitOptions.None);
        }

        return Enumerable.Empty<string>();
    }

    private string ReadCsv()
    {
        return File.ReadAllText(_csvPath);
    }
}

