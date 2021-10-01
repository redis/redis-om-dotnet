using System;
using System.Text;

namespace NRedisPlus
{
    public class Uuid4IdGenerationStrategy : IIdGenerationStrategy
    {
        public string GenerateId()
        {
            var guid = Guid.NewGuid();
            var guidBytes = Encoding.UTF8.GetBytes(guid.ToString());
            var id = Convert.ToBase64String(guidBytes);
            return id;
        }
    }
}