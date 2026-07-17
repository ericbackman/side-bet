# Movement playtest — setup & tuning

A tunable 2D platformer controller for finding "buttery" feel. Two scripts:
`PlayerController2D.cs` (movement) and `CameraFollow2D.cs` (smooth camera). Everything is
tweakable **live in the Inspector while the game is playing** — that's the whole point.

> If these are in `side-bet` but you're still opening the `learning-gamedev` project in Unity,
> copy `Assets/Scripts/Player/` into that project (or open `side-bet` instead). They're
> self-contained.

## Scene setup (~5 minutes, one time)

1. **Player:** GameObject → 2D Object → Sprites → **Square**. Rename it `Player`.
   - Add Component → **Rigidbody 2D**.
   - Add Component → **Capsule Collider 2D** (capsule = smooth edges, won't snag on seams).
   - Add Component → **PlayerController2D**. (It auto-sets gravity/rotation/interpolation.)
2. **Ground:** make a few `Square` sprites, scale them into platforms, and add a **Box Collider 2D**
   to each. Lay out a little test course (a floor + some ledges + a gap to test coyote time).
   - *(Optional, cleaner):* make a layer called `Ground`, put the platforms on it, and set the
     player's **Ground Layer** to `Ground`. Default `Everything` works fine for a quick test.
3. **Camera:** select **Main Camera** → Add Component → **CameraFollow2D** → drag `Player` into **Target**.
4. Press **Play**. Move with **A/D** or **←/→**, jump with **Space** (or **W**/**↑**).
5. With the game still running, select `Player` and **drag the sliders in the Inspector** — you'll
   feel the change instantly. (Note: live tweaks reset when you stop Play, so write down the
   numbers you like.)

## Tuning recipe — start here, then chase the feel

| Knob | What it changes | Try |
|---|---|---|
| `maxSpeed` | top run speed | 8–11 |
| `groundAccel` / `groundDecel` | snappy vs slidey starts/stops | 90 / 110 (snappy) · 25 / 20 (icy) |
| `jumpHeight` + `timeToApex` | how high, and how snappy the rise | 3.2 + 0.38 (classic) · 4 + 0.30 (floaty-but-fast) |
| `fallGravityMult` | how fast you come down | 1.5–2.2 (higher = snappier) |
| `jumpCutMult` | tap = small hop, hold = full jump | 1.8–2.5 |
| `coyoteTime` | forgiveness after a ledge | 0.08–0.12 |
| `jumpBuffer` | early-press forgiveness | 0.10–0.15 |
| `apexHangThreshold` / `apexHangGravityScale` | the floaty "hang" at the top | 1.5 / 0.5 (subtle) · 0 to turn off |

**Two reference feels to aim at:**
- **Snappy/precise (Celeste-ish):** high accel/decel (90/110), lower jumpHeight (~2.8), short timeToApex (~0.32), fallGravityMult ~2.0, small apex hang.
- **Floaty/expressive:** lower accel (~50), higher jumpHeight (~4), longer timeToApex (~0.42), bigger apex hang.

## What's deliberately NOT here yet
- **Networking.** This is single-player so you can nail the *feel* first. Syncing responsive
  movement across machines (client prediction + reconciliation) is the genuinely hard part and
  is a separate milestone — do it *after* the feel is locked.
- Animation, wall-jump, dash, double-jump — all easy to bolt on once the base feel is right.
