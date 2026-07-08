namespace Combat.Player.AI
{
    /// <summary>
    /// A candidate paired with its simulated-outcome score, ready for the quality filter.
    /// </summary>
    public readonly struct AIScoredCandidate
    {
        public AIScoredCandidate(AICandidate candidate, float score)
        {
            Candidate = candidate;
            Score = score;
        }

        public AICandidate Candidate { get; }
        public float Score { get; }
    }
}
