using Platform;

namespace LevelGeneration
{
    public interface IAreaGenerator
    {
        IPlatform EntryPlatform { get; }
        
        void Generate();
        void Clear();
    }
}

