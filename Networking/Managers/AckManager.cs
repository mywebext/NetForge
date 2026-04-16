//NetForge.Networking.Managers/AckManager.cs
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace NetForge.Networking.Managers
{
    /// <summary>
    /// Tracks outbound packets that are waiting for ACK responses.
    /// </summary>
    /// <remarks>
    /// ACKs are matched by the original outbound message ID using
    /// <c>AckForMessageId</c> from the inbound ACK packet header.
    ///
    /// The expected remote endpoint is also validated so an ACK from an
    /// unexpected source does not complete the wrong pending wait.
    /// </remarks>
    public sealed class AckManager
    {
        private sealed class Pending
        {
            public TaskCompletionSource<bool> Tcs { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public required IPEndPoint Expected { get; init; }
        }

        private readonly ConcurrentDictionary<ulong, Pending> _pending = new();

        /// <summary>
        /// Registers a wait for an ACK tied to the specified outbound message ID.
        /// </summary>
        /// <param name="messageId">
        /// The outbound packet message ID that the ACK is expected to acknowledge.
        /// </param>
        /// <param name="expected">
        /// The expected remote endpoint that must send the ACK.
        /// </param>
        /// <returns>
        /// A task that completes <c>true</c> when the matching ACK is received,
        /// or <c>false</c> if the wait is canceled.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expected"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a wait is already registered for the same message ID.
        /// </exception>
        public Task<bool> RegisterWait(ulong messageId, IPEndPoint expected)
        {
            ArgumentNullException.ThrowIfNull(expected);

            Pending pending = new()
            {
                Expected = expected
            };

            if (!_pending.TryAdd(messageId, pending))
                throw new InvalidOperationException($"Duplicate messageId pending: {messageId}");

            return pending.Tcs.Task;
        }

        /// <summary>
        /// Cancels a pending ACK wait for the specified outbound message ID.
        /// </summary>
        /// <param name="messageId">
        /// The outbound message ID whose wait should be canceled.
        /// </param>
        /// <remarks>
        /// If no pending wait exists, this method does nothing.
        /// </remarks>
        public void CancelWait(ulong messageId)
        {
            if (_pending.TryRemove(messageId, out Pending? pending))
                pending.Tcs.TrySetResult(false);
        }

        /// <summary>
        /// Attempts to complete a pending ACK wait using the received ACK information.
        /// </summary>
        /// <param name="ackForMessageId">
        /// The original outbound message ID being acknowledged.
        /// </param>
        /// <param name="from">
        /// The remote endpoint that sent the ACK.
        /// </param>
        /// <remarks>
        /// If the ACK comes from an unexpected endpoint, the pending entry is
        /// restored and the ACK is ignored.
        ///
        /// If no matching pending wait exists, this method does nothing.
        /// </remarks>
        public void HandleAck(ulong ackForMessageId, IPEndPoint from)
        {
            if (from is null)
                return;

            if (!_pending.TryRemove(ackForMessageId, out Pending? pending))
                return;

            if (!pending.Expected.Equals(from))
            {
                _pending.TryAdd(ackForMessageId, pending);
                return;
            }

            pending.Tcs.TrySetResult(true);
        }

        /// <summary>
        /// Returns true if the specified message ID is currently waiting for an ACK.
        /// </summary>
        public bool IsWaiting(ulong messageId)
        {
            return _pending.ContainsKey(messageId);
        }

        /// <summary>
        /// Clears all pending waits and completes them as canceled.
        /// </summary>
        public void CancelAll()
        {
            foreach (KeyValuePair<ulong, Pending> entry in _pending)
            {
                if (_pending.TryRemove(entry.Key, out Pending? pending))
                    pending.Tcs.TrySetResult(false);
            }
        }
    }
}