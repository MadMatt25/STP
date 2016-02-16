using System;
using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;
using STPLocalSearch.Data;

namespace STPLocalSearch.Reduce
{
    public static class TerminalBottleneckTest
    {
        public static ReductionResult RunTest(Graph graph)
        {
            List<int> bottlenecks = new List<int>();
            for (int i = 0; i < graph.Terminals.Count; i++)
            {
                var pathToAll = Algorithms.DijkstraPathToAll(graph.Terminals[i], graph);
                // Only add the maximum bottleneck of all paths
                bottlenecks.Add(pathToAll.SelectMany(x => x.Value.Edges).Max(e => e.Cost));
            }

            int B = bottlenecks.Max();
            
            List<Edge> redundant = new List<Edge>();
            foreach (var edge in graph.Edges.Where(edge => edge.Cost > B))
                redundant.Add(edge);

            foreach (var edge in redundant)
                graph.RemoveEdge(edge);

            return new ReductionResult(graph, 0);
        }
    }
}
