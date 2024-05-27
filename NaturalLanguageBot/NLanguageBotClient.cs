using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Google.Rpc.Context.AttributeContext.Types;

namespace NaturalLanguageBot;

public class NLanguageBotClient
{
    private static NLanguageBotClient? _instance;

    private static readonly object Lock = new ();

    private readonly TelegramBotClient _botClient;

    private readonly NLanguageApiClient _nLanguageApiClient;

    private readonly EmojiTranslator _emojiTranslator;
    
    public static NLanguageBotClient GetInstance(Settings appSettings)
    {
        if (_instance is null)
        {
            lock (Lock)
            {
                _instance ??= new NLanguageBotClient(appSettings);
            }
        }

        return _instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource cts = new ();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        ReceiverOptions receiverOptions = new ()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await _botClient.GetMeAsync(cancellationToken);

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        // Send cancellation request to stop bot
        await cts.CancelAsync();
    }

    private NLanguageBotClient(Settings appSettings)
    {
        _botClient = new TelegramBotClient(appSettings.TelegramAccessKey);
        _nLanguageApiClient = new NLanguageApiClient();
        _emojiTranslator = new EmojiTranslator(_nLanguageApiClient, appSettings.EmojisCsvPath);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;

        Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

        try
        {
            var entities = await _emojiTranslator.TextToEntities(messageText, 10);

            var response = $"{string.Join(" ", entities.Select(e => e.Name))}\n{string.Join(" ", await _emojiTranslator.EntitiesToEmojis(entities))}";

            // Echo received message text
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: response,
                cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Exception occured:\n" + exception.Message,
                cancellationToken: cancellationToken);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}