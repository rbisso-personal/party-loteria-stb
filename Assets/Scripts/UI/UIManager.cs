using UnityEngine;
using PartyLoteria.Data;
using PartyLoteria.Game;
using PartyLoteria.Network;

namespace PartyLoteria.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // Screen references (set by UIBuilder or Inspector)
        public GameObject ConnectingScreen { get; set; }
        public GameObject LobbyScreen { get; set; }
        public GameObject GameScreen { get; set; }
        public GameObject ResultsScreen { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SubscribeToEvents();

            // If we're already connected, create room
            if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
            {
                HandleConnected();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            var network = NetworkManager.Instance;
            if (network != null)
            {
                network.OnConnected += HandleConnected;
                network.OnDisconnected += HandleDisconnected;
                network.OnReconnectAttempt += HandleReconnectAttempt;
            }

            var game = GameManager.Instance;
            if (game != null)
            {
                game.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var network = NetworkManager.Instance;
            if (network != null)
            {
                network.OnConnected -= HandleConnected;
                network.OnDisconnected -= HandleDisconnected;
                network.OnReconnectAttempt -= HandleReconnectAttempt;
            }

            var game = GameManager.Instance;
            if (game != null)
            {
                game.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandleConnected()
        {
            Debug.Log("[UIManager] Connected - creating room");
            GameManager.Instance?.CreateRoom();
        }

        private void HandleDisconnected()
        {
            Debug.Log("[UIManager] Disconnected - showing connecting screen");
            ShowConnectingScreen();
        }

        private void HandleReconnectAttempt(int attempt)
        {
            Debug.Log($"[UIManager] Reconnect attempt {attempt}");
            ShowConnectingScreen();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            Debug.Log($"[UIManager] Phase changed to {phase}");

            switch (phase)
            {
                case GamePhase.Waiting:
                    ShowLobbyScreen();
                    break;
                case GamePhase.Playing:
                case GamePhase.Paused:
                    ShowGameScreen();
                    break;
                case GamePhase.Finished:
                    ShowResultsScreen();
                    break;
            }
        }

        private void HideAllScreens()
        {
            if (ConnectingScreen != null) ConnectingScreen.SetActive(false);
            if (LobbyScreen != null) LobbyScreen.SetActive(false);
            if (GameScreen != null) GameScreen.SetActive(false);
            if (ResultsScreen != null) ResultsScreen.SetActive(false);
        }

        public void ShowConnectingScreen()
        {
            HideAllScreens();
            if (ConnectingScreen != null) ConnectingScreen.SetActive(true);
        }

        public void ShowLobbyScreen()
        {
            HideAllScreens();
            if (LobbyScreen != null) LobbyScreen.SetActive(true);
        }

        public void ShowGameScreen()
        {
            HideAllScreens();
            if (GameScreen != null) GameScreen.SetActive(true);
        }

        public void ShowResultsScreen()
        {
            HideAllScreens();
            if (ResultsScreen != null) ResultsScreen.SetActive(true);
        }

        // Called by UIBuilder after creating screens
        public void SetScreens(GameObject connecting, GameObject lobby, GameObject game, GameObject results)
        {
            ConnectingScreen = connecting;
            LobbyScreen = lobby;
            GameScreen = game;
            ResultsScreen = results;
        }
    }
}
