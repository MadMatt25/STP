using System;
using System.Collections.Generic;

namespace STPLocalSearch.Data
{
    public sealed class FibonacciHeap<TKey, TValue>
    {
        private readonly List<Node> _roots = new List<Node>();
        private Node _minimum;
        private int _count;

        /// <summary>
        /// Method to insert a new item in the Fibonacci Heap.
        /// Items in the heap are sorted by key.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// <param name="value">The value of the item.</param>
        /// <returns>The added node in the heap.</returns>
        public Node Add(TKey key, TValue value)
        {
            Node node = new Node(key, value);
            _count++;
            _roots.Add(node);

            // There currently is no minimum, so this node is the new minimum
            if (_minimum == null)
                _minimum = node;
            else if (Comparer<TKey>.Default.Compare(node.Key, _minimum.Key) < 0)
                _minimum = node;
            return node;
        }

        /// <summary>
        /// Method to peek at the current minimum key and its value.
        /// </summary>
        /// <returns>A KeyValuePair containing the minimum key and its value.</returns>
        public Node Peek()
        {
            if (_minimum == null)
                throw new InvalidOperationException("Cannot peek at the minimum as there is none.");

            return _minimum;
        }

        /// <summary>
        /// Method to extract the minimum key and its value from the heap.
        /// After calling this method, this item will be removed from the heap.
        /// </summary>
        /// <returns>A KeyValuePar containing the minimum key and its value.</returns>
        public Node ExtractMin()
        {
            if (_count == 0)
                throw new InvalidOperationException("Cannot extract from an empty tree.");

            // Extracting the minimum happens in three phases
            // Phase 1. Take the current minimum and remove it. Its children now become roots.
            Node extracted = _minimum;
            foreach (var child in extracted.Children)
            {
                child.Parent = null;
                _roots.Add(child);
            }
            extracted.Children.Clear();
            _roots.Remove(extracted);

            // Phase 2. Update the current _minimum value
            if (_roots.Count == 0)
                _minimum = null; //Tree is empty
            else
            {
                _minimum = _roots[0];
                Consolidate();
            }
            _count--;
            return extracted;
        }

        /// <summary>
        /// Method to decrease the key of an item in the heap.
        /// </summary>
        /// <param name="node">The node of which to decrease the key.</param>
        /// <param name="key">The new key for this node.</param>
        public void DecreaseKey(Node node, TKey key)
        {
            if (Comparer<TKey>.Default.Compare(node.Key, key) < 0)
                throw new InvalidOperationException("Current key is smaller. Can not decrease.");
            node.Key = key;
            if (Comparer<TKey>.Default.Compare(node.Key, _minimum.Key) < 0)
                _minimum = node;

            // Check if heap property is violated
            if (node.Parent != null && Comparer<TKey>.Default.Compare(node.Key, node.Parent.Key) < 0) 
            {
                // While parent is not a root, mark it. If it was already marked, cut it.
                var parent = node.Parent;
                // While not root and marked, cut.
                while (parent.Parent != null && parent.Mark)
                {
                    var grandParent = parent.Parent;
                    grandParent.Children.Remove(parent);
                    parent.Parent = null;
                    parent.Mark = false; // Roots are not marked
                    _roots.Add(parent);

                    // Continue upwards
                    parent = grandParent;
                }
                // First unmarked parent! If it's not a root, mark it
                if (parent.Parent != null)
                    parent.Mark = true;

                // Cut the node from its parent
                node.Parent.Children.Remove(node);
                node.Parent = null;
                _roots.Add(node);
            }
        }

        /// <summary>
        /// Method to check if the Fibonacci heap is empty.
        /// </summary>
        /// <returns>A boolean indicating whether this heap is empty.</returns>
        public bool IsEmpty()
        {
            return _count == 0;
        }

        /// <summary>
        /// Method to consolidate the tree.
        /// Consolidating the tree happens as follows: we decrease the number of roots by 
        /// linking together the roots that have the same degree (same number of children)
        /// In the end, this produces a list of maximum O(log n) roots.
        /// </summary>
        private void Consolidate()
        {
            var upperBound = (int)Math.Floor(Math.Log(_count, (1.0 + Math.Sqrt(5)) / 2.0)) + 1;
            // This array stores a node with degree = index. 
            // E.g.: nodePerDegree[3] is a node with degree 3.
            // If two nodes with same degree are found, they get merged.
            var nodePerDegree = new Node[upperBound];
            for (int i = 0; i < _roots.Count; i++)
            {
                var node = _roots[i];
                var degree = node.Degree;
                while (nodePerDegree[degree] != null) // Loop as long as there are two items with same degree.
                {
                    var otherNode = nodePerDegree[degree];
                    if (Comparer<TKey>.Default.Compare(node.Key, otherNode.Key) > 0) // The other node is smaller!
                    {
                        // Switch the nodes. Now other node has the greater value.
                        var temp = node;
                        node = otherNode;
                        otherNode = temp;
                    }
                    _roots.Remove(otherNode);
                    i--;
                    node.AddChild(otherNode); // Link this node to the other node (which has a greater value)
                    otherNode.Mark = false; // Other node gets linked together with node, so marking can be erased
                    nodePerDegree[degree] = null;
                    degree++; // Degree of node is increased by one.
                }
                nodePerDegree[degree] = node;
            }
            _minimum = null;
            _roots.Clear();
            // Recreate the list of roots and update the minimum
            for (int i = 0; i < nodePerDegree.Length; i++)
            {
                var node = nodePerDegree[i];
                if (node == null)
                    continue;
                if (_minimum == null || Comparer<TKey>.Default.Compare(node.Key, _minimum.Key) < 0)
                    _minimum = node;
                node.Mark = false; // Roots are unmarked
                _roots.Add(node);
            }
        }

        /// <summary>
        /// Class used to represent a node in the Fibonacci heap.
        /// This class is private as to not expose unnecessary information
        /// to the user of the Fibonacci heap.
        /// </summary>
        public class Node
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public Node Parent { get; set; }
            public List<Node> Children { get; private set; }
            public bool Mark { get; set; }
            public int Degree { get { return Children.Count; } }

            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
                Children = new List<Node>();
            }

            public void AddChild(Node child)
            {
                child.Parent = this;
                Children.Add(child);
            }
        }
    }
}