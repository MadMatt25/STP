using System;
using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;

namespace STPLocalSearch.Solve
{
    public static class TMSTE
    {
        public static Graph RunSolver(Graph graph, Graph tmst)
        {
            HashSet<Edge> redundantEdges = new HashSet<Edge>(graph.Edges);
            foreach (var mstEdge in tmst.Edges)
            {
                var v1 = mstEdge.Either();
                var v2 = mstEdge.Other(v1);
                var path = Algorithms.DijkstraPath(v1, v2, graph);
                foreach (var edge in path.Edges.Where(edge => redundantEdges.Contains(edge)))
                    redundantEdges.Remove(edge);
            }

            var solutionEdge = graph.Clone();
            foreach (var edge in redundantEdges)
                solutionEdge.RemoveEdge(edge);
            Console.Write("Solution created. Taking MST...                                     \r");
            // Break cycles in the graph!
            // Solution: Take MST
            // Observation: mostly there are none
            solutionEdge = Algorithms.Kruskal(solutionEdge);
            var TMSTEremoveVertices = new HashSet<Vertex>();
            foreach (var vertex in solutionEdge.Vertices)
            {
                if (solutionEdge.GetDegree(vertex) == 1 && !graph.Terminals.Contains(vertex))
                    TMSTEremoveVertices.Add(vertex);
            }
            foreach (var vertex in TMSTEremoveVertices)
                solutionEdge.RemoveVertex(vertex);

            return solutionEdge;
        }
    }
}
