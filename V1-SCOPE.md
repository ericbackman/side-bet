# Side Bet — v1 Scope (locked 2026-06-20)

**v1 is a server-authoritative, casino-style betting party game. No player movement, no physics.**
The "matches" you bet on are quick, outcome/timing-based mini-games the server resolves. This keeps
the hard, time-eating problem (networked physics) entirely out of v1.

If a proposed feature needs movement or physics, it is **not v1** — write it down for later instead.

## In scope (v1)
- Join a room by code (Unity Relay) — you + friends, no port-forwarding, no cost.
- Persistent chip bankrolls + a running leaderboard across rounds.
- A betting phase each round: wager chips on an outcome at fixed decimal odds.
- Outcome/timing mini-games the **server** resolves (start: `DiceDuel`; later: higher/lower, reaction-duel).
- Server-authoritative settlement + payout; results sync to every screen.
- Minimal functional UI (bet buttons, standings, host "play round").

## Explicitly OUT of scope (defer to v2+)
- Player movement / avatars / walk-around lobbies.
- Networked physics of any kind.
- Crossplay, Steam release, dedicated servers.
- Cosmetics, progression, accounts, persistence between sessions.

## Definition of done (the v1 demo)
Two computers connect via Relay → both join a room → bet on a `DiceDuel` round → the server
resolves it → bankrolls + leaderboard update → repeat for several rounds.

## Build path to that
1. **M1 (gate)** — generate the Unity project, connect two machines via Relay (shared-counter proof).
2. Wire `MatchNetwork` to a minimal UI (bet buttons + standings + host "play round").
3. Trivial dice-resolve visual + real roster (replace the hardcoded host/guest).
4. Loop + a couple more mini-games for variety.

Everything in `Assets/Scripts/Game/Core/` is pure C# and unit-tested; the Unity layer just mirrors
it over the network.
