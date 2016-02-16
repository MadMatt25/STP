using System;
using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;

namespace STPLocalSearch.Reduce
{
    public static class TriangleTest
    {
        public static Graph RunTest(Graph graph)
        {
            var mst = Algorithms.Kruskal(graph);
            var S = mst.GetAllEdges().Max(edge => edge.Cost);
            List<Edge> redundant = new List<Edge>();
            foreach (var edge in graph.GetAllEdges().Where(e => e.Cost > S))
                redundant.Add(edge);
            foreach (var edge in redundant)
                graph.RemoveEdge(edge);

            return graph;
        }
    }
}
