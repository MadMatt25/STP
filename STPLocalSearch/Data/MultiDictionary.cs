using System;
using System.Collections;
using System.Collections.Generic;

namespace STPLocalSearch.Data
{
    public class MultiDictionary<TKey, TValue>
    {
        private readonly Dictionary<Tuple<TKey, TKey>, TValue> _dictionary = new Dictionary<Tuple<TKey, TKey>, TValue>();

        public MultiDictionary()
        {

        }

        public bool ContainsKey(TKey key1, TKey key2)
        {
            return FindKeyInDictionary(key1, key2) != null;
        }

        public void Add(TKey key1, TKey key2, TValue value)
        {
            var key = FindKeyInDictionary(key1, key2);
            if (key != null)
                throw new InvalidOperationException("This key is already in the dictionary!");
            else
                _dictionary.Add(new Tuple<TKey, TKey>(key1, key2), value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public void Remove(TKey key1, TKey key2)
        {
            var key = FindKeyInDictionary(key1, key2);
            _dictionary.Remove(key);
        }

        public TValue this[TKey key1, TKey key2]
        {
            get
            {
                var key = FindKeyInDictionary(key1, key2);
                if (key != null)
                    return _dictionary[key];
                else
                    throw new InvalidOperationException("There is no item with this key in the dictionary.");
            }
            set
            {
                var key = FindKeyInDictionary(key1, key2);
                if (key != null)
                    _dictionary[key] = value;
                else
                    throw new InvalidOperationException("There is no item with this key in the dictionary.");
            }
        }

        public ICollection Keys
        {
            get { return _dictionary.Keys; }
        }

        public ICollection Values
        {
            get { return _dictionary.Values; }
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        private Tuple<TKey, TKey> FindKeyInDictionary(TKey key1, TKey key2)
        {
            Tuple<TKey, TKey> key;
            key = new Tuple<TKey, TKey>(key1, key2);
            if (_dictionary.ContainsKey(key))
                return key;

            key = new Tuple<TKey, TKey>(key2, key1);
            if (_dictionary.ContainsKey(key))
                return key;

            return null;
        }
    }
}
