using System.Collections.Generic;
using System.Linq;

namespace STPLocalSearch.Graphs
{
    public class Path
    {
        public static readonly Path EmptyPath = new Path();

        private Path()
        {
            Edges = new List<Edge>();
            Start = null;
        }

        public Path(Vertex start)
        {
            Edges = new List<Edge>();
            Start = start;
        }

        /// <summary>
        /// The edges in the path.
        /// </summary>
        public List<Edge> Edges { get; private set; }

        /// <summary>
        /// The vertices in the path
        /// </summary>
        public List<Vertex> Vertices
        {
            get
            {
                List<Vertex> vertices = new List<Vertex>();
                vertices.Add(Start);
                foreach (var edge in Edges)
                {
                    vertices.Add(edge.Other(vertices.Last()));
                }
                return vertices;
            }
        }

        public Vertex Start { get; private set; }

        public Vertex End
        {
            get
            {
                if (Edges.Count == 0)
                    return Start;
                if (Edges.Count == 1)
                    return Edges[0].Other(Start);
                
                // Look at the last two edges.
                var nexttolast = Edges[Edges.Count - 2];
                var last = Edges[Edges.Count - 1];
                // Connected via last.Either()
                var lastEither = last.Either();
                if (lastEither == nexttolast.Either() || lastEither == nexttolast.Other(nexttolast.Either()))
                    return last.Other(lastEither);
                return lastEither;
            }
        }

        public int TotalCost { get { return Edges.Sum(e => e.Cost); } }
    }
}
