using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STPLocalSearch.Graphs;
using STPLocalSearch.Data;

namespace STPLocalSearch.Reduce
{
    public static class VeronoiRegionTest
    {
        public static ReductionResult RunTest(Graph graph, int upperBound)
        {
            var graphContracted = graph.Clone();
            int reductionBound = int.MaxValue;

            // 1. Contract edges in MST that are in same Voronoi region
            var degreeTwo = graphContracted.Vertices.Where(x => graphContracted.GetDegree(x) == 2).ToList(); // Vertices which could be removed by contracting their adjacent edges
            foreach (var vertex in degreeTwo)
            {
                var voronoi = graph.GetVoronoiRegionForVertex(vertex);
                var edge1 = graphContracted.GetEdgesForVertex(vertex)[0];
                var edge2 = graphContracted.GetEdgesForVertex(vertex)[1];
                var vertex1 = edge1.Other(vertex);
                var vertex2 = edge2.Other(vertex);

                if (graph.GetVoronoiRegionForVertex(vertex1) == voronoi &&
                    graph.GetVoronoiRegionForVertex(vertex2) == voronoi)
                {
                    // Contract the two edges.
                    graphContracted.AddEdge(vertex1, vertex2, edge1.Cost + edge2.Cost);
                    graphContracted.RemoveEdge(edge1);
                    graphContracted.RemoveEdge(edge2);
                }
            }

            var G = new Graph(graph.Terminals);

            var result = new ReductionResult();
            result.ReductionUpperBound = reductionBound;

            return result;
        }
    }
}
