using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// An exception thrown when command execution fails against Redis.
    /// </summary>
    public class RedisStateException : RedisOmException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisStateException"/> class.
        /// </summary>
        /// <param name="message">the message.</param>
        public RedisStateException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisStateException"/> class.
        /// </summary>
        /// <param name="message">the message.</param>
        /// <param name="exception">inner exception.</param>
        public RedisStateException(string message, Exception exception)
            : base(message, exception)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisStateException"/> class.
        /// </summary>
        /// <param name="exception">inner exception.</param>
        public RedisStateException(Exception exception)
            : base(exception.Message, exception)
        {
        }
    }
}