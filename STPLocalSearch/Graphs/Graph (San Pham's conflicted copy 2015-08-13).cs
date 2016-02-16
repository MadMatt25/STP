using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using STPLocalSearch.Graphs;
using System.Threading.Tasks;

namespace STPLocalSearch
{
    public class Graph
    {
        private Graph(int nbVertices)
        {
            _adjacencies = new Dictionary<Vertex, LinkedList<Edge>>();
            _degree = new Dictionary<Vertex, int>();
            Vertices = new List<Vertex>(nbVertices);
            RequiredNodes = new List<Vertex>();
            
            // Create the vertices
            for (int i = 0; i < nbVertices; i++)
            {
                var v = new Vertex(i + 1); //Start counting at 1.
                Vertices.Add(v);
                _adjacencies.Add(v, new LinkedList<Edge>());
                _degree.Add(v, 0);
            }
        }

        public Graph(List<Vertex> vertices)
        {
            _adjacencies = new Dictionary<Vertex, LinkedList<Edge>>();
            _degree = new Dictionary<Vertex, int>();
            Vertices = vertices;
            foreach (var vertex in Vertices)
            {
                _adjacencies.Add(vertex, new LinkedList<Edge>());
                _degree.Add(vertex, 0);
            }
            RequiredNodes = new List<Vertex>();
        }

        private readonly Dictionary<Vertex, LinkedList<Edge>> _adjacencies;
        private readonly Dictionary<Vertex, int> _degree; 

        /// <summary>
        /// List of all the vertices in the graph.
        /// </summary>
        public List<Vertex> Vertices { get; private set; }

        /// <summary>
        /// The number of edges in the graph.
        /// </summary>
        public int NumberOfEdges { get { return _adjacencies.Sum(x => x.Value.Count) / 2; } } //div 2 as every edge is stored twice
        /// <summary>
        /// The number of vertices in the graph.
        /// </summary>
        public int NumberOfVertices { get { return Vertices.Count; } }

        /// <summary>
        /// The total cost of this graph.
        /// </summary>
        public int TotalCost { get { return GetAllEdges().Sum(x => x.Cost); } }

        /// <summary>
        /// The list of required nodes that should be covered by the Steiner tree.
        /// </summary>
        public List<Vertex> RequiredNodes { get; private set; }

        /// <summary>
        /// Method to add an edge to the graph.
        /// </summary>
        /// <param name="from">Vertex from which the edge starts.</param>
        /// <param name="to">Vertex at which the edge ends.</param>
        /// <param name="cost">The cost of the edge.</param>
        public void AddEdge(Vertex from, Vertex to, int cost)
        {
            if (!_adjacencies.ContainsKey(from) || !_adjacencies.ContainsKey(to))
                throw new ArgumentException("The vertices connected by this edge should be in the graph.");

            var edge = new Edge(from, to, cost);
            _adjacencies[from].AddLast(edge);
            _adjacencies[to].AddLast(edge);

            IncreaseDegree(from);
            IncreaseDegree(to);
        }

        /// <summary>
        /// Method to remove an edge from the graph.
        /// </summary>
        /// <param name="edge">The edge to be removed.</param>
        public void RemoveEdge(Edge edge)
        {
            var v1 = edge.Either();
            var v2 = edge.Other(v1);
            _adjacencies[v1].Remove(edge);
            _adjacencies[v2].Remove(edge);

            DecreaseDegree(v1);
            DecreaseDegree(v2);

            if (GetDegree(v1) == 0)
                RemoveVertex(v1);
            if (GetDegree(v2) == 0)
                RemoveVertex(v2);
        }

        /// <summary>
        /// Method to remove a vertex from this graph. 
        /// Also removes all the edges that this vertex was connected to.
        /// </summary>
        /// <param name="vertex">The vertex to remove.</param>
        public void RemoveVertex(Vertex vertex)
        {
            if (!Vertices.Contains(vertex))
                return;
            Vertices.Remove(vertex);
            var edges = _adjacencies[vertex];
            List<Edge> removeEdges = new List<Edge>();
            removeEdges.AddRange(edges);
            foreach (var edge in removeEdges)
                RemoveEdge(edge);
        }

        /// <summary>
        /// Method to increase the degree of this vertex. 
        /// This is used when an edge is added to the graph connecting this vertex.
        /// <param name="v">The vertex of which to increase the degree</param>
        /// </summary>
        public void IncreaseDegree(Vertex v)
        {
            if (!_degree.ContainsKey(v))
                throw new ArgumentException("The given vertex v is not contained in this graph.");
            _degree[v]++;
        }

        /// <summary>
        /// Method to decrease the degree of this vertex.
        /// This is used when an edge connecting this vertex is removed from the graph.
        /// <param name="v">The vertex of which to increase the degree</param>
        /// </summary>
        public void DecreaseDegree(Vertex v)
        {
            if (!_degree.ContainsKey(v))
                throw new ArgumentException("The given vertex v is not contained in this graph.");
            _degree[v]--;
        }

        /// <summary>
        /// Method to get the degree of a vertex in the graph.
        /// </summary>
        /// <param name="v">The vertex of which to get the degree.</param>
        /// <returns>The degree of the given vertex.</returns>
        public int GetDegree(Vertex v)
        {
            if (!_degree.ContainsKey(v))
                throw new ArgumentException("The given vertex v is not contained in this graph.");
            return _degree[v];
        }

        /// <summary>
        /// Method to get the edges that are connected to a given vertex.
        /// </summary>
        /// <param name="v">The vertex to get all the edges for.</param>
        /// <returns>A list containing all the edges that the given vertex is connected to.</returns>
        public LinkedList<Edge> GetEdgesForVertex(Vertex v)
        {
            if (_adjacencies.ContainsKey(v))
                return _adjacencies[v];

            throw new ArgumentException("The given vertex is not in this graph.");
        }

        /// <summary>
        /// Method to get an iterator for all edges in the graph.
        /// </summary>
        /// <returns>An IEnumerable containing all the edges in the graph.</returns>
        public IEnumerable<Edge> GetAllEdges()
        {
            HashSet<Edge> done = new HashSet<Edge>();
            foreach (var vertex in Vertices)
            {
                foreach (var edge in GetEdgesForVertex(vertex))
                {
                    if (done.Contains(edge))
                        continue;
                    done.Add(edge);
                    yield return edge;
                }
            }
        } 

        /// <summary>
        /// Method to create and return the distance graph of this graph.
        /// </summary>
        /// <returns>The distance graph of the current instance of the graph.</returns>
        public Graph CreateDistanceGraph()
        {
            var n = this.NumberOfVertices;
            Graph distanceGraph = new Graph(Vertices);
            for (int from = 0; from < n; from++)
            {
                for (int to = from + 1; to < n; to++)
                {
                    var vFrom = Vertices[from];
                    var vTo = Vertices[to];
                    var cost = Algorithms.Dijkstra(vFrom, vTo, this);
                    distanceGraph.AddEdge(vFrom, vTo, cost);
                }
            }

            // Can do this in parallel as well.
            // Currently not used to give PC some more power to do other stuff while program runs

            //object locker = new object();
            //Parallel.For(0, n, (from) =>
            //for (int to = from + 1; to < n; to++)
            //{
            //    var vFrom = Vertices[from];
            //    var vTo = Vertices[to];
            //    var cost = Algorithms.Dijkstra(vFrom, vTo, this);
            //    lock (locker)
            //    {
            //      distanceGraph.AddEdge(vFrom, vTo, cost);
            //    }
            //}
            
            return distanceGraph;
        }

        public Graph CreateTerminalDistanceGraph()
        {
            Console.Write("Calculating Terminal Distance graph...\r");
            var n = this.RequiredNodes.Count;
            var edgesCalculated = 0;
            var edgesToCalculate = (n * (n - 1)) / 2;
            int beforeUpdate = (edgesToCalculate / 200) + 1;
            Graph distanceGraph = new Graph(RequiredNodes);
            for (int from = 0; from < n; from++)
            {
                for (int to = from + 1; to < n; to++)
                {
                    var vFrom = RequiredNodes[from];
                    var vTo = RequiredNodes[to];
                    var cost = Algorithms.Dijkstra(vFrom, vTo, this);
                    distanceGraph.AddEdge(vFrom, vTo, cost);
                    edgesCalculated++;
                    if (edgesCalculated % beforeUpdate == 0)
                        Console.Write("Calculating Terminal Distance graph... {0}%\r", (int)(edgesCalculated * 100.0 / edgesToCalculate));
                }
            }
            Console.Write("                                           \r");

            return distanceGraph;
        }

        public Graph CreateTerminalDistanceGraphParallel()
        {
            Console.Write("Calculating Terminal Distance graph...\r");
            var n = this.RequiredNodes.Count;
            var edgesCalculated = 0;
            var edgesToCalculate = (n * (n - 1)) / 2;
            int beforeUpdate = edgesToCalculate / 200;
            object graphLock = new object();
            Graph distanceGraph = new Graph(RequiredNodes);
            for (int from = 0; from < n; from++)
            {
                Parallel.For(from + 1, n, (to) =>
                {
                    var vFrom = RequiredNodes[from];
                    var vTo = RequiredNodes[to];
                    var cost = Algorithms.Dijkstra(vFrom, vTo, this);
                    lock (graphLock)
                    {
                        distanceGraph.AddEdge(vFrom, vTo, cost);
                    }
                    System.Threading.Interlocked.Increment(ref edgesCalculated);
                    if (edgesCalculated % beforeUpdate == 0)
                        Console.Write("Calculating Terminal Distance graph... {0}%\r", (int)(edgesCalculated * 100.0 / edgesToCalculate));
                });
            }
            Console.Write("                                           \r");

            return distanceGraph;
        }

        public Graph Clone()
        {
            List<Vertex> vertices = Vertices.ToList();
            Graph clone = new Graph(vertices);
            foreach (var edge in GetAllEdges())
            {
                var v1 = edge.Either();
                var v2 = edge.Other(v1);
                clone._adjacencies[v1].AddLast(edge);
                clone._adjacencies[v2].AddLast(edge);

                clone.IncreaseDegree(v1);
                clone.IncreaseDegree(v2);
            }

            return clone;
        }

        /// <summary>
        /// Method to parse a graph from an OR benchmark file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>An instance of the Graph class containing the graph from the file.</returns>
        public static Graph Parse(string path)
        {
            Console.WriteLine("Parsing {0}", path);
            //return ParseORBenchmark(path);
            return ParseSteinLibBenchmark(path);
        }

        private static Graph ParseSteinLibBenchmark(string path)
        {
            Graph g = null;

            using (StreamReader sr = new StreamReader(File.Open(path, FileMode.Open)))
            {
                string line;
                
                int nodes;
                int edges;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "SECTION Graph")
                    {
                        nodes = int.Parse(sr.ReadLine().Split(' ')[1]);
                        edges = int.Parse(sr.ReadLine().Split(' ')[1]);
                        g = new Graph(nodes);

                        for (int i = 0; i < edges; i++)
                        {
                            string[] edgeData = sr.ReadLine().Split(' ');
                            int from = int.Parse(edgeData[1]);
                            int to = int.Parse(edgeData[2]);
                            int cost = int.Parse(edgeData[3]);
                            g.AddEdge(g.Vertices.Single(x => x.VertexName == from), g.Vertices.Single(x => x.VertexName == to), cost);
                        }

                    }
                    else if (line == "SECTION Terminals")
                    {
                        int terminals = int.Parse(sr.ReadLine().Split(' ')[1]);
                        for (int i = 0; i < terminals; i++)
                        {
                            int terminalNumber = int.Parse(sr.ReadLine().Split(' ')[1]);
                            var t = g.Vertices.Single(x => x.VertexName == terminalNumber);
                            g.RequiredNodes.Add(t);
                        }
                    }
                }
            }

            return g;
        }

        private static Graph ParseORBenchmark(string path)
        {
            Graph g;

            using (StreamReader sr = new StreamReader(File.Open(path, FileMode.Open)))
            {
                // Read the number of edges and vertices
                string metadata = sr.ReadLine().RemoveWhitespace();
                int nbEdges = int.Parse(metadata.Split(' ')[1]);
                int nbVertices = int.Parse(metadata.Split(' ')[0]);
                g = new Graph(nbVertices);

                // Read all edges and add them to the graph
                for (int i = 0; i < nbEdges; i++)
                {
                    string rawline = sr.ReadLine().RemoveWhitespace();
                    int start = int.Parse(rawline.Split(' ')[0]);
                    int end = int.Parse(rawline.Split(' ')[1]);
                    int cost = int.Parse(rawline.Split(' ')[2]);

                    g.AddEdge(g.Vertices.Single(x => x.VertexName == start), g.Vertices.Single(x => x.VertexName == end), cost);
                }

                // Read the Steiner nodes and save them
                int numberOfRequiredNodes = int.Parse(sr.ReadLine().RemoveWhitespace());
                IEnumerable<Vertex> requiredNodes = sr.ReadLine().RemoveWhitespace().Split(' ').Where(x => x.Length > 0).Select(x => g.Vertices.Single(v => v.VertexName == int.Parse(x)));
                g.RequiredNodes.AddRange(requiredNodes);
            }

            return g;
        }
    }
}
