using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Scenarios
{
    public class ScenarioContext
    {
        public long UserId { get; set; }
        public ScenarioType CurrentScenario { get; set; }
        public int CurrentStep { get; set; }
        public Dictionary<string, object?> Data { get; } = new();
    }
}
