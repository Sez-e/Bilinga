namespace bili;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;

public class TGbot
{
    private readonly string BotToken;
    private readonly TelegramBotClient botClient;
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly string UsersFile = "users.txt";

    public TGbot(string botToken)
    {
        BotToken = botToken;
        botClient = new TelegramBotClient(BotToken);
    }

    public async Task Start()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() 
        };
        
        if (!File.Exists(UsersFile))
        {
            File.Create(UsersFile);
            Console.WriteLine("Файл пользователей создан.");
        }

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Бот запущен...");
        _ = Task.Run(() => ScheduleMessages(cts.Token));
    }

    public void Stop()
    {
        cts.Cancel();
        Console.WriteLine("Бот остановлен.");
    }

    private async Task ScheduleMessages(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var next9AM = now.Date.AddHours(9);
            var next9PM = now.Date.AddHours(21);

            if (now > next9AM) next9AM = next9AM.AddDays(1);
            if (now > next9PM) next9PM = next9PM.AddDays(1);

            var nextNotification = now < next9AM ? next9AM : next9PM;
            var delay = nextNotification - now;

            Console.WriteLine($"Следующее уведомление в {nextNotification}");

            try
            {
                await Task.Delay(delay, token);
                await SendNotifications();
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Бот остановлен.");
                break;
            }
        }
    }

    private async Task SendNotifications()
    {
        var users = LoadUsers();
        foreach (var userId in users)
        {
            try
            {
                await botClient.SendTextMessageAsync(userId, "Ты уже позанимался английским? Пора сделать это!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения пользователю {userId}: {ex.Message}");
            }
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update,
        CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
            return;

        var message = update.Message;
        var chatId = message.Chat.Id;

        if (message.Text!.ToLower() == "/start")
        {
            AddUser(chatId);
            await botClient.SendTextMessageAsync(chatId, "Привет! Теперь ты подписан на напоминания в 9:00 и 21:00.");
        }
        else if (message.Text.ToLower() == "/stop")
        {
            RemoveUser(chatId);
            await botClient.SendTextMessageAsync(chatId, "Ты отписался от напоминаний.");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    private HashSet<long> LoadUsers()
    {
        var users = new HashSet<long>();
        foreach (var line in File.ReadAllLines(UsersFile))
        {
            if (long.TryParse(line, out long userId))
                users.Add(userId);
        }

        return users;
    }

    private void AddUser(long chatId)
    {
        var users = LoadUsers();
        if (users.Add(chatId))
        {
            File.AppendAllText(UsersFile, chatId + Environment.NewLine);
            Console.WriteLine($"Добавлен новый пользователь: {chatId}");
        }
    }

    private void RemoveUser(long chatId)
    {
        var users = LoadUsers();
        if (users.Remove(chatId))
        {
            File.WriteAllLines(UsersFile, users.Select(u => u.ToString()));
            Console.WriteLine($"Пользователь {chatId} удалён.");
        }
    }
}