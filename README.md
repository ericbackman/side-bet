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

- [x] **M0 — repo + git hygiene + CI + branch protection: DONE.** Public repo, Git LFS +
  Unity `.gitignore` + Smart-Merge config, GitHub Actions runs the core tests **green**, and
  `main` is protected (PR + 1 approval + the `test` check required). *Pending you:* generate
  the Unity project (editor) and add Stevie as a collaborator.
- [ ] **M1 — connectivity spike** (two machines linked via Relay). Scripts scaffolded in
  `Assets/Scripts/Net/` (reference template); runs once Unity + packages + UGS are set up. ← first proof
- [x] **M2 (core) — betting/round logic DONE + unit-tested (28 tests, CI green).** Pure C# in
  `Assets/Scripts/Game/Core/`. Unity-side wiring (NetworkVariable bankroll, RPCs) still to do.
- [~] **M3 — mini-game:** `DiceDuel` server-authoritative logic done + tested; needs a Unity front end.
- [ ] **M4 — betting + payout UI** (first fun demo)
- [ ] **M5 — loop + leaderboard + juice**

> **`main` is protected:** every change goes through a PR that needs 1 approval **and** a
> green `test` check before it can merge. Push to a branch, open a PR, have the other person
> review — see [CONTRIBUTING.md](CONTRIBUTING.md).

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
