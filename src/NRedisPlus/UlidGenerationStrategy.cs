using System;

namespace NRedisPlus
{
    public class UlidGenerationStrategy : IIdGenerationStrategy
    {
        public string GenerateId()
        {
            return Ulid.NewUlid().ToString();
        }
    }
}