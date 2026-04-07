using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusVtp
{
    public static class TpHelper
    {
        public static void SetAllDictionaryValues<TKey>(Dictionary<TKey, bool> dict, bool value)
        {
            var keys = dict.Keys.ToList();
            foreach (var key in keys)
            {
                dict[key] = value;
            }
        }
    }
}
