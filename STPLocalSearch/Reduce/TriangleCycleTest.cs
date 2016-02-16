using System;
using System.Collections.Generic;
using System.Linq;
using STPLocalSearch.Graphs;

namespace STPLocalSearch.Reduce
{
    public static class TriangleCycleTest
    {
        public static Graph RunTest(Graph graph)
        {
            var enumerator = AllTriangles(graph);
            
            Console.WriteLine("There are {0} triangles in this graph.", enumerator.Count());
            return graph;
        }

        private static IEnumerable<List<Edge>> AllTriangles(Graph graph)
        {
            List<Vertex> vertices = new List<Vertex>(graph.Vertices);
            vertices.Sort((x, y) => graph.GetDegree(x).CompareTo(graph.GetDegree(y)));
            vertices.Reverse(); // From highest to lowest
            Dictionary<Vertex, int> index = new Dictionary<Vertex, int>();
            Dictionary<Vertex, List<Vertex>> A = new Dictionary<Vertex, List<Vertex>>();

            for (int i = 0; i < vertices.Count; i++)
            {
                index.Add(vertices[i], i);
                A.Add(graph.Vertices[i], new List<Vertex>());
            }

            foreach (var firstVertex in vertices)
            {
                foreach (var firstEdge in graph.GetEdgesForVertex(firstVertex))
                {
                    var secondVertex = firstEdge.Other(firstVertex);
                    if (index[firstVertex] < index[secondVertex])
                    {
                        foreach (var thirdVertex in A[firstVertex].Intersect(A[secondVertex]))
                        {
                            var secondEdge =
                                graph.GetEdgesForVertex(firstVertex).Single(x => x.Other(firstVertex) == thirdVertex);
                            var thirdEdge =
                                graph.GetEdgesForVertex(secondVertex).Single(x => x.Other(secondVertex) == thirdVertex);
                            yield return new List<Edge>(new [] { firstEdge, secondEdge, thirdEdge });
                        }

                        A[secondVertex].Add(firstVertex);
                    }
                }
            }
        }
    }
}
