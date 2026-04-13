using NetForge.Networking.Enums;
using NetForge.Networking.Managers;
using NetForge.Networking.Nodes;
using System;

namespace NetForge.Networking.Packets.Library;

public class Packetlib001 : Packetlib
{
    public Packetlib001(Node node) : base(node){}

    public override void Process(Packet packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        int scope = (int)packet.Header.TypeScope * 100;
        OpCodes opcode = (OpCodes)(scope + (int)packet.Header.TypeCommand);

        switch (opcode)
        {
            case OpCodes.Ping:
                SessionManager.AdjustTrust(packet.Header.SessionId, 1);
                break;

            case OpCodes.FileDelete:
                if (!SessionManager.MeetsTrust(packet.Header.SessionId, TrustModel.Validated)) return;

                break;

            default:
                SessionManager.AdjustTrust(packet.Header.SessionId, -1); //Undefined Packet structure, something may be up
                break;
        }
    }
}