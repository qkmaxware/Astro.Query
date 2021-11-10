using System;
using System.Threading;

namespace Qkmaxware.Astro.Query {

/// <summary>
/// Rate limiter for APIs with rate limits
/// </summary>
public class RateLimiter {

    private int queriesPerDuration;
    private TimeSpan duration;

    public int QueriesPerSecond => (int)Math.Floor(queriesPerDuration / duration.TotalSeconds);
    private DateTime timestamp = DateTime.Now;
    private int queriesSinceTimestamp = 0;

    private object key = new object();

    /// <summary>
    /// Create a new rate limiter
    /// </summary>
    /// <param name="queries">max number of queries in the given timespan</param>
    /// <param name="duration">timespace over which the number of queries is allowed</param>
    public RateLimiter(int queries, TimeSpan duration) {
        this.queriesPerDuration = Math.Max(queries, 1);
        this.duration = duration;
    }

    /// <summary>
    /// Consume a query slot blocking if required
    /// </summary>
    /// <param name="func">function to execute</param>
    /// <typeparam name="T">return type of the function</typeparam>
    public T Invoke<T>(Func<T> func) where T:class {
        T value = default(T);
        Invoke(() => {
            value = func();
        });
        return value;
    }

    /// <summary>
    /// Consime a query slot by executing the given action 
    /// </summary>
    /// <param name="action">action to execute</param>
    public void Invoke (Action action) {
        lock(key) {
            // Delay if we need to
            var now = DateTime.Now;
            var timeSinceTimestamp = now - timestamp;
            if (timeSinceTimestamp > duration) {
                // Reset
                timestamp = now;
                queriesSinceTimestamp = 1;
            } else {
                queriesSinceTimestamp++;
                if (queriesSinceTimestamp > queriesPerDuration) {
                    // Delay / reset
                    Thread.Sleep(duration);
                    timestamp = DateTime.Now;
                    queriesSinceTimestamp = 1;
                }
            }
                        
            // Call the api action
            action();
        }
    }
}

}