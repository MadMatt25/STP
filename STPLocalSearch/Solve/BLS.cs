using STPLocalSearch.Graphs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using STPLocalSearch.Data;

namespace STPLocalSearch.Solve
{
    public static class BLS
    {
        //private const int MAX_LOOPS = 100;
        //private const int MAX_TRIES = 5;
        private const int MIN_BREAKOUT = 1;
        private const int MAX_BREAKOUT = 64;

        public static Graph RunSolver(Graph problemInstance, Graph initialSolution, int reductionBound)
        {
            var currentSolution = initialSolution.Clone();
            foreach (var vertex in problemInstance.Vertices)
                if (!currentSolution.ContainsVertex(vertex))
                    currentSolution.AddVertex(vertex);

            foreach (var terminal in currentSolution.Terminals)
                terminal.InitializeScore(100);
            // Non-terminals get initial score of 20. (not preferred, not ignored)
            foreach (var vertex in currentSolution.Vertices.Except(currentSolution.Terminals))
                vertex.InitializeScore(100);
            
            var currentBest = currentSolution.Clone();

            int currentBreakout = MIN_BREAKOUT;

            Debug.Write("\r\n\r\n\r\n");

            while (true)
            {
                Console.Write("\rRunning BLS... Current Breakout {0} - Best: {1} \r", currentBreakout, currentBest.TotalCost);
                // Get neighbour solution
                Stopwatch neighbourTimer = new Stopwatch();
                neighbourTimer.Start();
                var currentNeighbourhood = Neighbourhood.SteinerNodeInsertion; // currentBreakout <= 4 ? Neighbourhood.SteinerNodeRemoval : Neighbourhood.SteinerNodeInsertion;
                currentSolution = GetNeighbourSolution(currentSolution, problemInstance, currentBreakout, currentNeighbourhood);
                neighbourTimer.Stop();
                if (currentSolution.TotalCost >= currentBest.TotalCost && currentBreakout <= 16)
                {
                    // Switch neighbourhood
                    if (currentNeighbourhood == Neighbourhood.SteinerNodeRemoval)
                        currentNeighbourhood = Neighbourhood.SteinerNodeInsertion;
                    else
                        currentNeighbourhood = Neighbourhood.SteinerNodeRemoval;
                    neighbourTimer.Reset();
                    neighbourTimer.Start();
                    currentSolution = GetNeighbourSolution(currentSolution, problemInstance, currentBreakout, currentNeighbourhood);
                    neighbourTimer.Stop();
                }

                if (currentSolution.TotalCost < currentBest.TotalCost)
                {
                    Debug.WriteLine(" Improvement: {0}ms, with neigbourhood: {1} & BLS: {2}", neighbourTimer.ElapsedMilliseconds, currentNeighbourhood, currentBreakout);
                    currentBest = currentSolution.Clone();
                    currentBreakout = MIN_BREAKOUT;
                }
                // If reduction is possible, then consider this the best solution possible, cause a reduction step
                else if (currentBest.TotalCost < reductionBound)
                {
                    Debug.WriteLine("Preferred reduction over breakout. :)");
                    return currentBest;
                }
                // Do break-out!
                else if (currentBreakout == MAX_BREAKOUT)
                    break;
                else // if (problemInstance.Vertices.Except(problemInstance.Terminals).All(x => x.ReportsRealScore))
                    currentBreakout = Math.Min(MAX_BREAKOUT, currentBreakout * 2);
                
                currentSolution = currentBest.Clone();
            }

            Console.Write("\rRunning BLS...                                                             \r");
            var removeVertices = currentBest.Vertices.Where(x => currentBest.GetDegree(x) == 0).ToList();
            foreach (var vertex in removeVertices)
                currentBest.RemoveVertex(vertex);

            return currentBest;
        }

        private static Graph GetNeighbourSolution(Graph currentSolution, Graph problemInstance, int breakout, Neighbourhood neighbourhood)
        {
            if (breakout < 1)
                throw new ArgumentException("Breakout can not be less than 1.");

            switch (neighbourhood)
            {
                case Neighbourhood.SteinerNodeRemoval:
                    return GetNeighbourSolutionWithSteinerNodeRemovalNeighbourhood(currentSolution, problemInstance, breakout);
                case Neighbourhood.SteinerNodeInsertion:
                    return GetNeighbourSolutionWithSteinerNodeInsertionNeighbourhood(currentSolution, problemInstance, breakout);
                case Neighbourhood.Edge:
                    return GetNeighbourSolutionWithEdgeNeighbourhood(currentSolution, problemInstance, breakout);
                default:
                    return null;
            }
        }

        private static Graph GetNeighbourSolutionWithSteinerNodeRemovalNeighbourhood(Graph currentSolution, Graph problemInstance, int breakout)
        {
            var initialSolution = currentSolution.Clone();
            foreach (var vertex in problemInstance.Vertices)
                if (!initialSolution.ContainsVertex(vertex))
                    initialSolution.AddVertex(vertex);

            int MAX_COMBINATIONS = (int) Math.Ceiling(Math.Log(problemInstance.NumberOfEdges)) * 4;
            var possibleVictims = currentSolution.Vertices.Where(x => currentSolution.GetDegree(x) > 0)
                                                          .Except(problemInstance.Required)
                                                          .OrderBy(x => x.AverageScore)
                                                          .Take(breakout * 2).ToList();
            int numberOfCombinations = Combinations<object>.NumberOfCombinations(possibleVictims.Count, breakout) > MAX_COMBINATIONS ? MAX_COMBINATIONS : Combinations<object>.NumberOfCombinations(possibleVictims.Count, breakout);
            var allCombinations = new Combinations<Vertex>(possibleVictims, breakout).GetRandomCombinations(numberOfCombinations);

            foreach (var removeSteiner in allCombinations)
            {
                var workingSolution = initialSolution.Clone();
                // Pick Steiner node
                var removeEdges = new HashSet<Edge>();
                foreach (var steiner in removeSteiner)
                {
                    foreach (var edge in workingSolution.GetEdgesForVertex(steiner))
                        removeEdges.Add(edge);
                }

                //Disconnect and reconnect
                foreach (var edge in removeEdges)
                    workingSolution.RemoveEdge(edge, false);
                //Prune current solution
                IEnumerable<Vertex> degreeOne;
                while ((degreeOne =
                        workingSolution.Vertices.Except(problemInstance.Terminals)
                            .Where(x => workingSolution.GetDegree(x) == 1)).Any())
                {
                    foreach (var degreeZeroSteiner in degreeOne.ToList())
                        foreach (var edge in workingSolution.GetEdgesForVertex(degreeZeroSteiner).ToList())
                            workingSolution.RemoveEdge(edge, false);
                }

                var previousNodes = workingSolution.Vertices.Where(x => workingSolution.GetDegree(x) > 0).ToList();
                
                ReconnectTerminals(workingSolution, problemInstance);

                if (workingSolution.TotalCost < currentSolution.TotalCost)
                {
                    //Console.Beep(2000, 150);
                    //Console.Beep(2100, 150);

                    foreach (var vertex in problemInstance.Vertices.Except(problemInstance.Terminals)
                                                                   .Intersect(workingSolution.Vertices))
                        vertex.IncreaseScore(3);

                    return workingSolution;
                }
                else
                {
                    foreach (var vertex in workingSolution.Vertices.Where(x => workingSolution.GetDegree(x) > 0).Except(previousNodes))
                        vertex.DecreaseScore(1);
                }
            }

            return currentSolution;
        }

        private static Graph GetNeighbourSolutionWithSteinerNodeInsertionNeighbourhood(Graph currentSolution, Graph problemInstance, int breakout)
        {
            foreach (var degreeZero in currentSolution.Vertices.Where(x => currentSolution.GetDegree(x) == 0).ToList())
                currentSolution.RemoveVertex(degreeZero);

            var workingSolution = currentSolution.Clone();
                
            // 1. Pick a random starting key vertex (deg > 2)
            // 2. Follow path starting from one of its edges
            // 3. After finding another key node in current solution, take this path as new path to insert
            // 4. Optionally, explore in each starting direction from initial node and insert cheapest / most expensive path

            var startingKeyNodes =
                workingSolution.Vertices.Where(x => workingSolution.GetDegree(x) > 2)
                                        .OrderByDescending(x => x.AverageScore)
                                        .Take(breakout * 3 * (int)Math.Log(problemInstance.NumberOfVertices))
                                        .ToList();
            
            var previousNodes = workingSolution.Vertices.Where(x => workingSolution.GetDegree(x) > 0).ToList();

            foreach (var startingKeyNode in startingKeyNodes)
            {
                var path = new Path(startingKeyNode);
                var edge =
                    problemInstance.GetEdgesForVertex(startingKeyNode)
                        .OrderByDescending(x => x.Other(startingKeyNode).Score)
                        .Where(x => !workingSolution.ContainsEdge(x))
                        .Take((problemInstance.GetDegree(startingKeyNode)/2) + 1)
                        .RandomElement();

                if (edge == null)
                    continue;

                var fromVertex = startingKeyNode;
                var toVertex = edge.Other(fromVertex);
                path.Edges.Add(edge);

                while (!workingSolution.ContainsVertex(toVertex) || workingSolution.GetDegree(toVertex) <= 2)
                {
                    var prevFromVertex = fromVertex;
                    fromVertex = toVertex;
                    var edges = problemInstance.GetEdgesForVertex(fromVertex).ToList();
                    edge = edges.Where(x => x.WhereBoth(y => y != prevFromVertex) && x.WhereOne(y => !path.Vertices.Contains(y)))
                                .OrderByDescending(x => x.Other(fromVertex).Score)
                                .Take((problemInstance.GetDegree(fromVertex)/2) + 1)
                                .RandomElement();

                    if (edge == null)
                        break;

                    toVertex = edge.Other(fromVertex);
                    path.Edges.Add(edge);
                }

                foreach (var vertex in path.Vertices.Where(x => !workingSolution.ContainsVertex(x)))
                    workingSolution.AddVertex(vertex);

                AddEdgesToMST(workingSolution, path.Edges);

                //Prune current solution
                IEnumerable<Vertex> degreeOne;
                while ((degreeOne =
                    workingSolution.Vertices.Except(problemInstance.Terminals)
                        .Where(x => workingSolution.GetDegree(x) <= 1)).Any())
                {
                    foreach (var degreeZeroSteiner in degreeOne.ToList())
                        workingSolution.RemoveVertex(degreeZeroSteiner);
                }

                //if (workingSolution.ComponentCheck() > 1)
                //    Debugger.Break();

                //var mst = Algorithms.Kruskal(workingSolution);
                //if (mst.TotalCost < workingSolution.TotalCost)
                //    Debugger.Break();

                if (workingSolution.TotalCost < currentSolution.TotalCost)
                {
                    //Console.Beep(2000, 150);
                    //Console.Beep(2100, 150);

                    foreach (var vertex in problemInstance.Vertices.Except(problemInstance.Terminals)
                                                                    .Intersect(workingSolution.Vertices))
                        vertex.IncreaseScore(3);

                    return workingSolution;
                }
            }

            foreach (var vertex in workingSolution.Vertices.Where(x => workingSolution.GetDegree(x) > 0).Except(previousNodes))
                vertex.DecreaseScore(1);
            
            return currentSolution;
        }

        private static Graph GetNeighbourSolutionWithEdgeNeighbourhood(Graph currentSolution, Graph problemInstance, int breakout)
        {
            var currentBestNeighbourSolution = currentSolution.Clone();
            var initialSolution = currentSolution.Clone();

            var edgesToRemove = currentSolution.Edges.OrderByDescending(x => x.Cost).Take(breakout);

            foreach (var edge in edgesToRemove)
            {
                var workingSolution = initialSolution.Clone();
                workingSolution.RemoveEdge(edge, false);

                ReconnectTerminals(workingSolution, problemInstance);

                if (workingSolution.TotalCost < currentBestNeighbourSolution.TotalCost)
                    currentBestNeighbourSolution = workingSolution;
            }
            
            return currentBestNeighbourSolution;
        }

        private static void ReconnectTerminalsAlternative(Graph workingSolution, Graph problemInstance)
        {
            var components = workingSolution.CreateComponentTable();
            var componentsToConnect =
                components.Where(x => problemInstance.Terminals.Contains(x.Key))
                    .Select(x => x.Value)
                    .Distinct()
                    .ToList();

            if (componentsToConnect.Count <= 1) return;

            MultiDictionary<int, Tuple<Vertex, Vertex>> componentConnectingPathDictionary = new MultiDictionary<int, Tuple<Vertex, Vertex>>();
            Graph componentGraph = new Graph(componentsToConnect.Select(x => new Vertex(x)).ToList());

            for (int i = 0; i < componentsToConnect.Count - 1; i++)
            {
                int fromComponent = componentsToConnect[i];
                int toComponent = componentsToConnect[i + 1];

                int minDistance = int.MaxValue;
                foreach (var fromVertex in components.Where(x => x.Value == fromComponent).Select(x => x.Key))
                {
                    var distances = Algorithms.DijkstraToAll(fromVertex, problemInstance);
                    foreach (var toVertex in components.Where(x => x.Value == toComponent).Select(x => x.Key))
                    {
                        int distance = distances[toVertex];
                        if (!componentConnectingPathDictionary.ContainsKey(fromComponent, toComponent))
                        {
                            componentConnectingPathDictionary.Add(fromComponent, toComponent, new Tuple<Vertex, Vertex>(fromVertex, toVertex));
                            componentGraph.AddEdge(new Edge(componentGraph.Vertices.Single(x => x.VertexName == fromComponent), componentGraph.Vertices.Single(x => x.VertexName == toComponent), minDistance));
                        }

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            componentConnectingPathDictionary[fromComponent, toComponent] = new Tuple<Vertex, Vertex>(fromVertex, toVertex);
                            componentGraph.GetEdgesForVertex(
                                componentGraph.Vertices.Single(x => x.VertexName == fromComponent))
                                .Single(
                                    x =>
                                        x.Other(componentGraph.Vertices.Single(y => y.VertexName == fromComponent)) ==
                                        componentGraph.Vertices.Single(y => y.VertexName == toComponent))
                                .Cost = minDistance;
                        }
                    }
                }
            }
            componentGraph = Algorithms.Kruskal(componentGraph);
            foreach (var edge in componentGraph.Edges)
            {
                var v1 = edge.Either();
                var v2 = edge.Other(v1);
                var vertices = componentConnectingPathDictionary[v1.VertexName, v2.VertexName];
                var path = Algorithms.DijkstraPath(vertices.Item1, vertices.Item2, problemInstance);
                foreach (var pathEdge in path.Edges)
                    workingSolution.AddEdge(pathEdge);
            }
        }

        private static void ReconnectTerminals(Graph workingSolution, Graph problemInstance)
        {
            Stopwatch reconnectStopwatch = new Stopwatch();
            Stopwatch randomStopwatch = new Stopwatch();
            reconnectStopwatch.Start();

            var components = workingSolution.CreateComponentTable();
            var componentsToConnect =
                components.Where(x => problemInstance.Terminals.Contains(x.Key))
                    .Select(x => x.Value)
                    .Distinct()
                    .ToList();

            if (componentsToConnect.Count <= 1) return;

            MultiDictionary<int, Tuple<Vertex, Vertex>> componentConnectingPathDictionary = new MultiDictionary<int, Tuple<Vertex, Vertex>>();
            List<Vertex> componentGraphVertices = new List<Vertex>();
            foreach (var i in componentsToConnect)
                componentGraphVertices.Add(new Vertex(i));
            Graph componentGraph = new Graph(componentGraphVertices);

            for (int i = 0; i < componentsToConnect.Count; i++)
            {
                int fromComponent = componentsToConnect[i];
                for (int j = i + 1; j < componentsToConnect.Count; j++)
                {
                    int toComponent = componentsToConnect[j];
                    int minDistance = int.MaxValue;
                    
                    foreach (var fromVertex in components.Where(x => x.Value == fromComponent)
                                                         .Select(x => x.Key)
                                                         .OrderByDescending(x => x.AverageScore)
                                                         .Take(75)
                                                         .RandomElements(50))
                    // Take the first 75 "most preferred nodes" (highest score) and choose 50 of them randomly
                    {
                        randomStopwatch.Start();
                        var distances = Algorithms.DijkstraToAll(fromVertex, problemInstance);
                        randomStopwatch.Stop();
                        foreach (var toVertex in distances.Keys)
                        {
                            if (components[toVertex] != toComponent)
                                continue;

                            int distance = distances[toVertex];
                            if (!componentConnectingPathDictionary.ContainsKey(fromComponent, toComponent))
                            {
                                componentConnectingPathDictionary.Add(fromComponent, toComponent, new Tuple<Vertex, Vertex>(fromVertex, toVertex));
                                componentGraph.AddEdge(new Edge(componentGraphVertices[i], componentGraphVertices[j], minDistance));
                            }

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                componentConnectingPathDictionary[fromComponent, toComponent] = new Tuple<Vertex, Vertex>(fromVertex, toVertex);
                                componentGraph.GetEdgesForVertex(componentGraphVertices[i])
                                    .Single(x => x.Other(componentGraphVertices[i]) == componentGraphVertices[j])
                                    .Cost = minDistance;
                            }
                        }
                    }
                }
            }
            componentGraph = Algorithms.Kruskal(componentGraph);
            foreach (var edge in componentGraph.Edges)
            {
                var v1 = edge.Either();
                var v2 = edge.Other(v1);
                var vertices = componentConnectingPathDictionary[v1.VertexName, v2.VertexName];
                var path = Algorithms.DijkstraPath(vertices.Item1, vertices.Item2, problemInstance);
                foreach (var pathEdge in path.Edges)
                    workingSolution.AddEdge(pathEdge);
            }

            reconnectStopwatch.Stop();
        }

        private static void AddEdgesToMST(Graph mst, List<Edge> edges)
        {
            foreach (var edge in edges)
            {
                if (edge.WhereBoth(x => mst.GetDegree(x) > 0)) //Both vertices of this edge are in the MST, introducing this edge creates cycle!
                {
                    var v1 = edge.Either();
                    var v2 = edge.Other(v1);
                    var components = mst.CreateComponentTable();

                    if (components[v1] == components[v2])
                    {
                        // Both are in the same component, and a path exists. 
                        // Travel the path to see if adding this edge makes it cheaper
                        var path = Algorithms.DijkstraPath(v1, v2, mst);

                        // However, we can remove a set of edges between two nodes
                        // When going from T1 to T3
                        // E.g.: T1 - v2 - v3 - v4 - T2 - v5 - v6 - T3
                        // The edges between T1, v2, v3, v4, T2 cost more than the path between T1 and T3.
                        // Removing those edges, and adding the new edge from T1 to T3, also connects
                        // T1 to T2, so still a tree!
                        var from = path.Start;
                        var last = path.Start;
                        int betweenTerminals = 0;
                        var subtractedPath = new Path(path.Start);
                        List<Edge> edgesInSubtraction = new List<Edge>();
                        Dictionary<Edge, List<Edge>> subtractions = new Dictionary<Edge, List<Edge>>();
                        for (int i = 0; i < path.Edges.Count; i++)
                        {
                            var pe = path.Edges[i];
                            betweenTerminals += pe.Cost;
                            last = pe.Other(last);
                            edgesInSubtraction.Add(pe);
                            if (mst.GetDegree(last) > 2 || mst.Terminals.Contains(last) || i == path.Edges.Count - 1)
                            {
                                var subtractedEdge = new Edge(from, last, betweenTerminals);
                                subtractions.Add(subtractedEdge, edgesInSubtraction);
                                edgesInSubtraction = new List<Edge>();
                                subtractedPath.Edges.Add(subtractedEdge);
                                from = last;
                                betweenTerminals = 0;
                            }
                        }

                        var mostCostly = subtractedPath.Edges[0];
                        for (int i = 1; i < subtractedPath.Edges.Count; i++)
                        {
                            if (subtractedPath.Edges[i].Cost > mostCostly.Cost)
                                mostCostly = subtractedPath.Edges[i];
                        }

                        if (mostCostly.Cost >= edge.Cost)
                        {
                            foreach (var e in subtractions[mostCostly])
                                mst.RemoveEdge(e, false);
                            mst.AddEdge(edge);
                        }
                    }
                    else // Connect the two disconnected components!
                        mst.AddEdge(edge);
                } 
                else
                    mst.AddEdge(edge);
            }
        }

        private enum Neighbourhood
        {
            SteinerNodeRemoval,
            SteinerNodeInsertion,
            Edge
        }
    }
}
