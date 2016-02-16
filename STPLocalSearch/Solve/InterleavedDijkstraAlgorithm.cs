using System;
using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Data;
using STPLocalSearch.Graphs;
using System.Diagnostics;

namespace STPLocalSearch.Solve
{
    public static class InterleavedDijkstraAlgorithm
    {
        public static Graph RunSolver(Graph graph)
        {
            var solution = new Graph(graph.Vertices);

            DijkstraState state = new DijkstraState();
            // Create the states needed for every execution of the Dijkstra algorithm
            foreach (var terminal in graph.Terminals)
                state.AddVertexToInterleavingDijkstra(terminal, graph);

            // Initialize
            Vertex currentVertex = state.GetNextVertex();
            FibonacciHeap<int, Vertex> labels = state.GetLabelsFibonacciHeap();
            HashSet<Vertex> visited = state.GetVisitedHashSet();
            Dictionary<Vertex, Path> paths = state.GetPathsFound();
            Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node> nodes = state.GetNodeMapping();
            Dictionary<Vertex, Edge> comingFrom = state.GetComingFromDictionary();

            Dictionary<Vertex, int> components = solution.CreateComponentTable();
            Dictionary<Vertex, double> terminalFValues = CreateInitialFValuesTable(graph);

            int maxLoopsNeeded = graph.Terminals.Count * graph.NumberOfVertices;
            int loopsDone = 0;
            int updateInterval = 100;

            int longestPath = graph.Terminals.Max(x => Algorithms.DijkstraToAll(x, graph).Max(y => y.Value));

            while (state.GetLowestLabelVertex() != null)
            {
                if (loopsDone % updateInterval == 0)
                    Console.Write("\rRunning IDA... {0:0.0}%                           \r", 100.0 * loopsDone / maxLoopsNeeded);
                loopsDone++;

                if (state.GetLowestLabelVertex() != currentVertex)
                {
                    // Interleave. Switch to the Dijkstra procedure of the vertex which currently has the lowest distance.
                    state.SetLabelsFibonacciHeap(labels);
                    state.SetVisitedHashSet(visited);
                    state.SetPathsFound(paths);
                    state.SetComingFromDictionary(comingFrom);
                    
                    currentVertex = state.GetNextVertex();
                    labels = state.GetLabelsFibonacciHeap();
                    visited = state.GetVisitedHashSet();
                    paths = state.GetPathsFound();
                    nodes = state.GetNodeMapping();
                    comingFrom = state.GetComingFromDictionary();
                }

                // Do one loop in Dijkstra algorithm
                var currentNode = labels.ExtractMin();
                var current = currentNode.Value;

                if (currentNode.Key > longestPath / 2)
                    break; //Travelled across the half of longest distance. No use in going further.

                // Consider all edges ending in unvisited neighbours
                var edges = graph.GetEdgesForVertex(current).Where(x => !visited.Contains(x.Other(current)));
                // Update labels on the other end
                foreach (var edge in edges)
                {
                    if (currentNode.Key + edge.Cost < nodes[edge.Other(current)].Key)
                    {
                        labels.DecreaseKey(nodes[edge.Other(current)], currentNode.Key + edge.Cost);
                        comingFrom[edge.Other(current)] = edge;
                    }
                }

                visited.Add(current);
                if (current != currentVertex)
                {
                    // Travel back the new path
                    List<Edge> pathEdges = new List<Edge>();
                    Vertex pathVertex = current;
                    while (pathVertex != currentVertex)
                    {
                        pathEdges.Add(comingFrom[pathVertex]);
                        pathVertex = comingFrom[pathVertex].Other(pathVertex);
                    }

                    pathEdges.Reverse();
                    Path path = new Path(currentVertex);
                    path.Edges.AddRange(pathEdges);
                    paths[current] = path;
                }

                // Find matching endpoints from two different terminals
                var mutualEnd = state.FindPathsEndingInThisVertex(current);
                if (mutualEnd.Count() > 1)
                {
                    var terminals = mutualEnd.Select(x => x.Start).ToList();

                    // Step 1. Calculate new heuristic function value for this shared point.
                    // f(x) = (Cost^2)/(NumberOfTerminals^3)
                    var f1 = Math.Pow(mutualEnd.Sum(p => p.TotalCost), 2) / Math.Pow(terminals.Count, 3);
                    var f2 = Math.Pow(mutualEnd.Sum(p => p.TotalCost), 1) / Math.Pow(terminals.Count, 2);
                    var f3 = Math.Pow(mutualEnd.Sum(p => p.TotalCost), 3) / Math.Pow(terminals.Count, 2);
                    var terminalsAvgF = terminals.Select(x => terminalFValues[x]).Average();
                    var terminalsMinF = terminals.Select(x => terminalFValues[x]).Min();
                    var f = (new[] { f1, f2, f3 }).Max();
                    Debug.WriteLine("F value: {0}, Fmin: {3} - Connecting terminals: {1} via {2}", f, string.Join(", ", terminals.Select(x => x.VertexName)), current.VertexName, terminalsMinF);

                    // Do not proceed if f > avgF AND working in same component
                    if (terminals.Select(x => components[x]).Distinct().Count() == 1 && f > terminalsMinF)
                        continue;

                    Debug.WriteLine("Proceeding with connection...");

                    // Step 2. Disconnect terminals in mutual component.
                    foreach (var group in terminals.GroupBy(x => components[x]))
                    {
                        if (group.Count() <= 1)
                            continue;

                        HashSet<Edge> remove = new HashSet<Edge>();
                        var sameComponentTerminals = group.ToList();
                        for (int i = 0; i < sameComponentTerminals.Count-1; i++)
                        {
                            for (int j = i+1; j< sameComponentTerminals.Count; j++)
                            {
                                var removePath = Algorithms.DijkstraPath(sameComponentTerminals[i], sameComponentTerminals[j], solution);
                                foreach (var e in removePath.Edges)
                                    remove.Add(e);
                            }
                        }

                        foreach (var e in remove)
                            solution.RemoveEdge(e, false);
                    }

                    components = solution.CreateComponentTable();

                    // Step 3. Reconnect all now disconnected terminals via shared endpoint
                    foreach (var t in terminals)
                    {
                        var path = Algorithms.DijkstraPath(t, current, graph);
                        foreach (var edge in path.Edges)
                            solution.AddEdge(edge);
                        // Update f value
                        terminalFValues[t] = f;
                    }

                    components = solution.CreateComponentTable();
                }
            }

            // If this solution is connected, take MST
            if (graph.Terminals.Select(x => components[x]).Distinct().Count() == 1)
            {
                // Clean up!
                foreach (var vertex in solution.Vertices.Where(x => solution.GetDegree(x) == 0).ToList())
                    solution.RemoveVertex(vertex);

                int componentNumber = graph.Terminals.Select(x => components[x]).Distinct().Single();
                foreach (var vertex in components.Where(x => x.Value != componentNumber).Select(x => x.Key).ToList())
                    solution.RemoveVertex(vertex);

                solution = Algorithms.Kruskal(solution);
                return solution;
            }
            
            // If the solution is not connected, it is not a good solution.
            return null;
        }

        private static Dictionary<Vertex, double> CreateInitialFValuesTable(Graph graph)
        {
            Dictionary<Vertex, double> fvalues = new Dictionary<Vertex, double>();

            foreach (var t in graph.Terminals)
                fvalues.Add(t, double.MaxValue);

            return fvalues;
        }
        
        private static Dictionary<int, double> CalculateComponentFunctionValues(Graph graph,
            Dictionary<Vertex, int> components)
        {
            Dictionary<int, double> fValues = new Dictionary<int, double>();
            foreach (var componentNumber in components.Values.Distinct())
            {
                //Get all edges in this component
                HashSet<Edge> edgesInComponent = new HashSet<Edge>();
                var verticesInComponent = components.Where(x => x.Value == componentNumber).Select(x => x.Key);
                var nrOfTerminals = verticesInComponent.Count(x => graph.Terminals.Contains(x));
                foreach (var v in verticesInComponent)
                {
                    foreach (var e in graph.GetEdgesForVertex(v))
                        edgesInComponent.Add(e);
                }

                if (edgesInComponent.Count > 0)
                {
                    double fVal = edgesInComponent.Sum(x => x.Cost)/Math.Pow(nrOfTerminals, 2);
                    fValues.Add(componentNumber, fVal);
                }
                else
                    fValues.Add(componentNumber, double.MaxValue);
            }
            return fValues;
        }

        private class DijkstraState
        {
            private readonly Dictionary<Vertex, FibonacciHeap<int, Vertex>> _dictLabels = new Dictionary<Vertex, FibonacciHeap<int, Vertex>>();
            private readonly Dictionary<Vertex, Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node>> _dictNodes = new Dictionary<Vertex, Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node>>();
            private readonly Dictionary<Vertex, HashSet<Vertex>> _dictVisited = new Dictionary<Vertex, HashSet<Vertex>>();
            private readonly Dictionary<Vertex, Dictionary<Vertex, Path>> _dictPathsfound = new Dictionary<Vertex, Dictionary<Vertex, Path>>();
            private readonly Dictionary<Vertex, Dictionary<Vertex, Edge>> _dictComingFrom = new Dictionary<Vertex, Dictionary<Vertex, Edge>>();
            private readonly Dictionary<Vertex, int> _dictComponent = new Dictionary<Vertex, int>();
            private int _internalNewComponentCount = 0;

            /// <summary>
            /// The vertices from which there is a Dijkstra algorithm being run.
            /// </summary>
            private readonly HashSet<Vertex> _verticesInSearch = new HashSet<Vertex>();
            private Vertex _currentlyUsingVertex = null;
            private Vertex _currentlyLowestLabelUnusedVertex = null;

            public DijkstraState() { }

            public void AddVertexToInterleavingDijkstra(Vertex vertex, Graph graph)
            {
                if (_verticesInSearch.Contains(vertex))
                    throw new InvalidOperationException("Can not add the vertex, because it is already added.");

                _verticesInSearch.Add(vertex);
                var labels = new FibonacciHeap<int, Vertex>();
                var nodes = new Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node>();
                // Initialize labels.
                foreach (var v in graph.Vertices)
                {
                    var node = labels.Add(v == vertex ? 0 : int.MaxValue, v);
                    nodes.Add(v, node);
                }

                _dictLabels.Add(vertex, labels);
                _dictNodes.Add(vertex, nodes);
                _dictVisited.Add(vertex, new HashSet<Vertex>());
                _dictPathsfound.Add(vertex, new Dictionary<Vertex, Path>());
                _dictComingFrom.Add(vertex, new Dictionary<Vertex, Edge>());
                _dictComponent.Add(vertex, ++_internalNewComponentCount); // All vertices start in a different component.
            }

            /// <summary>
            /// Method to get the next vertex to apply interleaving Dijkstra on.
            /// </summary>
            /// <returns>The vertex on which to apply interleaving Dijkstra.</returns>
            public Vertex GetNextVertex()
            {
                // Find the next vertex
                if (_currentlyLowestLabelUnusedVertex == null)
                    _currentlyLowestLabelUnusedVertex = GetLowestLabelUnusedVertex();

                _currentlyUsingVertex = _currentlyLowestLabelUnusedVertex;
                _currentlyLowestLabelUnusedVertex = null;
                return _currentlyUsingVertex;
            }

            /// <summary>
            /// Method to peek at the next vertex to apply interleaving Dijkstra on.
            /// </summary>
            /// <returns>The vertex on which to apply interleaving Dijkstra next.</returns>
            public Vertex PeekNextVertex()
            {
                // Find the next vertex
                if (_currentlyLowestLabelUnusedVertex == null)
                    _currentlyLowestLabelUnusedVertex = GetLowestLabelUnusedVertex();
                return _currentlyLowestLabelUnusedVertex;
            }

            /// <summary>
            /// Get the vertex of which the current Dijkstra procedure's lowest label is lowest of all procedures.
            /// </summary>
            /// <returns>The vertex for which the Dijkstra procedure currently has the lowest label.</returns>
            public Vertex GetLowestLabelUnusedVertex()
            {
                if (_currentlyLowestLabelUnusedVertex != null)
                    return _currentlyLowestLabelUnusedVertex;

                int value = int.MaxValue;
                Vertex vertex = null;
                foreach (var kvPair in _dictLabels)
                {
                    if (kvPair.Key == _currentlyUsingVertex) continue;
                    var fibonacciHeap = kvPair.Value;
                    if (fibonacciHeap.IsEmpty()) continue;

                    var node = fibonacciHeap.Peek();
                    if (node.Key <= value)
                    {
                        vertex = kvPair.Key;
                        value = node.Key;
                    }
                }
                _currentlyLowestLabelUnusedVertex = vertex;
                return _currentlyLowestLabelUnusedVertex;
            }

            /// <summary>
            /// Gets the vertex which currently has the lowest label in its Dijkstra procedure.
            /// </summary>
            /// <returns>The vertex which currently has the lowest label.</returns>
            public Vertex GetLowestLabelVertex()
            {
                int value = int.MaxValue;
                Vertex vertex = null;

                //This vertex is either the lowest unused, or the current vertex.
                if (_currentlyUsingVertex != null && !_dictLabels[_currentlyUsingVertex].IsEmpty())
                {
                    value = _dictLabels[_currentlyUsingVertex].Peek().Key;
                    vertex = _currentlyUsingVertex;
                }

                var next = GetLowestLabelUnusedVertex();
                if (next != null)
                {
                    var nextValue = _dictLabels[next].Peek().Key;
                    if (nextValue < value)
                        vertex = next;
                }

                return vertex;
            }

            public List<Path> FindPathsEndingInThisVertex(Vertex end)
            {
                List<Path> paths = new List<Path>();
                foreach (var kvPair in _dictPathsfound)
                {
                    var pathsDictionary = kvPair.Value;
                    if (pathsDictionary.ContainsKey(end))
                        paths.Add(pathsDictionary[end]);
                }
                return paths;
            }

            public FibonacciHeap<int, Vertex> GetLabelsFibonacciHeap()
            {
                return _dictLabels[_currentlyUsingVertex];
            }

            public void SetLabelsFibonacciHeap(FibonacciHeap<int, Vertex> heap)
            {
                _dictLabels[_currentlyUsingVertex] = heap;
            }

            public HashSet<Vertex> GetVisitedHashSet()
            {
                return _dictVisited[_currentlyUsingVertex];
            }

            public void SetVisitedHashSet(HashSet<Vertex> set)
            {
                _dictVisited[_currentlyUsingVertex] = set;
            }

            public Dictionary<Vertex, Path> GetPathsFound()
            {
                return _dictPathsfound[_currentlyUsingVertex];
            }

            public void SetPathsFound(Dictionary<Vertex, Path> paths)
            {
                _dictPathsfound[_currentlyUsingVertex] = paths;
            }

            public Dictionary<Vertex, FibonacciHeap<int, Vertex>.Node> GetNodeMapping()
            {
                return _dictNodes[_currentlyUsingVertex];
            }

            public Dictionary<Vertex, Edge> GetComingFromDictionary()
            {
                return _dictComingFrom[_currentlyUsingVertex];
            }

            public void SetComingFromDictionary(Dictionary<Vertex, Edge> comingFrom)
            {
                _dictComingFrom[_currentlyUsingVertex] = comingFrom;
            }
        }
    }
}
