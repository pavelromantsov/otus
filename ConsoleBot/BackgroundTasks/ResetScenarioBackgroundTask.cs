using ConsoleBot.Scenarios;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleBot.BackgroundTasks
{
    public sealed class ResetScenarioBackgroundTask : BackgroundTask
    {
        private readonly TimeSpan _resetScenarioTimeout;
        private readonly IScenarioContextRepository _scenarioRepository;
        private readonly ITelegramBotClient _bot;

        public ResetScenarioBackgroundTask(
            TimeSpan resetScenarioTimeout,
            IScenarioContextRepository scenarioRepository,
            ITelegramBotClient bot)
            : base(TimeSpan.FromHours(1), nameof(ResetScenarioBackgroundTask))
        {
            _resetScenarioTimeout = resetScenarioTimeout;
            _scenarioRepository = scenarioRepository;
            _bot = bot;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var contexts = await _scenarioRepository.GetContexts(ct);

            foreach (var context in contexts)
            {
                // Пытаемся достать userId из Data
                if (!context.Data.TryGetValue("UserId", out var rawUserId) ||
                    rawUserId is not long userId)
                {
                    continue;
                }

                // Проверяем возраст контекста
                if (now - context.CreatedAt < _resetScenarioTimeout)
                    continue;

                // Сбрасываем контекст
                await _scenarioRepository.ResetContext(userId, ct);

                // Клавиатура
                var keyboard = new ReplyKeyboardMarkup(
                    new[]
                    {
                    new[] { new KeyboardButton("/addtask") },
                    new[] { new KeyboardButton("/show") },
                    new[] { new KeyboardButton("/report") }
                    })
                {
                    ResizeKeyboard = true
                };

                var text = $"Сценарий отменен, так как не поступил ответ в течение {_resetScenarioTimeout}";

                // Сообщение пользователю
                await _bot.SendMessage(
                    chatId: userId,
                    text: text,
                    replyMarkup: keyboard,
                    cancellationToken: ct);
            }
        }
    }
}
