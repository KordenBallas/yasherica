using Zenject;
using Platform;
using LevelGeneration;

namespace LevelGeneration
{
    public interface IPlatformFactoryRegistry
    {
        void RegisterFactory(PlatformType type, IFactory<IPlatform> factory);
        IPlatform Create(PlatformType type);
    }
}

