using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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

        Console.WriteLine("Bot démarré. Appuyez sur Entrée pour quitter.");
        Console.ReadLine();

        cts.Cancel();
    }

    // Vérifie toutes les 30 minutes si nouvelle version de wordpress
    private static async Task CheckWordPressUpdates(ITelegramBotClient botClient)
    {
        while (true)
        {
            try
            {
                var latestVersion = await GetLatestWordPressVersion();
                if (!string.IsNullOrEmpty(latestVersion) && latestVersion != LastVersion)
                {
                    LastVersion = latestVersion;
                    await botClient.SendTextMessageAsync(
                        chatId: ChatId,
                        text: $"Nouvelle version de WordPress disponible : {latestVersion}"
                    );
                    Console.WriteLine($"Nouvelle version détectée : {latestVersion}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la vérification de WordPress : {ex.Message}");
            }

            // Attendre 30 minutes
            await Task.Delay(TimeSpan.FromMinutes(30));
        }
    }

    // Récupère la dernière version stable de WordPress via l’API.
    private static async Task<string> GetLatestWordPressVersion()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(WordPressApiUrl);
        var json = JObject.Parse(response);

        return json["offers"]?[0]?["current"]?.ToString() ?? "";
    }

    // Gère les messages
    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var latestVersion = await GetLatestWordPressVersion();
        if (update.Message is not { Text: { } messageText })
            return;

        var chatId = update.Message.Chat.Id;
        Console.WriteLine($"Message reçu de {chatId} : {messageText}");

        if (messageText.Equals("bonjour", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "au revoir !",
                cancellationToken: cancellationToken
            );
        }
        if (messageText.Equals("/version", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Version actuelle de WordPress : {latestVersion}",
                cancellationToken: cancellationToken
            );
        }
        if (messageText.Equals("/info", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Ce bot vous envoie un message si il y a une nouvelle version de WordPress qui sort",
                cancellationToken: cancellationToken
            );
        }
        if (messageText.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Bonjour, voici les différentes commandes de ce bot \n/version : Dit la version actuelle \n/info : Description du bot",
                cancellationToken: cancellationToken
            );
        }
    }

    // Gère les erreurs
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
