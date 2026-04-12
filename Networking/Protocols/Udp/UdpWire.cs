/*
using System.Text;

namespace NetForge.Networking
{
    internal static class UdpWire
    {
        public const int HeaderSize = 42;

        public static byte[] Encode(
            string signature4,
            PacketType type,
            UdpFlags flags,
            ulong senderInstanceId,
            ulong messageId,
            ulong sessionId,
            ulong ackForMessageId,
            byte[] payload)
        {
            payload ??= System.Array.Empty<byte>();
            if (signature4 == null || signature4.Length != 4)
                throw new System.ArgumentException("signature must be 4 chars", nameof(signature4));
            if (payload.Length > ushort.MaxValue)
                throw new System.ArgumentOutOfRangeException(nameof(payload), "payload too large");

            var buf = new byte[HeaderSize + payload.Length];

            // Signature (ASCII 4)
            var sig = Encoding.ASCII.GetBytes(signature4);
            System.Buffer.BlockCopy(sig, 0, buf, 0, 4);

            WriteU16BE(buf, 4, (ushort)type);
            WriteU16BE(buf, 6, (ushort)flags);

            WriteU64BE(buf, 8, senderInstanceId);
            WriteU64BE(buf, 16, messageId);
            WriteU64BE(buf, 24, sessionId);
            WriteU64BE(buf, 32, ackForMessageId);

            WriteU16BE(buf, 40, (ushort)payload.Length);

            if (payload.Length > 0)
                System.Buffer.BlockCopy(payload, 0, buf, HeaderSize, payload.Length);

            return buf;
        }

        public static bool TryDecode(
            byte[] buf,
            int count,
            out string sig,
            out PacketType type,
            out UdpFlags flags,
            out ulong senderInstanceId,
            out ulong messageId,
            out ulong sessionId,
            out ulong ackForMessageId,
            out System.ArraySegment<byte> payload)
        {
            sig = null;
            type = 0;
            flags = 0;
            senderInstanceId = 0;
            messageId = 0;
            sessionId = 0;
            ackForMessageId = 0;
            payload = default;

            if (buf == null || count < HeaderSize) return false;

            sig = Encoding.ASCII.GetString(buf, 0, 4);
            if (sig != "FEUC" && sig != "FESH") return false;

            type = (PacketType)ReadU16BE(buf, 4);
            flags = (UdpFlags)ReadU16BE(buf, 6);

            senderInstanceId = ReadU64BE(buf, 8);
            messageId = ReadU64BE(buf, 16);
            sessionId = ReadU64BE(buf, 24);
            ackForMessageId = ReadU64BE(buf, 32);

            ushort payloadLen = ReadU16BE(buf, 40);
            if (HeaderSize + payloadLen > count) return false;

            payload = new System.ArraySegment<byte>(buf, HeaderSize, payloadLen);
            return true;
        }

        private static void WriteU16BE(byte[] b, int o, ushort v)
        {
            b[o + 0] = (byte)(v >> 8);
            b[o + 1] = (byte)(v);
        }

        private static ushort ReadU16BE(byte[] b, int o)
        {
            return (ushort)((b[o + 0] << 8) | b[o + 1]);
        }

        private static void WriteU64BE(byte[] b, int o, ulong v)
        {
            b[o + 0] = (byte)(v >> 56);
            b[o + 1] = (byte)(v >> 48);
            b[o + 2] = (byte)(v >> 40);
            b[o + 3] = (byte)(v >> 32);
            b[o + 4] = (byte)(v >> 24);
            b[o + 5] = (byte)(v >> 16);
            b[o + 6] = (byte)(v >> 8);
            b[o + 7] = (byte)(v);
        }

        private static ulong ReadU64BE(byte[] b, int o)
        {
            return ((ulong)b[o + 0] << 56)
                 | ((ulong)b[o + 1] << 48)
                 | ((ulong)b[o + 2] << 40)
                 | ((ulong)b[o + 3] << 32)
                 | ((ulong)b[o + 4] << 24)
                 | ((ulong)b[o + 5] << 16)
                 | ((ulong)b[o + 6] << 8)
                 | ((ulong)b[o + 7]);
        }
    }
}
*/