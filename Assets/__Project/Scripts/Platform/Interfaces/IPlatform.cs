using System.Collections.Generic;
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
        
        void Initialize(IPlatformVisual visual);
        void Enter();
        void Exit();
        void AddNeighbor(IPlatform platform);
        void AddContent(IPlatformContent content);
    }
}

