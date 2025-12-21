using System.Collections.Generic;
using System.Linq;

namespace Platform
{
    public class Platform : IPlatform
    {
        public int Id { get; private set; }
        
        public PlatformStateMachine StateMachine { get; private set; }
        public IReadOnlyList<IPlatformContent> Contents => contents.AsReadOnly();
        public IReadOnlyList<IPlatform> Neighbors => neighbors.AsReadOnly();
        public IPlatformVisual Visual { get; private set; }
        
        private readonly List<IPlatformContent> contents = new();
        private readonly List<IPlatform> neighbors = new();
        
        public Platform(int id)
        {
            Id = id;
            StateMachine = new PlatformStateMachine();
        }
        
        public void Initialize(IPlatformVisual visual)
        {
            Visual = visual;
            StateMachine.Initialize(this, new PlatformIdleState());
        }
        
        public void Enter()
        {
            StateMachine.ChangeState(new PlatformActiveState());
        }
        
        public void Exit()
        {
            // Return to idle if not completed
            if (StateMachine.CurrentState is not PlatformCompletedState)
            {
                StateMachine.ChangeState(new PlatformIdleState());
            }
        }
        
        public void AddNeighbor(IPlatform platform)
        {
            if (platform != null && !neighbors.Contains(platform))
            {
                neighbors.Add(platform);
            }
        }
        
        public void AddContent(IPlatformContent content)
        {
            if (content != null && !contents.Contains(content))
            {
                contents.Add(content);
            }
        }
    }
}

