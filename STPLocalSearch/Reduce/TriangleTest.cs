using System;
using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;
using STPLocalSearch.Data;

namespace STPLocalSearch.Reduce
{
    public static class TriangleTest
    {
        public static ReductionResult RunTest(Graph graph)
        {
            var mst = Algorithms.Kruskal(graph.TerminalDistanceGraph);
            var S = mst.Edges.Max(edge => edge.Cost);
            foreach (var edge in graph.Edges.Where(e => e.Cost > S).ToList())
                graph.RemoveEdge(edge);

            return new ReductionResult(graph, 0);
        }
    }
}
