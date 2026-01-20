using System.Collections.Generic;
using System.Linq;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Discovery
{
    /// <summary>
    /// Discovers side stories from Resources/SideStories folder.
    /// Zero configuration - simply drop SideStoryDefinition assets into the folder.
    /// </summary>
    public class ResourcesSideStoryDiscoveryService : ISideStoryDiscoveryService
    {
        private const string ResourcesPath = "SideStories";

        private List<SideStoryDefinition> _allSideStories = new();
        private Dictionary<string, SideStoryDefinition> _byId = new();
        private Dictionary<string, List<SideStoryDefinition>> _byNpc = new();
        private Dictionary<string, List<SideStoryDefinition>> _byAttr = new();

        public IReadOnlyList<SideStoryDefinition> AllSideStories => _allSideStories;

        public ResourcesSideStoryDiscoveryService()
        {
            Refresh();
        }

        public SideStoryDefinition GetById(string storyId)
        {
            if (string.IsNullOrEmpty(storyId))
                return null;

            return _byId.TryGetValue(storyId, out var definition) ? definition : null;
        }

        public IReadOnlyList<SideStoryDefinition> GetByNpc(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return new List<SideStoryDefinition>();

            return _byNpc.TryGetValue(npcId, out var definitions)
                ? definitions
                : new List<SideStoryDefinition>();
        }

        public IReadOnlyList<SideStoryDefinition> GetByAttr(string attr)
        {
            if (string.IsNullOrEmpty(attr))
                return new List<SideStoryDefinition>();

            return _byAttr.TryGetValue(attr.ToLowerInvariant(), out var definitions)
                ? definitions
                : new List<SideStoryDefinition>();
        }

        public IReadOnlyList<SideStoryDefinition> GetByAttrs(IEnumerable<string> attrs)
        {
            if (attrs == null)
                return new List<SideStoryDefinition>();

            var result = new HashSet<SideStoryDefinition>();

            foreach (var attr in attrs)
            {
                if (_byAttr.TryGetValue(attr.ToLowerInvariant(), out var definitions))
                {
                    foreach (var def in definitions)
                    {
                        result.Add(def);
                    }
                }
            }

            return result.ToList();
        }

        public void Refresh()
        {
            _allSideStories.Clear();
            _byId.Clear();
            _byNpc.Clear();
            _byAttr.Clear();

            var loadedAssets = Resources.LoadAll<SideStoryDefinition>(ResourcesPath);

            if (loadedAssets == null || loadedAssets.Length == 0)
            {
                Debug.Log($"[ResourcesSideStoryDiscoveryService] No side stories found in Resources/{ResourcesPath}");
                return;
            }

            foreach (var definition in loadedAssets)
            {
                if (definition == null || string.IsNullOrEmpty(definition.StoryId))
                {
                    Debug.LogWarning("[ResourcesSideStoryDiscoveryService] Skipping invalid SideStoryDefinition (null or missing ID)");
                    continue;
                }

                // Add to main list
                _allSideStories.Add(definition);

                // Index by ID
                if (_byId.ContainsKey(definition.StoryId))
                {
                    Debug.LogWarning($"[ResourcesSideStoryDiscoveryService] Duplicate story ID: {definition.StoryId}");
                }
                else
                {
                    _byId[definition.StoryId] = definition;
                }

                // Index by NPC
                if (!string.IsNullOrEmpty(definition.NpcId))
                {
                    if (!_byNpc.TryGetValue(definition.NpcId, out var npcList))
                    {
                        npcList = new List<SideStoryDefinition>();
                        _byNpc[definition.NpcId] = npcList;
                    }
                    npcList.Add(definition);
                }

                // Index by attributes
                foreach (var attribute in definition.Attributes)
                {
                    var normalizedAttr = attribute.AttributeKey.ToLowerInvariant();
                    if (!_byAttr.TryGetValue(normalizedAttr, out var attrList))
                    {
                        attrList = new List<SideStoryDefinition>();
                        _byAttr[normalizedAttr] = attrList;
                    }
                    attrList.Add(definition);
                }
            }

            Debug.Log($"[ResourcesSideStoryDiscoveryService] Discovered {_allSideStories.Count} side stories");
        }
    }
}
