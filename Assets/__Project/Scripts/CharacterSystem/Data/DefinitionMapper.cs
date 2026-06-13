using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data.Definitions;

namespace CharacterSystem.Data
{
    /// <summary>
    /// The only bridge from ScriptableObject definitions to pure-C# Core records.
    /// TRS offsets deliberately stay out of Core; they are consumed directly
    /// from the definitions by the runtime SocketMounter.
    /// </summary>
    public static class DefinitionMapper
    {
        public static SkeletonData ToSkeletonData(SkeletonDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var sockets = new List<SocketInfo>(definition.Tier1Sockets.Count);
            foreach (var socket in definition.Tier1Sockets)
            {
                if (socket == null)
                {
                    continue;
                }

                sockets.Add(new SocketInfo(socket.Id, socket.ParentBoneName, SocketTier.Skeleton));
            }

            return new SkeletonData(definition.Id, new List<string>(definition.BoneNames), sockets);
        }

        public static PartData ToPartData(PartDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var sockets = new List<SocketInfo>(definition.ContributedSockets.Count);
            foreach (var socket in definition.ContributedSockets)
            {
                if (socket == null)
                {
                    continue;
                }

                sockets.Add(new SocketInfo(socket.Id, socket.ParentBoneName, SocketTier.Part, definition.Id));
            }

            return new PartData(
                definition.Id,
                definition.Slot != null ? definition.Slot.Id : null,
                definition.TargetSkeleton != null ? definition.TargetSkeleton.Id : null,
                new List<string>(definition.BoneNames),
                sockets);
        }
    }
}
