using System;
using System.Collections.Generic;
using System.Linq;

namespace STPLocalSearch
{
    public static class Extensions
    {
        /// <summary>
        /// This method removes the unneeded whitespace at the start and end of a string.
        /// </summary>
        /// <param name="input">The string to remove the whitespaces from.</param>
        /// <returns>The same string, but without whitespaces at the beginning.</returns>
        public static string RemoveWhitespace(this string input)
        {
            if (input == null)
                return "";

            string output = input;
            while (output.Length > 0 && output[0] == ' ')
            {
                output = output.Substring(1);
            }

            while (output.Length > 0 && output[output.Length - 1] == ' ')
            {
                output = output.Substring(0, output.Length - 1);
            }

            return output;
        }

        public static T RandomElement<T>(this IEnumerable<T> enumerable) where T : class
        {
            var list = enumerable.ToList();
            if (list.Count == 0)
                return null;

            var index = new Random().Next(0, list.Count - 1);
            return list[index];
        }

        public static IEnumerable<T> RandomElements<T>(this IEnumerable<T> enumerable, int numberOfRandoms, int numberOfItemsInEnumerable = -1) where T : class
        {
            var rnd  = new Random();
            HashSet<int> indices = new HashSet<int>();
            int n = numberOfItemsInEnumerable == -1 ? enumerable.Count() : numberOfItemsInEnumerable;
            // Need more items than there are, no randomness at all!
            if (numberOfRandoms >= n)
            {
                foreach (var item in enumerable)
                    yield return item;
                yield break;
            }

            // Pick random indices
            for (int i = 0; i < numberOfRandoms; i++)
                while (!indices.Add(rnd.Next(n))) { }

            int currentIndex = 0;
            foreach (var item in enumerable)
            {
                if (indices.Contains(currentIndex))
                    yield return item;
                currentIndex++;
            }
        }
    }
}
