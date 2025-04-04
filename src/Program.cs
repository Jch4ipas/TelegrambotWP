using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http;
using Newtonsoft.Json.Linq;

class Program
{

    private static readonly string strValue = Environment.GetEnvironmentVariable("envChatId");
    private static readonly string BotToken = Environment.GetEnvironmentVariable("envBotToken");
    private static readonly long ChatId = Convert.ToInt64(strValue);
    private static readonly string WordPressApiUrl = "https://api.wordpress.org/core/version-check/1.7/";
    private static string LastVersion = "";

    static async Task Main()
    {
        var botClient = new TelegramBotClient(BotToken);

        _ = Task.Run(() => CheckWordPressUpdates(botClient));

        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );
        Console.WriteLine(strValue);
        Console.WriteLine("Bot started. Press Enter to exit.");
        Console.ReadLine();
        await Task.Delay(-1);
        cts.Cancel();
    }

    // Check every 30 minutes if a new wordpress version is release.
    private static async Task CheckWordPressUpdates(ITelegramBotClient botClient)
    {
        while (true)
        {
            try
            {
                var latestVersion61 = await GetLatestWordPress61Version();

                if (!string.IsNullOrEmpty(latestVersion61) && latestVersion61 != LastVersion)
                {
                    LastVersion = latestVersion61;
                    await botClient.SendTextMessageAsync(
                        chatId: ChatId,
                        text: $"New WordPress version detected for branch 6.1.x : {latestVersion61}"
                    );
                    Console.WriteLine($"New version detected : {latestVersion61}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while verifying WordPress : {ex.Message}");
            }

            // Wait 30 minute
            await Task.Delay(TimeSpan.FromMinutes(30));
        }
    }


    // Fetch the latest stable version of WordPress via API.
    private static async Task<string> GetLatestWordPressVersion()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(WordPressApiUrl);
        var json = JObject.Parse(response);

        return json["offers"]?[0]?["current"]?.ToString() ?? "";
    }
    private static async Task<string> GetLatestWordPress61Version()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(WordPressApiUrl);
        var json = JObject.Parse(response);
        var offers = json["offers"] as JArray;
        if (offers != null)
        {
            foreach (var offer in offers)
            {
                var current = offer["current"]?.ToString();
                if (!string.IsNullOrEmpty(current) && current.StartsWith("6.1."))
                {
                    return current;
                }
            }
        }
        return "";
    }

    // Handle message
    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var latestVersion = await GetLatestWordPressVersion();
        if (update.Message is not { Text: { } messageText })
            return;

        var chatId = update.Message.Chat.Id;
        Console.WriteLine($"Message received from {chatId} : {messageText}");

        if (messageText.Equals("/version", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: $"The current version of WordPress : {latestVersion}",
                cancellationToken: cancellationToken
            );
        }
        if (messageText.Equals("/info", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: $"This bot will notify you whenever a new version of WordPress is released.",
                cancellationToken: cancellationToken
            );
        }
        if (messageText.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: $"Hello, here are the different commands of this bot \n/version : Gives you the latest version of WordPress \n/info : Bot description\n/Subscribe : Will notify you whenever a new version of wordpress is out\n/menu : give you a simple menu",
                cancellationToken: cancellationToken
            );
        }
    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        string errorMessage = exception switch
        {
            ApiRequestException apiEx => $"Erreur API Telegram : {apiEx.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}