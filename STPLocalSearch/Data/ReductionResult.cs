using STPLocalSearch.Graphs;
using System.Collections.Generic;

namespace STPLocalSearch.Data
{
    public class ReductionResult
    {
        public ReductionResult()
        {
            RemovedVertices = new List<Vertex>();
            RemovedEdges = new List<Edge>();
        }

        public int ReductionUpperBound { get; set; }

        public List<Edge> RemovedEdges { get; private set; }
        public  List<Vertex> RemovedVertices { get; private set; }
    }
}
