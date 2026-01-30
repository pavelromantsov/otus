namespace ConsoleBot.Scenarios
{
    public class ScenarioContext
    {
        public ScenarioType CurrentScenario { get; set; }

        public string? CurrentStep { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; }
        public ScenarioContext(ScenarioType scenario)
        {
            CurrentScenario = scenario;
            CreatedAt = DateTime.UtcNow;
        }
    }

    public enum ScenarioType
    {
        None,
        AddTask,
        AddList,
        DeleteList,
        DeleteTask
    }

    public enum ScenarioResult
    {
        Transition,
        Completed
    }
}

