using System;
using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// The scarce Blank Rack: a capped container of Part-Blank instances,
    /// deliberately separate from the cauldron's artifact inventory so blanks
    /// can never be tossed into the pot. The cap IS the multi-track tension -
    /// how many organs can incubate at once.
    /// </summary>
    public interface IBlankRack
    {
        int Capacity { get; }

        IReadOnlyList<BlankInstance> Blanks { get; }

        event Action OnChanged;

        /// <summary>
        /// Creates an instance of the given blank definition and racks it.
        /// Returns false when the rack is full or the id is empty.
        /// </summary>
        bool TryAdd(string definitionId, out BlankInstance instance);

        /// <summary>Removes the blank with the given instance id (e.g. after unseal).</summary>
        bool Remove(int instanceId);

        bool TryGet(int instanceId, out BlankInstance instance);

        /// <summary>The next instance id this rack would mint — captured by the save layer so a
        /// restored run keeps minting unique ids.</summary>
        int NextInstanceId { get; }

        /// <summary>Replaces the rack contents from a save image (P2-2 restore); entries beyond
        /// <see cref="Capacity"/> are dropped. Fires <see cref="OnChanged"/> once.</summary>
        void RestoreFrom(IReadOnlyList<BlankInstance> blanks, int nextInstanceId);
    }
}
