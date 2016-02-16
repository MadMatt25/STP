using System.Linq;
using STPLocalSearch.Graphs;
using STPLocalSearch.Data;

namespace STPLocalSearch.Reduce
{
    public static class DegreeTest
    {
        /// <summary>
        /// Runs the Degree Test to reduce the graph.
        /// </summary>
        /// <param name="graph">The graph on which to run the test.</param>
        /// <returns>The reduced graph.</returns>
        public static ReductionResult RunTest(Graph graph)
        {
            // Use some simple rules to reduce the problem.
            // The rules are: - if a vertex v has degree 1 and is not part of the required nodes, remove the vertex.
            //                - if a vertex v has degree 1 and is part of the required nodes, the one edge it is 
            //                  connected to has to be in the solution, and as a consequence, the node at the other side
            //                  is either a Steiner node or also required.
            //                - (not implemented yet) if a vertex v is required and has degree 2, and thus is connected to 2 edges, 
            //                  namely e1 and e2, and cost(e1) < cost(e2) and e1 = (u, v) and u is 
            //                  also a required vertex, then every solution must contain e1
            
            // Remove leaves as long as there are any
            var leaves = graph.Vertices.Where(v => graph.GetDegree(v) == 1 && !graph.Terminals.Contains(v)).ToList();
            while (leaves.Count > 0)
            {
                foreach (var leaf in leaves)
                    graph.RemoveVertex(leaf);
                leaves = graph.Vertices.Where(v => graph.GetDegree(v) == 1 && !graph.Terminals.Contains(v)).ToList();
            }

            // When a leaf is required, add the node on the other side of its one edge to required nodes
            var requiredLeaves = graph.Vertices.Where(v => graph.GetDegree(v) == 1 && graph.Terminals.Contains(v)).ToList();
            foreach (var requiredLeaf in requiredLeaves)
            {
                // Find the edge this leaf is connected to
                var alsoRequired = graph.GetEdgesForVertex(requiredLeaf).Single().Other(requiredLeaf);
                if (!graph.Terminals.Contains(alsoRequired))
                    graph.RequiredSteinerNodes.Add(alsoRequired);
            }
            
            return new ReductionResult(graph, 0);
        }
    }
}
