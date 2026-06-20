using System.Collections.Generic;

namespace SideBet.Core
{
    /// <summary>
    /// A mini-game the round bets on. Server-authoritative by contract: only the server calls
    /// <see cref="Resolve"/>, and clients can never influence the result. Implementations
    /// expose the outcomes players can bet on (typically one per competitor) and produce the
    /// winning outcome id when the match is played out.
    /// </summary>
    public interface IMiniGame
    {
        IReadOnlyList<Outcome> Outcomes { get; }

        /// <summary>Play the match out and return the winning outcome id.</summary>
        string Resolve();
    }
}
