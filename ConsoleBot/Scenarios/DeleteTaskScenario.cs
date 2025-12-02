using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.Services;
using ConsoleBot.TelegramBot.Dto;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Scenarios
{
    public class DeleteListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _toDoListService;
        private readonly IToDoService _toDoService;
        private readonly IScenarioContextRepository _contextRepository;
        public DeleteListScenario(IUserService userService, IToDoListService toDoListService, IToDoService toDoService, IScenarioContextRepository contextRepository)
        {
            _userService = userService;
            _toDoListService = toDoListService;
            _toDoService = toDoService;
            _contextRepository = contextRepository;

        }

        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.DeleteList;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient botClient, ScenarioContext context, Message message, ToDoList? list, CancellationToken cancellationToken)
        {
            //var userId = message.From.userId; 
            var callbackQuery = new CallbackQuery();
            switch (context.CurrentStep)
            {
                case null:
                    // Получаем пользователя по Telegram идентификатору
                    var user = await _userService.GetUserByTelegramUserIdAsync(message.From.Id, cancellationToken);

                    // Если пользователь не найден, регистрируем его
                    if (user == null)
                    {
                        await _userService.RegisterUserAsync(message.From.Id, cancellationToken);
                        user = await _userService.GetUserByTelegramUserIdAsync(message.From.Id, cancellationToken);
                    }

                    //var user = await _userService.GetUserByTelegramUserIdAsync(message.From.Id,  cancellationToken);
                    context.Data["User"] = user;

                    // Получаем список всех списков пользователя
                    var lists = await _toDoListService.GetUserLists(user.UserId, cancellationToken);

                    // Формируем клавиатуру с выбором списка
                    var inlineKeyboard = new InlineKeyboardMarkup(lists.Select(list => InlineKeyboardButton.WithCallbackData(list.Name, new ToDoListCallbackDto("deletelist", list.Id).ToString())));
                    await botClient.SendMessage(user.TelegramUserId, "Выберите список для удаления:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
                    context.CurrentStep = "Approve";
                    return ScenarioResult.Transition;

                case "Approve":
                    // Получаем выбранный список из CallbackQuery
                    var dto = ToDoListCallbackDto.FromString(message.Text);
                    var selectedList = await _toDoListService.Get(dto.ToDoListId!.Value, cancellationToken);
                    context.Data["SelectedList"] = selectedList;

                    // Спрашиваем подтверждение
                    await botClient.SendMessage(message.From.Id, $"Подтверждаете удаление списка {selectedList.Name}?",
                        replyMarkup: new InlineKeyboardMarkup(
                            new[]
                            {
                            InlineKeyboardButton.WithCallbackData("✅ Да", "yes"),
                            InlineKeyboardButton.WithCallbackData("❌ Нет", "no")
                            }), cancellationToken: cancellationToken
                    );
                    context.CurrentStep = "Delete";
                    return ScenarioResult.Transition;

                case "Delete":
                    // Проверяем подтверждение
                    var confirmation = message.Text;
                    if (confirmation == "yes")
                    {
                        var listToDelete = (ToDoList)context.Data["SelectedList"];
                        //await _toDoService.DeleteAllForList(listToDelete.Id, cancellationToken);
                        await _toDoListService.Delete(listToDelete.Id, cancellationToken);
                        await botClient.SendMessage(message.From.Id, "Список успешно удалён.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendMessage(message.From.Id, "Удаление отменено.", cancellationToken: cancellationToken);
                    }
                    return ScenarioResult.Completed;

                default:
                    await botClient.SendMessage(message.Chat.Id, "Что-то пошло не так. Повторите попытку.", cancellationToken: cancellationToken);
                    context.CurrentStep = null;
                    return ScenarioResult.Transition;
            }
        }
    }
}