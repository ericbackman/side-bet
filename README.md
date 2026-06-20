# Side Bet (working title)

A party game where you and your friends **bet, sportsbook-style, on the outcome of
mini-games you play against each other.** Make the odds, talk your trash, sandbag your
own match, cash out. The casino is your friend group.

> v1 / learning project — built by two people (Eric + Stevie) partly to learn a proper
> multi-person git workflow. The concept will shift as we play it.

## Stack

- **Engine:** Unity 6 LTS (2D template)
- **Netcode:** Netcode for GameObjects (NGO) + Unity Transport
- **Connectivity:** Unity **Relay** + **Lobby** + **Authentication** — friends join by code
  over the internet, no port-forwarding, no Steam, free tier.
- **Authority:** host/server-authoritative for all outcomes and payouts (clients never
  self-report results).

## Core loop (Minimum Playable)

`Lobby → Betting phase (wager chips on an outcome, odds shown) → Mini-game plays →
Server resolves → Payout → Bankrolls / leaderboard update → repeat.`

## Status

- [ ] M0 — repo, Unity project, git/LFS/Smart-Merge, branch protection
- [ ] M1 — connectivity spike (two machines linked via Relay, shared state) ← first proof
- [ ] M2 — game skeleton (bankroll + round state machine, betting math as pure C# + tests)
- [ ] M3 — one server-authoritative mini-game
- [ ] M4 — betting + payout (first fun demo)
- [ ] M5 — loop + leaderboard + juice

## Getting started

See **[SETUP.md](SETUP.md)** for the full ordered checklist, and **[CONTRIBUTING.md](CONTRIBUTING.md)**
for the git workflow. Short version: install the pinned Unity version, clone, `git lfs install`,
configure Smart Merge, open in Unity.

## Layout

```
Assets/Scripts/Net/    # Stevie — Relay/Lobby/NGO, server-authoritative state, hosting
Assets/Scripts/Game/   # Eric  — betting/odds/bankroll (pure C# + tests), round state machine
Assets/Scripts/UI/     # Eric  — lobby, betting, results screens
Assets/Scenes/         # coordinate ownership (see CONTRIBUTING.md)
Assets/Prefabs/        # coordinate ownership
```
