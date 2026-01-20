using System.Collections.Generic;
using Combat.Controller;
using LevelGeneration;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Unified platform class that supports any combination of states.
    /// State activation is content-driven through IPlatformStateFactory.
    /// </summary>
    public class Platform : IPlatform
    {
        public class Factory : PlaceholderFactory<int, Platform> { }

        public int Id { get; private set; }

        public PlatformStateMachine StateMachine { get; private set; }
        public IReadOnlyList<IPlatformContent> Contents => _contents.AsReadOnly();
        public IReadOnlyList<IPlatform> Neighbors => _neighbors.AsReadOnly();
        public IPlatformVisual Visual { get; private set; }
        public IPlatformStateFactory StateFactory { get; private set; }
        public StoryPlatformData StoryData { get; private set; }

        private readonly List<IPlatformContent> _contents = new();
        private readonly List<IPlatform> _neighbors = new();
        private ICombatController _combatController;

        public Platform(int id, IPlatformStateFactory stateFactory)
        {
            Id = id;
            StateFactory = stateFactory;
            StateMachine = new PlatformStateMachine();
        }

        public virtual void Initialize(IPlatformVisual visual)
        {
            Visual = visual;

            // Initialize all content
            foreach (var content in _contents)
            {
                content.Initialize(this);
            }

            // Set up state machine using factory
            InitializeStateMachine();
        }

        protected virtual void InitializeStateMachine()
        {
            // Factory creates appropriate idle state based on content
            var idleState = StateFactory.CreateIdleState(this);
            StateMachine.Initialize(this, idleState);
        }

        public virtual void Enter()
        {
            // Factory creates appropriate active state based on content
            var activeState = StateFactory.CreateActiveState(this);
            StateMachine.ChangeState(activeState);
        }

        public virtual void Exit()
        {
            // Return to idle if not completed
            if (StateMachine.CurrentState is not PlatformCompletedState)
            {
                var idleState = StateFactory.CreateIdleState(this);
                StateMachine.ChangeState(idleState);
            }
        }

        public void AddNeighbor(IPlatform platform)
        {
            if (platform != null && !_neighbors.Contains(platform))
            {
                _neighbors.Add(platform);
            }
        }

        public void AddContent(IPlatformContent content)
        {
            if (content != null && !_contents.Contains(content))
            {
                _contents.Add(content);
            }
        }

        public void TransitionToState(IPlatformState state)
        {
            StateMachine.ChangeState(state);
        }

        public void SetStoryData(StoryPlatformData storyData)
        {
            StoryData = storyData;
        }

        public void SetCombatController(ICombatController controller)
        {
            _combatController = controller;
        }

        public ICombatController GetCombatController()
        {
            return _combatController;
        }
    }
}
