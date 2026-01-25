using System.Collections.Generic;

namespace Lastfm.Scrobbler.Core.Interfaces
{
    /// <summary>
    /// Interface for managing a queue of scrobbles per user.
    /// </summary>
    /// <typeparam name="T">The scrobble item type.</typeparam>
    /// <typeparam name="TUserId">The user identifier type.</typeparam>
    public interface IScrobbleQueue<T, TUserId>
    {
        /// <summary>
        /// Adds a scrobble to the queue for a specific user.
        /// </summary>
        /// <param name="item">The scrobble item.</param>
        /// <param name="userId">The user identifier.</param>
        void Enqueue(T item, TUserId userId);

        /// <summary>
        /// Retrieves and removes up to the specified number of scrobbles for a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="count">Maximum number of items to dequeue.</param>
        /// <returns>Enumerable of dequeued scrobbles.</returns>
        IEnumerable<T> Dequeue(TUserId userId, int count);

        /// <summary>
        /// Gets all pending scrobbles for a specific user without removing them.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Enumerable of pending scrobbles.</returns>
        IEnumerable<T> GetPending(TUserId userId);

        /// <summary>
        /// Gets the total count of pending scrobbles across all users.
        /// </summary>
        /// <returns>Total number of pending scrobbles.</returns>
        int GetTotalPendingCount();
    }
}
