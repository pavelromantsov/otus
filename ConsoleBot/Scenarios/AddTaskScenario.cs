using System.Globalization;
using System.Threading;
using ConsoleBot.Core.Entities;
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

    public AddTaskScenario(IUserService userService, IToDoService toDoService, IToDoListService toDoListService, IScenarioContextRepository contextRepository)
    {
        _userService = userService;
        _toDoService = toDoService;
        _toDoListService = toDoListService;
        _contextRepository = contextRepository;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddTask;

    public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoList? list, CancellationToken cancellationToken)
    {
        // Проверка регистрации пользователя
        var user = await _userService.GetUserByTelegramUserIdAsync(message.From.Id, cancellationToken);
        if (user == null)
        {
            await botClient.SendMessage(message.From.Id, "Вы не зарегистрированы.", cancellationToken: cancellationToken);
            return ScenarioResult.Completed;
        }

        switch (context.CurrentStep)
        {
            case null:
                // Начало сценария: запрос названия задачи
                await botClient.SendMessage(message.From.Id, "Введите название задачи:", cancellationToken: cancellationToken);
                context.CurrentStep = "Name";
                return ScenarioResult.Transition;

            case "Name":
                // Получаем название задачи и переходим к выбору списка
                var taskName = message.Text;
                context.Data["TaskName"] = taskName;
                await SelectListStep(botClient, context, message, user, cancellationToken);
                context.CurrentStep = "SelectList";
                return ScenarioResult.Transition;

            case "SelectList":
                // Выбор списка произошел, продолжаем сценарий
                var selectedListId = (Guid?)context.Data["SelectedListId"];
                var taskName = (string)context.Data["TaskName"];
                await AddTaskToList(botClient, context, user, taskName, selectedListId, cancellationToken);
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Invalid;
        }
    }

    private async Task SelectListStep(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoUser user, CancellationToken cancellationToken)
    {
        // Получаем все списки пользователя
        var lists = await _toDoListService.GetUserLists(user.UserId, cancellationToken);

        // Формируем inline-клавиатуру
        var rows = new List<IEnumerable<InlineKeyboardButton>>();
        foreach (var list in lists)
        {
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(list.Name, ToDoListCallbackDto.Create("selectlist", list.Id)) });
        }
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📌 Без списка", ToDoListCallbackDto.Create("selectlist", null)) });

        // Отправляем сообщение с клавиатурой
        await botClient.SendMessage(message.Chat.Id, "Выберите список:", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: cancellationToken);
    }

    private async Task AddTaskToList(ITelegramBotClient botClient, ScenarioContext context, ToDoUser user, string taskName, Guid? listId, CancellationToken cancellationToken)
    {
        // Добавляем задачу в список
        var deadline = DateTime.Now.AddDays(7); // Ставим дедлайн через неделю
        await _toDoService.AddAsync(user, taskName, deadline, listId, cancellationToken);
        await botClient.SendMessage(user.TelegramUserId, "Задача успешно добавлена.", cancellationToken: cancellationToken);
    }

    private async Task HandleCallbackQuery(ITelegramBotClient botClient, ScenarioContext context, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Парсим данные из callback-запроса
        var dto = ToDoListCallbackDto.FromString(callbackQuery.Data);

        if (dto.Action == "selectlist")
        {
            // Пользователь выбрал список
            context.Data["SelectedListId"] = dto.ToDoListId;
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "Список выбран.", cancellationToken: cancellationToken);
        }
    }

    private long GetChatId(Update update)
    {
        return update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;
    }

    private long GetUserId(Update update)
    {
        return update.Message?.From.Id ?? update.CallbackQuery.From.Id;
    }
}



    //public class AddTaskScenario: IScenario
    //{
    //    private readonly IUserService _userService;
    //    private readonly IToDoService _toDoService;
    //    private readonly IToDoListService _toDoListService;
    //    private IScenarioContextRepository _contextRepository;
    //    private readonly Update update;

    //    public AddTaskScenario(IUserService userService, IToDoService toDoService, IScenarioContextRepository contextRepository, ToDoListService toDoListService)
    //    {
    //        _userService = userService;
    //        _toDoService = toDoService;
    //        _toDoListService = toDoListService;
    //        _contextRepository = contextRepository;
    //    }

    //    public bool CanHandle(ScenarioType scenario)
    //    {
    //        return scenario == ScenarioType.AddTask;
    //    }
    //    public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoList? list, CancellationToken cancellationToken)
    //    {

    //        var callbackQuery = new CallbackQuery();

    //        // Проверка регистрации пользователя
    //        var user = await _userService.GetUserByTelegramUserIdAsync(message.From.Id, cancellationToken);
    //        if (user == null)
    //        {
    //            await botClient.SendMessage(message.From.Id, "Вы не зарегистрированы.", cancellationToken: cancellationToken);
    //            return ScenarioResult.Completed;
    //        }

    //        switch (context.CurrentStep)
    //        {
    //            case null:
    //                // Начало сценария: запрос названия задачи
    //                await botClient.SendMessage(message.From.Id, "Введите название задачи:", cancellationToken: cancellationToken);
    //                context.CurrentStep = "Name";
    //                return ScenarioResult.Transition;

    //            case "Name":
    //                // Получаем название задачи и переходим к выбору списка
    //                var taskName = message.Text;
    //                context.Data["TaskName"] = taskName;
    //                await SelectListStep(botClient, context, message, user, cancellationToken);
    //                context.CurrentStep = "SelectList";
    //                return ScenarioResult.Transition;

    //            case "SelectList":
    //                // Выбор списка произошел, продолжаем сценарий
    //                var selectedListId = (Guid?)context.Data["SelectedListId"];
    //                var taskSelectList = (string)context.Data["TaskName"];
    //                await AddTaskToList(botClient, context, user, taskSelectList, list, cancellationToken);
    //                return ScenarioResult.Completed;

    //            default:
    //                return ScenarioResult.Completed;
    //        }
    //    }

    //    private async Task SelectListStep(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoUser user, CancellationToken cancellationToken)
    //    {
    //        // Получаем все списки пользователя
    //        var lists = await _toDoListService.GetUserLists(user.UserId, cancellationToken);

    //        // Формируем inline-клавиатуру
    //        var rows = new List<IEnumerable<InlineKeyboardButton>>();
    //        foreach (var list in lists)
    //        {
    //            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(list.Name,new ToDoListCallbackDto("selectlist", list.Id).ToString()) });
    //        }
    //        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📌 Без списка", new ToDoListCallbackDto("selectlist", null).ToString()) });

    //        // Отправляем сообщение с клавиатурой
    //        await botClient.SendMessage(message.Chat.Id, "Выберите список:", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: cancellationToken);
    //    }

    //    private async Task AddTaskToList(ITelegramBotClient botClient, ScenarioContext context, ToDoUser user, string taskName, ToDoList listId, CancellationToken cancellationToken)
    //    {
    //        // Добавляем задачу в список
    //        var deadline = DateTime.Now.AddDays(7); // Ставим дедлайн через неделю
    //        await _toDoService.AddAsync(user, taskName, deadline, listId, cancellationToken);
    //        await botClient.SendMessage(user.TelegramUserId, "Задача успешно добавлена.", cancellationToken: cancellationToken);
    //    }

    //    private async Task HandleCallbackQuery(ITelegramBotClient botClient, ScenarioContext context, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    //    {
    //        // Парсим данные из callback-запроса
    //        var dto = ToDoListCallbackDto.FromString(callbackQuery.Data);

    //        if (dto.Action == "selectlist")
    //        {
    //            // Пользователь выбрал список
    //            context.Data["SelectedListId"] = dto.ToDoListId;
    //            await botClient.AnswerCallbackQuery(callbackQuery.Id, "Список выбран.", cancellationToken: cancellationToken);
    //        }
    //    }

    //    private long GetChatId(Update update)
    //    {
    //        return update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;
    //    }

    //    private long GetUserId(Update update)
    //    {
    //        return update.Message?.From.Id ?? update.CallbackQuery.From.Id;
    //    }

    //}
    //public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoList? list, CancellationToken ct)
    //{
    //    switch (context.CurrentStep)
    //    {
    //        case null:
    //            var user = await _userService.GetUserByTelegramUserIdAsync(message.From.Id, ct);
    //            context.Data["User"] = user;
    //            await botClient.SendMessage(message.Chat.Id, "Введите название задачи:", cancellationToken: ct);
    //            context.CurrentStep = "Name";
    //            await _contextRepository.SetContext(message.From.Id, context, ct);
    //            return ScenarioResult.Transition;

    //        case "Name":
    //            // Получаем название задачи и переходим к выбору списка
    //            var taskName = message.Text;
    //            context.Data["TaskName"] = taskName;
    //            await SelectListStep(botClient, context, user, ct);
    //            context.CurrentStep = "SelectList";
    //            return ScenarioResult.Transition;

    //        case "Deadline":
    //            if (DateTime.TryParseExact(message.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline))
    //            {
    //                var _user = (ToDoUser)context.Data["User"];
    //                var _name = (string)context.Data["Name"];
    //                await _toDoService.AddAsync(_user, _name, deadline, list, ct);
    //                await botClient.SendMessage(message.Chat.Id, "Задача успешно добавлена.", cancellationToken: ct);
    //                return ScenarioResult.Completed;
    //            }
    //            else
    //            {
    //                await botClient.SendMessage(message.Chat.Id, "Ошибка: неверный формат даты. Повторите ввод.", cancellationToken: ct);
    //                return ScenarioResult.Transition;
    //            }

    //        default:
    //            await botClient.SendMessage(message.Chat.Id, "Что-то пошло не так. Повторите попытку.", cancellationToken: ct);
    //            context.CurrentStep = null;
    //            return ScenarioResult.Transition;
    //    }
    //}
    //private async Task SelectListStep(ITelegramBotClient botClient, ScenarioContext context, ToDoUser user, CancellationToken cancellationToken)
    //{
    //    // Получаем все списки пользователя
    //    var lists = await _toDoListService.GetUserLists(user.UserId, cancellationToken);

    //    // Формируем inline-клавиатуру
    //    var rows = new List<IEnumerable<InlineKeyboardButton>>();
    //    foreach (var list in lists)
    //    {
    //        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(list.Name, ToDoListCallbackDto.Create("selectlist", list.Id)) });
    //    }
    //    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📌 Без списка", ToDoListCallbackDto.Create("selectlist", null)) });

    //    // Отправляем сообщение с клавиатурой
    //    await botClient.SendMessage(context.ChatId, "Выберите список:", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: cancellationToken);
    //}
    //private async Task AddTaskToList(ITelegramBotClient botClient, ScenarioContext context, ToDoUser user, string taskName, Guid? listId, CancellationToken cancellationToken)
    //{
    //    // Добавляем задачу в список
    //    var deadline = DateTime.Now.AddDays(7); // По умолчанию ставим неделю вперед
    //    await _toDoService.AddAsync(user, taskName, deadline, listId, cancellationToken);
    //    await botClient.SendMessage(context.ChatId, "Задача успешно добавлена.", cancellationToken: cancellationToken);
    //}

    //private async Task HandleCallbackQuery(ITelegramBotClient botClient, ScenarioContext context, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    //{
    //    // Парсим данные из callback-запроса
    //    var dto = ToDoListCallbackDto.FromString(callbackQuery.Data);

    //    if (dto.Action == "selectlist")
    //    {
    //        // Пользователь выбрал список
    //        context.Data["SelectedListId"] = dto.ToDoListId;
    //        await botClient.AnswerCallbackQuery(callbackQuery.Id, "Список выбран.", cancellationToken: cancellationToken);
    //    }
    //}

    //private long GetChatId(Update update)
    //{
    //    return update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;
    //}

    //private long GetUserId(Update update)
    //{
    //    return update.Message?.From.Id ?? update.CallbackQuery.From.Id;
    //}
}

