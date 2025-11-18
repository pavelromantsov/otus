using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBot.Scenarios
{
    public class InMemoryScenarioContextRepository:IScenarioContextRepository
    {
        private readonly Dictionary<long, ScenarioContext> _contexts = new Dictionary<long, ScenarioContext>();

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
            _contexts.Remove(userId);
            return Task.CompletedTask;
        }
    }
}