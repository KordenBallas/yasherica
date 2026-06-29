using System.Collections.Generic;

namespace Narrative.Interaction
{
    /// <summary>Default <see cref="INpcInteractionRegistry"/> — a small list keyed by NPC instance id.</summary>
    public sealed class NpcInteractionRegistry : INpcInteractionRegistry
    {
        private readonly List<NpcInteractionHandle> _handles = new List<NpcInteractionHandle>();

        public IReadOnlyList<NpcInteractionHandle> Handles => _handles;

        public void Register(NpcInteractionHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            // Replace any stale handle for the same id (e.g. a recast/recurring actor).
            Unregister(handle.Id);
            _handles.Add(handle);
        }

        public void Unregister(string id)
        {
            for (int i = _handles.Count - 1; i >= 0; i--)
            {
                if (_handles[i].Id == id)
                {
                    _handles.RemoveAt(i);
                }
            }
        }

        public bool TryGet(string id, out NpcInteractionHandle handle)
        {
            for (int i = 0; i < _handles.Count; i++)
            {
                if (_handles[i].Id == id)
                {
                    handle = _handles[i];
                    return true;
                }
            }

            handle = null;
            return false;
        }
    }
}
