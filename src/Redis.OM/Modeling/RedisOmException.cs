using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Base exception for exceptions thrown by Redis OM.
    /// </summary>
    public abstract class RedisOmException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisOmException"/> class.
        /// </summary>
        /// <param name="message">the message.</param>
        protected RedisOmException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisOmException"/> class.
        /// </summary>
        /// <param name="message">the message.</param>
        /// <param name="exception">inner exception.</param>
        protected RedisOmException(string message, Exception exception)
            : base(message, exception)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisOmException"/> class.
        /// </summary>
        /// <param name="exception">inner exception.</param>
        protected RedisOmException(Exception exception)
            : base(exception.Message, exception)
        {
        }
    }
}