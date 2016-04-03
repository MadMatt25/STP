using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using STPLocalSearch.Data;
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
        public static ReductionResult RunTest(Graph graph)
        {
            if (!graph.Terminals.All(graph.ContainsVertex))
                Debugger.Break();
            
            var tmst = Algorithms.Kruskal(graph.TerminalDistanceGraph);
            var terminalSpecialDistances = new MultiDictionary<Vertex, int>();
            for (int i = 0; i < graph.Terminals.Count - 1; i++)
            {
                var tFrom = graph.Terminals[i];
                var toAll = Algorithms.DijkstraPathToAll(tFrom, tmst);
                for (int j = i + 1; j < graph.Terminals.Count; j++)
                {
                    var tTo = graph.Terminals[j];
                    var path = toAll[tTo];
                    var sd = path.Edges.Max(x => x.Cost);
                    terminalSpecialDistances.Add(tFrom, tTo, sd);
                }
            }

            var result = new ReductionResult();
            
            // Find all special distances between terminals
            int count = 0;
            int e = graph.NumberOfEdges;

            Dictionary<Vertex, Path> nearest = new Dictionary<Vertex, Path>();
            HashSet<Edge> remove = new HashSet<Edge>();

            int edgesWithoutRemoval = 0;

            foreach (var edge in graph.Edges.OrderByDescending(x => x.Cost))
            {
                count++;
                var vFrom = edge.Either();
                var vTo = edge.Other(vFrom);

                int SDEstimate = int.MaxValue;
                Path pathToNearestFrom = null;
                if (nearest.ContainsKey(vFrom))
                    pathToNearestFrom = nearest[vFrom];
                else
                {
                    pathToNearestFrom = Algorithms.NearestTerminal(vFrom, graph);
                    nearest.Add(vFrom, pathToNearestFrom);
                }
                var aNearestTerminalFrom = pathToNearestFrom.End;

                Path pathToNearestTo = null;
                if (nearest.ContainsKey(vTo))
                    pathToNearestTo = nearest[vTo];
                else
                { 
                    pathToNearestTo = Algorithms.NearestTerminal(vTo, graph);
                    nearest.Add(vTo, pathToNearestTo);
                }
                var bNearestTerminalTo = pathToNearestTo.End;

                // SD = Max( dist(v, z_a), dist(w, z_b), sd(z_a, z_b) )
                var sd = Math.Max(pathToNearestFrom.TotalCost, pathToNearestTo.TotalCost);
                if (aNearestTerminalFrom != bNearestTerminalTo)
                {
                    var sdTerminals = terminalSpecialDistances[aNearestTerminalFrom, bNearestTerminalTo];
                    sd = Math.Max(sd, sdTerminals);
                }

                if (sd < SDEstimate)
                    SDEstimate = sd;

                if (edge.Cost > SDEstimate)
                {
                    edgesWithoutRemoval = 0;
                    remove.Add(edge);
                }
                else if (++edgesWithoutRemoval >= graph.NumberOfEdges / 100) // Expecting a 1% reduction
                {
                    break;
                }
            }

            foreach (var edge in remove)
            {
                graph.RemoveEdge(edge);
                result.RemovedEdges.Add(edge);
            }

            return result;
        }
    }
}
