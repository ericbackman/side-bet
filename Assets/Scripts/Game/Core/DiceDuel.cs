using System;
using System.Collections.Generic;

namespace SideBet.Core
{
    /// <summary>
    /// The simplest server-authoritative mini-game: two competitors each roll a die, high roll
    /// wins, ties re-roll. The RNG is INJECTED (a Func returning 1..6) so that (a) tests are
    /// deterministic and (b) in production the SERVER owns the randomness — a client cannot
    /// nudge the result. Odds here are illustrative; a real version would price them from
    /// player ratings or a pari-mutuel pool.
    /// </summary>
    public sealed class DiceDuel : IMiniGame
    {
        private readonly string _aOutcomeId;
        private readonly string _bOutcomeId;
        private readonly Func<int> _rollD6;
        private readonly List<Outcome> _outcomes;

        public DiceDuel(
            string aOutcomeId, string aName,
            string bOutcomeId, string bName,
            Func<int> rollD6, Odds odds)
        {
            _aOutcomeId = aOutcomeId;
            _bOutcomeId = bOutcomeId;
            _rollD6 = rollD6 ?? throw new ArgumentNullException(nameof(rollD6));
            _outcomes = new List<Outcome>
            {
                new Outcome(aOutcomeId, aName + " wins", odds),
                new Outcome(bOutcomeId, bName + " wins", odds),
            };
        }

        public IReadOnlyList<Outcome> Outcomes => _outcomes;

        public string Resolve()
        {
            // Re-roll ties. Bounded so a broken RNG can't hang the server forever.
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                int a = _rollD6();
                int b = _rollD6();
                if (a > b) return _aOutcomeId;
                if (b > a) return _bOutcomeId;
            }
            throw new InvalidOperationException("DiceDuel could not break a tie after 1000 rolls — check the RNG.");
        }
    }
}
