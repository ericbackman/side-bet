// M1 proof-of-sync. Put this on a NetworkObject placed in the scene (or spawned by the
// host). When you and Stevie both see the number climb on your separate machines,
// networking works and M1 is done. Reference template; verify against installed packages.
using Unity.Netcode;
using UnityEngine;

namespace SideBet.Net
{
    public sealed class SharedCounter : NetworkBehaviour
    {
        // Server owns the value; everyone can read it.
        private readonly NetworkVariable<int> _count = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void OnGUI()
        {
            if (!IsSpawned) return;
            GUILayout.BeginArea(new Rect(10, 130, 340, 70), GUI.skin.box);
            GUILayout.Label($"Shared counter: {_count.Value}");
            if (GUILayout.Button("+1  (everyone should see it)"))
                IncrementServerRpc();
            GUILayout.EndArea();
        }

        // RequireOwnership=false so any client can ask the server to bump it.
        [ServerRpc(RequireOwnership = false)]
        private void IncrementServerRpc() => _count.Value += 1;
    }
}
