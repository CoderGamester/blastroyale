using System.Linq;
using GameLovers.GameData;

namespace FirstLight.Game.Utils
{
    /// <summary>Game-wide refresh after loading a different saved player state.</summary>
    public static class ObservableExtensions
    {
        public static void NotifyAll<TKey, TValue>(this IObservableDictionary<TKey, TValue> dictionary)
        {
            // Observers may mutate the collection, so iterate a stable set of keys.
            foreach (var key in dictionary.Select(pair => pair.Key).ToArray())
            {
                if (dictionary.ContainsKey(key)) dictionary.InvokeUpdate(key);
            }
        }

        public static void NotifyAll<T>(this IObservableList<T> list)
        {
            for (var index = 0; index < list.Count; index++) list.InvokeUpdate(index);
        }
    }
}
