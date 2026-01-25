using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Lastfm.Scrobbler.Core.Interfaces;

namespace Lastfm.Scrobbler.Core;

/// <summary>
/// In-memory scrobble queue implementation with per-user queuing.
/// </summary>
/// <typeparam name="T">The scrobble item type.</typeparam>
/// <typeparam name="TUserId">The user identifier type.</typeparam>
public class ScrobbleQueue<T, TUserId> : IScrobbleQueue<T, TUserId> where TUserId : notnull
{
    private readonly ConcurrentDictionary<TUserId, ConcurrentQueue<T>> _userQueues = new();

    /// <inheritdoc />
    public void Enqueue(T item, TUserId userId)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (userId == null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var queue = _userQueues.GetOrAdd(userId, _ => new ConcurrentQueue<T>());
        queue.Enqueue(item);
    }

    /// <inheritdoc />
    public IEnumerable<T> Dequeue(TUserId userId, int count)
    {
        if (userId == null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        }

        if (!_userQueues.TryGetValue(userId, out var queue))
        {
            return Enumerable.Empty<T>();
        }

        var items = new List<T>();
        for (int i = 0; i < count; i++)
        {
            if (queue.TryDequeue(out var item))
            {
                items.Add(item);
            }
            else
            {
                break;
            }
        }

        return items;
    }

    /// <inheritdoc />
    public IEnumerable<T> GetPending(TUserId userId)
    {
        if (userId == null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        if (_userQueues.TryGetValue(userId, out var queue))
        {
            return queue.ToArray();
        }

        return Enumerable.Empty<T>();
    }

    /// <inheritdoc />
    public int GetTotalPendingCount()
    {
        return _userQueues.Values.Sum(q => q.Count);
    }
}
