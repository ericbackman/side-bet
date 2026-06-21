// ─────────────────────────────────────────────────────────────────────────────
// M2 BRIDGE — connects the tested SideBet.Core game loop to Netcode for GameObjects.
//
// THE PATTERN (server-authoritative): the SERVER owns a GameSession (the tested Core).
// Clients never mutate it — they send REQUESTS via ServerRpc, the server validates and
// applies them through the Core, then mirrors the resulting state to everyone through
// NetworkVariable / NetworkList. This is "never trust the client", exactly like a web
// backend validating requests.
//
// STATUS: reference template, written without a Unity compiler — verify the NGO API against
// your installed package versions. Put this on a NetworkObject in the scene. Lives in
// Assembly-CSharp (no asmdef here) so it can use both UnityEngine and SideBet.Core.
// ─────────────────────────────────────────────────────────────────────────────
using SideBet.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SideBet.GameNet
{
    /// <summary>One player's chips, in a network-serializable form for the standings list.</summary>
    public struct Standing : INetworkSerializable, System.IEquatable<Standing>
    {
        public FixedString64Bytes PlayerId;
        public long Chips;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref PlayerId);
            s.SerializeValue(ref Chips);
        }

        public bool Equals(Standing other) => PlayerId.Equals(other.PlayerId) && Chips == other.Chips;
    }

    public sealed class MatchNetwork : NetworkBehaviour
    {
        // Mirrored state: everyone can READ, only the server can WRITE. Clients bind their UI to these.
        public readonly NetworkVariable<int> RoundNumber = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkList<Standing> Standings = new NetworkList<Standing>(
            null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // The authoritative model. Exists ONLY on the server — clients keep this null.
        private GameSession _session;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            // TODO(stevie/eric): build the real roster from NetworkManager.ConnectedClientsIds.
            _session = new GameSession(new[]
            {
                new Player("host", "Host", 1000),
                new Player("guest", "Guest", 1000),
            });
            PublishStandings();
        }

        // A client asks to place a bet. The SERVER decides whether it's legal (funds, phase,
        // odds) by delegating to the Core — a forged/over-budget request is simply rejected.
        [ServerRpc(RequireOwnership = false)]
        public void PlaceBetServerRpc(FixedString64Bytes playerId, FixedString64Bytes outcomeId, long stake)
        {
            try
            {
                _session.PlaceBet(playerId.ToString(), outcomeId.ToString(), stake);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Bet rejected: {e.Message}"); // no silent failure
                return;
            }
            PublishStandings();
        }

        // Host-driven round control (called from the host's UI, not by clients).
        public void HostStartRound(IMiniGame game)
        {
            if (!IsServer) return;
            _session.StartRound(game);
            RoundNumber.Value = _session.RoundNumber;
            PublishStandings();
        }

        public void HostPlayAndSettle()
        {
            if (!IsServer) return;
            _session.PlayAndSettle();
            PublishStandings(); // the new bankrolls now sync to every screen
        }

        private void PublishStandings()
        {
            Standings.Clear();
            foreach (var p in _session.Standings())
                Standings.Add(new Standing { PlayerId = p.Id, Chips = p.Bankroll });
        }
    }
}
