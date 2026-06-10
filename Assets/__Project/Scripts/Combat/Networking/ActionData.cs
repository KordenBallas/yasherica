using Combat.Core;
using Unity.Netcode;

namespace Combat.Networking
{
    /// <summary>
    /// Serializable data structure for transmitting actions over the network.
    /// </summary>
    public struct ActionData : INetworkSerializable
    {
        public int PlayerId;
        public int UnitId;
        public ActionType ActionType;

        // Move action data
        public int TargetQ;
        public int TargetR;

        // Ability action data
        public int AbilityId;
        public bool HasDirection;
        public int Direction;

        // Reorder action data
        public int[] NewOrder;

        // Retarget action data
        public int AbilityIndexInQueue;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref UnitId);
            serializer.SerializeValue(ref ActionType);
            serializer.SerializeValue(ref TargetQ);
            serializer.SerializeValue(ref TargetR);
            serializer.SerializeValue(ref AbilityId);
            serializer.SerializeValue(ref HasDirection);
            serializer.SerializeValue(ref Direction);
            serializer.SerializeValue(ref NewOrder);
            serializer.SerializeValue(ref AbilityIndexInQueue);
        }
    }
}
