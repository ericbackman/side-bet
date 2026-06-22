using System;
using System.Collections.Generic;

namespace SideBet.Core
{
    /// <summary>
    /// The v1 no-physics live match: a momentum scalar that drifts toward 0, periodic scoring rolls
    /// weighted by momentum, and a clock. The "match" is basically DiceDuel with a clock — the
    /// betting is the gameplay; this is the dice the bets are loaded against.
    ///
    /// CHAOS-NOT-PAYOUT LAW: modifiers rubber-band toward the TRAILER and/or inject variance. They
    /// never push toward the money that triggered them, so you can't "buy the result" by dumping
    /// chips — you only unleash unpredictable comeback chaos. The sabotage is the narration, not a
    /// payout you can farm.
    /// </summary>
    public sealed class MomentumMatch : ITickingMiniGame
    {
        private readonly string _teamA;
        private readonly string _teamB;
        private readonly IRandomSource _rng;
        private readonly List<Outcome> _outcomes;
        private readonly double _scoreRatePerSec;
        private readonly double _momentumHalfLife;

        private double _momentum;          // -1 toward B .. +1 toward A
        private double _secondsRemaining;
        private double _leaderFreeze;      // seconds the current leader can't score (Riot effect)
        private int _scoreA, _scoreB;
        private string _lastEvent = "Kickoff";

        public MomentumMatch(string teamA, string teamB, Odds oddsA, Odds oddsB, IRandomSource rng,
                             double matchSeconds = 60.0, double scoreRatePerSec = 0.07, double momentumHalfLife = 8.0)
        {
            _teamA = teamA;
            _teamB = teamB;
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _secondsRemaining = matchSeconds;
            _scoreRatePerSec = scoreRatePerSec;
            _momentumHalfLife = momentumHalfLife;
            _outcomes = new List<Outcome>
            {
                new Outcome(teamA, teamA + " wins", oddsA),
                new Outcome(teamB, teamB + " wins", oddsB),
            };
        }

        public IReadOnlyList<Outcome> Outcomes => _outcomes;
        public bool IsResolved => _secondsRemaining <= 0;
        public MatchSnapshot Snapshot =>
            new MatchSnapshot(_scoreA, _scoreB, _momentum, Math.Max(0, _secondsRemaining), _lastEvent);

        public void Tick(double dt)
        {
            if (IsResolved) return;
            _secondsRemaining -= dt;
            if (_leaderFreeze > 0) _leaderFreeze -= dt;

            _momentum *= Math.Pow(0.5, dt / _momentumHalfLife); // decays toward 0

            if (_rng.NextDouble() < _scoreRatePerSec * dt)
            {
                double pA = Clamp(0.5 + 0.5 * _momentum, 0.05, 0.95); // momentum tilts who scores
                bool toA = _rng.NextDouble() < pA;

                if (_leaderFreeze > 0)
                {
                    bool leaderWouldScore = (toA && _scoreA > _scoreB) || (!toA && _scoreB > _scoreA);
                    if (leaderWouldScore) { _lastEvent = "Leader frozen — chance squandered!"; return; }
                }

                if (toA) { _scoreA++; _lastEvent = $"GOAL {_teamA}! ({_scoreA}-{_scoreB})"; }
                else { _scoreB++; _lastEvent = $"GOAL {_teamB}! ({_scoreA}-{_scoreB})"; }
            }
        }

        public string ApplyModifier(in Modifier m)
        {
            // +1 means "push momentum toward A"; toward whoever is TRAILING (rubber-band).
            int trailerSign = _scoreA < _scoreB ? +1 : (_scoreB < _scoreA ? -1 : 0);
            double k = m.Magnitude;

            switch (m.Kind)
            {
                case ModifierKind.MomentumNudge:
                    _momentum = Clamp(_momentum + trailerSign * 0.25 * k, -1, 1);
                    _lastEvent = $"Momentum shift! ({m.Reason})";
                    break;

                case ModifierKind.OpponentChaos:
                    // pure variance + a little rubber-band — "the pitch gets chaotic"
                    _momentum = Clamp(_momentum + trailerSign * 0.2 * k + (_rng.NextDouble() - 0.5) * 0.4 * k, -1, 1);
                    _lastEvent = $"CHAOS — {m.TriggeredAgainstTeamId} rattled! ({m.Reason})";
                    break;

                case ModifierKind.CrowdInterference:
                    _momentum = Clamp(_momentum + trailerSign * 0.6 * k, -1, 1);
                    _leaderFreeze = 6.0 * k;
                    _lastEvent = $"FIELD INVASION on {m.TriggeredAgainstTeamId}! Crowd storms in! ({m.Reason})";
                    break;
            }
            return _lastEvent;
        }

        public string Resolve()
        {
            if (_scoreA > _scoreB) return _teamA;
            if (_scoreB > _scoreA) return _teamB;
            return _momentum >= 0 ? _teamA : _teamB; // momentum breaks a draw
        }

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
