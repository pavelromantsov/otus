using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using ConsoleBot.TelegramBot.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleBot.Scenarios
{
    public class DeleteListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _toDoListService;
        private readonly IToDoService _toDoService;
        private readonly IScenarioContextRepository _contextRepository;

        public DeleteListScenario(
            IUserService userService,
            IToDoListService toDoListService,
            IToDoService toDoService,
            IScenarioContextRepository contextRepository)
        {
            _userService = userService;
            _toDoListService = toDoListService;
            _toDoService = toDoService;
            _contextRepository = contextRepository;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteList;

        public async Task<ScenarioResult> HandleMessageAsync(
                    ITelegramBotClient botClient,
                    ScenarioContext context,
                    Message message,
                    ToDoList? list,
                    CancellationToken ct)
        {
            switch (context.CurrentStep)
            {
                case null:
                    {
                        var user = await _userService
                            .GetUserByTelegramUserIdAsync(message.Chat.Id, ct)
                            ?? await RegisterAndGetUser(message.Chat.Id, message.Chat.Username, ct);

                        context.Data["User"] = user;

                        var lists = await _toDoListService.GetUserLists(user.UserId, ct);
                        if (lists.Count == 0)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "У вас нет списков для удаления.",
                                cancellationToken: ct);

                            return ScenarioResult.Completed;
                        }

                        var buttons = lists
                            .Select(l => InlineKeyboardButton.WithCallbackData(
                                l.Name,
                                new ToDoListCallbackDto("deletelist", l.Id).ToString()))
                            .Chunk(2);

                        var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                        await botClient.SendMessage(
                                message.Chat.Id,
                                "Выберите список для удаления:",
                                replyMarkup: inlineKeyboard,
                                cancellationToken: ct);

                        context.CurrentStep = "Approve";
                        await _contextRepository.SetContext(message.Chat.Id, context, ct);
                        return ScenarioResult.Transition;
                    }

                case "Approve":
                    {
                        if (!context.Data.TryGetValue("Callback", out var rawCb) || rawCb is not CallbackQuery cb)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "Не удалось определить выбранный список.",
                                cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        var dto = ToDoListCallbackDto.FromString(cb.Data!);
                        if (!dto.ToDoListId.HasValue)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "Некорректные данные списка.",
                                cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        var selectedList = await _toDoListService.Get(dto.ToDoListId.Value, ct);
                        if (selectedList == null)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "Список не найден.",
                                cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        context.Data["SelectedList"] = selectedList;

                        var confirmKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(
                                    "✅ Да",
                                    new ToDoListCallbackDto("deletelist_yes", selectedList.Id).ToString()),
                                InlineKeyboardButton.WithCallbackData(
                                    "❌ Нет",
                                    new ToDoListCallbackDto("deletelist_no", selectedList.Id).ToString())
                            }
                        });

                        await botClient.SendMessage(
                            cb.Message!.Chat.Id,
                            $"Подтверждаете удаление списка {selectedList.Name} и всех его задач?",
                            replyMarkup: confirmKeyboard,
                            cancellationToken: ct);

                        context.CurrentStep = "Delete";
                        await _contextRepository.SetContext(cb.From.Id, context, ct);
                        return ScenarioResult.Transition;
                    }

                case "Delete":
                    {
                        if (!context.Data.TryGetValue("Callback", out var rawCb) || rawCb is not CallbackQuery cb)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "Не удалось получить подтверждение.",
                                cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        var dto = ToDoListCallbackDto.FromString(cb.Data!);
                        var action = dto.Action.ToLowerInvariant();

                        if (action == "deletelist_yes")
                        {
                            if (!context.Data.TryGetValue("SelectedList", out var rawList) ||
                                rawList is not ToDoList listToDelete)
                            {
                                await botClient.SendMessage(
                                    cb.Message!.Chat.Id,
                                    "Список не найден в контексте.",
                                    cancellationToken: ct);
                                return ScenarioResult.Completed;
                            }

                            if (!context.Data.TryGetValue("User", out var rawUser) ||
                                rawUser is not ToDoUser user)
                            {
                                await botClient.SendMessage(
                                    cb.Message!.Chat.Id,
                                    "Пользователь не найден в контексте.",
                                    cancellationToken: ct);
                                return ScenarioResult.Completed;
                            }

                            var tasks = await _toDoService.GetByUserIdAndList(user.UserId, listToDelete.Id, ct);
                            foreach (var taskId in tasks.Select(t => t.Id))
                            {
                                _toDoService.Delete(taskId, ct);
                            }

                            await _toDoListService.Delete(listToDelete.Id, ct);

                            await botClient.SendMessage(
                                cb.Message!.Chat.Id,
                                "Список успешно удалён.",
                                cancellationToken: ct);
                        }
                        else if (action == "deletelist_no")
                        {
                            await botClient.SendMessage(
                                cb.Message!.Chat.Id,
                                "Удаление отменено.",
                                cancellationToken: ct);
                        }

                        return ScenarioResult.Completed;
                    }

                default:
                    {
                        await botClient.SendMessage(
                            message.Chat.Id,
                            "Что-то пошло не так. Повторите попытку.",
                            cancellationToken: ct);

                        context.CurrentStep = null;
                        return ScenarioResult.Transition;
                    }
            }
        }

        private async Task<ToDoUser> RegisterAndGetUser(long telegramUserId, string telegramUserName, CancellationToken ct)
        {
            await _userService.RegisterUserAsync(telegramUserId, telegramUserName, ct);
            return await _userService.GetUserByTelegramUserIdAsync(telegramUserId, ct);
        }
    }
}
