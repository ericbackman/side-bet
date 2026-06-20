// M1 throwaway UI — uses IMGUI (OnGUI) so there's NO Canvas/prefab wiring to do for the
// spike. Replace with proper uGUI once connectivity is proven. Reference template; verify
// against installed packages.
using Unity.Netcode;
using UnityEngine;

namespace SideBet.Net
{
    [RequireComponent(typeof(RelayConnector))]
    public sealed class ConnectionUI : MonoBehaviour
    {
        private RelayConnector _connector;
        private string _codeInput = "";
        private string _status = "Not connected";

        private void Awake() => _connector = GetComponent<RelayConnector>();

        private void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            bool connected = nm != null && (nm.IsClient || nm.IsServer);

            if (connected)
            {
                GUILayout.BeginArea(new Rect(10, 10, 340, 110), GUI.skin.box);
                GUILayout.Label($"Status: {_status}");
                if (!string.IsNullOrEmpty(_connector.JoinCode))
                    GUILayout.Label($"Join code: {_connector.JoinCode}");
                GUILayout.Label($"Players connected: {nm.ConnectedClientsIds.Count}");
                GUILayout.EndArea();
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 340, 150), GUI.skin.box);
            GUILayout.Label("SIDE BET — M1 connectivity spike");
            if (GUILayout.Button("Host")) Host();
            GUILayout.BeginHorizontal();
            _codeInput = GUILayout.TextField(_codeInput, GUILayout.Width(180));
            if (GUILayout.Button("Join")) Join(_codeInput);
            GUILayout.EndHorizontal();
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        // async void is acceptable for UI handlers; we always catch + log (no silent failures).
        private async void Host()
        {
            _status = "Starting host...";
            try
            {
                string code = await _connector.StartHost();
                _status = $"Hosting. Share code: {code}";
            }
            catch (System.Exception e)
            {
                _status = "Host failed: " + e.Message;
                Debug.LogException(e);
            }
        }

        private async void Join(string code)
        {
            _status = "Joining...";
            try
            {
                await _connector.JoinAsClient(code.Trim().ToUpperInvariant());
                _status = "Joined.";
            }
            catch (System.Exception e)
            {
                _status = "Join failed: " + e.Message;
                Debug.LogException(e);
            }
        }
    }
}
