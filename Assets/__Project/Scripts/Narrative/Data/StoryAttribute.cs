using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narrative.Data
{
    /// <summary>
    /// Flexible key-value attribute system for dynamic story connections.
    /// Enables thematic, location-based, emotional, or custom story linking.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [Serializable]
    public class StoryAttribute
    {
        [Tooltip("Attribute category (e.g., 'theme', 'location', 'emotion', 'npc')")]
        [SerializeField] private string _attributeKey;

        [Tooltip("Attribute value (e.g., 'betrayal', 'forest', 'tense', 'merchant')")]
        [SerializeField] private string _attributeValue;

        [Tooltip("How to match this attribute with others")]
        [SerializeField] private AttributeMatchType _matchType = AttributeMatchType.Exact;

        [Tooltip("Weight for attribute-based selection (higher = more important)")]
        [SerializeField] [Range(0f, 1f)] private float _weight = 0.5f;

        public string AttributeKey => _attributeKey;
        public string AttributeValue => _attributeValue;
        public AttributeMatchType MatchType => _matchType;
        public float Weight => _weight;

        public StoryAttribute() { }

        public StoryAttribute(string key, string value, AttributeMatchType matchType = AttributeMatchType.Exact, float weight = 0.5f)
        {
            _attributeKey = key;
            _attributeValue = value;
            _matchType = matchType;
            _weight = weight;
        }

        /// <summary>
        /// Checks if this attribute matches another based on match type.
        /// </summary>
        public bool Matches(StoryAttribute other)
        {
            if (other == null || _attributeKey != other._attributeKey)
                return false;

            switch (_matchType)
            {
                case AttributeMatchType.Exact:
                    return _attributeValue == other._attributeValue;

                case AttributeMatchType.Contains:
                    return _attributeValue.Contains(other._attributeValue) ||
                           other._attributeValue.Contains(_attributeValue);

                case AttributeMatchType.StartsWith:
                    return _attributeValue.StartsWith(other._attributeValue) ||
                           other._attributeValue.StartsWith(_attributeValue);

                case AttributeMatchType.Any:
                    return true;

                default:
                    return false;
            }
        }

        public override string ToString()
        {
            return $"{_attributeKey}:{_attributeValue} ({_matchType})";
        }
    }

    /// <summary>
    /// How to match story attributes.
    /// </summary>
    public enum AttributeMatchType
    {
        /// <summary>
        /// Values must match exactly.
        /// Example: "forest" matches only "forest"
        /// </summary>
        Exact,

        /// <summary>
        /// One value contains the other.
        /// Example: "dark_forest" contains "forest"
        /// </summary>
        Contains,

        /// <summary>
        /// One value starts with the other.
        /// Example: "forest_entrance" starts with "forest"
        /// </summary>
        StartsWith,

        /// <summary>
        /// Match any value with the same key.
        /// Example: All "location" attributes match
        /// </summary>
        Any
    }

    /// <summary>
    /// Collection of story attributes with query utilities.
    /// Pure C# data structure for runtime attribute management.
    /// </summary>
    [Serializable]
    public class StoryAttributeCollection
    {
        [SerializeField] private List<StoryAttribute> _attributes = new();

        public IReadOnlyList<StoryAttribute> Attributes => _attributes;

        public StoryAttributeCollection() { }

        public StoryAttributeCollection(IEnumerable<StoryAttribute> attributes)
        {
            _attributes = new List<StoryAttribute>(attributes);
        }

        /// <summary>
        /// Adds an attribute to the collection.
        /// </summary>
        public void Add(StoryAttribute attribute)
        {
            if (attribute != null)
            {
                _attributes.Add(attribute);
            }
        }

        /// <summary>
        /// Adds multiple attributes to the collection.
        /// </summary>
        public void AddRange(IEnumerable<StoryAttribute> attributes)
        {
            _attributes.AddRange(attributes);
        }

        /// <summary>
        /// Gets all attributes with a specific key.
        /// </summary>
        public IReadOnlyList<StoryAttribute> GetByKey(string key)
        {
            return _attributes.Where(a => a.AttributeKey == key).ToList();
        }

        /// <summary>
        /// Checks if the collection has an attribute with the specified key and value.
        /// </summary>
        public bool Has(string key, string value)
        {
            return _attributes.Any(a => a.AttributeKey == key && a.AttributeValue == value);
        }

        /// <summary>
        /// Checks if the collection has any attribute with the specified key.
        /// </summary>
        public bool HasKey(string key)
        {
            return _attributes.Any(a => a.AttributeKey == key);
        }

        /// <summary>
        /// Gets the number of matching attributes between this collection and another.
        /// Returns a score based on matches and weights.
        /// </summary>
        public float GetMatchScore(StoryAttributeCollection other)
        {
            if (other == null || other._attributes.Count == 0)
                return 0f;

            float score = 0f;
            int matchCount = 0;

            foreach (var attribute in _attributes)
            {
                foreach (var otherAttribute in other._attributes)
                {
                    if (attribute.Matches(otherAttribute))
                    {
                        score += (attribute.Weight + otherAttribute.Weight) / 2f;
                        matchCount++;
                    }
                }
            }

            return score;
        }

        /// <summary>
        /// Gets a simple match count (number of matching attributes).
        /// </summary>
        public int GetMatchCount(StoryAttributeCollection other)
        {
            if (other == null || other._attributes.Count == 0)
                return 0;

            int count = 0;

            foreach (var attribute in _attributes)
            {
                if (other._attributes.Any(a => attribute.Matches(a)))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Clears all attributes.
        /// </summary>
        public void Clear()
        {
            _attributes.Clear();
        }

        public override string ToString()
        {
            return $"Attributes: [{string.Join(", ", _attributes)}]";
        }
    }

    /// <summary>
    /// Predefined attribute keys for common use cases.
    /// These are recommendations, not strict requirements.
    /// </summary>
    public static class StoryAttributeKeys
    {
        public const string Theme = "theme";              // "betrayal", "redemption", "mystery"
        public const string Location = "location";        // "forest", "city", "dungeon"
        public const string Emotion = "emotion";          // "tense", "hopeful", "dark"
        public const string Npc = "npc";                  // "merchant", "guard", "wizard"
        public const string Tone = "tone";                // "serious", "comedic", "dramatic"
        public const string Genre = "genre";              // "combat", "puzzle", "exploration"
        public const string Tag = "tag";                  // General-purpose tags
        public const string Difficulty = "difficulty";    // "easy", "medium", "hard"
        public const string Length = "length";            // "short", "medium", "long"
    }
}
