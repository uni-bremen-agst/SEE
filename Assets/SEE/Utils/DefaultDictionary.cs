using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// A dictionary which adds and returns a default value if the key is not present.
    /// Note that the dictionary must not be downcast to a normal dictionary, as this would
    /// remove the default value functionality.
    /// </summary>
    /// <typeparam name="K">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="V">The type of the values in the dictionary.</typeparam>
    public class DefaultDictionary<K, V> : Dictionary<K, V> where V : new()
    {
        public new V this[K key]
        {
            get => this.GetOrAdd(key, () => new V());
            set => base[key] = value;
        }
    }
}
