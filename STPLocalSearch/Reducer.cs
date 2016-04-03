using System;
using System.Collections.Generic;
using System.Threading;
using STPLocalSearch.Data;
using STPLocalSearch.Graphs;
using STPLocalSearch.Reduce;

namespace STPLocalSearch
{
    public class Reducer
    {
        private readonly Graph _instance;
        private int _solutionUpperBound;
        private int _reductionUpperBound;
        private Thread _reductionThread;
        private bool _running;

        public delegate void ReductionFoundEventHandler(List<Edge> removeEdges, List<Vertex> removeVertices);
        public event ReductionFoundEventHandler ReductionFound;

        public delegate void ReductionUpperBoundChangedEventHandler(int reductionUpperBound);
        public event ReductionUpperBoundChangedEventHandler ReductionUpperBoundChanged;

        public event EventHandler InitialReductionsDone;
        public event EventHandler Done;

        public Reducer(Graph instance)
        {
            _instance = instance.Clone();
            _solutionUpperBound = Int32.MaxValue;
            _reductionUpperBound = 0;
            _running = false;
            Status = "Not running.";
        }

        /// <summary>
        /// Method to start the reducer.
        /// </summary>
        public void Start()
        {
            if (_running)
                throw new InvalidOperationException("Reducer cannot be started. It is still running.");
         
            _reductionThread = new Thread(ReductionThread);
            _reductionThread.Start();
        }

        /// <summary>
        /// Method to abort the reducer. Reached unsaved progress will 
        /// be lost after calling this method.
        /// </summary>
        public void Abort()
        {
            if (_reductionThread != null && _reductionThread.IsAlive)
                _reductionThread.Abort();
        }

        /// <summary>
        /// Method to update the known upper bound for an optimal solution to
        /// this instance of the Steiner Tree Problem. If the reducer is not
        /// running and a reduction is possible with this new bound, 
        /// it is automatically restarted.
        /// </summary>
        /// <param name="upperBound">The known upper bound for this instance of STP.</param>
        public void SignalNewUpperBound(int upperBound)
        {
            if (upperBound < _solutionUpperBound)
                _solutionUpperBound = upperBound;

            if (_solutionUpperBound < _reductionUpperBound && !_running)
            {
                _reductionThread = new Thread(StartBoundBasedReductions);
                _reductionThread.Start();
            }
        }

        /// <summary>
        /// Property indicates whether the reducer is actively working.
        /// </summary>
        public bool IsRunning { get { return _running; } }

        public string Status { get; private set; }

        public int ReductionUpperBound { get { return _reductionUpperBound; } }

        private void ReductionThread()
        {
            _running = true;

            int e = _instance.NumberOfEdges;
            int v = _instance.NumberOfVertices;

            do
            {
                e = _instance.NumberOfEdges;
                v = _instance.NumberOfVertices;
                SimpleReductions();
                BoundBasedReductions();
            } while (_instance.NumberOfVertices < v || _instance.NumberOfEdges < e);

            _running = false;
            Status = "Not running.";
            Done?.Invoke(this, EventArgs.Empty);
        }

        private void StartBoundBasedReductions()
        {
            _running = true;
            int e = _instance.NumberOfEdges;
            int v = _instance.NumberOfVertices;

            BoundBasedReductions();

            if (_instance.NumberOfVertices < v || _instance.NumberOfEdges < e)
                ReductionThread();
            else
            {
                _running = false;
                Status = "Not running.";
                Done?.Invoke(this, EventArgs.Empty);
            }
        }



        private void SimpleReductions()
        {
            ReductionResult reductionResult;

            Status = "Running degree test.";
            reductionResult = DegreeTest.RunTest(_instance);
            ExecuteFoundReduction(reductionResult);

            InitialReductionsDone?.Invoke(this, EventArgs.Empty);

            Status = "Running triangle test.";
            reductionResult = TriangleTest.RunTest(_instance);
            ExecuteFoundReduction(reductionResult);

            Status = "Running degree test.";
            reductionResult = DegreeTest.RunTest(_instance);
            ExecuteFoundReduction(reductionResult);

            Status = "Running approximate special distance test.";
            reductionResult = SpecialDistanceApproxTest.RunTest(_instance);
            ExecuteFoundReduction(reductionResult);

            Status = "Running degree test.";
            reductionResult = DegreeTest.RunTest(_instance);
            ExecuteFoundReduction(reductionResult);

            Status = "Running triangle test.";
            reductionResult = TriangleTest.RunTest(_instance);
            ExecuteFoundReduction(reductionResult);

            Status = "Running degree test.";
            reductionResult = DegreeTest.RunTest(_instance);
            ExecuteFoundReduction(reductionResult);
        }

        private void BoundBasedReductions()
        {
            ReductionResult reductionResult;
            int reductionUpperBound = 0;

            Status = "Running weak VR test.";
            reductionResult = WeakVeronoiRegionTest.RunTest(_instance, _solutionUpperBound);
            ExecuteFoundReduction(reductionResult);
            if (reductionResult.ReductionUpperBound > reductionUpperBound)
                reductionUpperBound = reductionResult.ReductionUpperBound;

            //Status = "Running VR test.";
            //reductionResult = VeronoiRegionTest.RunTest(_instance, _solutionUpperBound);
            //ExecuteFoundReduction(reductionResult);
            //if (reductionResult.ReductionUpperBound > reductionUpperBound)
            //    reductionUpperBound = reductionResult.ReductionUpperBound;

            Status = "Running reachability test.";
            reductionResult = ReachabilityTest.RunTest(_instance, _solutionUpperBound);
            ExecuteFoundReduction(reductionResult);
            if (reductionResult.ReductionUpperBound > reductionUpperBound)
                reductionUpperBound = reductionResult.ReductionUpperBound;

            if (reductionUpperBound > _reductionUpperBound)
            {
                _reductionUpperBound = reductionUpperBound;
                ReductionUpperBoundChanged?.Invoke(_reductionUpperBound);
            }
        }

        private void ExecuteFoundReduction(ReductionResult reductionResult)
        {
            if (reductionResult.RemovedEdges.Count == 0 && reductionResult.RemovedVertices.Count == 0)
                return;

            foreach (var edge in reductionResult.RemovedEdges)
                _instance.RemoveEdge(edge);

            foreach (var vertex in reductionResult.RemovedVertices)
                _instance.RemoveVertex(vertex);

            ReductionFound?.Invoke(reductionResult.RemovedEdges, reductionResult.RemovedVertices);
        }
    }
}
