using UnityEngine;
using PartyLoteria.UI;

namespace PartyLoteria.Config
{
    /// <summary>
    /// Game configuration that can be set from the Unity inspector.
    /// Attach this to your UIBuilder GameObject.
    /// </summary>
    public class GameConfig : MonoBehaviour
    {
        [Header("Network Settings")]
        [Tooltip("URL of the game server (where server.js is running)")]
        [SerializeField] private string serverUrl = "https://party-loteria-ircg2u7krq-uc.a.run.app";

        [Header("Player Client Settings")]
        [Tooltip("Base URL for the player client (where mobile players connect).\nFor local testing with real devices, use your computer's IP address.\nExample: http://192.168.1.100:5173")]
        [SerializeField] private string playerClientUrl = "https://party-loteria-client.netlify.app";

        [Header("Auto-detect Settings")]
        [Tooltip("Try to auto-detect local IP for player client URL (recommended for testing)")]
        [SerializeField] private bool useLocalIp = false;

        [SerializeField] private int playerClientPort = 5173;

        private void Awake()
        {
            ApplyConfig();
        }

        // Re-apply config when values change in the inspector (editor only)
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyConfig();
            }
        }

        private void ApplyConfig()
        {
            // Set player client URL
            string finalPlayerUrl = playerClientUrl;

            if (useLocalIp)
            {
                string localIp = GetLocalIPAddress();
                if (!string.IsNullOrEmpty(localIp))
                {
                    finalPlayerUrl = $"http://{localIp}:{playerClientPort}";
                    Debug.Log($"[Config] Auto-detected local IP: {localIp}");
                }
            }

            LobbyScreenController.SetPlayerUrlBase(finalPlayerUrl);

            // Server URL is set via NetworkManager inspector or here
            var networkManager = GetComponentInChildren<Network.NetworkManager>();
            if (networkManager != null)
            {
                // NetworkManager gets URL from its own serialized field
                Debug.Log($"[Config] Server URL: {serverUrl}");
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string ipStr = ip.ToString();
                        // Prefer 192.168.x.x addresses
                        if (ipStr.StartsWith("192.168."))
                        {
                            return ipStr;
                        }
                    }
                }
                // Fallback to first IPv4
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Config] Failed to get local IP: {e.Message}");
            }
            return null;
        }

        // For editor testing
        [ContextMenu("Log Current Config")]
        private void LogConfig()
        {
            Debug.Log($"Server URL: {serverUrl}");
            Debug.Log($"Player Client URL: {playerClientUrl}");
            if (useLocalIp)
            {
                Debug.Log($"Local IP: {GetLocalIPAddress()}");
            }
        }
    }
}
