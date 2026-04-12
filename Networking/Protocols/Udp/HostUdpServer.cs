/*
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using NetForge.Networking;
using NetForge.Networking.Managers;
using static FileEngine.Service.Service;
using NetForge.Networking.Enums;
namespace NetForge.Networking.Udp
{
    public sealed class HostUdpServer : IDisposable
    {
        private readonly AckManager _acks = new();
        private readonly SessionManager _sessions = new();

        private UdpClient _udp;
        private CancellationTokenSource _cts;

        private readonly int _stripeCount;
        private readonly Channel<UdpPacket>[] _stripes;
        private readonly Task[] _workers;

        private readonly TimeSpan _sessionTimeout = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _keepAliveTick = TimeSpan.FromSeconds(5);

        private long _nextMessageId = 0;

        public HostUdpServer(int stripeCount = 0)
        {
            _stripeCount = stripeCount > 0 ? stripeCount : Math.Max(2, Environment.ProcessorCount);
            _stripes = new Channel<UdpPacket>[_stripeCount];
            _workers = new Task[_stripeCount];

            for (int i = 0; i < _stripeCount; i++)
            {
                _stripes[i] = Channel.CreateUnbounded<UdpPacket>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });

                int idx = i;
                _workers[i] = Task.Run(() => WorkerLoop(_stripes[idx].Reader));
            }
        }

        public void Start()
        {
            lock (LockObject)
            {
                if (_cts != null) return;

                _cts = new CancellationTokenSource();
                _udp = new UdpClient(UdpHostPort);

                // receive + maintenance
                Task.Run(() => ReceiveLoop(_cts.Token));
                Task.Run(() => MaintenanceLoop(_cts.Token));

                // FIRST-RUN: notify installer UI
                SendStartupToInstaller();
            }
        }

        public void Stop()
        {
            lock (LockObject)
            {
                if (_cts == null) return;

                try { _cts.Cancel(); } catch { }
                try { _udp?.Close(); } catch { }
                try { _udp?.Dispose(); } catch { }

                _udp = null;

                try { _cts.Dispose(); } catch { }
                _cts = null;
            }
        }

        public void Dispose() => Stop();

        private async Task ReceiveLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                UdpReceiveResult res;
                try { res = await _udp.ReceiveAsync(ct); }
                catch { break; }

                if (!UdpWire.TryDecode(
                    res.Buffer, res.Buffer.Length,
                    out var sig,
                    out var type,
                    out var flags,
                    out var senderInstanceId,
                    out var messageId,
                    out var sessionId,
                    out var ackForMessageId,
                    out var payload))
                {
                    continue;
                }

                // Host accepts FEUC only
                if (sig != "FEUC") continue;

                // ACK packet? handle immediately, no dispatch
                if ((flags & UdpFlags.IsAck) != 0)
                {
                    _acks.HandleAck(ackForMessageId, res.RemoteEndPoint);
                    continue;
                }

                // If ACK required, fire ACK immediately (fast path)
                if ((flags & UdpFlags.AckRequired) != 0)
                {
                    SendAck(res.RemoteEndPoint, ackForMessageId: messageId);
                }

                var pkt = new UdpPacket
                {
                    Signature = sig,
                    Type = type,
                    Flags = flags,
                    SenderInstanceId = senderInstanceId,
                    MessageId = messageId,
                    SessionId = sessionId,
                    AckForMessageId = ackForMessageId,
                    Payload = payload,
                    RemoteEndPoint = res.RemoteEndPoint
                };

                // dispatch stripe by (sessionId if set else senderInstanceId)
                ulong key = pkt.SessionId != 0 ? pkt.SessionId : pkt.SenderInstanceId;
                int stripe = (int)(key % (ulong)_stripeCount);

                _stripes[stripe].Writer.TryWrite(pkt);
            }
        }

        private async Task WorkerLoop(ChannelReader<UdpPacket> reader)
        {
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var pkt))
                {
                    try { HandlePacket(pkt); }
                    catch
                    {
                        // TODO: error reporting packet to client (PacketType.Error)
                    }
                }
            }
        }

        private void HandlePacket(UdpPacket pkt)
        {
            // Establish/refresh sessions using Hello/Handshake
            // Note: installer "Startup" also uses Hello in this spine.
            if (pkt.Type == PacketTypes.Hello || pkt.Type == PacketTypes.Handshake)
            {
                var s = _sessions.UpsertFromClientHello(pkt.SenderInstanceId, pkt.RemoteEndPoint);

                // Reply with Handshake containing assigned SessionId (so client can include it in future packets)
                // Payload: 8 bytes SessionId (BE) + 8 bytes HostInstanceId (BE)
                byte[] pay = new byte[16];
                WriteU64BE(pay, 0, s.SessionId);
                WriteU64BE(pay, 8, (ulong)InstanceId);

                Send(PacketTypes.Handshake,
                    flags: UdpFlags.IsHandshake | UdpFlags.AckRequired,
                    sessionId: s.SessionId,
                    payload: pay,
                    ep: pkt.RemoteEndPoint);

                return;
            }

            // If packet claims a sessionId, validate it
            if (pkt.SessionId != 0)
            {
                if (_sessions.TryGetBySessionId(pkt.SessionId, out var s))
                    _sessions.Touch(s, pkt.RemoteEndPoint);
                else
                {
                    // unknown session -> ask client to handshake again
                    Send(PacketTypes.Handshake,
                        flags: UdpFlags.IsHandshake,
                        sessionId: 0,
                        payload: Array.Empty<byte>(),
                        ep: pkt.RemoteEndPoint);
                    return;
                }
            }

            // Keepalive support
            if (pkt.Type == PacketTypes.Ping)
            {
                Send(PacketTypes.Pong, UdpFlags.IsKeepAlive, pkt.SessionId, null, pkt.RemoteEndPoint);
                return;
            }

            // Everything else is a "command packet" (this is where you invent packet functions)
            OnCommandPacket(pkt);
        }

        private void OnCommandPacket(UdpPacket pkt)
        {
            // THIS is your packet function hub.
            // Build handlers here as you invent packet behaviors.

            switch (pkt.Type)
            {
                case PacketTypes.Status:
                    // TODO: decode payload, update UI/client status tracking
                    break;

                case PacketTypes.RequestFile:
                case PacketTypes.RequestRange:
                case PacketTypes.Chunk:
                case PacketTypes.CancelTransfer:
                case PacketTypes.PauseTransfer:
                case PacketTypes.ResumeTransfer:
                    // TODO: file transfer subsystem
                    break;

                case PacketTypes.ManifestRequest:
                case PacketTypes.ManifestResponse:
                case PacketTypes.HashList:
                case PacketTypes.VerifyResult:
                    // TODO: integrity / manifest subsystem
                    break;

                case PacketTypes.InstallBegin:
                case PacketTypes.InstallStep:
                case PacketTypes.InstallComplete:
                case PacketTypes.InstallFailed:
                    // TODO: install workflow packets
                    break;

                case PacketTypes.TxBegin:
                case PacketTypes.TxStageFile:
                case PacketTypes.TxCommit:
                case PacketTypes.TxRollback:
                case PacketTypes.TxEnd:
                    // TODO: transaction / rollback subsystem
                    break;

                default:
                    // TODO: unknown packet policy (ignore / warn / error)
                    break;
            }
        }

        private void SendStartupToInstaller()
        {
            if (UdpClientPort <= 0) return;

            var ep = new IPEndPoint(IPAddress.Loopback, UdpClientPort);

            // Startup packet: Hello with AckRequired so we know installer received it.
            _ = SendCriticalAsync(PacketTypes.Hello,
                flags: UdpFlags.AckRequired,
                sessionId: 0,
                payload: null,
                ep: ep,
                retries: 5,
                ackTimeoutMs: 600,
                retryDelayMs: 250);
        }

        // -------- Sending API --------

        public ulong Send(PacketTypes type, UdpFlags flags, ulong sessionId, byte[] payload, IPEndPoint ep)
        {
            ulong mid = NextMessageId();

            byte[] buf = UdpWire.Encode(
                signature4: "FESH",
                type: type,
                flags: flags,
                senderInstanceId: (ulong)InstanceId,
                messageId: mid,
                sessionId: sessionId,
                ackForMessageId: 0,
                payload: payload);

            _udp.Send(buf, buf.Length, ep);
            return mid;
        }

        public async Task<bool> SendCriticalAsync(
            PacketTypes type,
            UdpFlags flags,
            ulong sessionId,
            byte[] payload,
            IPEndPoint ep,
            int retries,
            int ackTimeoutMs,
            int retryDelayMs)
        {
            if (retries < 1) retries = 1;
            if (ackTimeoutMs < 1) ackTimeoutMs = 1;

            // critical implies AckRequired
            flags |= UdpFlags.AckRequired;

            for (int i = 0; i < retries; i++)
            {
                ulong mid = NextMessageId();

                Task<bool> wait = _acks.RegisterWait(mid, ep);

                byte[] buf = UdpWire.Encode(
                    signature4: "FESH",
                    type: type,
                    flags: flags,
                    senderInstanceId: (ulong)InstanceId,
                    messageId: mid,
                    sessionId: sessionId,
                    ackForMessageId: 0,
                    payload: payload);

                _udp.Send(buf, buf.Length, ep);

                // wait ack
                var done = await Task.WhenAny(wait, Task.Delay(ackTimeoutMs));
                if (done == wait && await wait)
                    return true;

                _acks.CancelWait(mid);

                if (retryDelayMs > 0)
                    await Task.Delay(retryDelayMs);
            }

            return false;
        }

        private void SendAck(IPEndPoint ep, ulong ackForMessageId)
        {
            // ACK packet: flags IsAck, AckForMessageId set, no payload
            ulong mid = NextMessageId();

            byte[] buf = UdpWire.Encode(
                signature4: "FESH",
                type: PacketTypes.Acknowledge,
                flags: UdpFlags.IsAck,
                senderInstanceId: (ulong)InstanceId,
                messageId: mid,
                sessionId: 0,
                ackForMessageId: ackForMessageId,
                payload: Array.Empty<byte>());

            _udp.Send(buf, buf.Length, ep);
        }

        private ulong NextMessageId()
            => (ulong)Interlocked.Increment(ref _nextMessageId);

        private async Task MaintenanceLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(_keepAliveTick, ct); }
                catch { break; }

                _sessions.Prune(_sessionTimeout);
            }
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
    }
}
*/