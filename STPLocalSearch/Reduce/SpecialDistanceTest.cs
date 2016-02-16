using System;
using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;
using STPLocalSearch.Data;

namespace STPLocalSearch.Reduce
{
    public static class SpecialDistanceTest
    {
        /// <summary>
        /// Runs the Special Distance Test to reduce the graph.
        /// </summary>
        /// <param name="graph">The graph on which to run the test.</param>
        /// <returns>The reduced graph.</returns>
        public static ReductionResult RunTest(Graph graph)
        {
            List<Edge> redundant = new List<Edge>();
            
            var distanceGraph = graph.CreateDistanceGraph();
            var specialDistanceGraph = CreateInitialSpecialDistanceGraph(graph);
            for (int i = 0; i < graph.NumberOfVertices; i++)
            {
                Console.Write("SD Test: {0}/{1}   \r", i, graph.NumberOfVertices);
                // Step 1. L := { i }
                //         for all j set delta_j = d_ij
                // In each iteration, delta_j means the current special distance from the start vertex to j.
                var vFrom = graph.Vertices[i];
                List<Vertex> L = new List<Vertex>(new [] { vFrom }); // Initially only the start veretx is labeled.
                // Initially set special distance equal to distance
                foreach (var edge in specialDistanceGraph.GetEdgesForVertex(vFrom))
                {
                    edge.Cost =
                        distanceGraph.GetEdgesForVertex(vFrom).Single(x => x.Other(vFrom) == edge.Other(vFrom)).Cost;
                    //edge.Cost = Math.Min(edge.Cost,
                    //    distanceGraph.GetEdgesForVertex(vFrom).Single(x => x.Other(vFrom) == edge.Other(vFrom)).Cost);
                }

                List<Vertex> unhandledTerminals = null; // K \ L
                while ((unhandledTerminals = graph.Terminals.Where(x => !L.Contains(x)).ToList()).Count > 0)
                { // While K \ L is not empty
                    // Find the terminal which minimizes delta(j) for all j in K \ L
                    int currentMinimum = int.MaxValue;
                    Vertex k = null;
                    var edgesFrom = specialDistanceGraph.GetEdgesForVertex(vFrom);
                    foreach (var terminal in unhandledTerminals)
                    {
                        var deltaEdge = edgesFrom.First(x => x.Other(vFrom) == terminal);
                        if (deltaEdge.Cost < currentMinimum)
                        {
                            currentMinimum = deltaEdge.Cost;
                            k = terminal;
                        }
                    }

                    L.Add(k);

                    // Re-lable all vertices that haven't gotten a definitive label yet.
                    var delta_k = edgesFrom.First(x => x.Other(vFrom) == k).Cost;
                    foreach (var unlabeled in graph.Vertices.Where(x => !L.Contains(x)))
                    {
                        var d_kj = distanceGraph.GetEdgesForVertex(k).First(x => x.Other(k) == unlabeled).Cost;
                        var deltaEdge = edgesFrom.First(x => x.Other(vFrom) == unlabeled);
                        deltaEdge.Cost = Math.Min(deltaEdge.Cost, Math.Max(delta_k, d_kj));
                    }
                }

                var specialEdges = specialDistanceGraph.GetEdgesForVertex(vFrom);
                var distanceEdges = distanceGraph.GetEdgesForVertex(vFrom);
                var edges = graph.GetEdgesForVertex(vFrom);
                foreach (var redundantEdge in specialEdges.Where(x => x.Cost < distanceEdges.First(y => y.Other(vFrom) == x.Other(vFrom)).Cost))
                {
                    // Special distance is smaller than distance. Edge is redundant.
                    var edge = edges.FirstOrDefault(x => x.Other(vFrom) == redundantEdge.Other(vFrom));
                    if (edge != null)
                        redundant.Add(edge);
                }
            }

            foreach (var edge in redundant)
            {
                graph.RemoveEdge(edge);
            }

            Console.Write("                                  \r");

            return new ReductionResult(graph, 0);
        }

        /// <summary>
        /// Method to create and return a complete graph (like a distance graph), but
        /// each edge has cost int.MaxValue. Those costs are later changed to be the
        /// special distances between vertices.
        /// </summary>
        /// <param name="graph">The graph to calculate the initial special distance graph for.</param>
        /// <returns>The initial special distance graph, containing all int.MaxValue special distances.</returns>
        private static Graph CreateInitialSpecialDistanceGraph(Graph graph)
        {
            var n = graph.NumberOfVertices;
            Graph specialGraph = new Graph(graph.Vertices);
            for (int from = 0; from < n; from++)
            {
                for (int to = from + 1; to < n; to++)
                {
                    var vFrom = graph.Vertices[from];
                    var vTo = graph.Vertices[to];
                    specialGraph.AddEdge(vFrom, vTo, int.MaxValue);
                }
            }
            return specialGraph;
        }
    }
}
