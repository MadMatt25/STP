using System.Linq;

namespace STPLocalSearch.Graphs
{
    public class Vertex
    {
        private readonly int[] _scoreHistory = new int[10];
        private int _scoreUpdates = 0;

        /// <summary>
        /// Constructs a vertex with a given "name".
        /// </summary>
        /// <param name="name">The identifier for the vertex in the graph.</param>
        public Vertex(int name)
        {
            VertexName = name;
        }

        public int VertexName { get; private set; }

        public int Score
        {
            get
            {
                //if (_scoreUpdates < _scoreHistory.Length)
                //    return 100;
                return _scoreHistory[_scoreHistory.Length - 1];
            }
        }

        public bool ReportsRealScore
        { get { return _scoreUpdates >= _scoreHistory.Length; } }

        public double AverageScore
        {
            get
            {
                //if (_scoreUpdates < _scoreHistory.Length)
                //    return 100;
                return _scoreHistory.Average();
            }
        }

        public void InitializeScore(int initialScore)
        {
            for (int i = 0; i < _scoreHistory.Length; i++)
                _scoreHistory[i] = initialScore;
            _scoreUpdates = 0;
        }

        public void IncreaseScore(int increase)
        {
            // Take most recent score and increase
            int newScore = _scoreHistory[_scoreHistory.Length - 1] + increase;
            if (newScore > 100)
                newScore = 100;

            UpdateScore(newScore);
        }

        public void DecreaseScore(int decrease)
        {
            int newScore = _scoreHistory[_scoreHistory.Length - 1] - decrease;
            if (newScore < 0)
                newScore = 0;

            UpdateScore(newScore);
        }

        private void UpdateScore(int score)
        {
            // Move older scores one position to the left
            for (int i = 0; i < _scoreHistory.Length - 1; i++)
                _scoreHistory[i] = _scoreHistory[i + 1];
            _scoreHistory[_scoreHistory.Length - 1] = score;
            _scoreUpdates++;
        }

        public override int GetHashCode()
        {
            return VertexName;
        }

        public override string ToString()
        {
            return "Vertex " + VertexName;
        }
    }
}