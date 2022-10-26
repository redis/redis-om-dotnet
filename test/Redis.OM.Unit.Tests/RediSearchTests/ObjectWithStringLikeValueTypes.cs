using System;
using System.Text.Json.Serialization;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Document(StorageType = StorageType.Json)]
    public class ObjectWithStringLikeValueTypes
    {
        [Indexed]
        public Ulid Ulid { get; set; }
        
        [Indexed]
        public bool Boolean { get; set; }
        
        [Indexed]
        public Guid Guid { get; set; }
        
        [Indexed]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AnEnum AnEnum { get; set; }
        
        [Indexed]
        public AnEnum AnEnumAsInt { get; set; }

        [Indexed]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EnumFlags Flags { get; set; }
    }
    
    [Document]
    public class ObjectWithStringLikeValueTypesHash
    {
        [Indexed]
        public Ulid Ulid { get; set; }
        
        [Indexed]
        public bool Boolean { get; set; }
        
        [Indexed]
        public Guid Guid { get; set; }
        
        [Indexed]
        public AnEnum AnEnum { get; set; }
    }
}