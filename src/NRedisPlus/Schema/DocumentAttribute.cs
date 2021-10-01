using System;

namespace NRedisPlus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DocumentAttribute : Attribute
    {
        public StorageType StorageType { get; set; } = StorageType.HASH;
        public IIdGenerationStrategy IdGenerationStrategy { get; set; } = new Uuid4IdGenerationStrategy();
        public string[]? Prefixes { get; set; }        
        public string? IndexName { get; set; }
        public string? Language { get; set; }
        public string? LanguageField { get; set; }
        public string? Filter { get; set; }        
    }
}
