using NetForge.Networking.Managers;
using NetForge.Networking.Nodes;
using System;

namespace NetForge.Networking.Packets.Library
{
    public abstract class Packetlib
    {
        private int? _version;

        protected Node Node { get; }
        protected SessionManager SessionManager => Node.SessionManager;
        protected AckManager AckManager => Node.AckManager;

        protected Packetlib(Node node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public abstract void Process(Packet packet);

        public virtual void Process(PacketHeader header, ArraySegment<byte> payload)
        {
            ArgumentNullException.ThrowIfNull(header);

            Process(new Packet
            {
                Header = header,
                Payload = payload
            });
        }

        public virtual void Process(PacketHeader header, byte[]? payload)
        {
            ArgumentNullException.ThrowIfNull(header);

            Process(header, payload is null
                ? ArraySegment<byte>.Empty
                : new ArraySegment<byte>(payload));
        }

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