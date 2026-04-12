// NetForge.Networking.Managers/SessionManager.cs
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;

namespace NetForge.Networking.Managers;

public sealed class SessionManager
{
    public sealed class Session
    {
        private readonly object _lock = new();

        public Session(ulong sessionId, ulong clientInstanceId, IPEndPoint endPoint)
        {
            ArgumentNullException.ThrowIfNull(endPoint);

            SessionId = sessionId;
            ClientInstanceId = clientInstanceId;
            EndPoint = endPoint;
            LastSeenUtc = DateTime.UtcNow;
            Established = false;
        }

        public ulong SessionId { get; }
        public ulong ClientInstanceId { get; }
        public IPEndPoint EndPoint { get; private set; }
        public DateTime LastSeenUtc { get; private set; }
        public bool Established { get; private set; }

        public void Update(IPEndPoint endPoint, bool established)
        {
            ArgumentNullException.ThrowIfNull(endPoint);

            lock (_lock)
            {
                EndPoint = endPoint;
                LastSeenUtc = DateTime.UtcNow;
                Established = established;
            }
        }

        public void Touch(IPEndPoint endPoint)
        {
            ArgumentNullException.ThrowIfNull(endPoint);

            lock (_lock)
            {
                EndPoint = endPoint;
                LastSeenUtc = DateTime.UtcNow;
            }
        }

        public SessionSnapshot Snapshot()
        {
            lock (_lock)
            {
                return new SessionSnapshot(
                    SessionId,
                    ClientInstanceId,
                    EndPoint,
                    LastSeenUtc,
                    Established);
            }
        }

        public bool IsExpired(DateTime nowUtc, TimeSpan timeout)
        {
            lock (_lock)
            {
                return (nowUtc - LastSeenUtc) > timeout;
            }
        }
    }

    public sealed record SessionSnapshot(
        ulong SessionId,
        ulong ClientInstanceId,
        IPEndPoint EndPoint,
        DateTime LastSeenUtc,
        bool Established);

    private readonly ConcurrentDictionary<ulong, Session> _bySessionId = new();
    private readonly ConcurrentDictionary<ulong, ulong> _sessionIdByClientInstanceId = new();

    public Session UpsertFromClientHello(ulong clientInstanceId, IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        if (clientInstanceId == 0)
            throw new ArgumentOutOfRangeException(nameof(clientInstanceId), "ClientInstanceId cannot be 0.");

        if (_sessionIdByClientInstanceId.TryGetValue(clientInstanceId, out ulong existingSessionId) &&
            _bySessionId.TryGetValue(existingSessionId, out Session? existingSession))
        {
            existingSession.Update(endPoint, established: true);
            return existingSession;
        }

        ulong newSessionId = NewSessionId();

        Session session = new(newSessionId, clientInstanceId, endPoint);
        session.Update(endPoint, established: true);

        _bySessionId[newSessionId] = session;
        _sessionIdByClientInstanceId[clientInstanceId] = newSessionId;

        return session;
    }

    public bool TryGetBySessionId(ulong sessionId, out Session? session)
    {
        return _bySessionId.TryGetValue(sessionId, out session);
    }

    public bool TryGetByClientInstanceId(ulong clientInstanceId, out Session? session)
    {
        session = null;

        if (!_sessionIdByClientInstanceId.TryGetValue(clientInstanceId, out ulong sessionId))
            return false;

        return _bySessionId.TryGetValue(sessionId, out session);
    }

    public bool Touch(ulong sessionId, IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        if (!_bySessionId.TryGetValue(sessionId, out Session? session))
            return false;

        session.Touch(endPoint);
        return true;
    }

    public bool RemoveBySessionId(ulong sessionId)
    {
        if (!_bySessionId.TryRemove(sessionId, out Session? removed))
            return false;

        _sessionIdByClientInstanceId.TryRemove(removed.ClientInstanceId, out _);
        return true;
    }

    public int Prune(TimeSpan timeout)
    {
        DateTime nowUtc = DateTime.UtcNow;
        int removedCount = 0;

        foreach (KeyValuePair<ulong, Session> entry in _bySessionId)
        {
            Session session = entry.Value;

            if (!session.IsExpired(nowUtc, timeout))
                continue;

            if (_bySessionId.TryRemove(entry.Key, out Session? removed))
            {
                _sessionIdByClientInstanceId.TryRemove(removed.ClientInstanceId, out _);
                removedCount++;
            }
        }

        return removedCount;
    }

    public void Clear()
    {
        _bySessionId.Clear();
        _sessionIdByClientInstanceId.Clear();
    }

    private static ulong NewSessionId()
    {
        Span<byte> buffer = stackalloc byte[8];
        ulong sessionId;

        do
        {
            RandomNumberGenerator.Fill(buffer);
            sessionId = BitConverter.ToUInt64(buffer);
        }
        while (sessionId == 0);

        return sessionId;
    }
}