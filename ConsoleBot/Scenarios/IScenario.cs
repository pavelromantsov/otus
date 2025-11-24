using ConsoleBot.Core.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleBot.Scenarios
{
    public interface IScenario
    {
        bool CanHandle (ScenarioType scenario);
        Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, ToDoList? list, CancellationToken ct);
    }
}
