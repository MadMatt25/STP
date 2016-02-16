using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Data;
using STPLocalSearch.Graphs;

namespace STPLocalSearch
{
    public static class Algorithms
    {
        public static List<Path> NearestTerminals(Vertex from, Graph graph, int n)
        {
            List<Path> foundPaths = new List<Path>();
            HashSet<Vertex> visited = new HashSet<Vertex>();
            Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node> nodes = new Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node>();
            FibonacciHeap<int, Vertex> labels = new FibonacciHeap<int, Vertex>();
            Dictionary<Vertex, Edge> comingFrom = new Dictionary<Vertex, Edge>();

            if (graph.Terminals.Contains(from))
                foundPaths.Add(new Path(from));

            // Initialize labels.
            foreach (var vertex in graph.Vertices)
            {
                var node = labels.Add(vertex == from ? 0 : int.MaxValue, vertex);
                nodes.Add(vertex, node);
                comingFrom.Add(vertex, null);
            }

            while (!labels.IsEmpty() && foundPaths.Count < n)
            {
                var currentNode = labels.ExtractMin();
                var current = currentNode.Value;

                // Consider all edges ending in unvisited neighbours
                var edges =
                    graph.GetEdgesForVertex(current).Where(x => !visited.Contains(x.Other(current)));
                // Update labels on the other end.
                foreach (var edge in edges)
                {
                    if (currentNode.Key + edge.Cost < nodes[edge.Other(current)].Key)
                    {
                        labels.DecreaseKey(nodes[edge.Other(current)], currentNode.Key + edge.Cost);
                        comingFrom[edge.Other(current)] = edge;
                    }
                }

                visited.Add(current);

                if (graph.Terminals.Contains(current) && current != from)
                {
                    // Now travel back, to find the actual path
                    List<Edge> pathEdges = new List<Edge>();
                    Vertex pathVertex = current;
                    while (pathVertex != from)
                    {
                        pathEdges.Add(comingFrom[pathVertex]);
                        pathVertex = comingFrom[pathVertex].Other(pathVertex);
                    }

                    pathEdges.Reverse();
                    Path path = new Path(from);
                    path.Edges.AddRange(pathEdges);
                    foundPaths.Add(path);
                }
            }

            return foundPaths;
        }

        public static Path NearestTerminal(Vertex from, Graph graph)
        {
            return NearestTerminals(from, graph, 1).FirstOrDefault();
        }

        public static int Dijkstra(Vertex from, Vertex to, Graph graph)
        {
            HashSet<Vertex> visited = new HashSet<Vertex>();
            Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node> nodes = new Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node>();
            FibonacciHeap<int, Vertex> labels = new FibonacciHeap<int, Vertex>();
            // Initialize labels.
            foreach (var vertex in graph.Vertices)
            {
                var n = labels.Add(vertex == from ? 0 : int.MaxValue, vertex);
                nodes.Add(vertex, n);
            }

            int currentLabel = int.MaxValue;
            while (!visited.Contains(to))
            {
                var currentNode = labels.ExtractMin();
                var current = currentNode.Value;
                currentLabel = currentNode.Key;
                
                // Consider all edges ending in unvisited neighbours
                var edges =
                    graph.GetEdgesForVertex(current).Where(x => !visited.Contains(x.Other(current)));
                // Update labels on the other end.
                foreach (var edge in edges)
                {
                    if (currentNode.Key + edge.Cost < nodes[edge.Other(current)].Key)
                        labels.DecreaseKey(nodes[edge.Other(current)], currentNode.Key + edge.Cost);
                }

                visited.Add(current);
            }

            return currentLabel;
        }

        public static Path DijkstraPath(Vertex from, Vertex to, Graph graph)
        {
            HashSet<Vertex> visited = new HashSet<Vertex>();
            Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node> nodes =
                new Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node>();
            Dictionary<Vertex, Edge> comingFrom = new Dictionary<Vertex, Edge>();
            FibonacciHeap<int, Vertex> labels = new FibonacciHeap<int, Vertex>();
            // Initialize labels.
            foreach (var vertex in graph.Vertices)
            {
                var n = labels.Add(vertex == from ? 0 : int.MaxValue, vertex);
                nodes.Add(vertex, n);
                comingFrom.Add(vertex, null);
            }

            while (!visited.Contains(to))
            {
                var currentNode = labels.ExtractMin();
                var current = currentNode.Value;

                // Consider all edges ending in unvisited neighbours
                var edges =
                    graph.GetEdgesForVertex(current).Where(x => !visited.Contains(x.Other(current)));
                // Update labels on the other end.
                foreach (var edge in edges)
                {
                    if (currentNode.Key + edge.Cost < nodes[edge.Other(current)].Key)
                    {
                        labels.DecreaseKey(nodes[edge.Other(current)], currentNode.Key + edge.Cost);
                        comingFrom[edge.Other(current)] = edge;
                    }
                }

                visited.Add(current);
            }

            // Now travel back, to find the actual path
            List<Edge> pathEdges = new List<Edge>();
            Vertex pathVertex = to;
            while (pathVertex != from)
            {
                pathEdges.Add(comingFrom[pathVertex]);
                pathVertex = comingFrom[pathVertex].Other(pathVertex);
            }

            pathEdges.Reverse();
            Path path = new Path(from);
            path.Edges.AddRange(pathEdges);
            return path;
        }

        public static Dictionary<Vertex, int> DijkstraToAll(Vertex from, Graph graph)
        {
            HashSet<Vertex> visited = new HashSet<Vertex>();
            Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node> nodes = new Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node>();
            Dictionary<Vertex, int> distances = new Dictionary<Vertex, int>();
            FibonacciHeap<int, Vertex> labels = new FibonacciHeap<int, Vertex>();
            // Initialize labels.
            foreach (var vertex in graph.Vertices)
            {
                var n = labels.Add(vertex == from ? 0 : int.MaxValue, vertex);
                nodes.Add(vertex, n);
            }

            while (distances.Count < graph.NumberOfVertices - 1)
            {
                var currentNode = labels.ExtractMin();
                var current = currentNode.Value;
                var currentLabel = currentNode.Key;

                // Consider all edges ending in unvisited neighbours
                var edges =
                    graph.GetEdgesForVertex(current).Where(x => !visited.Contains(x.Other(current)));
                // Update labels on the other end.
                foreach (var edge in edges)
                {
                    if (currentNode.Key + edge.Cost < nodes[edge.Other(current)].Key)
                        labels.DecreaseKey(nodes[edge.Other(current)], currentNode.Key + edge.Cost);
                }

                visited.Add(current);
                if (current != from)
                    distances.Add(current, currentLabel);
            }

            return distances;
        }

        public static Dictionary<Vertex, Path> DijkstraPathToAll(Vertex from, Graph graph, bool onlyTerminals)
        {
            Dictionary<Vertex, Edge> comingFrom = new Dictionary<Vertex, Edge>();
            HashSet<Vertex> visited = new HashSet<Vertex>();
            Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node> nodes =
                new Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node>();
            Dictionary<Vertex, Path> paths = new Dictionary<Vertex, Path>();
            FibonacciHeap<int, Vertex> labels = new FibonacciHeap<int, Vertex>();
            // Initialize labels.
            foreach (var vertex in graph.Vertices)
            {
                var n = labels.Add(vertex == from ? 0 : int.MaxValue, vertex);
                nodes.Add(vertex, n);
            }

            while (paths.Count < (onlyTerminals ? graph.Terminals.Count - 1 : graph.NumberOfVertices - 1))
            {
                var currentNode = labels.ExtractMin();
                var current = currentNode.Value;

                // Consider all edges ending in unvisited neighbours
                var edges =
                    graph.GetEdgesForVertex(current).Where(x => !visited.Contains(x.Other(current)));
                // Update labels on the other end.
                foreach (var edge in edges)
                {
                    if (currentNode.Key + edge.Cost < nodes[edge.Other(current)].Key)
                    {
                        labels.DecreaseKey(nodes[edge.Other(current)], currentNode.Key + edge.Cost);
                        comingFrom[edge.Other(current)] = edge;
                    }
                }

                visited.Add(current);
                if (current != from && (!onlyTerminals || graph.Terminals.Contains(current)))
                {
                    // Travel back the path
                    List<Edge> pathEdges = new List<Edge>();
                    Vertex pathVertex = current;
                    while (pathVertex != from)
                    {
                        pathEdges.Add(comingFrom[pathVertex]);
                        pathVertex = comingFrom[pathVertex].Other(pathVertex);
                    }

                    pathEdges.Reverse();
                    Path path = new Path(from);
                    path.Edges.AddRange(pathEdges);
                    paths[current] = path;
                }
            }

            return paths;
        }

        public static Dictionary<Vertex, Path> DijkstraPathToAll(Vertex from, Graph graph)
        {
            return DijkstraPathToAll(from, graph, false);
        }

        public static Dictionary<Vertex, Path> DijkstraPathToAllTerminals(Vertex from, Graph graph)
        {
            return DijkstraPathToAll(from, graph, true);
        }

        public static Graph Kruskal(Graph graph)
        {
            return KruskalA(graph);
        }

        private static Graph KruskalA(Graph graph)
        {
            HashSet<Vertex> inTree = new HashSet<Vertex>();
            Graph mst = graph.Clone();
            foreach (var edge in mst.Edges)
                mst.RemoveEdge(edge, false);

            Dictionary<Vertex, int> components = new Dictionary<Vertex, int>();
            for(int i = 0; i < graph.Vertices.Count; i++)
                components.Add(graph.Vertices[i], i);

            // Start with cheapest edge!
            var edges = graph.Edges.ToList();
            edges.Sort((x, y) => x.Cost.CompareTo(y.Cost));
            var startEdge = edges[0];
            inTree.Add(startEdge.Either());
            inTree.Add(startEdge.Other(startEdge.Either()));
            mst.AddEdge(startEdge);
            components[startEdge.Either()] = components[startEdge.Other(startEdge.Either())];
            edges.RemoveAt(0);

            while (mst.NumberOfEdges < graph.NumberOfVertices - 1)
            {
                // Take the first edge that does not introduce a cycle, thus connects 2 components
                Edge edge = null;
                do
                {
                    edge = edges[0];
                    edges.RemoveAt(0);
                } while (components[edge.Either()] == components[edge.Other(edge.Either())]);

                var v1 = edge.Either();
                var v2 = edge.Other(v1);
                inTree.Add(v1);
                inTree.Add(v2);
                mst.AddEdge(edge);

                // Merge the components
                int c1 = components[v1];
                int c2 = components[v2];
                foreach (var component in components.Where(x => x.Value == c2).ToList())
                {
                    components[component.Key] = c1;
                }
            }

            return mst;
        }

        private static Graph KruskalB(Graph graph)
        {
            HashSet<Vertex> inTree = new HashSet<Vertex>();
            Graph mst = graph.Clone();
            foreach (var edge in mst.Edges)
                mst.RemoveEdge(edge);

            // Start with one random vertex
            inTree.Add(graph.Vertices[0]);
            while (mst.NumberOfEdges < graph.NumberOfVertices - 1)
            {
                // Find new edge to add to the tree.
                var candidateEdges = inTree.SelectMany(graph.GetEdgesForVertex).ToList();
                candidateEdges.Sort((x, y) => x.Cost.CompareTo(y.Cost)); // Sort from cheap to expensive
                foreach (var edge in candidateEdges)
                {
                    var v1 = edge.Either();
                    var v2 = edge.Other(v1);
                    bool v1InTree = inTree.Contains(v1);
                    bool v2InTree = inTree.Contains(v2);
                    if (v1InTree ^ v2InTree) // The ^ is a XOR
                    {
                        // One side of this edge is in the MST, the other is not
                        mst.AddEdge(edge);
                        inTree.Add(v1);
                        inTree.Add(v2);
                        break;
                    }
                }
            }

            return mst;
        }

        private static bool ExistsPath(Vertex v1, Vertex v2, Graph graph, List<Vertex> visited)
        {
            foreach (var edge in graph.GetEdgesForVertex(v1))
            {
                if (edge.Other(v1) == v2)
                {
                    return true;
                }

                if (!visited.Contains(edge.Other(v1)))
                {
                    var visited2 = visited.ToList();
                    visited2.Add(v1);
                    if (ExistsPath(edge.Other(v1), v2, graph, visited2))
                        return true;
                }
            }
            return false;
        }
    }
}
