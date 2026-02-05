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
