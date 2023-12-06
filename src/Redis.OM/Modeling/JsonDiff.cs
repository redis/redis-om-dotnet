using System;
using System.Globalization;
using System.Text.Json;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// builds a diff for a json object.
    /// </summary>
    internal class JsonDiff : IObjectDiff
    {
        private readonly string _operation;
        private readonly string _path;
        private readonly JToken _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDiff"/> class.
        /// </summary>
        /// <param name="operation">the operation to perform.</param>
        /// <param name="path">the path to the item in the json.</param>
        /// <param name="value">the value to set.</param>
        internal JsonDiff(string operation, string path, JToken value)
        {
            _operation = operation;
            _path = path;
            _value = value;
        }

        /// <inheritdoc/>
        public string Script => nameof(Scripts.JsonDiffResolution);

        /// <inheritdoc/>
        public string[] SerializeScriptArgs()
        {
            return _value.Type switch
            {
                JTokenType.String => new[] { _operation, _path, $"\"{HttpUtility.JavaScriptStringEncode(_value.ToString())}\"" },
                JTokenType.Float => new[] { _operation, _path, ((JValue)_value).ToString(CultureInfo.InvariantCulture) },
                JTokenType.Boolean => new[] { _operation, _path, _value.ToString().ToLower() },
                JTokenType.Date => SerializeAsDateTime(),
                _ => new[] { _operation, _path, _value.ToString() }
            };
        }

        private string[] SerializeAsDateTime()
        {
            var jValue = (JValue)_value;
            if (jValue.Value is DateTimeOffset)
            {
                return new[]
                {
                    _operation,
                    _path,
                    $"{JsonSerializer.Serialize(_value.Value<DateTimeOffset>())}",
                };
            }

            return new[] { _operation, _path, $"{JsonSerializer.Serialize(_value.Value<DateTime>())}" };
        }
    }
}
