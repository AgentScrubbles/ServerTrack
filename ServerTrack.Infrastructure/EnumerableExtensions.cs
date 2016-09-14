using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ServerTrack.Infrastructure
{
    public static class EnumerableExtensions
    {
        public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue, TModel>(
            this IEnumerable<TModel> source, Func<TModel, TKey> keySelector, Func<TModel, TValue> valueSelector)
        {
            var dict = new ConcurrentDictionary<TKey, TValue>();
            source.AsParallel().ForAll(k =>
            {
                dict[keySelector.Invoke(k)] = valueSelector.Invoke(k);
            });
            return dict;
        }

        public static ConcurrentBag<TModel> ToConcurrengBag<TModel>(this IEnumerable<TModel> source)
        {
            var bag = new ConcurrentBag<TModel>();
            source.AsParallel().ForAll(k => bag.Add(k));
            return bag;
        } 

        public static ConcurrentDictionary<TKey, ConcurrentBag<TValue>> ToCombinedConcurrentDictionary
            <TKey, TValue, TModel>(this IEnumerable<TModel> source, Func<TModel, TKey> keySelector,
                Func<TModel, TValue> valueSelector)
        {
            return
                source.Select(k => new {key = keySelector.Invoke(k), value = valueSelector.Invoke(k)})
                    .GroupBy(k => k.key)
                    .ToConcurrentDictionary(k => k.Key, v => v.Select(j => j.value).ToConcurrengBag());
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
        {
            return source.ContainsKey(key) ? source[key] : default(TValue);
        }

        public static TModel[] ToIndexedArray<TModel>(this IEnumerable<TModel> source, Func<TModel, int> keySelector, Func<IEnumerable<TModel>, TModel> collisionSelector, int? maxIndex = null)
        {
            var dict = source.GroupBy(k => keySelector.Invoke(k)).ToDictionary(k => k.Key, v => collisionSelector.Invoke(v));
            var index = maxIndex ?? (dict.Keys.Any() ? dict.Keys.Max() + 1 : 0);
            var arr = new TModel[index];
            foreach (var key in dict.Keys)
            {
                arr[key] = dict[key];
            }
            return arr;
        }
    }
}
