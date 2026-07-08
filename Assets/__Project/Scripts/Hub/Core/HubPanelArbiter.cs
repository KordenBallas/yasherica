using System;

namespace Hub.Core
{
    /// <summary>
    /// Ownership arbiter for the Hub's ONE shared card panel: the part-offer presenter and the Heat
    /// pact presenter both drive the same <c>IMutationChoiceView</c> and its single selection event,
    /// so each claims the panel before showing and ignores selections while not the owner —
    /// otherwise a pact click would be read as a part pick (and vice versa).
    /// </summary>
    public sealed class HubPanelArbiter
    {
        public object Owner { get; private set; }

        public event Action<object> Claimed;

        public void Claim(object owner)
        {
            if (ReferenceEquals(Owner, owner))
            {
                return;
            }

            Owner = owner;
            Claimed?.Invoke(owner);
        }

        public bool IsOwner(object owner)
        {
            return ReferenceEquals(Owner, owner);
        }
    }
}
