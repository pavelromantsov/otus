using System.Collections.Concurrent;

namespace ConsoleBot.Scenarios
{
    public class InMemoryScenarioContextRepository:IScenarioContextRepository
    {
        private readonly ConcurrentDictionary<long, ScenarioContext> _contexts = new ConcurrentDictionary<long, ScenarioContext>();

        public async Task<ScenarioContext?> GetContext(long userId, CancellationToken ct)
        {
            return await Task.FromResult(_contexts.ContainsKey(userId) ? _contexts[userId] : null);
        }

        public Task SetContext(long userId, ScenarioContext context, CancellationToken ct)
        {
            _contexts[userId] = context;
            return Task.CompletedTask;
        }

        public Task ResetContext(long userId, CancellationToken ct)
        {
            _contexts.TryRemove(userId, out _);
            return Task.CompletedTask;
        }
    }
}