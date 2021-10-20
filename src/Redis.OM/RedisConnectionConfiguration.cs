namespace Redis.OM
{
    /// <summary>
    /// Configuration to use to configure redis.
    /// </summary>
    public class RedisConnectionConfiguration
    {
        /// <summary>
        /// Gets or sets the Host name.
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the Port.
        /// </summary>
        public int Port { get; set; } = 6379;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Builds SE connection string.
        /// </summary>
        /// <returns>A connection string.</returns>
        public string ToStackExchangeConnectionString() => $"{Host}:{Port},password{Password}";
    }
}
