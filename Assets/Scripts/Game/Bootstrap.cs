using UnityEngine;
using PartyLoteria.Network;

namespace PartyLoteria.Game
{
    /// <summary>
    /// Bootstraps the game by ensuring managers are created in the correct order.
    /// Attach this to an empty GameObject in your initial scene.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [Header("Manager Prefabs (Optional)")]
        [SerializeField] private GameObject networkManagerPrefab;
        [SerializeField] private GameObject gameManagerPrefab;

        private void Awake()
        {
            // Ensure NetworkManager exists
            if (NetworkManager.Instance == null)
            {
                if (networkManagerPrefab != null)
                {
                    Instantiate(networkManagerPrefab);
                }
                else
                {
                    var networkObj = new GameObject("NetworkManager");
                    var networkManager = networkObj.AddComponent<NetworkManager>();
                    // Server URL is set via SerializeField on the component
                }
            }

            // Ensure GameManager exists
            if (GameManager.Instance == null)
            {
                if (gameManagerPrefab != null)
                {
                    Instantiate(gameManagerPrefab);
                }
                else
                {
                    var gameObj = new GameObject("GameManager");
                    gameObj.AddComponent<GameManager>();
                }
            }
        }
    }
}
