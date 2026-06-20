# Side Bet — Setup & Path to First Playable

Working title for a party game where you and your friends **bet, sportsbook-style, on
the outcome of mini-games you play against each other.** Stack: **Unity + Netcode for
GameObjects (NGO) + Unity Relay/Lobby** so two friends connect over the internet with
no port-forwarding, no Steam, no cost.

This file is the ordered checklist to get from nothing → "my computer linked with
Stevie's, playing a round." Do the milestones in order; **M1 is the riskiest part, so
prove it before building anything fun.**

---

## Who owns what

| Area | Folder | Owner |
|---|---|---|
| Networking: Relay, Lobby, NGO connection, server-authoritative state, hosting | `Assets/Scripts/Net/` | **Stevie** (backend / uptime) |
| Gameplay: betting + odds + bankroll (pure C#), round state machine, mini-games, UI | `Assets/Scripts/Game/` + `Assets/Scripts/UI/` | **Eric** |
| Scenes / prefabs | `Assets/Scenes/`, `Assets/Prefabs/` | **coordinate** — see CONTRIBUTING.md |

Different folders = clean pull requests. The only thing you must coordinate on is who
edits a given **scene or prefab** at a time (Unity merges those badly even with Smart Merge).

---

## M0 — Foundations (first session, do it together on a call)

**1. Install Unity (BOTH of you, the SAME version).**
- Install **Unity Hub**, then in Hub install the **latest Unity 6 LTS** (6000.x LTS).
- Version mismatch silently corrupts a shared project — agree on the exact version and
  both install that one. The committed `ProjectSettings/ProjectVersion.txt` is the source
  of truth; don't "upgrade" it without telling the other person.

**2. Create the Unity project (Eric, once).**
- Unity Hub → New Project → **2D (Built-In Render Pipeline)** template (light, fast; this
  is a UI/betting game, not a 3D showcase) → name it `side-bet` → location
  `C:\Users\ericb\Github\`. Let Hub generate into `C:\Users\ericb\Github\side-bet\`.
- Move the four starter files already in this folder (`.gitignore`, `.gitattributes`,
  `README.md`, `CONTRIBUTING.md`, `SETUP.md`) into the generated project root if Hub
  created a fresh dir beside them. End state: `Assets/ Packages/ ProjectSettings/` and
  these docs all sit at the repo root.

**3. Initialize git + LFS + Smart Merge (Eric).**
```bash
cd /c/Users/ericb/Github/side-bet
git init -b main
git lfs install                       # one-time per machine; enables the LFS filters in .gitattributes

# Configure Unity Smart Merge as the merge driver (adjust the Unity version in the path):
UYM="/c/Program Files/Unity/Hub/Editor/6000.0.XXf1/Editor/Data/Tools/UnityYAMLMerge.exe"
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "\"$UYM\" merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary

git add -A
git commit -m "chore: initial Unity project + repo hygiene (LFS, smart merge)"
```

**4. Create the GitHub repo + add Stevie.**
```bash
gh repo create ericbackman/side-bet --private --source=. --push
gh repo add-collaborator ericbackman/side-bet <stevie-github-username> --permission push
```

**5. Protect `main` (this is the whole point of the exercise).**
- GitHub → Settings → Branches → add rule for `main`: **Require a pull request before
  merging** + **Require 1 approval**. No direct pushes to `main` — ever.
- (Optional now, great later: require status checks once CI exists.)

**6. Stevie onboards (his first git rep).**
```bash
git clone https://github.com/ericbackman/side-bet.git
cd side-bet
git lfs install
# repeat the 3 `git config merge.unityyamlmerge.*` commands above with HIS Unity path
```
Then both: open the project in Unity, confirm it loads. Do one **throwaway PR** each
(edit README, branch, PR, the other approves+merges) just to rehearse the loop.

---

## M1 — Connectivity spike  ← "my computer linked with Stevie" (do this next, before anything fun)

Goal: Eric hosts → gets a **join code** → Stevie enters it → both connected via Relay →
a single networked object both can see update live (a shared counter or two avatars).

**7. Install the multiplayer packages** (Window → Package Manager → Unity Registry):
- `Netcode for GameObjects`
- `Unity Transport`
- `Relay`, `Lobby`, `Authentication`  (Unity Gaming Services)

**8. Create a UGS project** (one-time): dashboard.unity.com → create a project → link it
in Unity (Edit → Project Settings → Services). Free tier covers all of this. Stevie owns
this account/keys (uptime is his thing); keys go in UGS, **never in git**.

**9. Build the spike** (Stevie's first real PR, in `Assets/Scripts/Net/`):
- Anonymous sign-in via Authentication.
- Host: `RelayService` allocation → get join code → start NGO host on Unity Transport with
  the relay data. Client: enter code → join allocation → start NGO client.
- One `NetworkBehaviour` with a `NetworkVariable<int>` both can increment.
- ✅ **Done when:** you two, on different machines/networks, see the same number tick up.

> Want a head start? Ask me to write these Net/ scripts (RelayBootstrap, connection UI) as
> a reviewable first PR for Stevie — it's the fiddliest part and I can scaffold it.

---

## M2–M5 — From "linked" to "fun"

- **M2 Game skeleton:** lobby UI, per-player **bankroll** (`NetworkVariable`), and a round
  **state machine** (`Betting → Playing → Resolving → Payout`). Write the betting/payout
  math as **pure C# with unit tests** (same pattern as the Understudy prototype) so it's
  testable off the network. (Eric, `Assets/Scripts/Game/`.)
- **M3 One mini-game:** the simplest server-authoritative outcome (reaction-time duel or
  higher/lower). The **host/server decides the result** — clients never self-report.
- **M4 Betting layer:** show odds → players place a wager (RPC) → resolve → pay out → update
  bankrolls. **← first genuinely fun demo with Stevie.**
- **M5 Loop + leaderboard + juice** (sounds, a win screen, trash-talk emotes).

**Milestone targets:** M0+M1 in your first one or two sessions (connectivity is the real
unknown). M4 is your "playable demo" goal.
