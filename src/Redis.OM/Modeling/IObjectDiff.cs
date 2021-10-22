namespace Redis.OM.Modeling
{
    /// <summary>
    /// Handles resolving the difference between a snapshot and a current iteration of an object.
    /// </summary>
    internal interface IObjectDiff
    {
        /// <summary>
        /// Gets the name of the script to use.
        /// </summary>
        string Script { get; }

        /// <summary>
        /// Arguments for the script serialized.
        /// </summary>
        /// <returns>The args.</returns>
        string[] SerializeScriptArgs();
    }
}
