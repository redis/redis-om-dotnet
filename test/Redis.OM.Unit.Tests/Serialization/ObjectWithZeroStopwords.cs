using System;
using System.Collections.Generic;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document(Stopwords = new string[0])]
    public class ObjectWithZeroStopwords
    {
        [Indexed]
        public string? Name { get; set; }
    }
}