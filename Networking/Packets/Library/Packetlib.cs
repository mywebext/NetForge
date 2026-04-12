//NetForge.Networking.Packets.Library/Packetlib.cs
using System;

namespace NetForge.Networking.Packets.Library
{
    public abstract class Packetlib
    {
        private int? _version;

        /// <summary>
        /// Canonical packet entry point.
        /// </summary>
        public abstract void Process(Packet packet);

        /// <summary>
        /// Convenience overload for header + payload.
        /// </summary>
        public virtual void Process(PacketHeader header, ArraySegment<byte> payload)
        {
            ArgumentNullException.ThrowIfNull(header);

            Process(new Packet
            {
                Header = header,
                Payload = payload
            });
        }

        /// <summary>
        /// Convenience overload for header + raw payload bytes.
        /// </summary>
        public virtual void Process(PacketHeader header, byte[]? payload)
        {
            ArgumentNullException.ThrowIfNull(header);

            Process(header, payload is null
                ? ArraySegment<byte>.Empty
                : new ArraySegment<byte>(payload));
        }

        /// <summary>
        /// Convenience overload for header only.
        /// </summary>
        public virtual void Process(PacketHeader header)
        {
            ArgumentNullException.ThrowIfNull(header);

            Process(header, ArraySegment<byte>.Empty);
        }

        public int Version()
        {
            _version ??= Functions.NumbersFromTypeName(GetType());
            return _version.Value;
        }
    }
}