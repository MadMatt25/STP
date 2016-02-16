using System;
using System.Collections.Generic;
using System.Linq;
using Gurobi;
using STPLocalSearch.Graphs;
using System.Diagnostics;

namespace STPLocalSearch.Solve
{
    public static class LPSolver
    {
        public static Graph RunSolver(Graph graph)
        {
            GRBEnv env = new GRBEnv();
            env.Set(GRB.IntParam.OutputFlag, 0);
            env.Set(GRB.IntParam.LogToConsole, 0);
            env.Set(GRB.IntParam.Presolve, 2);
            env.Set(GRB.DoubleParam.Heuristics, 0.0);
            GRBModel model = new GRBModel(env);
            GRBVar[] variables = new GRBVar[graph.NumberOfEdges];
            model.SetCallback(new LPSolverCallback());
            Dictionary<Edge, GRBVar> edgeVars = new Dictionary<Edge, GRBVar>();

            // Add variables to the LP model
            for (int i = 0; i < graph.NumberOfEdges; i++)
            {
                variables[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "x_" + i);
                edgeVars.Add(graph.Edges[i], variables[i]);
            }
            model.Update();

            // Add constraints to the LP model
            Console.Write("\rRunning LP. Creating constraints...\r");
            //var nonTerminals = graph.Vertices.Except(graph.Terminals).ToList();
            ulong conNr = 0;
            //var terminalCombinations = new List<List<Vertex>>();

            // Assume, without loss of generality, that Terminals[0] is the root, and thus is always included
            int rootNr = 1;
            foreach (var rootTerminal in graph.Terminals)
            //var rootTerminal = graph.Terminals[0];
            {
                Console.Write("\rRunning LP. Creating constraints... {0}/{1}\r", rootNr, graph.Terminals.Count);
                foreach (var combination in GetBFS(graph, rootTerminal))
                {
                    var nodes = combination.ToList(); //new HashSet<Vertex>(combination);
                    if (nodes.Count == graph.NumberOfVertices || graph.Terminals.All(nodes.Contains))
                        continue;
                    //Debug.WriteLine("Combination: {0}", string.Join(" ", nodes));
                    //for (int i = 1; i <= nodes.Count; i++)
                    {
                        var edges = nodes//.Take(i)
                                         .SelectMany(graph.GetEdgesForVertex)
                                         .Distinct()
                                         .Where(x => x.WhereOne(y => !nodes.Contains(y)));
                        GRBLinExpr expression = 0;
                        foreach (var edge in edges)
                            expression.AddTerm(1, edgeVars[edge]);
                        model.AddConstr(expression >= 1.0, "subset_" + conNr);
                        conNr++;

                        if (conNr % 100000 == 0)
                        {
                            //model = model.Presolve(); //Pre-solve the model every 1000 constraints.
                            int constrBefore = model.GetConstrs().Length, varsBefore = model.GetVars().Length;
                            Debug.WriteLine("Presolve called.");
                            var presolved = model.Presolve();
                            Debug.WriteLine("Model has {0} constraints, {1} variables. Presolve has {2} constraints, {3} variables",
                                constrBefore, varsBefore, presolved.GetConstrs().Length, presolved.GetVars().Length);
                        }
                    }
                }

                //Debug.WriteLine("   ");
                //Debug.WriteLine("   ");
                rootNr++;
            }

            //terminalCombinations.Add(new List<Vertex>(new[] { graph.Terminals[0] }));
            //for (int j = 1; j < graph.Terminals.Count - 1; j++)
            //    terminalCombinations.AddRange(new Combinations<Vertex>(graph.Terminals.Skip(1), j).Select(combination => combination.Union(new[] { graph.Terminals[0] }).ToList()));

            //long nonTerminalSetsDone = 0;
            //long nonTerminalSets = 0;
            //for (int i = 0; i <= nonTerminals.Count; i++)
            //    nonTerminalSets += Combinations<Vertex>.NumberOfCombinations(nonTerminals.Count, i);

            //for (int i = 0; i <= nonTerminals.Count; i++)
            //{
            //    foreach (var nonTerminalSet in new Combinations<Vertex>(nonTerminals, i))
            //    {
            //        foreach (var nodes in (from a in terminalCombinations
            //                               select new HashSet<Vertex>(a.Union(nonTerminalSet))))
            //        {
            //            var edges = nodes.SelectMany(graph.GetEdgesForVertex)
            //                             .Distinct()
            //                             .Where(x => x.WhereOne(y => !nodes.Contains(y)));
            //            GRBLinExpr expression = 0;
            //            foreach (var edge in edges)
            //                expression.AddTerm(1, edgeVars[edge]);
            //            model.AddConstr(expression >= 1.0, "subset_" + conNr);
            //            conNr++;
            //        }
            //        nonTerminalSetsDone++;
            //        if (nonTerminalSetsDone % 100 == 0)
            //            Console.Write("\rRunning LP. Creating constraints... {0}/{1} ({2:0.000}%)\r", nonTerminalSetsDone, nonTerminalSets, nonTerminalSetsDone * 100.0 / nonTerminalSets);
            //    }
            //}

            // Solve the LP model
            Console.Write("\rRunning LP. Creating objective & updating...                                   \r");
            GRBLinExpr objective = new GRBLinExpr();
            for (int i = 0; i < graph.NumberOfEdges; i++)
                objective.AddTerm(graph.Edges[i].Cost, variables[i]);
            model.SetObjective(objective, GRB.MINIMIZE);
            Console.Write("\rRunning LP. Tuning...                                   \r");
            model.Tune();
            Debug.WriteLine("Presolve called.");
            model.Presolve();
            Console.Write("\rRunning LP. Solving...                               \r");
            Debug.WriteLine("Optimize called.");
            model.Optimize();

            Graph solution = graph.Clone();
            HashSet<Edge> includedEdges = new HashSet<Edge>();
            for (int i = 0; i < solution.NumberOfEdges; i++)
            {
                var value = variables[i].Get(GRB.DoubleAttr.X);
                if (value == 1)
                    includedEdges.Add(solution.Edges[i]);
            }

            foreach (var edge in solution.Edges.ToList())
                if (!includedEdges.Contains(edge))
                    solution.RemoveEdge(edge);

            Console.Write("\r                                                  \r");

            return solution;
        }
        
        public static IEnumerable<IEnumerable<Vertex>> GetAllConnectedCombinations(Graph graph, Vertex start)
        {
            var visited = new HashSet<Vertex>();
            var terminals = new HashSet<Vertex>(graph.Terminals);
            var stack = new Stack<Vertex>();
            Dictionary<Vertex, Vertex> childParentDictionary = new Dictionary<Vertex, Vertex>();

            stack.Push(start);
            childParentDictionary.Add(start, null);

            while (stack.Count != 0)
            {
                var current = stack.Pop();
                if (!visited.Add(current))
                    continue;

                var neighbours = graph.GetNeighboursForVertex(current)
                                      .Where(n => !visited.Contains(n))
                                      .ToList();

                // Travel left to right
                neighbours.Reverse();
                foreach (var neighbour in neighbours)
                {
                    stack.Push(neighbour);
                    childParentDictionary.Add(neighbour, current);
                }

                if (!neighbours.Any() || neighbours.All(visited.Contains))
                {
                    List<Vertex> vertices = new List<Vertex>();
                    Vertex parentNode = current;
                    do
                    {
                        vertices.Add(parentNode);
                    } while ((parentNode = childParentDictionary[parentNode]) != null);

                    yield return vertices;
                }
            }
        }

        public static IEnumerable<IEnumerable<Vertex>> GetBFS(Graph graph, Vertex root)
        {
            yield return new[] { root };

            foreach (var end in graph.Vertices.Except(new [] { root }))
            {
                HashSet<Vertex> visited = new HashSet<Vertex>();
                visited.Add(root);

                foreach (var bfs in BFS(graph, visited, end, root))
                    yield return bfs;
            }
        }

        public static IEnumerable<IEnumerable<Vertex>> BFS2(Graph graph, List<Vertex> visited)
        {
            List<Vertex> nodes = graph.GetNeighboursForVertex(visited.Last());
            var visitedHash = new HashSet<Vertex>(visited);

            foreach (var node in nodes)
            {
                if (visitedHash.Contains(node))
                    continue;
                //if (node == end)
                {
                    visited.Add(node);
                    yield return visited;
                    visited.RemoveAt(visited.Count - 1);
                    //break;
                }
            }

            foreach (var node in nodes)
            {
                if (visitedHash.Contains(node))
                    continue;
                visited.Add(node);
                foreach (var bfs in BFS2(graph, visited))
                    yield return bfs;
                visited.RemoveAt(visited.Count - 1);
            }
        }

        public static IEnumerable<IEnumerable<Vertex>> BFS(Graph graph, HashSet<Vertex> visited, Vertex end, Vertex expand)
        {
            if (graph.Terminals.All(visited.Contains) || visited.Count == graph.NumberOfVertices - 1)
                yield break;
            
            List<Vertex> nodes = graph.GetNeighboursForVertex(expand);

            //for (int i = 0; i < nodes.Count; i++ )
            //{
            //    foreach (var combination in new Combinations<Vertex>(nodes, i + 1))
            //    {
            //        if (combination.Any(x => visited.Contains(x)))
            //            continue;
            //        if (combination.Contains(end))
            //        {
            //            foreach (var n in combination)
            //                visited.Add(n);
            //            foreach (var n in combination)
            //                foreach (var bfs in BFS(graph, visited, end, n))
            //                    yield return bfs;
            //            foreach (var n in combination)
            //                visited.Remove(n);
            //            break;
            //        }
            //    }
            //}

            foreach (var node in nodes)
            {
                if (visited.Contains(node))
                    continue;
                if (node == end)
                {
                    visited.Add(node);
                    yield return visited;
                    visited.Remove(node);
                    break;
                }
            }

            //for (int i = 0; i < nodes.Count; i++)
            //{
            //    if (visited.Count + i == graph.NumberOfVertices - 1)
            //        break; // Adding i+1 results in all nodes, which is useless.

            //    foreach (var combination in new Combinations<Vertex>(nodes, i + 1))
            //    {
            //        if (combination.Any(visited.Contains)) // || combination.Contains(end))
            //            continue;
            //        foreach (var n in combination)
            //            visited.Add(n);
            //        foreach (var n in combination)
            //            foreach (var bfs in BFS(graph, visited, end, n))
            //                yield return bfs;
            //        foreach (var n in combination)
            //            visited.Remove(n);
            //    }
            //}

            foreach (var node in nodes)
            {
                if (visited.Contains(node) || node == end)
                    continue;
                visited.Add(node);
                foreach (var bfs in BFS(graph, visited, end, node))
                    yield return bfs;
                visited.Remove(node);
            }
        }

        private class LPSolverCallback : GRBCallback
        {
            protected override void Callback()
            {
                if (where == GRB.Callback.PRESOLVE)
                {
                    // Presolve callback
                    int cdels = GetIntInfo(GRB.Callback.PRE_COLDEL);
                    int rdels = GetIntInfo(GRB.Callback.PRE_ROWDEL);
                    if (cdels != 0 || rdels != 0)
                    {
                        Debug.WriteLine("PRESOLVE: " + cdels + " columns and " + rdels + " rows are removed ");
                    }
                }
                else if (where == GRB.Callback.SIMPLEX)
                    Debug.WriteLine("SIMPLEX");
                else if (where == GRB.Callback.MIP)
                    Debug.WriteLine("MIP");
                else if (where == GRB.Callback.MESSAGE)
                    Debug.WriteLine(GetStringInfo(GRB.Callback.MSG_STRING));
            }
        }
    }
}
