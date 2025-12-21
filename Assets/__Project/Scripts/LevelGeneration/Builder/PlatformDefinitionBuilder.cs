using System.Collections.Generic;

namespace LevelGeneration
{
    public interface IPlatformDefinitionBuilder
    {
        IPlatformDefinitionBuilder WithContent(PlatformContentType contentType);
        PlatformDefinition Build();
    }
    
    public class PlatformDefinitionBuilder : IPlatformDefinitionBuilder
    {
        private int id;
        private PlatformType type = PlatformType.Simple;
        private readonly List<PlatformContentType> contentTypes = new();
        
        public static PlatformDefinitionBuilder NewInstance()
        {
            return new PlatformDefinitionBuilder();
        }
        
        public PlatformDefinitionBuilder WithId(int id)
        {
            this.id = id;
            return this;
        }
        
        public PlatformDefinitionBuilder WithType(PlatformType type)
        {
            this.type = type;
            return this;
        }
        
        public IPlatformDefinitionBuilder WithContent(PlatformContentType contentType)
        {
            if (!contentTypes.Contains(contentType))
            {
                contentTypes.Add(contentType);
            }
            return this;
        }
        
        public PlatformDefinition Build()
        {
            return new PlatformDefinition
            {
                Id = id,
                Type = type,
                ContentTypes = new List<PlatformContentType>(contentTypes)
            };
        }
    }
}

