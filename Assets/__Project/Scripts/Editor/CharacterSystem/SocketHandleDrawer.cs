using CharacterSystem.Core;
using CharacterSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Shared SceneView drawing for sockets: a sphere at the socket origin, RGB axis
    /// lines for orientation, and the socket id as a label. Tier-2 sockets use a
    /// distinct color so part-contributed sockets are recognizable at a glance.
    /// </summary>
    public static class SocketHandleDrawer
    {
        private const float SphereSize = 0.02f;
        private const float AxisLength = 0.05f;

        private static readonly Color Tier1Color = new Color(0.2f, 0.9f, 1f);
        private static readonly Color Tier2Color = new Color(1f, 0.5f, 0.9f);

        public static void DrawSockets(IModularCharacter character)
        {
            foreach (var socket in character.GetAvailableSockets())
            {
                var socketTransform = character.GetSocketTransform(socket.Id);
                if (socketTransform != null)
                {
                    DrawSocket(socketTransform, socket);
                }
            }
        }

        public static void DrawSocket(Transform socketTransform, SocketInfo info)
        {
            var position = socketTransform.position;

            Handles.color = info.Tier == SocketTier.Skeleton ? Tier1Color : Tier2Color;
            Handles.SphereHandleCap(0, position, socketTransform.rotation, SphereSize, EventType.Repaint);

            Handles.color = Color.red;
            Handles.DrawLine(position, position + socketTransform.right * AxisLength);
            Handles.color = Color.green;
            Handles.DrawLine(position, position + socketTransform.up * AxisLength);
            Handles.color = Color.blue;
            Handles.DrawLine(position, position + socketTransform.forward * AxisLength);

            Handles.Label(position + Vector3.up * 0.03f, info.Id);
        }
    }
}
