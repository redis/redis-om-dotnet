using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// An exception thrown when trying to index classes in Redis.
    /// </summary>
    public class RedisIndexingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisIndexingException"/> class.
        /// </summary>
        /// <param name="message">the message.</param>
        public RedisIndexingException(string message)
            : base(message)
        {
        }
    }
}