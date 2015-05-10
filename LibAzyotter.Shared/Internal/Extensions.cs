using System.Collections.Generic;
using System.Linq;

namespace LibAzyotter.Internal
{
    internal static class Extensions
    {
        public static IReadOnlyDictionary<TKey, TValue> AsDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            var dic = source as IReadOnlyDictionary<TKey, TValue>;
            return dic != null
                ? dic
                : source.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
