using NetForge.Networking.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetForge.Networking.Packets;
using NetForge.Networking.Nodes;
namespace NetForge.Security.Synchronization;

    public class Key : Packet
    {
        public Key(Node localhost)
        {
            byte[] payload = [];

            PacketHeader Header = new PacketHeader
            {
                TypeScope = TypeScopes.Key,
                TypeCommand = CommandTypes.Ping,
                Flags = PacketFlags.IsAck,
                Encryption = Algorithms.None,
                Protocol = localhost.ProtocolType,
                SourceNodeType = localhost.NodeType,
                Signature = localhost.Signature,
                SenderInstanceId = localhost.InstanceId,
                MessageId = 1,
                RoutingKey = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                PayloadLength = (ulong)payload.Length
            };
            Payload = new ArraySegment<byte>(payload);
            RemoteEndPoint = localhost.RemoteEndPoint;
        }
    }
