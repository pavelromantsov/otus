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
            CancellationToken cancellationToken)
        {
            switch (context.CurrentStep)
            {
                case null:
                    {
                        var user = await _userService.GetUserByTelegramUserIdAsync(message.Chat.Id, cancellationToken)?? await RegisterAndGetUser(message.Chat.Id, cancellationToken);
                        context.Data["User"] = user;
                        var lists = await _toDoListService.GetUserLists(user.UserId, cancellationToken);
                        if (lists.Count == 0)
                        {
                            await botClient.SendMessage(message.Chat.Id,"У вас нет списков для удаления.", cancellationToken: cancellationToken);
                            return ScenarioResult.Completed;
                        }
                        var buttons = lists.Select(l => InlineKeyboardButton.WithCallbackData(l.Name,new ToDoListCallbackDto("deletelist", l.Id).ToString())).Chunk(2); 
                        var inlineKeyboard = new InlineKeyboardMarkup(buttons);
                        await botClient.SendMessage(message.Chat.Id,"Выберите список для удаления:",replyMarkup: inlineKeyboard,cancellationToken: cancellationToken);
                        context.CurrentStep = "Approve";
                        await _contextRepository.SetContext(message.Chat.Id, context, cancellationToken);
                        return ScenarioResult.Transition;
                    }

                case "Approve":
                    {
                        if (!context.Data.TryGetValue("Callback", out var rawCb) || rawCb is not CallbackQuery cb)
                        {
                            await botClient.SendMessage(message.Chat.Id,"Не удалось определить выбранный список.", cancellationToken: cancellationToken);
                            return ScenarioResult.Completed;
                        }

                        var dto = ToDoListCallbackDto.FromString(cb.Data!);
                        if (!dto.ToDoListId.HasValue)
                        {
                            await botClient.SendMessage(message.Chat.Id,"Некорректные данные списка.", cancellationToken: cancellationToken);
                            return ScenarioResult.Completed;
                        }

                        var selectedList = await _toDoListService.Get(dto.ToDoListId.Value, cancellationToken);
                        if (selectedList == null)
                        {
                            await botClient.SendMessage(message.Chat.Id, "Список не найден.", cancellationToken: cancellationToken);
                            return ScenarioResult.Completed;
                        }

                        context.Data["SelectedList"] = selectedList;

                        var confirmKeyboard = new InlineKeyboardMarkup(new[]
                        {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Да", "yes"),
                        InlineKeyboardButton.WithCallbackData("❌ Нет", "no")
                    }
                });

                        await botClient.SendMessage(cb.Message!.Chat.Id,$"Подтверждаете удаление списка {selectedList.Name} и всех его задач?", replyMarkup: confirmKeyboard,cancellationToken: cancellationToken);

                        context.CurrentStep = "Delete";
                        await _contextRepository.SetContext(cb.From.Id, context, cancellationToken);
                        return ScenarioResult.Transition;
                    }

                case "Delete":
                    {
                        if (!context.Data.TryGetValue("Callback", out var rawCb) || rawCb is not CallbackQuery cb)
                        {
                            await botClient.SendMessage(message.Chat.Id,"Не удалось получить подтверждение.", cancellationToken: cancellationToken);
                            return ScenarioResult.Completed;
                        }

                        var action = cb.Data;

                        if (action == "yes")
                        {
                            var listToDelete = (ToDoList)context.Data["SelectedList"];

                            var user = (ToDoUser)context.Data["User"];
                            var tasks = await _toDoService.GetByUserIdAndList(user.UserId, listToDelete.Id, cancellationToken);
                            foreach (var taskId in tasks.Select(t => t.Id))
                            {
                                _toDoService.Delete(taskId, cancellationToken);
                            }

                            await _toDoListService.Delete(listToDelete.Id, cancellationToken);

                            await botClient.SendMessage(cb.Message!.Chat.Id, "Список успешно удалён.", cancellationToken: cancellationToken);
                        }
                        else if (action == "no")
                        {
                            await botClient.SendMessage(cb.Message!.Chat.Id, "Удаление отменено.", cancellationToken: cancellationToken);
                        }

                        return ScenarioResult.Completed;
                    }

                default:
                    await botClient.SendMessage(message.Chat.Id,"Что-то пошло не так. Повторите попытку.", cancellationToken: cancellationToken);
                    context.CurrentStep = null;
                    return ScenarioResult.Transition;
            }
        }

        private async Task<ToDoUser> RegisterAndGetUser(long telegramUserId, CancellationToken ct)
        {
            await _userService.RegisterUserAsync(telegramUserId, ct);
            return await _userService.GetUserByTelegramUserIdAsync(telegramUserId, ct);
        }
    }
}
