using System;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The connection-approval payload codec: a fresh join sends nothing; a rejoin claims a seat
    /// with <c>[version][mode][playerId:int32][token:uint64]</c> (little-endian). Pure bytes —
    /// the session service feeds it NGO's <c>ConnectionData</c> on both ends.
    /// </summary>
    public static class ArenaConnectPayload
    {
        private const byte Version = 1;
        private const byte ModeRejoin = 1;
        private const int RejoinLength = 14;

        public static byte[] EncodeRejoin(int playerId, ulong token)
        {
            var payload = new byte[RejoinLength];
            payload[0] = Version;
            payload[1] = ModeRejoin;
            WriteInt(payload, 2, playerId);
            WriteULong(payload, 6, token);
            return payload;
        }

        public static bool TryDecodeRejoin(byte[] payload, out int playerId, out ulong token)
        {
            playerId = 0;
            token = 0;

            if (payload == null || payload.Length != RejoinLength)
                return false;
            if (payload[0] != Version || payload[1] != ModeRejoin)
                return false;

            playerId = ReadInt(payload, 2);
            token = ReadULong(payload, 6);
            return true;
        }

        private static void WriteInt(byte[] buffer, int offset, int value)
        {
            for (int i = 0; i < 4; i++)
            {
                buffer[offset + i] = (byte)(value >> (8 * i));
            }
        }

        private static int ReadInt(byte[] buffer, int offset)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                value |= buffer[offset + i] << (8 * i);
            }

            return value;
        }

        private static void WriteULong(byte[] buffer, int offset, ulong value)
        {
            for (int i = 0; i < 8; i++)
            {
                buffer[offset + i] = (byte)(value >> (8 * i));
            }
        }

        private static ulong ReadULong(byte[] buffer, int offset)
        {
            ulong value = 0;
            for (int i = 0; i < 8; i++)
            {
                value |= (ulong)buffer[offset + i] << (8 * i);
            }

            return value;
        }
    }
}
