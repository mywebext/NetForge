namespace NetForge.Networking
{
    [System.Flags]
    public enum UdpFlags : ushort
    {
        None = 0,
        AckRequired = 1 << 0, // receiver must ACK messageId
        IsAck = 1 << 1, // this packet is an ACK for AckForMessageId
        IsHandshake = 1 << 2, // used during session establishment
        IsKeepAlive = 1 << 3, // keepalive traffic
    }
}
