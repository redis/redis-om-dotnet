using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// An attribute to use to decorate class level objects you wish to store in redis.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoSuggestionAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the index, will default to sugg:className.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets payload for the AutoSuggestion.
        /// </summary>
        public bool Payload { get; set; }
    }
}
