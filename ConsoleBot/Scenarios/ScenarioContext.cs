using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBot.Scenarios
{
    public class ScenarioContext 
    {
        public ScenarioType CurrentScenario { get; set; }

        public string? CurrentStep { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public ScenarioContext(ScenarioType scenario)
        { 
            CurrentScenario = scenario;
        }
    }

    public enum ScenarioType 
    {
        None,
        AddTask
    }

    public enum ScenarioResult
    {
        Transition,
        Completed
    }
}

