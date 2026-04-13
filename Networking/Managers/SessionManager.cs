// NetForge.Networking.Managers/SessionManager.cs
using NetForge.Networking.Enums;
using System;
using System.Collections.Concurrent;
using System.Net;

namespace NetForge.Networking.Managers;

public sealed class SessionManager
{
    public sealed class Session
    {
        private readonly object _lock = new();

        public ulong SessionId { get; }
        public ulong ClientInstanceId { get; private set; }
        public IPEndPoint? EndPoint { get; private set; }
        public DateTime EstablishedUtc { get; }
        public DateTime LastSeenUtc { get; private set; }

        public bool Established { get; private set; }

        // Raw score used internally by your tests.
        public int TrustScore { get; private set; }

        public TrustModel TrustLevel => ResolveTrustLevel(TrustScore);

        public Session(ulong sessionId, ulong clientInstanceId, IPEndPoint? endPoint)
        {
            SessionId = sessionId;
            ClientInstanceId = clientInstanceId;
            EndPoint = endPoint;
            EstablishedUtc = DateTime.UtcNow;
            LastSeenUtc = EstablishedUtc;

            Established = false;
            TrustScore = 0;
        }

        public void MarkEstablished()
        {
            lock (_lock)
            {
                Established = true;
                LastSeenUtc = DateTime.UtcNow;
            }
        }

        public void Touch(IPEndPoint? endPoint = null, ulong? clientInstanceId = null)
        {
            lock (_lock)
            {
                if (endPoint is not null)
                    EndPoint = endPoint;

                if (clientInstanceId.HasValue && clientInstanceId.Value != 0)
                    ClientInstanceId = clientInstanceId.Value;

                LastSeenUtc = DateTime.UtcNow;
            }
        }

        public void SetTrustScore(int score)
        {
            lock (_lock)
            {
                TrustScore = Math.Clamp(score, 0, 100);
                LastSeenUtc = DateTime.UtcNow;
            }
        }

        public void AdjustTrustScore(int delta)
        {
            lock (_lock)
            {
                TrustScore = Math.Clamp(TrustScore + delta, 0, 100);
                LastSeenUtc = DateTime.UtcNow;
            }
        }

        public bool MeetsTrust(TrustModel minimum)
        {
            lock (_lock)
            {
                return Established && TrustScore >= (int)minimum;
            }
        }

        private static TrustModel ResolveTrustLevel(int score)
        {
            if (score >= (int)TrustModel.Authenticated) return TrustModel.Authenticated;
            if (score >= (int)TrustModel.Elevated) return TrustModel.Elevated;
            if (score >= (int)TrustModel.Trusted) return TrustModel.Trusted;
            if (score >= (int)TrustModel.Validated) return TrustModel.Validated;
            if (score >= (int)TrustModel.Basic) return TrustModel.Basic;
            if (score >= (int)TrustModel.HandShaking) return TrustModel.HandShaking;
            return TrustModel.None;
        }
    }

    private readonly ConcurrentDictionary<ulong, Session> _sessions = new();

    public Session Create(ulong sessionId, ulong clientInstanceId, IPEndPoint? endPoint)
    {
        var session = new Session(sessionId, clientInstanceId, endPoint);
        _sessions[sessionId] = session;
        return session;
    }

    public Session Upsert(ulong sessionId, ulong clientInstanceId, IPEndPoint? endPoint)
    {
        return _sessions.AddOrUpdate(
            sessionId,
            _ => new Session(sessionId, clientInstanceId, endPoint),
            (_, existing) =>
            {
                existing.Touch(endPoint, clientInstanceId);
                return existing;
            });
    }

    public bool TryGet(ulong sessionId, out Session? session)
        => _sessions.TryGetValue(sessionId, out session);

    public bool Remove(ulong sessionId)
        => _sessions.TryRemove(sessionId, out _);

    public bool MeetsTrust(ulong sessionId, TrustModel minimum)
    {
        return _sessions.TryGetValue(sessionId, out Session? session) &&
               session is not null &&
               session.MeetsTrust(minimum);
    }

    public bool AdjustTrust(ulong sessionId, int delta)
    {
        if (!_sessions.TryGetValue(sessionId, out Session? session) || session is null)
            return false;

        session.AdjustTrustScore(delta);
        return true;
    }

    public bool SetTrust(ulong sessionId, int score)
    {
        if (!_sessions.TryGetValue(sessionId, out Session? session) || session is null)
            return false;

        session.SetTrustScore(score);
        return true;
    }
}