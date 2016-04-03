using System.Linq;

namespace STPLocalSearch.Graphs
{
    public class Vertex
    {
        private readonly int[] _scoreHistory = new int[10];
        private int _scoreUpdates = 0;

        public const int MIN_SCORE = 0;
        public const int MAX_SCORE = 100;

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
                //    return MAX_SCORE;
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
                //    return MAX_SCORE;
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
            if (newScore > MAX_SCORE)
                newScore = MAX_SCORE;

            UpdateScore(newScore);
        }

        public void DecreaseScore(int decrease)
        {
            if (decrease == int.MaxValue)
                UpdateScore(-1);
            else
            {
                int newScore = _scoreHistory[_scoreHistory.Length - 1] - decrease;
                if (newScore < MIN_SCORE)
                    newScore = MIN_SCORE;
                UpdateScore(newScore);
            }
        }

        private void UpdateScore(int score)
        {
            // Move older scores one position to the left
            for (int i = 0; i < _scoreHistory.Length - 1; i++)
                _scoreHistory[i] = _scoreHistory[i + 1];
            _scoreHistory[_scoreHistory.Length - 1] = score;
            _scoreUpdates++;
        }

        public void FlipScore()
        {
            UpdateScore(MAX_SCORE - Score);
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