using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
    private static readonly HashSet<long> SubscribedUsers = new();
    static Dictionary<long, string> userVersions = new Dictionary<long, string>();
    private static Dictionary<string, string> lastKnownVersions = new();

    private static readonly string filePathSubscribe = "./subscribed_users.json";
    private static readonly string filePathlastKnowVersions = "./lastKnowVersions.json";

    private static readonly string filePathVersion = "./version_users.json";


    static void SaveData<T>(string filePath, T data)
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    static void LoadDataSubscriber()
    {
        if (File.Exists(filePathSubscribe))
        {
            string json = File.ReadAllText(filePathSubscribe);
            if (!string.IsNullOrWhiteSpace(json)){
            var loadedData = JsonSerializer.Deserialize<HashSet<long>>(json);
            if (loadedData != null)
            {
                SubscribedUsers.Clear();  // Supprime les anciennes valeurs (si besoin)
                foreach (var user in loadedData)
                {
                    SubscribedUsers.Add(user);  // Ajoute les valeurs chargées
                    }
                }
            }
        }
    }
    static void LoadDictionaryData(string filePath, ref Dictionary<long, string> data)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var loadedData = JsonSerializer.Deserialize<Dictionary<long, string>>(json);
                if (loadedData != null)
                {
                    data.Clear();
                    foreach (var kvp in loadedData)
                    {
                        data[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }
    static void LoadDictionaryDatastring(string filePath, ref Dictionary<string, string> data)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var loadedData = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loadedData != null)
                {
                    data.Clear();
                    foreach (var kvp in loadedData)
                    {
                        data[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }
    static async Task Main()
    {
        LoadDataSubscriber();
        LoadDictionaryData(filePathVersion, ref userVersions);
        LoadDictionaryDatastring(filePathlastKnowVersions, ref lastKnownVersions);
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
        Console.WriteLine("Users subscribed: " + string.Join(", ", SubscribedUsers));
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
                foreach (var kvp in userVersions)
                {
                    long userId = kvp.Key;
                    string selectedVersion = kvp.Value;

                    if (double.TryParse(selectedVersion, out var version))
                    {
                        var latest = await GetLatestWordPressSelectedVersion(version);
                        if (!string.IsNullOrEmpty(latest))
                        {
                            if (!lastKnownVersions.TryGetValue(selectedVersion, out var lastStored) || lastStored != latest)
                {
                                lastKnownVersions[selectedVersion] = latest;
                                SaveData(filePathlastKnowVersions, lastKnownVersions);
                        await botClient.SendMessage(
                            chatId: userId,
                                    text: $"New WordPress version detected for branch {version} : {latest}"
                    );
                            }
                        }
                    }
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
    private static async Task<string> GetLatestWordPressSelectedVersion(double Selected)
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
                if (!string.IsNullOrEmpty(current) && current.StartsWith(Selected+"."))
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
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            await HandleCallbackQuery(botClient, update.CallbackQuery);
            return; // Très important : on ne continue pas plus loin
        }
        var latestVersion = await GetLatestWordPressVersion();
        var latestVersionSelected = await GetLatestWordPressVersion();
        if (update.Message is not { Text: { } messageText })
            return;

        var chatId = update.Message.Chat.Id;
        Console.WriteLine($"Message received from {chatId} : {messageText}");

        if (messageText.Equals("/Subscribe", StringComparison.OrdinalIgnoreCase))
            {
                if (SubscribedUsers.Add(chatId)) // Add the user if not already
                {
                    SaveData(filePathSubscribe, SubscribedUsers);
                    await Choose_Version(botClient, chatId);
                    Console.WriteLine("L'utilisateur " + chatId + "a été ajouter au fichier json");
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "You are now subscribed to WordPress version updates!",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "You are already subscribed.",
                        cancellationToken: cancellationToken
                    );
                }
            }

        if (messageText.Equals("/lastversion", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: $"The current version of WordPress : {latestVersion}",
                cancellationToken: cancellationToken
            );
        }
        if (messageText.Equals("/version", StringComparison.OrdinalIgnoreCase))
        {
            if (userVersions.TryGetValue(chatId, out var selectedVersionStr) && 
                double.TryParse(selectedVersionStr, out var selectedVersion))
            {
                var latestSelected = await GetLatestWordPressSelectedVersion(selectedVersion);
                if (!string.IsNullOrEmpty(latestSelected))
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"The latest version for WordPress {selectedVersionStr} is: {latestSelected}",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"❌ Couldn't find any release for WordPress branch {selectedVersionStr}.",
                        cancellationToken: cancellationToken
                    );
                }
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠️ You haven't selected a WordPress version yet. Use /SelectVersion to choose one.",
                    cancellationToken: cancellationToken
                );
            }
        }
        if (messageText.Equals("/myVersion", StringComparison.OrdinalIgnoreCase))
        {
            if (userVersions.TryGetValue(chatId, out var selectedVersion))
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Your selected WordPress version: {selectedVersion}",
                    cancellationToken: cancellationToken
                );
            }
            else
        {
            await botClient.SendMessage(
                chatId: chatId,
                    text: "You haven't selected a WordPress version yet. Use /SelectVersion to choose one.",
                cancellationToken: cancellationToken
            );
            }
        }
        if (messageText.Equals("/SelectVersion", StringComparison.OrdinalIgnoreCase))
        {
            await Choose_Version(botClient, chatId);
        }
        if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            string startMessage = 
                "👋 *Welcome to the WordPress Update Bot!*\n\n" +
                "This bot will notify you whenever a *new version of WordPress* is released for the branch you choose.\n\n" +
                "📖 *Here’s how to get started:*\n\n" +
                "1️⃣ *Subscribe* to receive notifications for WordPress updates. Use the command `/Subscribe` to subscribe.\n" +
                "2️⃣ After subscribing, you'll be prompted to select a *WordPress version* (e.g., 6.7, 6.6, etc.).\n" +
                "3️⃣ Once you've selected your version, you'll receive notifications whenever there's a new release for your selected branch.\n\n" +
                "Type */help* to see all the commands.\n\n"+
                "⚙️ *Notifications:* The bot checks every 30 minutes for new versions and sends you a message if there's an update.\n\n" +
                "🙌 Thank you for using this bot! Let's keep your WordPress up to date!";
                await botClient.SendMessage(
                    chatId: chatId,
                    text: startMessage,
                    cancellationToken: cancellationToken,
                    parseMode: ParseMode.Markdown
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
    private static async Task Choose_Version(ITelegramBotClient botClient, long chatId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("6.7", "6.7"), InlineKeyboardButton.WithCallbackData("6.6", "6.6") },
                new[] { InlineKeyboardButton.WithCallbackData("6.5", "6.5"), InlineKeyboardButton.WithCallbackData("6.4", "6.4") },
                new[] { InlineKeyboardButton.WithCallbackData("6.3", "6.3"), InlineKeyboardButton.WithCallbackData("6.2", "6.2") },
                new[] { InlineKeyboardButton.WithCallbackData("6.1", "6.1")}
            });

        await botClient.SendMessage(
            chatId: chatId,
            text: "Choose an branch to be notify (You must select a version to receive updates):",
            replyMarkup: inlineKeyboard
        );
    }

    private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        string selectedVersion = callbackQuery.Data;
        long userId = callbackQuery.From.Id;


        // Convertie en double
        if (double.TryParse(selectedVersion, out var selected))
        {
            Console.WriteLine($"User {userId} select the version {selectedVersion}");
        }

        userVersions[userId] = selectedVersion;
        SaveData(filePathVersion, userVersions);

        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: $"You select the version {selectedVersion}",
            showAlert: false
        );

        await botClient.SendMessage(
            chatId: callbackQuery.Message.Chat.Id,
            text: $"Version save : {selectedVersion}"
        );
        await botClient.SendMessage(
            chatId: callbackQuery.Message.Chat.Id,
            text: $"You are now subscribed to this bot and will be notify when a new version of the branch {selectedVersion} is out."
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