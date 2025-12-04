using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleBot.Scenarios
{
    public class AddListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _toDoListService;
        private IScenarioContextRepository _contextRepository;
        private readonly ToDoService _toDoService;


        public AddListScenario(IUserService userService, IToDoListService toDoListService, ToDoService toDoService, IScenarioContextRepository contextRepository)
        {
            _userService = userService;
            _toDoListService = toDoListService;
            _toDoService = toDoService;
            _contextRepository = contextRepository;
        }
        bool IScenario.CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.AddList;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoList? list, CancellationToken ct)
        {
            switch (context.CurrentStep)
            {
                case null:
                    var user = await _userService.GetUserByTelegramUserIdAsync(message.Chat.Id, ct);
                    context.Data["User"] = user;
                    await botClient.SendMessage(message.Chat.Id, "Введите название списка:", cancellationToken: ct);
                    context.CurrentStep = "Name";
                    return ScenarioResult.Transition;

                case "Name":
                    var name = message.Text;
                    var userObj = (ToDoUser)context.Data["User"];
                    if(userObj == null)
                    {
                        userObj = await _userService.GetUserByTelegramUserIdAsync(message.Chat.Id, ct); // Регистрируем пользователя заново, если нужно
                        context.Data["User"] = userObj;
                    }
                    await _toDoListService.Add(userObj, name, ct);
                    await botClient.SendMessage(message.Chat.Id, "Список успешно создан.", cancellationToken: ct);
                    return ScenarioResult.Completed;


                default:
                    await botClient.SendMessage(message.Chat.Id, "Что-то пошло не так. Повторите попытку.", cancellationToken: ct);
                    context.CurrentStep = null;
                    return ScenarioResult.Transition;
            }
        }
    }
}
