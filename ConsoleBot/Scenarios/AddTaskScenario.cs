using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleBot.Scenarios
{
    public class AddTaskScenario: IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;

        public AddTaskScenario(IUserService userService, IToDoService toDoService)
        {
            _userService = userService;
            _toDoService = toDoService;
        }

        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.AddTask;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient botClient, ScenarioContext context, Message message, DateTime deadLine, CancellationToken ct)
        {

            switch (context.CurrentStep)
            {
                case null:
                    var user = await _userService.GetUserByTelegramUserIdAsync(message.From.Id, ct);
                    context.Data["User"] = user;
                    await botClient.SendMessage(message.Chat.Id, "Введите название задачи:", cancellationToken: ct);
                    context.CurrentStep = "Name";
                    return ScenarioResult.Transition;

                case "Name":
                    var name = message.Text;
                    context.Data["Name"] = name;
                    await botClient.SendMessage(message.Chat.Id, "Введите срок выполнения задачи в формате dd.MM.yyyy:", cancellationToken: ct);
                    context.CurrentStep = "Deadline";
                    return ScenarioResult.Transition;

                case "Deadline":
                    if (DateTime.TryParseExact(message.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline))
                    {
                        var _user = (ToDoUser)context.Data["User"];
                        var _name = (string)context.Data["Name"];
                        await _toDoService.AddAsync(_user, _name, deadline, ct);
                        await botClient.SendMessage(message.Chat.Id, "Задача успешно добавлена.", cancellationToken: ct);
                        return ScenarioResult.Completed;
                    }
                    else
                    {
                        await botClient.SendMessage(message.Chat.Id, "Ошибка: неверный формат даты. Повторите ввод.", cancellationToken: ct);
                        return ScenarioResult.Transition;
                    }

                default:
                    await botClient.SendMessage(message.Chat.Id, "Что-то пошло не так. Повторите попытку.", cancellationToken: ct);
                    context.CurrentStep = null;
                    return ScenarioResult.Transition;
            }
        }
        
    }
}
