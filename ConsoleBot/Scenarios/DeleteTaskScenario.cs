using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using ConsoleBot.TelegramBot.Dto;

namespace ConsoleBot.Scenarios
{
    public class DeleteTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _toDoListService;
        private readonly IToDoService _toDoService;
        private readonly IScenarioContextRepository _contextRepository;

        public DeleteTaskScenario(IUserService userService,
            IToDoListService toDoListService,
            IToDoService toDoService,
            IScenarioContextRepository contextRepository)
        {
            _userService = userService;
            _toDoListService = toDoListService;
            _toDoService = toDoService;
            _contextRepository = contextRepository;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteTask;

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
                        if (!context.Data.TryGetValue("ToDoItemId", out var rawId) || rawId is not Guid taskId)
                            return ScenarioResult.Completed;

                        var item = await _toDoService.Get(taskId, ct);
                        if (item == null)
                            return ScenarioResult.Completed;

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Да",  $"deletetask_yes|{taskId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Нет", $"deletetask_no|{taskId}")
                }
            });

                        await botClient.SendMessage(
                            message.Chat.Id,
                            $"Удалить задачу \"{item.Name}\"?",
                            replyMarkup: keyboard,
                            cancellationToken: ct);

                        context.CurrentStep = "Approve";
                        await _contextRepository.SetContext(message.From.Id, context, ct);
                        return ScenarioResult.Transition;
                    }

                case "Approve":
                    {
                        if (!context.Data.TryGetValue("Callback", out var rawCb) || rawCb is not CallbackQuery cb)
                            return ScenarioResult.Completed;

                        var parts = (cb.Data ?? "").Split('|', 2);
                        var action = parts[0];

                        if (action == "deletetask_yes")
                        {
                            if (!context.Data.TryGetValue("ToDoItemId", out var rawId) || rawId is not Guid taskId)
                                return ScenarioResult.Completed;

                            var item = await _toDoService.Get(taskId, ct);
                            if (item != null)
                                _toDoService.Delete(item.Id, ct); // или await, если async

                            await botClient.EditMessageText(
                                cb.Message!.Chat.Id,
                                cb.Message.MessageId,
                                "Задача удалена.",
                                cancellationToken: ct);

                            await botClient.AnswerCallbackQuery(cb.Id, "Задача удалена", cancellationToken: ct);
                            await _contextRepository.ResetContext(cb.From.Id, ct);
                            return ScenarioResult.Completed;
                        }
                        else if (action == "deletetask_no")
                        {
                            await botClient.EditMessageText(
                                cb.Message!.Chat.Id,
                                cb.Message.MessageId,
                                "Удаление отменено.",
                                cancellationToken: ct);

                            await botClient.AnswerCallbackQuery(cb.Id, "Удаление отменено", cancellationToken: ct);
                            await _contextRepository.ResetContext(cb.From.Id, ct);
                            return ScenarioResult.Completed;
                        }

                        await _contextRepository.ResetContext(cb.From.Id, ct);
                        return ScenarioResult.Completed;
                    }

                default:
                    return ScenarioResult.Completed;
            }
        }
    }
}
