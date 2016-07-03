using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GothicStarter.Utils
{
    public static class CommonUtils
    {
        /// <summary>
        /// Pro každý blok o velikosti <paramref name="blockSize"/> otočí pořadí všech prvků.
        /// př. blockSize = 2: [1, 2, 3, 4, 5, 6 ] -> [2, 1, 4, 3, 6, 5 ].
        /// </summary>
        /// <param name="data">Velikost dat musí být dělitelná počtem prvků.</param>
        public static void SwapElementsInBlocks<T>(this T[] data, int blockSize)
        {
            for (int i = 0; i < data.Length; i += blockSize)
            {
                for (int j = 0; j < blockSize / 2; j++)
                {
                    T tmp = data[i + j];
                    data[i + j] = data[i + blockSize - j];
                    data[i + blockSize - j] = tmp;
                }
            }
        }

        /// <summary>
        /// Vrátí defaudní hodnotu <paramref name="TValue"/>, pokud klíč v <paramref name="dictionary"/> neexistuje.
        /// </summary>
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) 
            => dictionary.ContainsKey(key) ? dictionary[key] : default(TValue);

        /// <summary>
        /// Rozdělí text podle <paramref name="delimiter"/>. Prázdné části zahodí.
        /// </summary>
        public static string[] Split(this string value, string delimiter) 
            => value.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
    }
}
