namespace FitnessBot.Core.Entities
{
    public enum ActivityType
    {
        // Ходьба, бег — важны шаги
        StepsBased = 0,
        // Силовая, йога — только время + калории
        TimeBased = 1,
        // Пользовательский тип
        Custom = 2       
    }
}
