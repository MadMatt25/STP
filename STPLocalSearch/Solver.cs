using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using STPLocalSearch.Graphs;
using STPLocalSearch.Solve;

namespace STPLocalSearch
{
    public class Solver
    {
        private readonly Graph _instance;
        private Graph _instanceDistanceGraph;
        private bool _instanceDisntaceGraphOutdated;
        private Thread _solutionThread;
        private bool _running;

        private BLS _bls;

        private readonly ConcurrentQueue<Edge> _edgesToRemove;
        private readonly ConcurrentQueue<Vertex> _verticesToRemove;  

        public delegate void SolutionUpperBoundChangedEventHandler(int solutionUpperBound);
        public event SolutionUpperBoundChangedEventHandler SolutionUpperBoundChanged;

        public event EventHandler Done;
        
        public Solver(Graph instance)
        {
            _instance = instance.Clone();
            _running = false;
            _bls = new BLS();

            _edgesToRemove = new ConcurrentQueue<Edge>();
            _verticesToRemove = new ConcurrentQueue<Vertex>();
        }

        /// <summary>
        /// Method to start the solver.
        /// </summary>
        public void Start()
        {
            if (_running)
                throw new InvalidOperationException("Solver cannot be started. It is still running.");

            _instanceDisntaceGraphOutdated = true;
            _solutionThread = new Thread(SolutionThread);
            _solutionThread.Start();
            Status = "Not running.";
        }

        /// <summary>
        /// Method to abort the solver. Reached unsaved progress will 
        /// be lost after calling this method.
        /// </summary>
        public void Abort()
        {
            if (_solutionThread != null && _solutionThread.IsAlive)
                _solutionThread.Abort();
        }

        /// <summary>
        /// Method to signal reductions. These will be executed during the
        /// process of searching for solutions.
        /// </summary>
        /// <param name="edges">The edges which can be removed from the graph.</param>
        /// <param name="vertices">The vertices which can be removed from the graph.</param>
        public void SignalReductions(List<Edge> edges, List<Vertex> vertices)
        {
            if (edges.Count == 0 && vertices.Count == 0)
                return;

            foreach (var edge in edges)
                _edgesToRemove.Enqueue(edge);

            foreach (var vertex in vertices)
                _verticesToRemove.Enqueue(vertex);
        }

        /// <summary>
        /// Property indicates whether the solver is actively working.
        /// </summary>
        public bool IsRunning { get { return _running; } }

        private object _currentSolutionLocker = new object();
        public Graph CurrentSolution { get; private set; }

        public int CurrentSolutionCost { get; private set; }

        public string Status { get; private set; }

        public int InstanceNumberOfEdges => _instance.NumberOfEdges;

        public int InstanceNumberOfVertices => _instance.NumberOfVertices;

        private void SolutionThread()
        {
            _running = true;

            if (!_edgesToRemove.IsEmpty || !_verticesToRemove.IsEmpty)
                PerformReductions();

            Status = "Preparing for TMSTE / TMSTV.";
            SetCurrentSolution(_instance);
            var tdist = _instance.TerminalDistanceGraph;
            var tmst = Algorithms.Kruskal(tdist);

            // TMSTE
            Status = "Running TMSTE.";
            var solutionEdge = TMSTE.RunSolver(_instance, tmst);
            SetCurrentSolution(solutionEdge);

            // TMSTV
            Status = "Running TMSTV.";
            var solutionVertex = TMSTV.RunSolver(_instance, tmst);
            SetCurrentSolution(solutionVertex);

            // Breakout Local Search
            Status = "Running BLS.";
            Bls();

            _running = false;
            Status = "Not running.";
            Done?.Invoke(this, EventArgs.Empty);
        }

        private void Bls()
        {
            _bls.CurrentSolution = CurrentSolution.Clone();
            _bls.ProblemInstance = _instance;

            bool flipped = false;

            foreach (var terminal in _bls.CurrentSolution.Terminals)
                terminal.InitializeScore(100);
            // Non-terminals get initial score of 20. (not preferred, not ignored)
            foreach (var vertex in _bls.CurrentSolution.Vertices.Except(_bls.CurrentSolution.Terminals))
                vertex.InitializeScore(100);

            int currentBreakout = BLS.MIN_BREAKOUT;
            var currentNeighbourhood = BLS.Neighbourhood.SteinerNodeInsertion; // currentBreakout <= 4 ? Neighbourhood.SteinerNodeRemoval : Neighbourhood.SteinerNodeInsertion;

            while (true)
            {
                // Console.Write("\rRunning BLS... Current Breakout {0} - Best: {1} \r", currentBreakout, currentBest.TotalCost);
                Status = "Running BLS. Breakout parameter: " + currentBreakout + " " + currentNeighbourhood + " Flipped: " + flipped;
                // Get neighbour solution
                Stopwatch neighbourTimer = new Stopwatch();
                neighbourTimer.Start();
                
                if (_instance.Vertices.Any(x => x.Score < 0))
                {
                    foreach (var remove in _instance.Vertices.Where(x => x.Score < 0))
                        _bls.CurrentSolution = _bls.RemoveVertexAndReconnect(_bls.CurrentSolution, _instance, remove);
                    SetCurrentSolution(_bls.CurrentSolution, true);
                    currentBreakout = BLS.MIN_BREAKOUT;
                }

                //if (currentNeighbourhood == BLS.Neighbourhood.SteinerNodeRemoval && _instanceDisntaceGraphOutdated)
                //{
                //    Status = "Creating distance graph...";
                //    _instanceDistanceGraph = _instance.CreateDistanceGraph();
                //    _instanceDisntaceGraphOutdated = false;
                //    Status = "Running BLS. Breakout parameter: " + currentBreakout + " " + currentNeighbourhood;
                //}

                _bls.GetNeighbourSolution(currentBreakout, currentNeighbourhood);
                neighbourTimer.Stop();
                
                // Perform reductions
                if (_edgesToRemove.Count > 0 || _verticesToRemove.Count > 0)
                {
                    PerformReductions();
                    currentBreakout = BLS.MIN_BREAKOUT;
                    currentNeighbourhood = BLS.Neighbourhood.SteinerNodeInsertion;
                }

                if (_bls.CurrentSolution.TotalCost < CurrentSolution.TotalCost)
                {
                    Debug.WriteLine(" Improvement: {0}ms, with neigbourhood: {1} & BLS: {2}", neighbourTimer.ElapsedMilliseconds, currentNeighbourhood, currentBreakout);
                    SetCurrentSolution(_bls.CurrentSolution);

                    currentNeighbourhood = BLS.Neighbourhood.SteinerNodeInsertion;
                    currentBreakout = BLS.MIN_BREAKOUT;
                    if (flipped) // Flip back
                        _instance.FlipScores();
                    flipped = false;
                }
                else if (!flipped && currentBreakout == BLS.MAX_BREAKOUT && currentNeighbourhood == BLS.Neighbourhood.SteinerNodeInsertion) // Try flipping scores
                {
                    flipped = true;
                    _instance.FlipScores();
                    currentBreakout = BLS.MIN_BREAKOUT;
                }
                // Do break-out!
                else if (currentBreakout == BLS.MAX_BREAKOUT && currentNeighbourhood == BLS.Neighbourhood.SteinerNodeInsertion) // Switch neighbourhood
                {
                    currentNeighbourhood = BLS.Neighbourhood.SteinerNodeRemoval;
                    currentBreakout = BLS.MIN_BREAKOUT;
                    flipped = false;
                    _instance.FlipScores();
                }
                else if (currentBreakout == 64 && currentNeighbourhood == BLS.Neighbourhood.SteinerNodeRemoval)
                    break;
                else
                    currentBreakout = Math.Min(BLS.MAX_BREAKOUT, currentBreakout * 2);

                _bls.CurrentSolution = CurrentSolution.Clone();
            }
        }

        private void PerformReductions()
        {
            Debug.WriteLine("Executing reductions...");
            Edge edge;
            int e = _edgesToRemove.Count;
            for (int i = 0; i < e; i++)
            {
                if (_edgesToRemove.TryDequeue(out edge))
                {
                    if (CurrentSolution != null && CurrentSolution.ContainsEdge(edge))
                    {
                        _instance.RemoveEdge(edge);
                        CurrentSolution.RemoveEdge(edge);
                        var v1 = edge.Either();
                        var v2 = edge.Other(v1);
                        var path = Algorithms.DijkstraPath(v1, v2, _instance);
                        foreach (var pathVertex in path.Vertices)
                        {
                            if (!CurrentSolution.ContainsVertex(pathVertex))
                                CurrentSolution.AddVertex(pathVertex);
                        }
                        foreach (var pathEdge in path.Edges)
                        {
                            if (!CurrentSolution.ContainsEdge(pathEdge))
                                CurrentSolution.AddEdge(pathEdge);
                        }
                        SetCurrentSolution(CurrentSolution, true);
                    }
                    else
                        _instance.RemoveEdge(edge, false);
                }
                else
                {
                    i--; //Try again!
                }
            }

            Vertex vertex;
            int v = _verticesToRemove.Count;
            for (int i = 0; i < v; i++)
            {
                if (_verticesToRemove.TryDequeue(out vertex))
                {
                    if (CurrentSolution != null && CurrentSolution.ContainsVertex(vertex))
                    {
                        vertex.DecreaseScore(int.MaxValue); // Give this vertex a very low score! 
                        _instance.RemoveVertex(vertex);
                    }
                    else
                        _instance.RemoveVertex(vertex);
                }
                else
                {
                    i--; //Try again!
                }
            }

            if (e > 0 || v > 0)
                _instanceDisntaceGraphOutdated = true;
        }

        private void SetCurrentSolution(Graph solution, bool acceptWorseSolution = false)
        {
            if (CurrentSolution == null || acceptWorseSolution || solution.TotalCost < CurrentSolution.TotalCost)
            {
                CurrentSolution = solution.Clone();

                var removeVertices = CurrentSolution.Vertices.Where(x => CurrentSolution.GetDegree(x) == 0).ToList();
                foreach (var vertex in removeVertices)
                    CurrentSolution.RemoveVertex(vertex);

                CurrentSolutionCost = CurrentSolution.TotalCost;
                SolutionUpperBoundChanged?.Invoke(CurrentSolution.TotalCost);
            }
        }
    }
}
