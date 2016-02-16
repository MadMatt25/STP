using System;

namespace STPLocalSearch.Graphs
{
    public class Edge
    {
        private Vertex _v1;
        private Vertex _v2;

        /// <summary>
        /// Constructs an edge from a given vertex to another vertex.
        /// </summary>
        /// <param name="v1">The first vertex connected by the edge.</param>
        /// <param name="v2">The second vertex connected by the edge.</param>
        /// <param name="cost">The cost of this edge.</param>
        public Edge(Vertex v1, Vertex v2, int cost)
        {
            _v1 = v1;
            _v2 = v2;
            Cost = cost;
        }

        /// <summary>
        /// Method that returns one vertex that is connected by this edge.
        /// </summary>
        /// <returns>A vertex that is connected by this edge.</returns>
        public Vertex Either()
        {
            return _v1;
        }

        /// <summary>
        /// Method to get the other vertex connected by this edge, given a first vertex.
        /// </summary>
        /// <param name="v">The first vertex that this edge connects.</param>
        /// <returns>The other vertex that this edge connects.</returns>
        public Vertex Other(Vertex v)
        {
            if (v == _v1) return _v2;
            if (v == _v2) return _v1;
            throw new ArgumentException("The given vertex is not connected by this edge.");
        }

        /// <summary>
        /// Checks whether at least one of the vertices matches a condition.
        /// </summary>
        /// <param name="func">Condition to match for one or both of the vertices connected by this edge.</param>
        /// <returns>Boolean indicating whether one or two of the connected vertices match the condition.</returns>
        public bool WhereVertex(Func<Vertex, bool> func)
        {
            return func(_v1) || func(_v2);
        }

        /// <summary>
        /// Checks whether just one of the vertices matches a condition.
        /// </summary>
        /// <param name="func">Condition to match for one of the vertices connected by this edge.</param>
        /// <returns>Boolean indicating whether one of the connected vertices match the condition.</returns>
        public bool WhereOne(Func<Vertex, bool> func)
        {
            return func(_v1) ^ func(_v2);
        }

        /// <summary>
        /// Checks whether both of the vertices matches a condition.
        /// </summary>
        /// <param name="func">Condition to match for both of the vertices connected by this edge.</param>
        /// <returns>Boolean indicating whether both of the connected vertices match the condition.</returns>
        public bool WhereBoth(Func<Vertex, bool> func)
        {
            return func(_v1) && func(_v2);
        }

        /// <summary>
        /// The nonnegative cost of the edge.
        /// </summary>
        public int Cost { get; set; }

        public override string ToString()
        {
            return string.Format("{0} --> {1}", _v1.VertexName < _v2.VertexName ? _v1 : _v2, _v1.VertexName < _v2.VertexName ? _v2 : _v1);
        }
    }
}
