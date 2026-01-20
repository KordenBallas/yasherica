using System.Collections.Generic;
using Combat.Controller;
using LevelGeneration;
using UnityEngine;

namespace Platform
{
    public interface IPlatform
    {
        int Id { get; }

        // Model (logic)
        PlatformStateMachine StateMachine { get; }
        IReadOnlyList<IPlatformContent> Contents { get; }
        IReadOnlyList<IPlatform> Neighbors { get; }

        // Visual (geometry)
        IPlatformVisual Visual { get; }

        // State factory for content-driven state creation
        IPlatformStateFactory StateFactory { get; }

        // Story data from scenario generation
        StoryPlatformData StoryData { get; }

        void Initialize(IPlatformVisual visual);
        void Enter();
        void Exit();
        void AddNeighbor(IPlatform platform);
        void AddContent(IPlatformContent content);
        void TransitionToState(IPlatformState state);

        // Story data configuration
        void SetStoryData(StoryPlatformData storyData);

        // Combat controller access (for states that need it)
        void SetCombatController(ICombatController controller);
        ICombatController GetCombatController();
    }
}

