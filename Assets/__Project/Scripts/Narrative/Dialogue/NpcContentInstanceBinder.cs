using System;
using Narrative.Generation;
using Platform;
using UnityEngine;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Service that binds NpcInstance from the NpcPool to NpcContent on platforms.
    /// Bridges the gap between procedural generation and platform content system.
    /// </summary>
    public interface INpcContentInstanceBinder
    {
        /// <summary>
        /// Binds an NpcInstance to the NpcContent based on matching NpcId.
        /// </summary>
        /// <param name="npcContent">The NpcContent to bind to</param>
        /// <returns>True if binding succeeded</returns>
        bool BindInstance(NpcContent npcContent);

        /// <summary>
        /// Binds a specific NpcInstance to the NpcContent.
        /// </summary>
        /// <param name="npcContent">The NpcContent to bind to</param>
        /// <param name="npcInstance">The NpcInstance to bind</param>
        void BindInstance(NpcContent npcContent, NpcInstance npcInstance);

        /// <summary>
        /// Event fired when an NpcInstance is bound to content.
        /// </summary>
        event Action<NpcContent, NpcInstance> OnInstanceBound;
    }

    /// <summary>
    /// Default implementation of INpcContentInstanceBinder.
    /// </summary>
    public class NpcContentInstanceBinder : INpcContentInstanceBinder
    {
        private readonly INpcPool _npcPool;

        public event Action<NpcContent, NpcInstance> OnInstanceBound;

        public NpcContentInstanceBinder(INpcPool npcPool)
        {
            _npcPool = npcPool;
        }

        public bool BindInstance(NpcContent npcContent)
        {
            if (npcContent == null)
            {
                Debug.LogWarning("[NpcContentInstanceBinder] Cannot bind: NpcContent is null");
                return false;
            }

            if (npcContent.Definition == null)
            {
                Debug.LogWarning("[NpcContentInstanceBinder] Cannot bind: NpcContent has no definition");
                return false;
            }

            var npcId = npcContent.Definition.NpcId;
            if (string.IsNullOrEmpty(npcId))
            {
                Debug.LogWarning("[NpcContentInstanceBinder] Cannot bind: NpcDefinition has no NpcId");
                return false;
            }

            // Try to get the NpcInstance from the pool
            var npcInstance = _npcPool?.GetNpc(npcId);
            if (npcInstance == null)
            {
                Debug.Log($"[NpcContentInstanceBinder] No NpcInstance found in pool for '{npcId}'");
                return false;
            }

            BindInstance(npcContent, npcInstance);
            return true;
        }

        public void BindInstance(NpcContent npcContent, NpcInstance npcInstance)
        {
            if (npcContent == null)
                throw new ArgumentNullException(nameof(npcContent));

            npcContent.BindRuntimeInstance(npcInstance);
            OnInstanceBound?.Invoke(npcContent, npcInstance);
        }
    }
}
