using System;
using System.Text;

namespace Redis.OM
{
    /// <summary>
    /// Generation strategy to creat UUID4 Ids.
    /// </summary>
    public class Uuid4IdGenerationStrategy : IIdGenerationStrategy
    {
        /// <inheritdoc/>
        public string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
