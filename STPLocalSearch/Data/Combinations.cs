using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace STPLocalSearch.Data
{
    public class Combinations<T> : IEnumerable<IEnumerable<T>>
    {
        private readonly IEnumerable<T> _items;
        private readonly int _p;
        private readonly int _n;

        public Combinations(IEnumerable<T> items, int p)
        {
            _items = items;
            _p = p;
            _n = _items.Count();
        }

        public IEnumerator<IEnumerable<T>> GetEnumerator()
        {
            if (_p == 0)
            {
                yield return Enumerable.Empty<T>();
                yield break;
            }

            int combinationsOfPOutOfN = NumberOfCombinations(_n, _p);
            for (int i = 0; i < combinationsOfPOutOfN; i++)
                yield return GetCombinationAtIndex(i);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<IEnumerable<T>> GetRandomCombinations(int numberOfRandomCombinations)
        {
            var rnd = new Random();
            HashSet<int> indices = new HashSet<int>();
            int n = NumberOfCombinations(_n, _p);
            // Need more items than there are, no randomness at all!
            if (numberOfRandomCombinations >= n)
            {
                foreach (var item in this)
                    yield return item;
                yield break;
            }

            // Pick random indices
            for (int i = 0; i < numberOfRandomCombinations; i++)
                while (!indices.Add(rnd.Next(n))) { }

            foreach (var index in indices)
            {
                yield return GetCombinationAtIndex(index);
            }
        }

        private List<T> GetCombinationAtIndex(int index)
        {
            List<T> combination = new List<T>(_p);
            int n = _n - 1;
            int p = _p - 1;
            int indexUpperBound = 0;

            foreach (var item in _items)
            {
                if (p < 0)
                    break;

                var prevIndex = indexUpperBound;
                indexUpperBound = NumberOfCombinations(n, p);
                if (index-prevIndex < indexUpperBound)
                {
                    combination.Add(item);
                    indexUpperBound = 0;
                    p--;
                }

                index -= prevIndex;
                n--;
                
                if (combination.Count == _p)
                    break;
            }

            return combination;
        }

        /// <summary>
        /// Method to calculate how many unordered p-sized combinations exist, from a set of n elements.
        /// </summary>
        /// <param name="n">The size of the entire set.</param>
        /// <param name="p">The size of one unordered combination.</param>
        /// <returns></returns>
        public static int NumberOfCombinations(int n, int p)
        {
            if (p < 0 || n < 0)
                throw new ArgumentException("p and n should be larger than or equal to zero.");

            if (p >= n || n == 1 || p == 0)
                return 1;

            return NumberOfCombinations(n - 1, p - 1) * n / p;
        }
    }
}
