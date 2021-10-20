namespace Redis.OM
{
    /// <summary>
    /// The strategy the library will use for generating unique IDs.
    /// </summary>
    public interface IIdGenerationStrategy
    {
        /// <summary>
        /// generates a unique id.
        /// </summary>
        /// <returns>the id.</returns>
        string GenerateId();
    }
}
