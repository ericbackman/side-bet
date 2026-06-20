// ─────────────────────────────────────────────────────────────────────────────
// M1 CONNECTIVITY — Unity Relay + Netcode for GameObjects.
//
// STATUS: reference template. It was written WITHOUT a Unity compiler available, so
// treat the exact Relay API calls as "verify against your installed package versions."
// This is Stevie's M1 task / first PR — adapt as needed and tick the boxes in SETUP.md.
//
// Requires packages (Window > Package Manager): Netcode for GameObjects, Unity Transport,
// Relay, Lobby, Authentication. And a linked Unity Gaming Services project.
//
// Put this + ConnectionUI on one GameObject; put a NetworkManager (with UnityTransport)
// in the scene; put SharedCounter on a spawned NetworkObject.
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay; // RelayServerData; if missing, use allocation.ToRelayServerData("dtls")
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace SideBet.Net
{
    /// <summary>
    /// Signs in to Unity Gaming Services anonymously, then hosts or joins a game over Unity
    /// Relay (no port-forwarding, free tier). The host gets a short join code to share; the
    /// client enters it. The relay data is wired into UnityTransport before NGO starts.
    /// </summary>
    public sealed class RelayConnector : MonoBehaviour
    {
        [SerializeField] private int maxPlayers = 4; // includes the host

        public string JoinCode { get; private set; }
        public bool ServicesReady { get; private set; }

        public async Task InitServices()
        {
            if (ServicesReady) return;
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            ServicesReady = true;
        }

        /// <summary>Create a relay allocation, start the NGO host, and return the join code.</summary>
        public async Task<string> StartHost()
        {
            await InitServices();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            if (!NetworkManager.Singleton.StartHost())
                throw new InvalidOperationException("NetworkManager.StartHost() returned false.");

            return JoinCode;
        }

        /// <summary>Join an existing relay allocation by code and start the NGO client.</summary>
        public async Task JoinAsClient(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
                throw new ArgumentException("Join code is required.", nameof(joinCode));

            await InitServices();

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            JoinCode = joinCode;
            if (!NetworkManager.Singleton.StartClient())
                throw new InvalidOperationException("NetworkManager.StartClient() returned false.");
        }
    }
}
