using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;
using STPLocalSearch.Data;

namespace STPLocalSearch.Reduce
{
    public static class ReachabilityTest
    {
        public static ReductionResult RunTest(Graph graph, int upperBound)
        {
            var result = new ReductionResult();
            int reductionBound = 0;

            // Given an upper bound for a solution to the Steiner Tree Problem in graphs,
            // a vertex i can be removed if max{distance(i, k)} > upper bound (with k a terminal).
            HashSet<Vertex> remove = new HashSet<Vertex>();
            Dictionary<Vertex, int> currentMaximums = graph.Vertices.ToDictionary(vertex => vertex, vertex => 0);

            foreach (var terminal in graph.Terminals)
            {
                var toAll = Algorithms.DijkstraToAll(terminal, graph);
                foreach (var vertex in graph.Vertices)
                {
                    if (vertex == terminal) continue;
                    if (toAll[vertex] > currentMaximums[vertex])
                        currentMaximums[vertex] = toAll[vertex];
                }
            }

            foreach (var vertex in graph.Vertices)
            {
                if (currentMaximums[vertex] > upperBound)
                    remove.Add(vertex);
                else if (currentMaximums[vertex] > reductionBound)
                    reductionBound = currentMaximums[vertex];
            }

            foreach (var vertex in remove)
            {
                graph.RemoveVertex(vertex);
                result.RemovedVertices.Add(vertex);
            }

            result.ReductionUpperBound = reductionBound;

            return result;
        }
    }
}
