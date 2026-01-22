using System.Collections.Generic;
using System.Linq;

namespace LevelGeneration
{
    public class PlatformGraphGenerator : IPlatformGraphGenerator
    {
        public PlatformGraphData GenerateGraph(ScenarioData scenario)
        {
            var graphData = new PlatformGraphData();
            var nodes = new List<GraphNode>();
            var edges = new List<GraphEdge>();
            
            int nodeId = 0;
            
            // Create nodes from requirements
            foreach (var requirement in scenario.RequiredPlatforms)
            {
                var node = new GraphNode
                {
                    Id = nodeId++,
                    Type = requirement.Type,
                    ContentTypes = new List<PlatformContentType>(requirement.ContentTypes),
                    IsKeyPlatform = true,
                    StoryData = requirement.StoryData
                };
                nodes.Add(node);
            }
            
            // Add filler platforms to reach estimated count
            int fillerCount = scenario.EstimatedPlatformCount - nodes.Count;
            for (int i = 0; i < fillerCount; i++)
            {
                nodes.Add(new GraphNode
                {
                    Id = nodeId++,
                    Type = PlatformType.Simple,
                    ContentTypes = new List<PlatformContentType> { PlatformContentType.None },
                    IsKeyPlatform = false
                });
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
            
            // Optionally add some branching (random connections)
            //AddRandomBranches(nodes, edges, scenario.DifficultyLevel);
            
            graphData.Nodes = nodes;
            graphData.Edges = edges;
            graphData.EntryNodeId = nodes.Count > 0 ? nodes[0].Id : -1;
            
            return graphData;
        }
        
        private void AddRandomBranches(List<GraphNode> nodes, List<GraphEdge> edges, int difficulty)
        {
            // Add some random branches for variety
            int branchCount = difficulty;
            var random = new System.Random();
            
            for (int i = 0; i < branchCount && nodes.Count > 2; i++)
            {
                int fromIdx = random.Next(0, nodes.Count - 1);
                int toIdx = random.Next(fromIdx + 1, nodes.Count);
                
                // Check if edge already exists
                bool exists = edges.Any(e => 
                    e.FromNodeId == nodes[fromIdx].Id && 
                    e.ToNodeId == nodes[toIdx].Id);
                
                if (!exists)
                {
                    edges.Add(new GraphEdge
                    {
                        FromNodeId = nodes[fromIdx].Id,
                        ToNodeId = nodes[toIdx].Id
                    });
                }
            }
        }
    }
}

