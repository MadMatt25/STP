using System;
using System.Diagnostics;
using System.IO;
using STPLocalSearch.Graphs;
using STPLocalSearch.Reduce;
using STPLocalSearch.Solve;
using STPLocalSearch.Data;

namespace STPLocalSearch
{
    // This experiment tries to solve the STP problem using local search.
    // Before using an algorithm, the problem is reduced using some 
    // simple rules.
    // The instances are downloaded from the SteinLib benchmarks at:
    // http://steinlib.zib.de/
    static class Program
    {
        private static void Main(string[] args)
        {
            bool success = false;
            string filePath = "";
            while (!success)
            {
                Console.Clear();
                Console.WriteLine("Enter the path to a .stp file or a directory containing .stp files:");
                filePath = Console.ReadLine().Trim('\"', ' ');
                Console.WriteLine();
                filePath = System.IO.Path.GetFullPath(filePath);
                success = (File.Exists(filePath) || Directory.Exists(filePath));
            }

            if ((File.GetAttributes(filePath) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                // Directory
                foreach (var file in Directory.GetFiles(filePath))
                {
                    Solve(file);
                }
            }
            else
            {
                Solve(filePath);
            }
            
            Console.WriteLine("Press enter to quit.");
            Console.ReadLine();
        }

        private static void Solve(string file)
        {
            // 1. Parse the file
            Graph graph = Graph.Parse(file);
            Graph currentBestSolution = graph.Clone();
            int previousUpperBound = int.MaxValue;
            int currentUpperBound = int.MaxValue;

            do
            {
                int minimumReductionBound = 0;

                // 2. Reduce the problem  
                int v = graph.NumberOfVertices;
                int e = graph.NumberOfEdges;
                graph = Reduce(graph, currentUpperBound, out minimumReductionBound);
                if (graph.NumberOfVertices == v && graph.NumberOfEdges == e && currentUpperBound < int.MaxValue)
                    break;

                //var oldColor = Console.ForegroundColor;
                //Console.ForegroundColor = ConsoleColor.Yellow;
                //var lp = LPSolver.RunSolver(graph);
                //Console.WriteLine("LP SOLVER: {0}, {1} components", lp.TotalCost, lp.ComponentCheck());
                //Console.ForegroundColor = oldColor;

                // 3. Find upper bound for STP
                currentBestSolution = ApproximateSolution(currentBestSolution, minimumReductionBound);
                previousUpperBound = currentUpperBound;
                currentUpperBound = currentBestSolution.TotalCost;
            } while (previousUpperBound - currentUpperBound > 0);

            //var exact = LPSolver.RunSolver(graph);
            //Console.WriteLine("LP SOLVER: {0}", exact.TotalCost);

            // Done
            Console.WriteLine();
        }

        private static Graph Reduce(Graph graph, int upperBound, out int minimumReductionBound)
        {
            var oldColor = Console.ForegroundColor;
            int v = graph.NumberOfVertices;
            int e = graph.NumberOfEdges;
            minimumReductionBound = 0;

            // 2. Reduce the problem
            Stopwatch s = new Stopwatch();
            s.Start();

            graph = DegreeTest.RunTest(graph).Graph;
            Console.WriteLine("  Degree test: {0} vertices, {1} edges, {2} required Steiner nodes", graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);
            
            graph = TriangleTest.RunTest(graph).Graph;
            Console.WriteLine("  Triangle test: {0} vertices, {1} edges, {2} required Steiner nodes", graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);
            
            if (upperBound < int.MaxValue)
            {
                ReductionResult reduction = null;
                reduction = WeakVeronoiRegionTest.RunTest(graph, upperBound);
                graph = reduction.Graph;
                Console.WriteLine("  Weak VR test: {0} vertices, {1} edges, {2} required Steiner nodes",
                    graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);
                if (reduction.MinimumReductionBound > minimumReductionBound)
                    minimumReductionBound = reduction.MinimumReductionBound;

                reduction = VeronoiRegionTest.RunTest(graph, upperBound);
                graph = reduction.Graph;
                Console.WriteLine("  VR test: {0} vertices, {1} edges, {2} required Steiner nodes",
                    graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);
                if (reduction.MinimumReductionBound > minimumReductionBound)
                    minimumReductionBound = reduction.MinimumReductionBound;

                reduction = ReachabilityTest.RunTest(graph, upperBound);
                graph = reduction.Graph;
                Console.WriteLine("  Reachability test: {0} vertices, {1} edges, {2} required Steiner nodes",
                    graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);
                if (reduction.MinimumReductionBound > minimumReductionBound)
                    minimumReductionBound = reduction.MinimumReductionBound;
            }

            graph = DegreeTest.RunTest(graph).Graph;
            Console.WriteLine("  Degree test: {0} vertices, {1} edges, {2} required Steiner nodes", graph.NumberOfVertices,
                graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);

            graph = SpecialDistanceApproxTest.RunTest(graph).Graph;
            Console.WriteLine("  SDA test: {0} vertices, {1} edges, {2} required Steiner nodes", graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);

            graph = DegreeTest.RunTest(graph).Graph;
            Console.WriteLine("  Degree test: {0} vertices, {1} edges, {2} required Steiner nodes", graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);

            graph = TriangleTest.RunTest(graph).Graph;
            Console.WriteLine("  Triangle test: {0} vertices, {1} edges, {2} required Steiner nodes", graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);

            graph = DegreeTest.RunTest(graph).Graph;
            Console.WriteLine("  Degree test: {0} vertices, {1} edges, {2} required Steiner nodes", graph.NumberOfVertices, graph.NumberOfEdges, graph.RequiredSteinerNodes.Count);

            s.Stop();
            var reduceTimeMs = s.ElapsedMilliseconds;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("> Reduction: {0} vertices (-{2:0.00}%), {1} edges (-{3:0.00}%) -- {4}ms -- Reduction bound: {5}", (v - graph.NumberOfVertices), (e - graph.NumberOfEdges), (v - graph.NumberOfVertices) * 100.0 / v, (e - graph.NumberOfEdges) * 100.0 / e, reduceTimeMs, minimumReductionBound);
            Console.ForegroundColor = oldColor;

            return graph;
        }

        private static Graph ApproximateSolution(Graph graph, int minimumReductionBound)
        {
            var oldColor = Console.ForegroundColor;
            Stopwatch s = new Stopwatch();
            Graph currentBest = null;

            s.Start();
            var tdist = graph.TerminalDistanceGraph;
            Console.Write("Terminal Distance Graph done. \r");
            var tmst = Algorithms.Kruskal(tdist);
            Console.Write("Terminal Minimal Spanning Tree done. \r");

            // TMSTE
            var solutionEdge = TMSTE.RunSolver(graph, tmst);
            currentBest = solutionEdge;
            // TMSTV
            var solutionVertex = TMSTV.RunSolver(graph, tmst);
            if (solutionVertex.TotalCost < currentBest.TotalCost)
                currentBest = solutionVertex;
            s.Stop();
            var tmstevTimeMs = s.ElapsedMilliseconds;
            s.Reset();

            // BLS
            s.Start();
            var solutionBLS = BLS.RunSolver(graph, currentBest, minimumReductionBound);
            s.Stop();
            var blsTimeMs = s.ElapsedMilliseconds;
            if (solutionBLS.TotalCost < currentBest.TotalCost)
                currentBest = solutionBLS;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Solution: TMSTE {0} -- TMSTV {1} -- {2}ms           ", solutionEdge.TotalCost, solutionVertex.TotalCost, tmstevTimeMs);
            Console.WriteLine("          Breakout Local Search: {0} -- {1}ms        ", solutionBLS.TotalCost, blsTimeMs);
            Console.ForegroundColor = oldColor;

            return currentBest;
        }
    }
}
