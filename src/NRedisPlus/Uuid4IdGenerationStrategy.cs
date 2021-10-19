using System;
using System.Text;

namespace NRedisPlus
{
    /// <summary>
    /// Generation strategy to creat UUID4 Ids.
    /// </summary>
    public class Uuid4IdGenerationStrategy : IIdGenerationStrategy
    {
        /// <inheritdoc/>
        public string GenerateId()
        {
            var guid = Guid.NewGuid();
            var guidBytes = Encoding.UTF8.GetBytes(guid.ToString());
            var id = Convert.ToBase64String(guidBytes);
            return id;
        }
    }
}
