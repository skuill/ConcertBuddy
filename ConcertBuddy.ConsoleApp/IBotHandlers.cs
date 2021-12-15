﻿using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp
{
    public interface IBotHandlers
    {
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken);

        Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
    }
}