using System.Collections.Generic;

namespace LevelGeneration
{
    public class PlatformGraphBuilder
    {
        private readonly List<IPlatformDefinitionBuilder> definitionBuilders = new();
        private int nextNodeId = 0;
        
        public PlatformGraphBuilder WithPlatform(IPlatformDefinitionBuilder builder)
        {
            if (builder is PlatformDefinitionBuilder defBuilder)
            {
                defBuilder.WithId(nextNodeId++);
            }
            definitionBuilders.Add(builder);
            return this;
        }
        
        public PlatformGraphData Build()
        {
            var graphData = new PlatformGraphData();
            var nodes = new List<GraphNode>();
            var edges = new List<GraphEdge>();
            
            // Build nodes from definitions
            foreach (var builder in definitionBuilders)
            {
                var definition = builder.Build();
                var node = new GraphNode
                {
                    Id = definition.Id,
                    Type = definition.Type,
                    ContentTypes = new List<PlatformContentType>(definition.ContentTypes),
                    IsKeyPlatform = true  // All builder-created platforms are key platforms
                };
                nodes.Add(node);
            }
            
            // Create linear edges (each platform connects to next)
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                edges.Add(new GraphEdge
                {
                    FromNodeId = nodes[i].Id,
                    ToNodeId = nodes[i + 1].Id
                });
            }
            
            graphData.Nodes = nodes;
            graphData.Edges = edges;
            graphData.EntryNodeId = nodes.Count > 0 ? nodes[0].Id : -1;
            
            return graphData;
        }
    }
}

