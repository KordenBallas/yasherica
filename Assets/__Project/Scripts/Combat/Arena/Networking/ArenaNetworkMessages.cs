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

    /// <summary>Host → all: the assembled round (commits in canonical PlayerId order + departures).</summary>
    public struct RoundBundleData : INetworkSerializable
    {
        public int RoundNumber;
        public int[] DepartedPlayerIds;
        public PlayerCommitData[] Commits;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoundNumber);
            SerializationHelpers.SerializeIntArray(serializer, ref DepartedPlayerIds);
            SerializationHelpers.SerializeArray(serializer, ref Commits);
        }
    }

    internal static class SerializationHelpers
    {
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
