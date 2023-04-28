namespace Redis.OM.Searching.Query
{
    /// <summary>
    /// Represents a return field with a name and an optional alias.
    /// </summary>
    public struct ReturnField
    {
        /// <summary>
        /// The name of the return field.
        /// </summary>
        public string Name;

        /// <summary>
        /// An optional alias for the return field.
        /// </summary>
        public string? Alias;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReturnField"/> struct with the specified name and alias.
        /// </summary>
        /// <param name="name">The name of the return field.</param>
        /// <param name="alias">An optional alias for the return field.</param>
        public ReturnField(string name, string alias)
        {
            Name = name;
            Alias = alias;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReturnField"/> struct with the specified name.
        /// </summary>
        /// <param name="name">The name of the return field.</param>
        public ReturnField(string name)
        {
            Name = name;
            Alias = null;
        }
    }
}