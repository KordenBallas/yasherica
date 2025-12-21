using System.Collections.Generic;
using Zenject;
using Platform;
using LevelGeneration;

namespace LevelGeneration
{
    public class PlatformFactory : IPlatformFactoryRegistry
    {
        private readonly Dictionary<PlatformType, IFactory<IPlatform>> factories = new();
        
        public void RegisterFactory(PlatformType type, IFactory<IPlatform> factory)
        {
            factories[type] = factory;
        }
        
        public IPlatform Create(PlatformType type)
        {
            if (!factories.TryGetValue(type, out var factory))
            {
                throw new System.ArgumentException($"No factory registered for platform type: {type}");
            }
            return factory.Create();
        }
    }
}

