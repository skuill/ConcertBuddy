using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

public class SearchCallbackCommand : AbstractCommand<Message?, CallbackQuery>
{
    private const string CurrentCommand = CommandList.COMMAND_SEARCH;

    private readonly ILogger<SearchCallbackCommand> _logger;

    public SearchCallbackCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ILogger<SearchCallbackCommand> logger, CallbackQuery data)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _logger = logger;
    }

    public override async Task<Message?> ExecuteAsync()
    {
        _logger?.LogDebug("Handle [{CurrentCommand}] callback command: [{CallbackData}]", CurrentCommand, Data.Data);

        if (Data == null)
        {
            _logger?.LogError("Command: [{CurrentCommand}]. Unexpected case. [Data] field is null.", CurrentCommand);
            return null;
        }
        if (Data!.Message == null)
        {
            _logger?.LogError("Command: [{CurrentCommand}]. Unexpected case. [Data.Message] field is null.", CurrentCommand);
        }

        var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CurrentCommand, out string errorMessage);
        if (!isValidQuery)
        {
            _logger?.LogError("Command: [{CurrentCommand}]. Error: {ErrorMessage}", CurrentCommand, errorMessage);
            Result = CommandResult.Error;
            await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
            return null;
        }

        _logger?.LogDebug("Command: [{CurrentCommand}]. Handle search command: [{CallbackData}]", CurrentCommand, Data.Data);

        string replyText = string.Empty;
        string artistName = string.Empty;
        int offset = 0;
        int limit = 5;

        var splitMessage = Data.GetSplitMessageText();

        if (splitMessage.Count() == 1)
        {
            replyText = $"Please pass artist's name as a parameter. For example: [{CurrentCommand} The Beatles]";

            return await TelegramBotClient.SendMessage(
                chatId: Data.Message!.Chat.Id,
                text: replyText,
                replyMarkup: new ReplyKeyboardRemove());
        }

        var parameters = Data.GetParametersFromMessageText(CurrentCommand);
        offset = int.Parse(parameters[0]);
        limit = int.Parse(parameters[1]);
        artistName = String.Join(' ', parameters.Skip(2));

        var artists = await MusicSearcherClient.SearchArtistsByName(artistName, limit: limit, offset: offset);

        if ((artists == null || !artists.Any()) && offset == 0)
        {
            if (offset == SearchConstants.SEARCH_ARTISTS_OFFSET_DEFAULT)
            {
                _logger?.LogInformation("Command: [{CurrentCommand}]. Can't find artist [{ArtistName}]", CurrentCommand, artistName);
                return await SendNotFoundAsync(Data.Message!.Chat.Id);
            }

            InlineKeyboardMarkup navigationKeyboard = InlineKeyboardMarkup.Empty()
                .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SEARCH, artistName, offset, limit: limit);

            replyText = "Nothing found here 😕! Try another search or go back";
            return await TelegramBotClient.EditMessageText(
                chatId: Data.Message!.Chat.Id,
                messageId: Data.Message.MessageId,
                text: replyText,
                replyMarkup: navigationKeyboard);
        }

        bool isForwardNavigationEnabled = artists!.Count() == limit;
        InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistsInlineKeyboard(artists!, offset + 1)
            .WithNavigationButtons(
                CommandList.CALLBACK_DATA_FORMAT_SEARCH,
                artistName,
                offset,
                limit: limit,
                isForwardNavigationEnabled: isForwardNavigationEnabled);

        replyText = "Choose the correct artist 💭:";

        return await TelegramBotClient.EditMessageText(
            chatId: Data.Message!.Chat.Id,
            messageId: Data.Message.MessageId,
            text: replyText,
            replyMarkup: inlineKeyboard);
    }
}
