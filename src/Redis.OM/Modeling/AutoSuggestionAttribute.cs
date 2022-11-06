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
        /// Gets or sets the string value for the AutoSuggestion.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the string value for the AutoSuggestion.
        /// </summary>
        public string? String { get; set; }

        /// <summary>
        /// Gets or sets the float Score for the AutoSuggestion.
        /// </summary>
        public string? Score { get; set; }

        /// <summary>
        /// Gets or sets the float Score for the AutoSuggestion.
        /// </summary>
        public AutoSuggestionOptionalParameters? OptionalParameters { get; set; }

        /// <summary>
        /// Gets or sets the float Score for the AutoSuggestion.
        /// </summary>
        public string? Prefix { get; set; }

        /// <summary>
        /// Gets or sets the float Score for the AutoSuggestion.
        /// </summary>
        public string? Payload { get; set; }
    }
}
