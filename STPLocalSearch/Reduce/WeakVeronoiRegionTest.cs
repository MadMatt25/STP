using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;
using STPLocalSearch.Data;

namespace STPLocalSearch.Reduce
{
    public static class WeakVeronoiRegionTest
    {
        public static ReductionResult RunTest(Graph graph, int upperBound)
        {
            var result = new ReductionResult();
            
            // T. Polzin, lemma 25: Vertex removal
            int reductionBound = 0;
            int allRadiusesExceptTwoMostExpensive = graph.Terminals.Select(graph.GetVoronoiRadiusForTerminal).OrderBy(x => x).Take(graph.Terminals.Count - 2).Sum();
            HashSet<Vertex> removeVertices = new HashSet<Vertex>();
            foreach (var vertex in graph.Vertices.Except(graph.Required))
            {
                var nearestTerminals = Algorithms.NearestTerminals(vertex, graph, 2);
                int lowerBound = nearestTerminals.Sum(x => x.TotalCost) + allRadiusesExceptTwoMostExpensive;
                
                if (lowerBound > upperBound)
                    removeVertices.Add(vertex);
                else if (lowerBound > reductionBound)
                    reductionBound = lowerBound;
            }

            foreach (var removeVertex in removeVertices)
            {
                graph.RemoveVertex(removeVertex);
                result.RemovedVertices.Add(removeVertex);
            }

            // Check for disconnected components (and remove those that not contain any terminals)
            var componentTable = graph.CreateComponentTable();
            var terminalComponents = new HashSet<int>();
            foreach (var vertex in graph.Terminals)
                terminalComponents.Add(componentTable[vertex]);

            foreach (var vertex in graph.Vertices.Where(x => !terminalComponents.Contains(componentTable[x])).ToList())
            {
                graph.RemoveVertex(vertex);
                result.RemovedVertices.Add(vertex);
            }

            // T. Polzin, lemma 26: edge removal
            allRadiusesExceptTwoMostExpensive = graph.Terminals.Select(graph.GetVoronoiRadiusForTerminal).OrderBy(x => x).Take(graph.Terminals.Count - 2).Sum();
            HashSet<Edge> removeEdges = new HashSet<Edge>();
            Dictionary<Vertex, int> distancesToBase = new Dictionary<Vertex, int>();
            foreach (var terminal in graph.Terminals)
            {
                var toAll = Algorithms.DijkstraToAll(terminal, graph);
                foreach (var vertex in graph.Vertices)
                {
                    if (vertex == terminal)
                        continue;

                    if (!distancesToBase.ContainsKey(vertex))
                        distancesToBase.Add(vertex, toAll[vertex]);
                    else if (toAll[vertex] < distancesToBase[vertex])
                        distancesToBase[vertex] = toAll[vertex];
                }
            }

            foreach (var edge in graph.Edges)
            {
                var v1 = edge.Either();
                var v2 = edge.Other(v1);

                if (graph.Terminals.Contains(v1) || graph.Terminals.Contains(v2))
                    continue;

                var v1z1 = distancesToBase[v1];
                var v2z2 = distancesToBase[v2];
                var lowerBound = edge.Cost + v1z1 + v2z2 + allRadiusesExceptTwoMostExpensive;

                if (lowerBound > upperBound)
                    removeEdges.Add(edge);
                else if (lowerBound > reductionBound)
                    reductionBound = lowerBound;
            }

            foreach (var removeEdge in removeEdges)
            {
                graph.RemoveEdge(removeEdge);
                result.RemovedEdges.Add(removeEdge);
            }

            result.ReductionUpperBound = reductionBound;
            return result;
        }
    }
}
