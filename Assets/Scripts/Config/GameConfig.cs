using UnityEngine;
using PartyLoteria.UI;

namespace PartyLoteria.Config
{
    /// <summary>
    /// Game configuration that can be set from the Unity inspector.
    /// Attach this to your UIBuilder GameObject.
    ///
    /// NOTE: For local testing, use NetworkManager's "Use Local Server" checkbox instead.
    /// This component only sets production defaults.
    /// </summary>
    public class GameConfig : MonoBehaviour
    {
        [Header("Production Settings")]
        [Tooltip("Base URL for the player client (where mobile players connect).\nFor local testing, use NetworkManager's 'Use Local Server' setting instead.")]
        [SerializeField] private string playerClientUrl = "https://party-loteria-client.netlify.app";

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
            // Set production player client URL as the default
            // NetworkManager will override this in Start() if useLocalServer is enabled
            LobbyScreenController.SetPlayerUrlBase(playerClientUrl);
            Debug.Log($"[Config] Player client URL set to: {playerClientUrl}");
        }

        // For editor testing
        [ContextMenu("Log Current Config")]
        private void LogConfig()
        {
            Debug.Log($"Player Client URL: {playerClientUrl}");
        }
    }
}
