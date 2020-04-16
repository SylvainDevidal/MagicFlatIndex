using System;
using System.Collections.Generic;
using System.Text;

namespace MagicFlatIndex
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            else
            {
                return value.Substring(0, Math.Min(value.Length, maxLength));
            }
        }

        public static void Shuffle<T>(this Random rnd, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rnd.Next(n--);
                (array[n], array[k]) = (array[k], array[n]);
            }
        }
    }
}
