using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// An exception thrown when command execution fails against Redis.
    /// </summary>
    public class RedisExecutionException : RedisOmException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisExecutionException"/> class.
        /// </summary>
        /// <param name="message">the message.</param>
        public RedisExecutionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisExecutionException"/> class.
        /// </summary>
        /// <param name="message">the message.</param>
        /// <param name="exception">inner exception.</param>
        public RedisExecutionException(string message, Exception exception)
            : base(message, exception)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisExecutionException"/> class.
        /// </summary>
        /// <param name="exception">inner exception.</param>
        public RedisExecutionException(Exception exception)
            : base(exception.Message, exception)
        {
        }
    }
}