using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleBot.Scenarios
{
    public interface IScenario
    {
        bool CanHandle (ScenarioType scenario);
        Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, DateTime deadline, CancellationToken ct);
    }
}
