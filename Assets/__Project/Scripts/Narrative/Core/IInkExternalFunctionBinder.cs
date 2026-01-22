namespace Narrative
{
    public interface IInkExternalFunctionBinder
    {
        void BindAllExternalFunctions();
        int LastCombatEnemyCount { get; }
    }
}
