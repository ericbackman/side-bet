# Contributing — two-person git workflow

This repo doubles as our way to learn a real multi-person git workflow. The rules are
deliberately a little strict so the *process* is the lesson. If something here is slowing
us down for no benefit, raise it in an issue — don't just route around it.

## The loop (every change, no exceptions)

1. **Pull `main` first**, always, *before opening Unity*:
   ```bash
   git checkout main && git pull
   ```
2. **Branch** off `main` with a typed name:
   - `feature/<thing>` · `fix/<thing>` · `chore/<thing>` · `experiment/<thing>`
   - e.g. `feature/relay-join-code`, `fix/payout-rounding`
3. **Commit small + often**, conventional-commit style:
   - `feat: add relay host + join code flow`
   - `fix: clamp negative bankroll on all-in loss`
   - `test: cover even-money payout`
4. **Push and open a Pull Request.** Describe *what* and *why*. Link the issue.
5. **The OTHER person reviews and merges.** Never approve-and-merge your own PR — the
   review is the whole point. Reviewer pulls the branch, opens it in Unity if it touches
   scenes/prefabs, and checks it actually runs.
6. **Delete the branch after merge.** Keep `main` always-green and always-runnable.

`main` is protected: no direct pushes, 1 approval required.

## Splitting work so we don't collide

- **Stay in your folder.** Stevie → `Assets/Scripts/Net/`. Eric → `Assets/Scripts/Game/`,
  `Assets/Scripts/UI/`. Two PRs in different folders almost never conflict.
- **Use Issues + a board** (To do / Doing / Done). Claim an issue before starting so we're
  not both building the lobby.

## Unity-specific rules (this is where Unity + git bites)

- **Never two people in the same scene or prefab at once.** Unity merges `.unity`/`.prefab`
  badly even with Smart Merge. Coordinate in chat: "I'm taking `Main.unity` for an hour."
  Prefer building UI/objects as **prefabs** and keeping scenes thin.
- **Always commit `.meta` files** alongside their assets. A missing `.meta` breaks the
  other person's project. (`.gitignore` is already set up to keep them.)
- **Binaries go through Git LFS** (configured in `.gitattributes`) — run `git lfs install`
  once per machine.
- **Don't bump the Unity version** in `ProjectSettings/ProjectVersion.txt` unilaterally —
  it forces an upgrade on the other person. Agree first.
- If you hit a scene/prefab conflict anyway: don't hand-edit the YAML. Re-do your change on
  a fresh pull, or let Smart Merge handle it (`git merge`), and if in doubt, ask.

## Running locally

- Open the project in Unity (same version for both of us — see `SETUP.md`).
- Multiplayer needs UGS signed in; keys live in Unity Gaming Services, **never in git**.
- No secrets, keys, or `.env` files in commits — ever.
