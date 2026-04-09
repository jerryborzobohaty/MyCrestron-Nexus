using System.Collections.Generic;
using System.Linq;

namespace NexusVtp
{
    /// <summary>
    /// Utility helper class providing common operations for touchpanel functionality
    /// </summary>
    public static class TpHelper
    {
        /// <summary>
        /// Sets all values in a boolean dictionary to the specified value
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
        /// <param name="dict">The dictionary to update</param>
        /// <param name="value">The boolean value to set for all dictionary entries</param>
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
