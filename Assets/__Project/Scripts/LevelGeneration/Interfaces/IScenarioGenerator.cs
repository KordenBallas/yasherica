namespace LevelGeneration
{
    public interface IScenarioGenerator
    {
        ScenarioData GenerateScenario(GameContext context);
    }
}

