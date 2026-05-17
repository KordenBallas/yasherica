using System;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Runtime instance of an NPC with bound state, role, and location.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class NpcInstance
    {
        private string _instanceId;
        private NpcDefinition _definition;
        private NpcRole _currentRole;
        private NpcInstanceState _state;
        private string _assignedStoryId;
        private string _assignedLocationId;
        private int _cooldownRemaining;
        private int _relationshipScore;

        /// <summary>
        /// Unique instance ID (generated at runtime).
        /// </summary>
        public string InstanceId => _instanceId;

        /// <summary>
        /// The underlying NPC definition.
        /// </summary>
        public NpcDefinition Definition => _definition;

        /// <summary>
        /// The NPC's base ID from definition.
        /// </summary>
        public string NpcId => _definition?.NpcId ?? string.Empty;

        /// <summary>
        /// Display name of the NPC.
        /// </summary>
        public string DisplayName => _definition?.DisplayName ?? "Unknown";

        /// <summary>
        /// Current role in the active story.
        /// </summary>
        public NpcRole CurrentRole => _currentRole;

        /// <summary>
        /// Current state of this NPC instance.
        /// </summary>
        public NpcInstanceState State => _state;

        /// <summary>
        /// ID of the story this NPC is assigned to.
        /// </summary>
        public string AssignedStoryId => _assignedStoryId;

        /// <summary>
        /// ID of the location this NPC is placed at.
        /// </summary>
        public string AssignedLocationId => _assignedLocationId;

        /// <summary>
        /// Platforms remaining before NPC is available again.
        /// </summary>
        public int CooldownRemaining => _cooldownRemaining;

        /// <summary>
        /// Player's relationship score with this NPC (-100 to 100).
        /// </summary>
        public int RelationshipScore => _relationshipScore;

        /// <summary>
        /// Whether this NPC is available for assignment.
        /// </summary>
        public bool IsAvailable => _state == NpcInstanceState.Available && _cooldownRemaining <= 0;

        /// <summary>
        /// Whether this NPC is currently assigned to a story.
        /// </summary>
        public bool IsAssigned => _state == NpcInstanceState.Assigned;

        /// <summary>
        /// Faction alignment from definition.
        /// </summary>
        public NpcFaction Faction => _definition?.Faction ?? NpcFaction.Neutral;

        /// <summary>
        /// Creates a new NPC instance from a definition.
        /// </summary>
        public NpcInstance(NpcDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);
            _currentRole = NpcRole.None;
            _state = NpcInstanceState.Available;
            _assignedStoryId = null;
            _assignedLocationId = null;
            _cooldownRemaining = 0;
            _relationshipScore = 0;
        }

        /// <summary>
        /// Assigns this NPC to a story with a specific role.
        /// </summary>
        public void Assign(string storyId, NpcRole role, string locationId = null)
        {
            if (string.IsNullOrEmpty(storyId))
                throw new ArgumentException("Story ID cannot be null or empty", nameof(storyId));

            _assignedStoryId = storyId;
            _currentRole = role;
            _assignedLocationId = locationId;
            _state = NpcInstanceState.Assigned;
        }

        /// <summary>
        /// Releases this NPC from their current assignment.
        /// </summary>
        public void Release()
        {
            _assignedStoryId = null;
            _currentRole = NpcRole.None;
            _assignedLocationId = null;
            _state = NpcInstanceState.Available;
        }

        /// <summary>
        /// Sets a cooldown period before this NPC can be used again.
        /// </summary>
        public void SetCooldown(int platforms)
        {
            _cooldownRemaining = Math.Max(0, platforms);
            if (_cooldownRemaining > 0)
            {
                _state = NpcInstanceState.OnCooldown;
            }
        }

        /// <summary>
        /// Decrements the cooldown by one platform.
        /// </summary>
        public void DecrementCooldown()
        {
            if (_cooldownRemaining > 0)
            {
                _cooldownRemaining--;
                if (_cooldownRemaining <= 0 && _state == NpcInstanceState.OnCooldown)
                {
                    _state = NpcInstanceState.Available;
                }
            }
        }

        /// <summary>
        /// Updates the relationship score with this NPC.
        /// </summary>
        public void UpdateRelationship(int delta)
        {
            _relationshipScore = Math.Clamp(_relationshipScore + delta, -100, 100);
        }

        /// <summary>
        /// Sets the relationship score directly.
        /// </summary>
        public void SetRelationship(int score)
        {
            _relationshipScore = Math.Clamp(score, -100, 100);
        }

        /// <summary>
        /// Marks this NPC as encountered by the player.
        /// </summary>
        public void MarkEncountered()
        {
            if (_state == NpcInstanceState.Available)
            {
                _state = NpcInstanceState.Encountered;
            }
        }

        /// <summary>
        /// Creates a serializable snapshot of this instance.
        /// </summary>
        public NpcInstanceSnapshot CreateSnapshot()
        {
            return new NpcInstanceSnapshot
            {
                InstanceId = _instanceId,
                NpcId = NpcId,
                CurrentRole = _currentRole,
                State = _state,
                AssignedStoryId = _assignedStoryId,
                AssignedLocationId = _assignedLocationId,
                CooldownRemaining = _cooldownRemaining,
                RelationshipScore = _relationshipScore
            };
        }

        /// <summary>
        /// Restores state from a snapshot.
        /// </summary>
        public void RestoreFromSnapshot(NpcInstanceSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            _instanceId = snapshot.InstanceId;
            _currentRole = snapshot.CurrentRole;
            _state = snapshot.State;
            _assignedStoryId = snapshot.AssignedStoryId;
            _assignedLocationId = snapshot.AssignedLocationId;
            _cooldownRemaining = snapshot.CooldownRemaining;
            _relationshipScore = snapshot.RelationshipScore;
        }
    }

    /// <summary>
    /// State of an NPC instance.
    /// </summary>
    public enum NpcInstanceState
    {
        Available,      // Ready for assignment
        Assigned,       // Currently assigned to a story
        OnCooldown,     // Temporarily unavailable
        Encountered,    // Has been met but not currently in a story
        Retired         // Permanently unavailable
    }

    /// <summary>
    /// Serializable snapshot of NPC instance state.
    /// </summary>
    [Serializable]
    public class NpcInstanceSnapshot
    {
        public string InstanceId;
        public string NpcId;
        public NpcRole CurrentRole;
        public NpcInstanceState State;
        public string AssignedStoryId;
        public string AssignedLocationId;
        public int CooldownRemaining;
        public int RelationshipScore;
    }
}
