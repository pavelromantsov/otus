using System.Globalization;
using System.Threading;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Exceptions;
using ConsoleBot.Core.Services;
using ConsoleBot.TelegramBot.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleBot.Scenarios
{
    public class AddTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoListService _toDoListService;
        private readonly IScenarioContextRepository _contextRepository;
        public AddTaskScenario(IUserService userService, IToDoService toDoService, IScenarioContextRepository contextRepository, IToDoListService toDoListService)
        {
            _userService = userService;
            _toDoService = toDoService;
            _toDoListService = toDoListService;
            _contextRepository = contextRepository;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddTask;

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoList? list, CancellationToken ct)
        {
            var user = await _userService.GetUserByTelegramUserIdAsync(message.Chat.Id, ct);
            if (user == null)
            {
                await botClient.SendMessage(message.Chat.Id, "Вы не зарегистрированы.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            switch (context.CurrentStep)
            {
                case null:
                    context.Data["User"] = user;
                    await botClient.SendMessage(message.Chat.Id, "Введите название задачи:", cancellationToken: ct);
                    context.CurrentStep = "Name";
                    await _contextRepository.SetContext(message.Chat.Id, context, ct);
                    return ScenarioResult.Transition;

                case "Name":
                    try
                    {
                        var newTaskName = message.Text?.Trim();
                            await _toDoService.ValidateStringAsync(newTaskName, ct);

                        context.Data["TaskName"] = newTaskName;
                        await SelectListStep(botClient, context, message, user, ct);
                        context.CurrentStep = "SelectList";
                        await _contextRepository.SetContext(message.Chat.Id, context, ct);
                        return ScenarioResult.Transition;
                    }
                    catch (DuplicateTaskException ex)
                    {
                        await botClient.SendMessage(message.Chat.Id, ex.Message + "\nВведите другое название задачи:", cancellationToken: ct);
                        context.CurrentStep = "Name";
                        await _contextRepository.SetContext(message.Chat.Id, context, ct);
                        return ScenarioResult.Transition;
                    }

                case "Deadline":
                    try
                    {
                        if (!DateTime.TryParseExact(message.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline))
                        {
                            await botClient.SendMessage(message.Chat.Id, "Ошибка: неверный формат даты. Введите в формате dd.MM.yyyy", cancellationToken: ct);
                            return ScenarioResult.Transition;
                        }

                        var toDoUser = (ToDoUser)context.Data["User"];
                        var taskName = (string)context.Data["TaskName"];

                        ToDoList? selectedList = null;
                        if (context.Data.TryGetValue("SelectedListId", out var rawListId) && rawListId is Guid guid)
                        {
                            selectedList = await _toDoListService.Get(guid, ct);
                        }

                        await _toDoService.AddAsync(toDoUser, taskName, deadline, selectedList, ct);
                        await botClient.SendMessage(message.Chat.Id, "Задача успешно добавлена.", cancellationToken: ct);
                        return ScenarioResult.Completed;
                    }
                    catch (DuplicateTaskException ex)
                    {
                        await botClient.SendMessage(message.Chat.Id,
                            ex.Message + "\nВведите другое название задачи:",
                            cancellationToken: ct);

                        context.CurrentStep = "Name";
                        await _contextRepository.SetContext(message.Chat.Id, context, ct);
                        return ScenarioResult.Transition;
                    }

                default:
                    // на случай неожиданного значения шага
                    await botClient.SendMessage(message.Chat.Id,
                        "Что-то пошло не так. Начнём с начала. Введите название задачи:",
                        cancellationToken: ct);
                    context.CurrentStep = "Name";
                    await _contextRepository.SetContext(message.Chat.Id, context, ct);
                    return ScenarioResult.Transition;
            }
        }

        private async Task SelectListStep(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoUser user, CancellationToken ct)
        {
            var lists = await _toDoListService.GetUserLists(user.UserId, ct);
            var rows = new List<InlineKeyboardButton[]>
        {
            // сначала "Без списка"
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "📌 Без списка",
                    new ToDoListCallbackDto("selectlist", null).ToString())
            }
        };

            foreach (var l in lists)
            {
                rows.Add(new[]
                {
                InlineKeyboardButton.WithCallbackData(
                    l.Name,
                    new ToDoListCallbackDto("selectlist", l.Id).ToString())
            });
            }

            await botClient.SendMessage(message.Chat.Id, "Выберите список:",replyMarkup: new InlineKeyboardMarkup(rows),cancellationToken: ct);
        }
        public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, ScenarioContext context, CallbackQuery callbackQuery, CancellationToken ct)
        {
            var dto = ToDoListCallbackDto.FromString(callbackQuery.Data!);

            if (dto.Action == "selectlist")
            {
                if (dto.ToDoListId.HasValue)
                {
                    context.Data["SelectedListId"] = dto.ToDoListId.Value;
                }
                else
                {
                    context.Data.Remove("SelectedListId");
                }

                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Список выбран.", cancellationToken: ct);
                context.CurrentStep = "Deadline";
                await _contextRepository.SetContext(callbackQuery.From.Id, context, ct);
                await botClient.SendMessage(callbackQuery.Message!.Chat.Id, "Введите дедлайн в формате dd.MM.yyyy:", cancellationToken: ct);
            }
     
        }   
    }
}

