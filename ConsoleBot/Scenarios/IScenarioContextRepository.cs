namespace ConsoleBot.Scenarios
{
    public interface IScenarioContextRepository
    {
        Task<ScenarioContext?> GetContext(long userId, CancellationToken ct);
        Task SetContext(long userId, ScenarioContext context, CancellationToken ct);
        Task ResetContext(long userId, CancellationToken ct);
        Task<IReadOnlyList<ScenarioContext>> GetContexts(CancellationToken ct);
    }
}
