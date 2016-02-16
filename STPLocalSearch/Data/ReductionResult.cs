using STPLocalSearch.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STPLocalSearch.Data
{
    public class ReductionResult
    {
        public ReductionResult(Graph g, int reductionBound)
        {
            Graph = g;
            MinimumReductionBound = reductionBound;
        }

        public Graph Graph { get; private set; }
        public int MinimumReductionBound { get; private set; }
    }
}
