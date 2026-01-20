using System;
using System.Collections.Generic;
using System.Linq;
using Narrative.Data;
using Narrative.Data.Definitions;

namespace Narrative.Graph
{
    /// <summary>
    /// Runtime story graph data structure.
    /// Represents the complete narrative structure as nodes and edges.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class StoryGraph
    {
        private readonly Dictionary<string, StoryNode> _nodes;
        private readonly Dictionary<string, List<StoryEdge>> _outgoingEdges;
        private readonly Dictionary<string, List<StoryEdge>> _incomingEdges;
        private readonly List<StoryNode> _allNodes;

        public IReadOnlyList<StoryNode> AllNodes => _allNodes;

        public StoryGraph()
        {
            _nodes = new Dictionary<string, StoryNode>();
            _outgoingEdges = new Dictionary<string, List<StoryEdge>>();
            _incomingEdges = new Dictionary<string, List<StoryEdge>>();
            _allNodes = new List<StoryNode>();
        }

        /// <summary>
        /// Builds the story graph from a collection of story definitions.
        /// </summary>
        public void BuildGraph(IReadOnlyList<BaseStoryDefinition> storyDefinitions)
        {
            if (storyDefinitions == null || storyDefinitions.Count == 0)
            {
                return;
            }

            Clear();

            // Phase 1: Create nodes for all stories
            foreach (var story in storyDefinitions)
            {
                if (story == null || string.IsNullOrEmpty(story.StoryId))
                    continue;

                var node = new StoryNode(story);
                _nodes[story.StoryId] = node;
                _allNodes.Add(node);
                _outgoingEdges[story.StoryId] = new List<StoryEdge>();
                _incomingEdges[story.StoryId] = new List<StoryEdge>();
            }

            // Phase 2: Create edges from relationships
            foreach (var node in _allNodes)
            {
                CreateEdgesFromRelationships(node);
            }

            // Phase 3: Link edges to nodes
            foreach (var kvp in _outgoingEdges)
            {
                var node = _nodes[kvp.Key];
                node.SetOutgoingEdges(kvp.Value);
            }

            foreach (var kvp in _incomingEdges)
            {
                var node = _nodes[kvp.Key];
                node.SetIncomingEdges(kvp.Value);
            }
        }

        private void CreateEdgesFromRelationships(StoryNode fromNode)
        {
            if (fromNode.Definition.Relationships == null)
                return;

            foreach (var relationship in fromNode.Definition.Relationships)
            {
                if (relationship.TargetStory == null ||
                    string.IsNullOrEmpty(relationship.TargetStory.StoryId))
                    continue;

                if (!_nodes.TryGetValue(relationship.TargetStory.StoryId, out var toNode))
                    continue;

                var edge = new StoryEdge(fromNode, toNode, relationship);
                _outgoingEdges[fromNode.StoryId].Add(edge);
                _incomingEdges[toNode.StoryId].Add(edge);
            }
        }

        /// <summary>
        /// Gets a story node by ID.
        /// </summary>
        public StoryNode GetNode(string storyId)
        {
            return _nodes.TryGetValue(storyId, out var node) ? node : null;
        }

        /// <summary>
        /// Gets all outgoing edges from a story node.
        /// </summary>
        public IReadOnlyList<StoryEdge> GetOutgoingEdges(string storyId)
        {
            return _outgoingEdges.TryGetValue(storyId, out var edges) ? edges : Array.Empty<StoryEdge>();
        }

        /// <summary>
        /// Gets all incoming edges to a story node.
        /// </summary>
        public IReadOnlyList<StoryEdge> GetIncomingEdges(string storyId)
        {
            return _incomingEdges.TryGetValue(storyId, out var edges) ? edges : Array.Empty<StoryEdge>();
        }

        /// <summary>
        /// Updates node states based on completed stories.
        /// </summary>
        public void UpdateNodeStates(StoryState currentState)
        {
            if (currentState == null)
                return;

            foreach (var node in _allNodes)
            {
                // Update node state based on completion
                if (currentState.IsNodeCompleted(node.StoryId))
                {
                    node.SetState(StoryNodeState.Completed);
                }
                else
                {
                    // Check if prerequisites are met
                    bool prerequisitesMet = ArePrerequisitesMet(node.Definition.Prerequisites, currentState);
                    node.SetState(prerequisitesMet ? StoryNodeState.Available : StoryNodeState.Locked);
                }
            }

            // Update edge activation based on conditions and node states
            foreach (var kvp in _outgoingEdges)
            {
                var fromNode = _nodes[kvp.Key];
                foreach (var edge in kvp.Value)
                {
                    UpdateEdgeActivation(edge, fromNode, currentState);
                }
            }
        }

        private void UpdateEdgeActivation(StoryEdge edge, StoryNode fromNode, StoryState currentState)
        {
            // Edge is active if:
            // 1. Source node is completed
            // 2. Edge condition is met (if any)
            // 3. Target node is not locked by other prerequisites

            if (fromNode.State != StoryNodeState.Completed)
            {
                edge.SetActive(false);
                return;
            }

            if (edge.Relationship.Condition.HasCondition)
            {
                bool conditionMet = EvaluateCondition(edge.Relationship.Condition, currentState);
                edge.SetActive(conditionMet);
            }
            else
            {
                edge.SetActive(true);
            }
        }

        private bool ArePrerequisitesMet(StoryPrerequisites prerequisites, StoryState currentState)
        {
            if (prerequisites == null || !prerequisites.HasAnyPrerequisites)
                return true;

            // Check completed stories
            if (prerequisites.RequiredCompletedStories != null)
            {
                foreach (var storyId in prerequisites.RequiredCompletedStories)
                {
                    if (!currentState.IsNodeCompleted(storyId))
                        return false;
                }
            }

            // Check completed quests
            if (prerequisites.RequiredCompletedQuests != null)
            {
                foreach (var questId in prerequisites.RequiredCompletedQuests)
                {
                    if (!currentState.IsQuestCompleted(questId))
                        return false;
                }
            }

            // Check active quests
            if (prerequisites.RequiredActiveQuests != null)
            {
                foreach (var questId in prerequisites.RequiredActiveQuests)
                {
                    if (!currentState.IsQuestActive(questId))
                        return false;
                }
            }

            // Check encountered NPCs
            if (prerequisites.RequiredEncounteredNpcs != null)
            {
                foreach (var npcId in prerequisites.RequiredEncounteredNpcs)
                {
                    if (!currentState.HasEncounteredNpc(npcId))
                        return false;
                }
            }

            // Check chapter number (if we have chapter info in state)
            // Note: This requires adding chapter number to StoryState
            // For now, we'll skip this check

            return true;
        }

        private bool EvaluateCondition(StoryRelationshipCondition condition, StoryState currentState)
        {
            switch (condition.ConditionType)
            {
                case ConditionType.None:
                    return true;

                case ConditionType.QuestCompleted:
                    return currentState.IsQuestCompleted(condition.RequiredQuestId);

                case ConditionType.NpcEncountered:
                    return currentState.HasEncounteredNpc(condition.RequiredNpcId);

                case ConditionType.InkVariable:
                    // This requires access to Ink runtime state
                    // Will be evaluated by StoryGraphProvider
                    return true;

                case ConditionType.ChapterNumber:
                    // This requires chapter number in state
                    // Will be evaluated by StoryGraphProvider
                    return true;

                case ConditionType.Custom:
                    // Custom conditions evaluated elsewhere
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets nodes in the critical path (main story progression).
        /// </summary>
        public IReadOnlyList<StoryNode> GetCriticalPath()
        {
            return _allNodes
                .Where(n => n.Definition.PlatformConfig.IsKeyProgression || n.Definition.StoryType == StoryType.Chapter)
                .OrderBy(n => n.Definition is StoryChapterDefinition chapter ? chapter.ChapterNumber : int.MaxValue)
                .ToList();
        }

        /// <summary>
        /// Gets all available nodes (Available state, prerequisites met).
        /// </summary>
        public IReadOnlyList<StoryNode> GetAvailableNodes()
        {
            return _allNodes.Where(n => n.State == StoryNodeState.Available).ToList();
        }

        /// <summary>
        /// Gets nodes connected to a specific node via active edges.
        /// </summary>
        public IReadOnlyList<StoryNode> GetConnectedNodes(string storyId)
        {
            if (!_outgoingEdges.TryGetValue(storyId, out var edges))
                return Array.Empty<StoryNode>();

            return edges
                .Where(e => e.IsActive)
                .Select(e => e.ToNode)
                .ToList();
        }

        /// <summary>
        /// Clears all nodes and edges.
        /// </summary>
        public void Clear()
        {
            _nodes.Clear();
            _outgoingEdges.Clear();
            _incomingEdges.Clear();
            _allNodes.Clear();
        }

        /// <summary>
        /// Gets statistics about the graph.
        /// </summary>
        public GraphStatistics GetStatistics()
        {
            int edgeCount = _outgoingEdges.Values.Sum(list => list.Count);
            int activeEdgeCount = _outgoingEdges.Values.Sum(list => list.Count(e => e.IsActive));

            return new GraphStatistics
            {
                NodeCount = _allNodes.Count,
                EdgeCount = edgeCount,
                ActiveEdgeCount = activeEdgeCount,
                AvailableNodeCount = _allNodes.Count(n => n.State == StoryNodeState.Available),
                CompletedNodeCount = _allNodes.Count(n => n.State == StoryNodeState.Completed),
                LockedNodeCount = _allNodes.Count(n => n.State == StoryNodeState.Locked)
            };
        }
    }

    /// <summary>
    /// Represents a story node in the graph.
    /// </summary>
    public class StoryNode
    {
        public BaseStoryDefinition Definition { get; }
        public StoryNodeState State { get; private set; }
        public IReadOnlyList<StoryEdge> OutgoingEdges { get; private set; }
        public IReadOnlyList<StoryEdge> IncomingEdges { get; private set; }

        public string StoryId => Definition.StoryId;

        public StoryNode(BaseStoryDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            State = StoryNodeState.Locked;
            OutgoingEdges = Array.Empty<StoryEdge>();
            IncomingEdges = Array.Empty<StoryEdge>();
        }

        public void SetState(StoryNodeState state)
        {
            State = state;
        }

        public void SetOutgoingEdges(List<StoryEdge> edges)
        {
            OutgoingEdges = edges;
        }

        public void SetIncomingEdges(List<StoryEdge> edges)
        {
            IncomingEdges = edges;
        }
    }

    /// <summary>
    /// Represents an edge (relationship) between two story nodes.
    /// </summary>
    public class StoryEdge
    {
        public StoryNode FromNode { get; }
        public StoryNode ToNode { get; }
        public StoryRelationship Relationship { get; }
        public bool IsActive { get; private set; }

        public StoryEdge(StoryNode fromNode, StoryNode toNode, StoryRelationship relationship)
        {
            FromNode = fromNode ?? throw new ArgumentNullException(nameof(fromNode));
            ToNode = toNode ?? throw new ArgumentNullException(nameof(toNode));
            Relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            IsActive = false;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }
    }

    /// <summary>
    /// State of a story node.
    /// </summary>
    public enum StoryNodeState
    {
        Locked,      // Prerequisites not met
        Available,   // Prerequisites met, can be played
        Active,      // Currently being played
        Completed    // Has been completed
    }

    /// <summary>
    /// Statistics about the story graph.
    /// </summary>
    public struct GraphStatistics
    {
        public int NodeCount;
        public int EdgeCount;
        public int ActiveEdgeCount;
        public int AvailableNodeCount;
        public int CompletedNodeCount;
        public int LockedNodeCount;
    }
}
