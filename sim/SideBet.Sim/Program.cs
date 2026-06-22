using System;
using System.Collections.Generic;
using SideBet.Core;

// ─────────────────────────────────────────────────────────────────────────────
// SIDE BET — headless live-betting sim. Run:  dotnet run --project sim/SideBet.Sim
// Edit the `script` below (who bets, when, how much, on which team) and re-run to see how
// the line swings and what chaos fires. This is the whole v1 brain working with ZERO Unity.
// ─────────────────────────────────────────────────────────────────────────────

const string A = "BLUE";
const string B = "RED";
var cfg = new PressureConfig();
var rng = new SeededRandom(20260622);

var players = new Dictionary<string, Player>
{
    ["eric"] = new Player("eric", "Eric", 1000),
    ["stevie"] = new Player("stevie", "Stevie", 1000),
    ["dana"] = new Player("dana", "Dana", 1000),
};

var match = new MomentumMatch(A, B, Odds.FromDecimal(2.0m), Odds.FromDecimal(2.0m), rng, matchSeconds: 60);
var tracker = new PressureTracker(A, B, 0.5, cfg);
var scheduler = new ModifierScheduler(cfg);
var market = new LiveBettingMarket(A, B, cfg);

// Scripted bets: (atSecond, playerId, team, stake). Stevie dumps hard on RED to swing the line.
var script = new List<(double t, string p, string team, long stake)>
{
    (5,  "eric",   A, 150),
    (12, "stevie", B, 150),
    (20, "stevie", B, 600),   // the big dump -> swing crosses tiers -> chaos
    (28, "dana",   B, 400),
    (45, "eric",   A, 300),
};

Console.WriteLine("=== SIDE BET — live betting sim :  BLUE  vs  RED ===\n");

int next = 0;
double t = 0, dt = 0.1, logEvery = 3.0, nextLog = 0;
while (!match.IsResolved)
{
    // process any scripted bets now due (odds stamped server-side from the live line)
    while (next < script.Count && script[next].t <= t)
    {
        var s = script[next++];
        var ln = tracker.LineAt(market.Wagers, t);
        double prob = s.team == A ? ln.A.LiveWinProb : ln.B.LiveWinProb;
        var odds = Odds.FromDecimal((decimal)Math.Max(1.01, 1.0 / prob));
        var res = market.PlaceLiveBet(players[s.p], s.team, s.stake, odds, t, match.Snapshot.SecondsRemaining);
        Console.WriteLine(res.Accepted
            ? $"[{t,5:0.0}s] {players[s.p].Name,-7} bets {s.stake,4} on {s.team} @ {odds.Decimal:0.00}"
            : $"[{t,5:0.0}s] {players[s.p].Name,-7} bet REJECTED ({res.Reason})");
    }

    // recompute the line, drain any modifiers it triggers, apply them to the match
    var line = tracker.LineAt(market.Wagers, t);
    foreach (var m in scheduler.Advance(line, t))
    {
        string label = match.ApplyModifier(m);
        Console.WriteLine($"[{t,5:0.0}s]   *** TIER {m.Tier} {m.Kind}  ->  {label}");
    }

    if (t >= nextLog)
    {
        nextLog += logEvery;
        var snap = match.Snapshot;
        Console.WriteLine($"[{t,5:0.0}s]   line  BLUE {line.A.LiveWinProb,4:P0} / RED {line.B.LiveWinProb,4:P0}  (swing {line.MaxSwing:P0})" +
                          $"  | score {snap.ScoreA}-{snap.ScoreB}  mom {snap.Momentum,5:+0.00;-0.00}  | {snap.LastEvent}");
    }

    match.Tick(dt);
    t += dt;
}

string winner = match.Resolve();
var final = match.Snapshot;
Console.WriteLine($"\n=== FULL TIME:  {final.ScoreA}-{final.ScoreB}  ->  {winner} wins ===");
market.Settle(winner, players);
Console.WriteLine("Bankrolls:");
foreach (var p in players.Values)
    Console.WriteLine($"  {p.Name,-7} {p.Bankroll} chips");
