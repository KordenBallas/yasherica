using Unity.Netcode;

namespace Combat.Arena.Networking
{
    /// <summary>
    /// One committed step on the wire: a move or an ability cast with its lock-time snapshot
    /// (origin, facing, frozen committed cells). Arrays serialize with explicit length loops so
    /// the format is unambiguous across NGO versions.
    /// </summary>
    public struct IntentData : INetworkSerializable
    {
        public int UnitId;
        public bool IsMove;
        public int TargetQ;
        public int TargetR;
        public int AbilityId;
        public bool HasFacing;
        public int Facing;
        public int OriginQ;
        public int OriginR;
        public int[] CellsQR;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref UnitId);
            serializer.SerializeValue(ref IsMove);
            serializer.SerializeValue(ref TargetQ);
            serializer.SerializeValue(ref TargetR);
            serializer.SerializeValue(ref AbilityId);
            serializer.SerializeValue(ref HasFacing);
            serializer.SerializeValue(ref Facing);
            serializer.SerializeValue(ref OriginQ);
            serializer.SerializeValue(ref OriginR);
            SerializationHelpers.SerializeIntArray(serializer, ref CellsQR);
        }
    }

    /// <summary>One player's locked round on the wire.</summary>
    public struct PlayerCommitData : INetworkSerializable
    {
        public int PlayerId;
        public int UnitId;
        public int FinalFacing;
        public IntentData[] Steps;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref UnitId);
            serializer.SerializeValue(ref FinalFacing);
            SerializationHelpers.SerializeArray(serializer, ref Steps);
        }
    }

    /// <summary>Client → host: the lock message (commit + previous round's lockstep hash, R10).</summary>
    public struct CommitEnvelopeData : INetworkSerializable
    {
        public int RoundNumber;
        public ulong PreviousRoundStateHash;
        public PlayerCommitData Commit;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoundNumber);
            serializer.SerializeValue(ref PreviousRoundStateHash);
            serializer.SerializeValue(ref Commit);
        }
    }

    /// <summary>One seat of the match.</summary>
    public struct RosterSlotData : INetworkSerializable
    {
        public ulong ClientId;
        public int PlayerId;
        public int UnitId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref UnitId);
        }
    }

    /// <summary>
    /// Host → all: the match handshake. The seed is everything — platform and spawn cells derive
    /// from it deterministically on every client; only the seat assignment travels.
    /// </summary>
    public struct MatchSetupData : INetworkSerializable
    {
        public int MatchSeed;
        public RosterSlotData[] Roster;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MatchSeed);
            SerializationHelpers.SerializeArray(serializer, ref Roster);
        }
    }

    /// <summary>Host → all: the assembled round (commits in canonical PlayerId order + departures + auto-passes).</summary>
    public struct RoundBundleData : INetworkSerializable
    {
        public int RoundNumber;
        public int[] DepartedPlayerIds;
        public int[] AutoPassedPlayerIds;
        public PlayerCommitData[] Commits;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoundNumber);
            SerializationHelpers.SerializeIntArray(serializer, ref DepartedPlayerIds);
            SerializationHelpers.SerializeIntArray(serializer, ref AutoPassedPlayerIds);
            SerializationHelpers.SerializeArray(serializer, ref Commits);
        }
    }

    /// <summary>Client → host: the local tasted-forms catalog (pre-draft, P4-5).</summary>
    public struct TastedCatalogData : INetworkSerializable
    {
        public string[] PartIds;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            SerializationHelpers.SerializeStringArray(serializer, ref PartIds);
        }
    }

    /// <summary>One draftable board instance on the wire.</summary>
    public struct DraftBoardEntryData : INetworkSerializable
    {
        public int EntryId;
        public string PartId;
        public string SlotId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref EntryId);
            serializer.SerializeValue(ref PartId);
            serializer.SerializeValue(ref SlotId);
        }
    }

    /// <summary>
    /// Host → all: the draft handshake. The composed board travels in full (host-authoritative
    /// composition — only the host holds every participant's catalog); the slot loadout rides
    /// along so completion can never diverge.
    /// </summary>
    public struct DraftStartData : INetworkSerializable
    {
        public float PickTimerSeconds;
        public string[] SlotLoadout;
        public DraftBoardEntryData[] Board;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PickTimerSeconds);
            SerializationHelpers.SerializeStringArray(serializer, ref SlotLoadout);
            SerializationHelpers.SerializeArray(serializer, ref Board);
        }
    }

    /// <summary>One draft action on the wire (request and canonical broadcast share the shape).</summary>
    public struct DraftPickData : INetworkSerializable
    {
        public int PickIndex;
        public int PlayerId;
        public int EntryId;
        public bool WasAutoPick;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PickIndex);
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref EntryId);
            serializer.SerializeValue(ref WasAutoPick);
        }
    }

    /// <summary>Host → all: one canonically applied pick + departures since the last broadcast.</summary>
    public struct DraftPickAppliedData : INetworkSerializable
    {
        public DraftPickData Pick;
        public int[] DepartedPlayerIds;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Pick);
            SerializationHelpers.SerializeIntArray(serializer, ref DepartedPlayerIds);
        }
    }

    // ---- X1 reconnect / resync / migration ----

    /// <summary>One status effect's snapshot triple on the wire.</summary>
    public struct StatusSnapshotData : INetworkSerializable
    {
        public int EffectId;
        public int Duration;
        public int StackCount;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref EffectId);
            serializer.SerializeValue(ref Duration);
            serializer.SerializeValue(ref StackCount);
        }
    }

    /// <summary>One ability's cooldown on the wire.</summary>
    public struct AbilityCooldownData : INetworkSerializable
    {
        public int AbilityId;
        public int CurrentCooldown;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AbilityId);
            serializer.SerializeValue(ref CurrentCooldown);
        }
    }

    /// <summary>One unit's sim-relevant fields on the wire.</summary>
    public struct UnitSnapshotData : INetworkSerializable
    {
        public int UnitId;
        public int OwnerPlayerId;
        public int Q;
        public int R;
        public int CurrentHP;
        public int MaxHP;
        public int Facing;
        public AbilityCooldownData[] Abilities;
        public StatusSnapshotData[] Statuses;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref UnitId);
            serializer.SerializeValue(ref OwnerPlayerId);
            serializer.SerializeValue(ref Q);
            serializer.SerializeValue(ref R);
            serializer.SerializeValue(ref CurrentHP);
            serializer.SerializeValue(ref MaxHP);
            serializer.SerializeValue(ref Facing);
            SerializationHelpers.SerializeArray(serializer, ref Abilities);
            SerializationHelpers.SerializeArray(serializer, ref Statuses);
        }
    }

    /// <summary>The authoritative round-start state on the wire.</summary>
    public struct StateSnapshotData : INetworkSerializable
    {
        public int Version;
        public int RoundNumber;
        public ulong LastRoundHash;
        public UnitSnapshotData[] Units;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Version);
            serializer.SerializeValue(ref RoundNumber);
            serializer.SerializeValue(ref LastRoundHash);
            SerializationHelpers.SerializeArray(serializer, ref Units);
        }
    }

    /// <summary>One seat's drafted loadout on the wire (slot→part pairs as parallel arrays).</summary>
    public struct LoadoutEntryData : INetworkSerializable
    {
        public int PlayerId;
        public string[] SlotIds;
        public string[] PartIds;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerId);
            SerializationHelpers.SerializeStringArray(serializer, ref SlotIds);
            SerializationHelpers.SerializeStringArray(serializer, ref PartIds);
        }
    }

    /// <summary>Host → one rejoiner: the full mid-match stand-up.</summary>
    public struct RejoinPackageData : INetworkSerializable
    {
        public int TargetPlayerId;
        public MatchSetupData Setup;
        public LoadoutEntryData[] Loadouts;
        public StateSnapshotData Snapshot;
        public int[] DepartedPlayerIds;
        public bool HasBundle;
        public RoundBundleData Bundle;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TargetPlayerId);
            serializer.SerializeValue(ref Setup);
            SerializationHelpers.SerializeArray(serializer, ref Loadouts);
            serializer.SerializeValue(ref Snapshot);
            SerializationHelpers.SerializeIntArray(serializer, ref DepartedPlayerIds);
            serializer.SerializeValue(ref HasBundle);
            if (HasBundle)
            {
                serializer.SerializeValue(ref Bundle);
            }
        }
    }

    /// <summary>Host → one diverged client: adopt this snapshot (R10 heal).</summary>
    public struct ResyncCommandData : INetworkSerializable
    {
        public int TargetPlayerId;
        public StateSnapshotData Snapshot;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TargetPlayerId);
            serializer.SerializeValue(ref Snapshot);
        }
    }

    /// <summary>Client → host: the state transfer landed.</summary>
    public struct ResyncAckData : INetworkSerializable
    {
        public int PlayerId;
        public int RoundNumber;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref RoundNumber);
        }
    }

    /// <summary>One peer's self-reported reachable address.</summary>
    public struct EndpointData : INetworkSerializable
    {
        public int PlayerId;
        public string Address;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref Address);
        }
    }

    /// <summary>Host → all: the migration address book.</summary>
    public struct AddressBookData : INetworkSerializable
    {
        public EndpointData[] Endpoints;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            SerializationHelpers.SerializeArray(serializer, ref Endpoints);
        }
    }

    internal static class SerializationHelpers
    {
        public static void SerializeStringArray<T>(BufferSerializer<T> serializer, ref string[] array)
            where T : IReaderWriter
        {
            int length = array?.Length ?? 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                array = new string[length];
            }

            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref array[i]);
            }
        }

        public static void SerializeIntArray<T>(BufferSerializer<T> serializer, ref int[] array)
            where T : IReaderWriter
        {
            int length = array?.Length ?? 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                array = new int[length];
            }

            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref array[i]);
            }
        }

        public static void SerializeArray<T, TElement>(BufferSerializer<T> serializer, ref TElement[] array)
            where T : IReaderWriter
            where TElement : struct, INetworkSerializable
        {
            int length = array?.Length ?? 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                array = new TElement[length];
            }

            for (int i = 0; i < length; i++)
            {
                array[i].NetworkSerialize(serializer);
            }
        }
    }
}
