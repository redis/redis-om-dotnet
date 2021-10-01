using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus
{
    public interface IRedisHydrateable
    {
        void Hydrate(IDictionary<string, string> dict);
        IDictionary<string, string> BuildHashSet();
    }
}
