namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;

    #endregion

    static class DictionaryExtensions
    {
        public static V Find<K, V>(this IDictionary<K, V> dict, K key)
        {
            return Find(dict, key, default(V));
        }

        public static V Find<K, V>(this IDictionary<K, V> dict, K key, V @default)
        {
            if (dict == null) throw new ArgumentNullException("dict");
            V value;
            return dict.TryGetValue(key, out value) ? value : @default;
        }
    }
}
