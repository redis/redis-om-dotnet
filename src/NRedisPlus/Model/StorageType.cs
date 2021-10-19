namespace NRedisPlus.Model
{
    /// <summary>
    /// Determine how the item will be stored in Redis.
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// Store as a hash.
        /// </summary>
        Hash = 0,

        /// <summary>
        /// Store as JSON.
        /// </summary>
        Json = 1,
    }
}
