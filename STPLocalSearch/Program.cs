using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace STPLocalSearch
{
    // This experiment tries to solve the STP problem using local search.
    // Before using an algorithm, the problem is reduced using some 
    // simple rules.
    // The instances are downloaded from the SteinLib benchmarks at:
    // http://steinlib.zib.de/
    static class Program
    {
        private static readonly STPSolver solver = new STPSolver();
        private static bool _quit = false;
        private static bool _processingCommand = false;
        private static readonly ConcurrentQueue<string> _outputQueue = new ConcurrentQueue<string>();
        private static string _currentInput;

        private static void Main(string[] args)
        {
            Console.WriteLine("STP Solver using problem reduction and Breakout Local Search");
            Console.WriteLine();
            solver.PrintOutput += Solver_PrintOutput;

            while (!_quit)
            {
                
                WriteOutputQueue();
                Console.Write("> ");

                string line = Console.ReadLine().Trim(' ');
                _processingCommand = true;
                ProcessCommand(line);
                _processingCommand = false;
            }
        }

        private static void ProcessCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            Regex regex = new Regex("([^\"\\s]+)|\"(.+)\"+", RegexOptions.Singleline);
            List <string> splitted = new List<string>();
            var match = regex.Matches(command);
            foreach (Match m in match)
                splitted.Add(m.Value.Trim('"'));

            var cmd = splitted[0];
            var args = splitted.Skip(1).ToArray();

            if (cmd.ToLower() == "solve")
                StartSolving(args);
            else if (cmd.ToLower() == "help")
                PrintHelp();
            else if (cmd.ToLower() == "status")
                PrintStatus();
            else if (cmd.ToLower() == "stop" || cmd.ToLower() == "abort")
                StopSolving();
            else if (cmd.ToLower() == "exit")
                Quit();
            else if (cmd.ToLower() == "clear" || cmd.ToLower() == "clr")
                Console.Clear();
            else
            {
                Console.Write("\r   \r");
                Console.WriteLine("  \"{0}\" is not recognized as a valid command. Type help for an overview.", command);
                Console.Write("\r> ");
            }
        }

        private static void Quit()
        {
            StopSolving();
            _quit = true;
        }

        private static void StartSolving(string[] args)
        {
            try
            {
                if (args.Length == 0)
                    throw new ArgumentException("No file path is provided.");

                solver.SetPath(args[0]);
                solver.Start();
            }
            catch (Exception e)
            {
                var c = Console.ForegroundColor;
                Console.Write("\r   \r");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: {0}", e.Message);
                Console.ForegroundColor = c;
                Console.Write("\r> ");
            }
        }

        private static void StopSolving()
        {
            solver.Stop();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("  Overview of commands:");
            var c = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("<path to file>");
            Console.ForegroundColor = c;
            Console.Write("\tSolve the STP in file.");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("status");
            Console.ForegroundColor = c;
            Console.Write("\tShow status of STP solver.");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("stop");
            Console.ForegroundColor = c;
            Console.Write("\tStop the STP solver.");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("help");
            Console.ForegroundColor = c;
            Console.Write("\tPrint help.");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("exit");
            Console.ForegroundColor = c;
            Console.Write("\tQuit the application.");
            Console.WriteLine();
        }

        private static void PrintStatus()
        {
            bool moveUp = false;
            while (true)
            {
                if (moveUp)
                    Console.CursorTop -= 8;

                WriteOutputQueue();

                var c = Console.ForegroundColor;

                ClearCurrentConsoleLine();
                Console.Write("  Instance: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(solver.InstanceName);
                Console.ForegroundColor = c;
                Console.WriteLine();

                ClearCurrentConsoleLine();
                Console.Write("  Number of edges: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(solver.Solver.InstanceNumberOfEdges);
                Console.ForegroundColor = c;
                Console.WriteLine();

                Console.Write("  Number of vertices: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(solver.Solver.InstanceNumberOfVertices);
                Console.ForegroundColor = c;
                Console.WriteLine();

                ClearCurrentConsoleLine();
                Console.Write("  Current solution: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(solver.Solver.CurrentSolutionCost);
                Console.ForegroundColor = c;
                Console.WriteLine();

                ClearCurrentConsoleLine();
                Console.Write("  Solver status: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(solver.Solver.Status);
                Console.ForegroundColor = c;
                Console.WriteLine();

                ClearCurrentConsoleLine();
                Console.Write("  Reducer status: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(solver.Reducer.Status);
                Console.ForegroundColor = c;
                Console.WriteLine();

                ClearCurrentConsoleLine();
                Console.Write("  Reduction upper bound: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(solver.Reducer.ReductionUpperBound);
                Console.ForegroundColor = c;
                Console.WriteLine();

                ClearCurrentConsoleLine();
                Console.WriteLine("  Press ESC to quit status updates.");

                moveUp = true;

                DateTime beginWait = DateTime.Now;
                while (!Console.KeyAvailable && DateTime.Now.Subtract(beginWait).TotalSeconds < 1)
                    Thread.Sleep(500);

                if (!solver.IsRunning || (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                {
                    for (int i = 0; i < 9; i++)
                    {
                        ClearCurrentConsoleLine();
                        Console.CursorTop--;
                    }
                    ClearCurrentConsoleLine();

                    break;
                }
            }
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private static void WriteOutputQueue()
        {
            if (_outputQueue.Count == 0)
                return;

            string outline = "";
            while (_outputQueue.Count > 0)
                if (_outputQueue.TryDequeue(out outline))
                {
                    ClearCurrentConsoleLine();
                    Console.WriteLine(outline);
                }
        }

        private static void Solver_PrintOutput(string text)
        {
            _outputQueue.Enqueue(text);
            
            if (!_processingCommand)
                WriteOutputQueue();
        }
    }
}
