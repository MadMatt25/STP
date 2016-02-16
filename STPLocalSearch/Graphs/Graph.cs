using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace STPLocalSearch.Graphs
{
    public class Graph
    {
        private Graph()
        {
            _adjacencies = new Dictionary<Vertex, List<Edge>>();
            _voronoiRegions = new Dictionary<Vertex, Vertex>();
            _voronoiRadius = new Dictionary<Vertex, int>();
            Terminals = new List<Vertex>();
            RequiredSteinerNodes = new HashSet<Vertex>();
        }

        private Graph(int nbVertices) : this()
        {
            _vertices = new HashSet<Vertex>();
            Vertices = new List<Vertex>(nbVertices);
            
            // Create the vertices
            for (int i = 0; i < nbVertices; i++)
            {
                var v = new Vertex(i + 1); //Start counting at 1.
                AddVertex(v);
            }
        }

        public Graph(List<Vertex> vertices) : this()
        {
            _vertices = new HashSet<Vertex>();
            Vertices = vertices.ToList();
            foreach (var vertex in vertices)
            {
                _vertices.Add(vertex);
                _adjacencies.Add(vertex, new List<Edge>());
            }
        }

        private readonly HashSet<Vertex> _vertices;
        private readonly Dictionary<Vertex, List<Edge>> _adjacencies;
        private readonly Dictionary<Vertex, Vertex> _voronoiRegions;
        private readonly Dictionary<Vertex, int> _voronoiRadius;

        /// <summary>
        /// List of all the vertices in the graph.
        /// </summary>
        public List<Vertex> Vertices { get; private set; }

        private readonly List<Edge> _edges = new List<Edge>();
        private bool _edgesUpToDate = false;
        public List<Edge> Edges
        {
            get
            {
                if (!_edgesUpToDate)
                {
                    _edges.Clear();
                    _edges.AddRange(GetAllEdges());
                    _edgesUpToDate = true;
                }

                return _edges;
            }
        }

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
        public int TotalCost { get { return Edges.Sum(x => x.Cost); } }

        /// <summary>
        /// The list of required nodes that should be covered by the Steiner tree.
        /// </summary>
        public List<Vertex> Terminals { get; private set; }

        /// <summary>
        /// The list of Steiner nodes that are determined as required by certain tests.
        /// </summary>
        public HashSet<Vertex> RequiredSteinerNodes { get; private set; }

        /// <summary>
        /// The list of all required nodes (required Steiner nodes and terminals).
        /// </summary>
        public IEnumerable<Vertex> Required
        {
            get { return Terminals.Union(RequiredSteinerNodes); }
        }

        private Graph _terminalDistance = null;
        /// <summary>
        /// The terminal distance graph. Note: The name may be misleading, because nodes
        /// that are not terminals, but known to be required are also included.
        /// </summary>
        public Graph TerminalDistanceGraph
        {
            get
            {
                if (_terminalDistance == null)
                    _terminalDistance = CreateTerminalDistanceGraph();
                return _terminalDistance;
            }
        }
        
        /// <summary>
        /// Method to add an edge to the graph.
        /// </summary>
        /// <param name="from">Vertex from which the edge starts.</param>
        /// <param name="to">Vertex at which the edge ends.</param>
        /// <param name="cost">The cost of the edge.</param>
        public void AddEdge(Vertex from, Vertex to, int cost)
        {
            var edge = new Edge(from, to, cost);
            AddEdge(edge);
        }

        public void AddEdge(Edge edge)
        {
            var from = edge.Either();
            var to = edge.Other(from);
            if (!_adjacencies.ContainsKey(from) || !_adjacencies.ContainsKey(to))
                throw new ArgumentException("This edge can not be added as it connects vertices that are not in the graph.");

            _adjacencies[from].Add(edge);
            _adjacencies[to].Add(edge);
            _edgesUpToDate = false;
            _terminalDistance = null;
        }

        /// <summary>
        /// Method to remove an edge from the graph.
        /// </summary>
        /// <param name="edge">The edge to be removed.</param>
        /// <param name="removeDegreeZero">Indicates whether when a vertex becomes of degree zero, it should be removed. Default: True.</param>
        public void RemoveEdge(Edge edge, bool removeDegreeZero = true)
        {
            var v1 = edge.Either();
            var v2 = edge.Other(v1);
            _adjacencies[v1].Remove(edge);
            _adjacencies[v2].Remove(edge);

            if (removeDegreeZero)
            {
                if (GetDegree(v1) == 0)
                    RemoveVertex(v1);
                if (GetDegree(v2) == 0)
                    RemoveVertex(v2);
            }

            _edgesUpToDate = false;
            _terminalDistance = null;
            _voronoiRegions.Clear();
        }

        public void AddVertex(Vertex vertex)
        {
            if (_vertices.Contains(vertex))
                return;

            Vertices.Add(vertex);
            _vertices.Add(vertex);
            _adjacencies.Add(vertex, new List<Edge>());
            _terminalDistance = null;
        }

        /// <summary>
        /// Method to remove a vertex from this graph. 
        /// Also removes all the edges that this vertex was connected to.
        /// </summary>
        /// <param name="vertex">The vertex to remove.</param>
        public void RemoveVertex(Vertex vertex)
        {
            if (!_vertices.Contains(vertex))
                return;
            _vertices.Remove(vertex);
            Vertices.Remove(vertex);
            foreach (var edge in _adjacencies[vertex].ToList())
                RemoveEdge(edge);

            _terminalDistance = null;
            _adjacencies.Remove(vertex);
        }

        /// <summary>
        /// Method to check if the graph contains a given vertex in constant time.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns>Boolean indicating whether the given vertex is present in the graph.</returns>
        public bool ContainsVertex(Vertex vertex)
        {
            return _vertices.Contains(vertex);
        }

        /// <summary>
        /// Method to check if the graph contains a given edge in linear time.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public bool ContainsEdge(Edge edge)
        {
            return _edges.Contains(edge);
        }
        
        /// <summary>
        /// Method to get the degree of a vertex in the graph.
        /// </summary>
        /// <param name="v">The vertex of which to get the degree.</param>
        /// <returns>The degree of the given vertex.</returns>
        public int GetDegree(Vertex v)
        {
            if (!_adjacencies.ContainsKey(v))
                throw new ArgumentException("The given vertex v is not contained in this graph.");
            return _adjacencies[v].Count;
        }

        /// <summary>
        /// Method to get the edges that are connected to a given vertex.
        /// </summary>
        /// <param name="v">The vertex to get all the edges for.</param>
        /// <returns>A list containing all the edges that the given vertex is connected to.</returns>
        public List<Edge> GetEdgesForVertex(Vertex v)
        {
            if (_adjacencies.ContainsKey(v))
                return _adjacencies[v];

            throw new ArgumentException("The given vertex is not in this graph.");
        }

        /// <summary>
        /// Method to get the neighbours of a given vertex.
        /// </summary>
        /// <param name="v">The vertex of which to get all the neighbours.</param>
        /// <returns>A list containing all the neighbours of the given vertex.</returns>
        public List<Vertex> GetNeighboursForVertex(Vertex v)
        {
            if (_adjacencies.ContainsKey(v))
                return _adjacencies[v].Select(x => x.Other(v)).ToList();

            throw new ArgumentException("The given vertex is not in this graph.");
        }

        /// <summary>
        /// Method to get the Terminal for which the given vertex belongs to its Veronoi region.
        /// </summary>
        /// <param name="vertex">The vertex for which to get the Veronoi region.</param>
        /// <returns>The terminal in which the vertex is in the Veronoi region.</returns>
        public Vertex GetVoronoiRegionForVertex(Vertex vertex)
        {
            if (Terminals.Contains(vertex))
                return vertex;
            if (_voronoiRegions.ContainsKey(vertex))
                return _voronoiRegions[vertex];

            ComputeVoronoiRegions();
            return _voronoiRegions[vertex];
        }

        public int GetVoronoiRadiusForTerminal(Vertex vertex)
        {
            if (!Terminals.Contains(vertex))
                throw new ArgumentException("The given vertex should be a terminal.");
            if (_voronoiRadius.ContainsKey(vertex))
                return _voronoiRadius[vertex];

            ComputeVoronoiRegions();
            return _voronoiRadius[vertex];
        }

        /// <summary>
        /// Method to get an IEnumerable with the non-terminal vertices in the Veronoi region of
        /// a certain terminal.
        /// </summary>
        /// <param name="terminal">The terminal to determine the Veronoi region.</param>
        /// <returns>List of non-terminals in the Veronoi region</returns>
        public IEnumerable<Vertex> GetVerticesInVoronoiRegion(Vertex terminal)
        {
            foreach (var vertex in Vertices)
            {
                if (GetVoronoiRegionForVertex(vertex) == terminal)
                    yield return vertex;
            }
        }

        /// <summary>
        /// Method that computes and stores all Voronoi regions for non-terminals.
        /// </summary>
        private void ComputeVoronoiRegions()
        {
            _voronoiRadius.Clear();
            _voronoiRegions.Clear();
            Dictionary<Vertex, int> distanceToBase = new Dictionary<Vertex, int>();
            foreach (var vertex in Vertices.Except(Terminals))
            {
                var closestTerminalPath = Algorithms.NearestTerminal(vertex, this);
                _voronoiRegions.Add(vertex, closestTerminalPath.End);
                distanceToBase.Add(vertex, closestTerminalPath.TotalCost);
            }

            foreach (var terminal in Terminals)
            {
                int radius = int.MaxValue;
                foreach (var vertex in GetVerticesInVoronoiRegion(terminal))
                {
                    int baseDistance = 0;
                    if (distanceToBase.ContainsKey(vertex))
                        baseDistance = distanceToBase[vertex];

                    foreach (var notInSameRegion in GetEdgesForVertex(vertex).Where(x => GetVoronoiRegionForVertex(x.Other(vertex)) != terminal))
                    {
                        if (baseDistance + notInSameRegion.Cost < radius)
                            radius = baseDistance + notInSameRegion.Cost;
                    }
                }
                _voronoiRadius.Add(terminal, radius);
            }
        }

        /// <summary>
        /// Method to get an iterator for all edges in the graph.
        /// </summary>
        /// <returns>An IEnumerable containing all the edges in the graph.</returns>
        private IEnumerable<Edge> GetAllEdges()
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
                var vFrom = Vertices[from];
                var distanceToAll = Algorithms.DijkstraToAll(vFrom, this);
                foreach (var dist in distanceToAll)
                {
                    var vTo = dist.Key;
                    if (vTo.VertexName > vFrom.VertexName)
                        continue;
                    distanceGraph.AddEdge(vFrom, vTo, dist.Value);
                }
            }

            return distanceGraph;
        }

        /// <summary>
        /// Creates a distance graph for this graph.
        /// Subtle note: the terminals used for this graph are the terminals for the problem,
        /// and the nodes that are considered required Steiner nodes.
        /// </summary>
        /// <returns></returns>
        private Graph CreateTerminalDistanceGraph()
        {
            Console.Write("\rCalculating Terminal Distance graph... ");
            var terminals = this.Required.ToList();
            var n = terminals.Count;
            var edgesCalculated = 0;
            var edgesToCalculate = (n * (n - 1)) / 2;
            int beforeUpdate = (edgesToCalculate / 200) + 1;
            Graph distanceGraph = new Graph(terminals);
            
            for (int from = 0; from < n; from++)
            {
                var vFrom = terminals[from];
                var distanceToAll = Algorithms.DijkstraToAll(vFrom, this);
                foreach (var dist in distanceToAll)
                {
                    var vTo = dist.Key;
                    if (vTo.VertexName > vFrom.VertexName || !terminals.Contains(vTo))
                        continue;
                    
                    distanceGraph.AddEdge(vFrom, vTo, dist.Value);
                    edgesCalculated++;
                    if (edgesCalculated % beforeUpdate == 0)
                        Console.Write("\rCalculating Terminal Distance graph... {0}% ", (int)(edgesCalculated * 100.0 / edgesToCalculate));
                }
            }

            Console.Write("\r{0}\r", new string(' ', Console.CursorLeft));

            return distanceGraph;
        }

        /// <summary>
        /// Do a Depth First traversal on the graph, starting with a given vertex.
        /// </summary>
        /// <param name="start">The vertex from which to start the depth first traversal.</param>
        /// <returns>A depth first traversal from the given start vertex.</returns>
        public IEnumerable<Vertex> DepthFirstTraversal(Vertex start, bool ltr = true)
        {
            var visited = new HashSet<Vertex>();
            var stack = new Stack<Vertex>();

            stack.Push(start);

            while (stack.Count != 0)
            {
                var current = stack.Pop();

                if (!visited.Add(current))
                    continue;

                yield return current;

                var neighbours = this.GetNeighboursForVertex(current)
                                      .Where(n => !visited.Contains(n));

                foreach (var neighbour in ltr ? neighbours.Reverse() : neighbours)
                    stack.Push(neighbour);
            }
        }

        /// <summary>
        /// Creates an exact copy of this graph.
        /// </summary>
        /// <returns></returns>
        public Graph Clone()
        {
            Graph clone = new Graph(Vertices);
            foreach (var edge in GetAllEdges())
                clone.AddEdge(edge);
            foreach (var terminal in Terminals)
                clone.Terminals.Add(terminal);
            foreach (var requiredSteinerNode in RequiredSteinerNodes)
                clone.RequiredSteinerNodes.Add(requiredSteinerNode);
            clone._terminalDistance = _terminalDistance;

            return clone;
        }

        public int ComponentCheck()
        {
            // Check if graph consists of more than one component
            Dictionary<Vertex, int> component = new Dictionary<Vertex, int>();
            int c = 1;
            foreach (var vertex in Vertices)
            {
                component.Add(vertex, c);
                c++;
            }
            foreach (var ge in Edges)
            {
                var v1 = ge.Either();
                var v2 = ge.Other(v1);
                var c1 = component[v1];
                var c2 = component[v2];
                if (c1 != c2) // merge components
                {
                    foreach (var kv in component.Where(x => x.Value == c2).ToList())
                        component[kv.Key] = c1;
                }
            }

            int com = component.Values.Distinct().Count();
            return com;
        }

        public Dictionary<Vertex, int> CreateComponentTable()
        {
            Dictionary<Vertex, int> component = new Dictionary<Vertex, int>();
            foreach (var vertex in Vertices)
                component.Add(vertex, component.Count);

            foreach (var ge in Edges)
            {
                var v1 = ge.Either();
                var v2 = ge.Other(v1);
                var c1 = component[v1];
                var c2 = component[v2];
                if (c1 != c2) // Merge components
                {
                    foreach (var kv in component.Where(x => x.Value == c2).ToList())
                        component[kv.Key] = c1;
                }
            }

            return component;
        }

        /// <summary>
        /// Method to parse a graph from an OR benchmark file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>An instance of the Graph class containing the graph from the file.</returns>
        public static Graph Parse(string path)
        {
            Console.Write("Parsing {0}", path);
            var graph = ParseSteinLibBenchmark(path);
            Console.Write("\r{0}\r", new String(' ', Console.CursorLeft));
            Console.Write("Instance: {0}\n", path.Split('\\').Last());
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" {0} vertices, {1} edges\n", graph.NumberOfVertices, graph.NumberOfEdges);
            Console.ForegroundColor = oldColor;
            return graph;
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
                    if (line.Equals("SECTION Graph", StringComparison.InvariantCultureIgnoreCase))
                    {
                        nodes = int.Parse(sr.ReadLine().Split(' ')[1]);
                        edges = int.Parse(sr.ReadLine().Split(' ')[1]);
                        List<Vertex> vertices = new List<Vertex>();
                        for (int i = 0; i < nodes; i++)
                            vertices.Add(new Vertex(i+1));

                        g = new Graph(vertices);

                        for (int i = 0; i < edges; i++)
                        {
                            string gline = sr.ReadLine();
                            if (gline == null || gline.Trim().Length == 0)
                                continue;

                            string[] edgeData = gline.Split(' ');
                            int from = int.Parse(edgeData[1]);
                            int to = int.Parse(edgeData[2]);
                            int cost = int.Parse(edgeData[3]);
                            g.AddEdge(vertices[from-1], vertices[to-1], cost);
                        }

                    }
                    else if (line.Equals("SECTION Terminals", StringComparison.InvariantCultureIgnoreCase))
                    {
                        int terminals = int.Parse(sr.ReadLine().Split(' ')[1]);
                        for (int i = 0; i < terminals; i++)
                        {
                            int terminalNumber = int.Parse(sr.ReadLine().Split(' ')[1]);
                            var t = g.Vertices.Single(x => x.VertexName == terminalNumber);
                            g.Terminals.Add(t);
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
                g.Terminals.AddRange(requiredNodes);
            }

            return g;
        }
    }
}
