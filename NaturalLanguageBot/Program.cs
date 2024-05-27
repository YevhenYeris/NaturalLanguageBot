using Microsoft.Extensions.Configuration;
using NaturalLanguageBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var config =
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", true)
        .AddEnvironmentVariables()
        .Build();

var settings = config.GetSection("Settings").Get<Settings>() ?? throw new ArgumentNullException($"Failed to init");

var bot = NLanguageBotClient.GetInstance(settings);

await bot.StartAsync(new CancellationToken());