# Get Side Bet running locally

Everything you (or Stevie) need to clone this repo and be playtesting in Unity. This is a
**Unity 6 (2D / URP) project** plus a pure-C# game Core that's unit-tested without the engine.

---

## 1. Install the tools (once per machine)

- **Unity Hub** — https://unity.com/download
- **Unity Editor `6000.5.0f1`** — open this project in Hub and it will prompt you to install the
  *exact* matching version automatically (the version is pinned in `ProjectSettings/ProjectVersion.txt`).
  Don't use a different version — a mismatch can corrupt the shared project.
- **Git** + **Git LFS** — https://git-scm.com , then run `git lfs install` once.
- **An IDE** (for the C# code): **JetBrains Rider** (free for non-commercial, best Unity support)
  or **Visual Studio Community**. VS Code works but is the weakest for Unity.

## 2. Clone

```bash
git clone https://github.com/ericbackman/side-bet.git
cd side-bet
git lfs install
```

> First clone is text-only and fast. Unity rebuilds its `Library/` cache on first open (can take
> a few minutes) — that folder is git-ignored on purpose; never commit it.

## 3. Open in Unity

- Unity Hub → **Add** → **Add project from disk** → select the `side-bet` folder.
- If Hub asks to install `6000.5.0f1`, say yes.
- Open it. Let it finish importing (status bar bottom-right).
- **If you see compile errors about `Unity.Netcode`:** the repo has some multiplayer scripts in
  `Assets/Scripts/Net/` that need the Netcode package (not needed for the movement playtest, but
  they must compile for the project to run). One-time fix: **Window → Multiplayer → Multiplayer
  Center → install "Netcode for GameObjects."** Commit the updated `Packages/manifest.json`
  afterward so the next person doesn't have to.
- **First time you open the project, Unity generates `.meta` files for the scripts** — commit
  those (`git add -A`) so references don't break for your teammate.

## 4. Playtest the movement (the current thread)

The tunable 2D platformer controller lives in `Assets/Scripts/Player/`.

- **If `Assets/Scenes/MovementPlaytest.unity` exists:** open it and press **Play**.
- **If not yet:** follow the 5-minute scene setup in
  [`Assets/Scripts/Player/MOVEMENT-PLAYTEST.md`](Assets/Scripts/Player/MOVEMENT-PLAYTEST.md)
  (make a Player with a Rigidbody2D + Capsule Collider + `PlayerController2D`, a few ground
  platforms, and put `CameraFollow2D` on the Main Camera).

**Controls:** move **A/D** or **←/→**, jump **Space** (or **W**/**↑**).
**The point:** select the Player while it's running and drag the Inspector sliders — every feel
dial (accel, jump height/time, fall gravity, coyote time, jump buffer, apex hang) updates live.
Tuning recipes are in `MOVEMENT-PLAYTEST.md`.

## 5. (Optional) Run the C# Core + sims — no Unity needed

The betting/game logic is engine-free and testable on its own (needs the .NET SDK):

```bash
dotnet test tests/SideBet.Core.Tests            # unit tests
dotnet run  --project sim/SideBet.Sim           # headless live-betting demo
```

## 6. How we work together (git)

`main` is protected — **no direct pushes**. Every change:

1. `git checkout main && git pull`
2. `git checkout -b feature/your-thing`
3. commit small, conventional messages (`feat:`, `fix:`, `chore:` …)
4. push, open a Pull Request, the **other** person reviews + merges
5. delete the branch after merge

Full details + the Unity-specific git rules (always commit `.meta` files, never two people in the
same scene at once) are in [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Layout

```
Assets/Scripts/Player/   movement playtest (PlayerController2D, CameraFollow2D)   <- start here
Assets/Scripts/Game/     game logic: Core/ (pure, tested) + MatchNetwork bridge
Assets/Scripts/Net/      Relay / Netcode connection scripts (for later multiplayer)
Assets/Scenes/           scenes
tests/                   xUnit tests for the pure Core (run with dotnet)
sim/                     headless console demo of the betting brain
```
