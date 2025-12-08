using System;
using System.Collections.Generic;
using UnityEngine;
using PartyLoteria.Data;
using PartyLoteria.Network;

namespace PartyLoteria.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private string[] winPatterns = new[] { "line" };
        [SerializeField] private int drawSpeed = 8;
        [SerializeField] private bool autoDrawCards = true;

        [Header("Language")]
        [SerializeField] private string language = "es";

        // State
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Waiting;
        public string RoomCode => NetworkManager.Instance?.CurrentRoomCode;
        public Card CurrentCard { get; private set; }
        public int CardsDrawn { get; private set; }
        public int TotalCards { get; private set; }
        public List<Player> Players { get; private set; } = new List<Player>();
        public Winner GameWinner { get; private set; }
        public string Language => language;
        public string[] WinPatterns => winPatterns;
        public int DrawSpeed => drawSpeed;

        // Events for UI
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<string> OnRoomCodeChanged;
        public event Action<Player> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<List<Player>> OnPlayersUpdated;
        public event Action<Card, int, int> OnCardDrawn;
        public event Action<Winner> OnWinnerDeclared;
        public event Action OnGameReset;
        public event Action<string> OnError;

        private float drawTimer;
        private bool isDrawing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Try to subscribe in Awake - if NetworkManager isn't ready yet, we'll retry in Start
            SubscribeToNetworkEvents();
        }

        private void Start()
        {
            // Retry subscription in case NetworkManager wasn't ready in Awake
            if (NetworkManager.Instance != null && !isSubscribed)
            {
                SubscribeToNetworkEvents();
            }
        }

        private bool isSubscribed = false;

        private void OnDestroy()
        {
            UnsubscribeFromNetworkEvents();
        }

        private void Update()
        {
            if (CurrentPhase == GamePhase.Playing && autoDrawCards && isDrawing)
            {
                drawTimer -= Time.deltaTime;
                if (drawTimer <= 0)
                {
                    DrawNextCard();
                    drawTimer = drawSpeed;
                }
            }
        }

        private void SubscribeToNetworkEvents()
        {
            var network = NetworkManager.Instance;
            if (network == null) return;

            network.OnRoomCreated += HandleRoomCreated;
            network.OnPlayerJoined += HandlePlayerJoined;
            network.OnPlayerLeft += HandlePlayerLeft;
            network.OnLobbyUpdate += HandleLobbyUpdate;
            network.OnGameStarted += HandleGameStarted;
            network.OnCardDrawn += HandleCardDrawn;
            network.OnGamePaused += HandleGamePaused;
            network.OnGameResumed += HandleGameResumed;
            network.OnGameOver += HandleGameOver;
            network.OnGameReset += HandleGameReset;
            network.OnGameError += HandleGameError;

            isSubscribed = true;
            Debug.Log("[GameManager] Subscribed to network events");
        }

        private void UnsubscribeFromNetworkEvents()
        {
            var network = NetworkManager.Instance;
            if (network == null) return;

            network.OnRoomCreated -= HandleRoomCreated;
            network.OnPlayerJoined -= HandlePlayerJoined;
            network.OnPlayerLeft -= HandlePlayerLeft;
            network.OnLobbyUpdate -= HandleLobbyUpdate;
            network.OnGameStarted -= HandleGameStarted;
            network.OnCardDrawn -= HandleCardDrawn;
            network.OnGamePaused -= HandleGamePaused;
            network.OnGameResumed -= HandleGameResumed;
            network.OnGameOver -= HandleGameOver;
            network.OnGameReset -= HandleGameReset;
            network.OnGameError -= HandleGameError;
        }

        // Public API
        public void CreateRoom()
        {
            Debug.Log($"[GameManager] CreateRoom called. NetworkManager.Instance={NetworkManager.Instance != null}");
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[GameManager] NetworkManager.Instance is null!");
                return;
            }
            NetworkManager.Instance.CreateRoom();
        }

        public void StartGame()
        {
            Debug.Log($"[GameManager] StartGame called. Players.Count={Players.Count}");

            if (Players.Count < 1)
            {
                Debug.LogWarning("[GameManager] Cannot start - need at least 1 player");
                OnError?.Invoke("Need at least 1 player to start");
                return;
            }

            Debug.Log($"[GameManager] Starting game with patterns={string.Join(", ", winPatterns)}, speed={drawSpeed}");
            NetworkManager.Instance?.StartGame(winPatterns, drawSpeed);
        }

        public void DrawNextCard()
        {
            NetworkManager.Instance?.DrawCard();
        }

        public void PauseGame()
        {
            NetworkManager.Instance?.PauseGame();
        }

        public void ResumeGame()
        {
            NetworkManager.Instance?.ResumeGame();
        }

        public void ResetGame()
        {
            NetworkManager.Instance?.ResetGame();
        }

        public void SetWinPatterns(string[] patterns)
        {
            winPatterns = patterns;
        }

        public void SetDrawSpeed(int seconds)
        {
            drawSpeed = Mathf.Clamp(seconds, 4, 12);
        }

        public void SetLanguage(string lang)
        {
            language = lang;
        }

        public void SetAutoDrawCards(bool auto)
        {
            autoDrawCards = auto;
        }

        // Event Handlers
        private void HandleRoomCreated(string roomCode)
        {
            CurrentPhase = GamePhase.Waiting;
            Players.Clear();
            CurrentCard = null;
            CardsDrawn = 0;
            GameWinner = null;

            OnRoomCodeChanged?.Invoke(roomCode);
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        private void HandlePlayerJoined(Player player)
        {
            Players.Add(player);
            OnPlayerJoined?.Invoke(player);
            OnPlayersUpdated?.Invoke(Players);
        }

        private void HandlePlayerLeft(string playerId, string playerName)
        {
            Players.RemoveAll(p => p.id == playerId);
            OnPlayerLeft?.Invoke(playerName);
            OnPlayersUpdated?.Invoke(Players);
        }

        private void HandleLobbyUpdate(Player[] players)
        {
            Debug.Log($"[GameManager] HandleLobbyUpdate received {players?.Length ?? 0} players");
            Players.Clear();
            Players.AddRange(players);
            Debug.Log($"[GameManager] Players list now has {Players.Count} players");
            OnPlayersUpdated?.Invoke(Players);
        }

        private void HandleGameStarted(GameStartedData data)
        {
            CurrentPhase = GamePhase.Playing;
            TotalCards = data.totalCards;
            CardsDrawn = 0;
            CurrentCard = null;

            // Store win patterns from server
            if (data.winPatterns != null && data.winPatterns.Length > 0)
            {
                winPatterns = data.winPatterns;
            }

            // Use drawSpeed from server (0 = manual draw)
            drawSpeed = data.drawSpeed;
            autoDrawCards = drawSpeed > 0;
            isDrawing = autoDrawCards;
            drawTimer = autoDrawCards ? drawSpeed : 0;

            Debug.Log($"[GameManager] Game started - patterns={string.Join(", ", winPatterns)}, drawSpeed={drawSpeed}, autoDrawCards={autoDrawCards}");

            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        private void HandleCardDrawn(Card card, int cardNumber, int totalCards)
        {
            CurrentCard = card;
            CardsDrawn = cardNumber;
            TotalCards = totalCards;

            OnCardDrawn?.Invoke(card, cardNumber, totalCards);
        }

        private void HandleGamePaused()
        {
            CurrentPhase = GamePhase.Paused;
            isDrawing = false;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        private void HandleGameResumed()
        {
            CurrentPhase = GamePhase.Playing;
            // Only auto-draw if not in manual mode
            isDrawing = autoDrawCards;
            if (autoDrawCards)
            {
                drawTimer = drawSpeed;
            }
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        private void HandleGameOver(GameOverData data)
        {
            CurrentPhase = GamePhase.Finished;
            isDrawing = false;
            GameWinner = data.winner;

            OnPhaseChanged?.Invoke(CurrentPhase);
            if (data.winner != null)
            {
                OnWinnerDeclared?.Invoke(data.winner);
            }
        }

        private void HandleGameReset()
        {
            CurrentPhase = GamePhase.Waiting;
            CurrentCard = null;
            CardsDrawn = 0;
            GameWinner = null;
            isDrawing = false;

            OnPhaseChanged?.Invoke(CurrentPhase);
            OnGameReset?.Invoke();
        }

        private void HandleGameError(string message)
        {
            OnError?.Invoke(message);
        }
    }
}
