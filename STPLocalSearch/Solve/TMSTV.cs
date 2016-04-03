using System;
using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;

namespace STPLocalSearch.Solve
{
    public static class TMSTV
    {
        public static Graph RunSolver(Graph graph, Graph tmst)
        {
            HashSet<Vertex> redundantVertices = new HashSet<Vertex>(graph.Vertices);
            foreach (var mstEdge in tmst.Edges)
            {
                var v1 = mstEdge.Either();
                var v2 = mstEdge.Other(v1);
                var path = Algorithms.DijkstraPath(v1, v2, graph);
                foreach (var vertex in path.Vertices.Where(vertex => redundantVertices.Contains(vertex)))
                    redundantVertices.Remove(vertex);
            }
            
            var solutionVertex = graph.Clone();
            foreach (var vertex in redundantVertices)
            {
                solutionVertex.RemoveVertex(vertex);
            }
            solutionVertex = Algorithms.Kruskal(solutionVertex);

            var TMSTVremoveVertices = new HashSet<Vertex>();
            foreach (var vertex in solutionVertex.Vertices)
            {
                if (solutionVertex.GetDegree(vertex) == 1 && !graph.Terminals.Contains(vertex))
                    TMSTVremoveVertices.Add(vertex);
            }
            foreach (var vertex in TMSTVremoveVertices)
                solutionVertex.RemoveVertex(vertex);

            return solutionVertex;
        }
    }
}
