using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using STPLocalSearch.Graphs;

namespace STPLocalSearch.Reduce
{
    public static class SpecialDistanceApproxTest
    {
        /// <summary>
        /// Runs an approximation of the Special Distance Test to reduce the graph.
        /// This test runs much faster and offers only a small difference in performance.
        /// </summary>
        /// <param name="graph">The graph on which to run the test.</param>
        /// <returns>The reduced graph.</returns>
        public static Graph RunTest(Graph graph)
        {
            if (!graph.RequiredNodes.All(graph.Vertices.Contains))
                Debugger.Break();

            List<Edge> redundant = new List<Edge>();

            var tmst = Algorithms.Kruskal(graph.CreateTerminalDistanceGraph());
            var specialDistanceGraph = new Graph(graph.RequiredNodes);
            for (int i = 0; i < graph.RequiredNodes.Count; i++)
            {
                Console.Write("\rCalculating SD between terminals: {0}/{1}   \r", i, graph.RequiredNodes.Count);
                var tFrom = graph.RequiredNodes[i];
                for (int j = i + 1; j < graph.RequiredNodes.Count; j++)
                {
                    var tTo = graph.RequiredNodes[j];
                    var path = Algorithms.DijkstraPath(tFrom, tTo, tmst);
                    var sd = path.Edges.Max(x => x.Cost);
                    specialDistanceGraph.AddEdge(tFrom, tTo, sd);
                }
            }

            Console.Write("\r                                                  \r");

            // Find all special distances between terminals
            int count = 0;
            int e = graph.NumberOfEdges;

            Dictionary<Vertex, Path> nearest = new Dictionary<Vertex, Path>();
            HashSet<Edge> edgesInNearestPaths = new HashSet<Edge>();
            
            for (int i = 0; i < graph.NumberOfEdges; i++)
            {
                var edge = graph.Edges[i];
                count++;
                Console.Write("\rSDApprox Test: {0}/{1}   \r", count, e);
                var vFrom = edge.Either();
                var vTo = edge.Other(vFrom);

                //if (graph.RequiredNodes.Contains(vFrom) || graph.RequiredNodes.Contains(vTo))
                //    continue;

                int SDEstimate = int.MaxValue;
                Path pathToNearestFrom = null;
                if (nearest.ContainsKey(vFrom))
                    pathToNearestFrom = nearest[vFrom];
                else
                {
                    pathToNearestFrom = Algorithms.NearestTerminal(vFrom, graph);
                    nearest.Add(vFrom, pathToNearestFrom);
                    foreach (var nearEdge in pathToNearestFrom.Edges)
                        edgesInNearestPaths.Add(nearEdge);
                }
                var aNearestTerminalFrom = pathToNearestFrom.End;

                Path pathToNearestTo = null;
                if (nearest.ContainsKey(vTo))
                    pathToNearestTo = nearest[vTo];
                else
                { 
                    pathToNearestTo = Algorithms.NearestTerminal(vTo, graph);
                    nearest.Add(vTo, pathToNearestTo);
                    foreach (var nearEdge in pathToNearestTo.Edges)
                        edgesInNearestPaths.Add(nearEdge);
                }
                var bNearestTerminalTo = pathToNearestTo.End;

                // SD = Max( dist(v, z_a), dist(w, z_b), sd(z_a, z_b) )
                var sd = Math.Max(pathToNearestFrom.TotalCost, pathToNearestTo.TotalCost);
                if (aNearestTerminalFrom != bNearestTerminalTo)
                {
                    var sdTerminals =
                        specialDistanceGraph.GetEdgesForVertex(aNearestTerminalFrom)
                            .Single(x => x.Other(aNearestTerminalFrom) == pathToNearestTo.End)
                            .Cost;
                    sd = Math.Max(sd, sdTerminals);
                }

                if (sd < SDEstimate)
                    SDEstimate = sd;
                
                if (edge.Cost > SDEstimate)
                {
                    graph.RemoveEdge(edge);
                    i--;
                    if (edgesInNearestPaths.Contains(edge))
                    {
                        List<Vertex> pathsToRecalculate = new List<Vertex>();
                        foreach (var path in nearest)
                        {
                            if (path.Value.Edges.Contains(edge))
                                pathsToRecalculate.Add(path.Key);
                        }
                        foreach (var vertex in pathsToRecalculate)
                            nearest.Remove(vertex);
                    }
                }
            }

            //foreach (var edge in redundant)
            //    graph.RemoveEdge(edge);

            if (!graph.RequiredNodes.All(graph.Vertices.Contains))
                Debugger.Break();

            return graph;
        }
    }
}
