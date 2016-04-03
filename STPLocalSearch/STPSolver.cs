using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using STPLocalSearch.Graphs;
using Path = System.IO.Path;
using System.Collections.Generic;

namespace STPLocalSearch
{
    public class STPSolver
    {
        private string _path;
        private string _instanceName;
        private bool _isRunning;
        private readonly Queue<string> _solveQueue;

        private Solver _solver;
        private Reducer _reducer;

        public delegate void PrintOutputEventHandler(string text);

        public event PrintOutputEventHandler PrintOutput;

        public STPSolver()
        {
            _solveQueue = new Queue<string>();   
        }

        /// <summary>
        /// Starts the solver. Before calling this method, a path should have been set.
        /// </summary>
        public void Start()
        {
            if (string.IsNullOrEmpty(_path))
                throw new InvalidOperationException("Can not start the solver without setting the path.");

            if (_isRunning)
                throw new InvalidOperationException("Can not start the solver. It is already running.");

            if (File.Exists(_path))
                Solve(_path);
            else if (Directory.Exists(_path))
            {
                foreach (var file in Directory.GetFiles(_path))
                    _solveQueue.Enqueue(file);

                if (_solveQueue.Count > 0)
                    Solve(_solveQueue.Dequeue());
            }
            _isRunning = true;
        }

        /// <summary>
        /// Stop the solver.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _reducer.Abort();
            _solver.Abort();
            _solveQueue.Clear();

            _isRunning = false;
        }

        /// <summary>
        /// Method to set the path to the instance to solve.
        /// </summary>
        /// <param name="path">Path to the instance.</param>
        public void SetPath(string path)
        {
            if (_isRunning)
                throw new InvalidOperationException("Can not set the path while the solver is running.");

            _path = null;
            if (File.Exists(path) || Directory.Exists(path))
                _path = Path.GetFullPath(path);
        }

        public string InstanceName => _instanceName;

        public bool IsRunning => _isRunning;

        public Solver Solver => _solver;

        public Reducer Reducer => _reducer;

        private void Solve(string file)
        {
            var graph = Graph.Parse(file);
            _instanceName = file.Split('\\').Last();
            Print("  Instance: {0} - {1} vertices, {2} edges", _instanceName, graph.NumberOfVertices, graph.NumberOfEdges);

            _reducer = new Reducer(graph);
            _solver = new Solver(graph);

            _reducer.ReductionFound += _reducer_ReductionFound;
            _reducer.InitialReductionsDone += _reducer_InitialReductionsDone;
            _reducer.ReductionUpperBoundChanged += _reducer_ReductionUpperBoundChanged;
            _reducer.Done += _reducer_Done;
            _solver.SolutionUpperBoundChanged += _solver_SolutionUpperBoundChanged;
            _solver.Done += _solver_Done;

            _reducer.Start();
        }

        private void Done()
        {
            _reducer.ReductionFound -= _reducer_ReductionFound;
            _reducer.ReductionUpperBoundChanged -= _reducer_ReductionUpperBoundChanged;
            _reducer.Done -= _reducer_Done;
            _solver.SolutionUpperBoundChanged -= _solver_SolutionUpperBoundChanged;
            _solver.Done -= _solver_Done;

            Print("  Solution: {0}", _solver.CurrentSolution.TotalCost);

            if (_solveQueue.Count == 0)
                _isRunning = false;
            else
                Solve(_solveQueue.Dequeue());
        }

        private void Print(string format, params object[] args)
        {
            string text = string.Format(format, args);
            PrintOutput?.Invoke(text);
        }

        private void _solver_Done(object sender, EventArgs e)
        {
            if (!_reducer.IsRunning)
                Done();
        }

        private void _reducer_Done(object sender, EventArgs e)
        {
            if (!_solver.IsRunning)
                Done();
        }

        private void _solver_SolutionUpperBoundChanged(int solutionUpperBound)
        {
            Debug.WriteLine("Solution upper bound changed: {0}", solutionUpperBound);
            _reducer.SignalNewUpperBound(solutionUpperBound);
        }

        private void _reducer_ReductionUpperBoundChanged(int reductionUpperBound)
        {

        }

        private void _reducer_InitialReductionsDone(object sender, EventArgs e)
        {
            Debug.WriteLine("Initial reductions done.");
            _reducer.InitialReductionsDone -= _reducer_InitialReductionsDone;
            _solver.Start();
        }

        private void _reducer_ReductionFound(System.Collections.Generic.List<Edge> removeEdges, System.Collections.Generic.List<Vertex> removeVertices)
        {
            Debug.WriteLine("Reduction found.");
            _solver.SignalReductions(removeEdges, removeVertices);
        }
    }
}
