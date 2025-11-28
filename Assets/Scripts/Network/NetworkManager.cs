using System;
using System.Threading;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Newtonsoft.Json;
using PartyLoteria.Data;
using Cysharp.Threading.Tasks;

namespace PartyLoteria.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Server Configuration")]
        [SerializeField] private string serverUrl = "http://localhost:3001";
        [SerializeField] private bool autoConnect = true;

        private SocketIOUnity socket;

        // Connection events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnConnectionError;
        public event Action<int> OnReconnectAttempt;

        // Room events
        public event Action<string> OnRoomCreated;
        public event Action<Player> OnPlayerJoined;
        public event Action<string, string> OnPlayerLeft;
        public event Action<Player[]> OnLobbyUpdate;

        // Game events
        public event Action<GameStartedData> OnGameStarted;
        public event Action<Card, int, int> OnCardDrawn;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<GameOverData> OnGameOver;
        public event Action OnGameReset;
        public event Action<string> OnGameError;

        public bool IsConnected => socket?.Connected ?? false;
        public string CurrentRoomCode { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoConnect)
            {
                Connect();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect()
        {
            if (socket != null && socket.Connected)
            {
                Debug.Log("[Network] Already connected");
                return;
            }

            Debug.Log($"[Network] Connecting to {serverUrl}...");

            var uri = new Uri(serverUrl);
            socket = new SocketIOUnity(uri, new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Reconnection = true,
                ReconnectionAttempts = 10,
                ReconnectionDelay = 1000,
                ReconnectionDelayMax = 5000
            });

            // Use Newtonsoft.Json for serialization
            socket.JsonSerializer = new NewtonsoftJsonSerializer();

            SetupEventHandlers();
            socket.Connect();
        }

        public void Disconnect()
        {
            if (socket != null)
            {
                socket.Disconnect();
                socket.Dispose();
                socket = null;
            }
        }

        private void SetupEventHandlers()
        {
            // Connection events
            socket.OnConnected += (sender, e) =>
            {
                Debug.Log("[Network] Connected to server");
                UnityMainThread.Execute(() => OnConnected?.Invoke());
            };

            socket.OnDisconnected += (sender, e) =>
            {
                Debug.Log($"[Network] Disconnected: {e}");
                UnityMainThread.Execute(() => OnDisconnected?.Invoke());
            };

            socket.OnError += (sender, e) =>
            {
                Debug.LogError($"[Network] Error: {e}");
                UnityMainThread.Execute(() => OnConnectionError?.Invoke(e));
            };

            socket.OnReconnectAttempt += (sender, attempt) =>
            {
                Debug.Log($"[Network] Reconnect attempt {attempt}");
                UnityMainThread.Execute(() => OnReconnectAttempt?.Invoke(attempt));
            };

            // Room events
            socket.On("room-created", response =>
            {
                try
                {
                    Debug.Log($"[Network] Raw room-created response: {response}");
                    var data = response.GetValue<RoomCreatedData>();
                    CurrentRoomCode = data.roomCode;
                    Debug.Log($"[Network] Room created: {data.roomCode}");
                    UnityMainThread.Execute(() => OnRoomCreated?.Invoke(data.roomCode));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Network] Error parsing room-created: {ex.Message}\n{ex.StackTrace}");
                }
            });

            socket.On("player-joined", response =>
            {
                var data = response.GetValue<PlayerJoinedData>();
                Debug.Log($"[Network] Player joined: {data.player.name}");
                UnityMainThread.Execute(() => OnPlayerJoined?.Invoke(data.player));
            });

            socket.On("player-left", response =>
            {
                var data = response.GetValue<PlayerLeftData>();
                Debug.Log($"[Network] Player left: {data.playerName}");
                UnityMainThread.Execute(() => OnPlayerLeft?.Invoke(data.playerId, data.playerName));
            });

            socket.On("update-lobby", response =>
            {
                // Server sends raw array, not wrapped object
                var players = response.GetValue<Player[]>();
                Debug.Log($"[Network] Lobby update: {players.Length} players");
                UnityMainThread.Execute(() => OnLobbyUpdate?.Invoke(players));
            });

            // Game events
            socket.On("game-started", response =>
            {
                var data = response.GetValue<GameStartedData>();
                Debug.Log($"[Network] Game started with {data.playerCount} players");
                UnityMainThread.Execute(() => OnGameStarted?.Invoke(data));
            });

            socket.On("card-drawn", response =>
            {
                var data = response.GetValue<CardDrawnData>();
                Debug.Log($"[Network] Card drawn: {data.card.name_es} ({data.cardNumber}/{data.totalCards})");
                UnityMainThread.Execute(() => OnCardDrawn?.Invoke(data.card, data.cardNumber, data.totalCards));
            });

            socket.On("game-paused", response =>
            {
                Debug.Log("[Network] Game paused");
                UnityMainThread.Execute(() => OnGamePaused?.Invoke());
            });

            socket.On("game-resumed", response =>
            {
                Debug.Log("[Network] Game resumed");
                UnityMainThread.Execute(() => OnGameResumed?.Invoke());
            });

            socket.On("game-over", response =>
            {
                var data = response.GetValue<GameOverData>();
                Debug.Log($"[Network] Game over: {data.reason}");
                UnityMainThread.Execute(() => OnGameOver?.Invoke(data));
            });

            socket.On("game-reset", response =>
            {
                Debug.Log("[Network] Game reset");
                UnityMainThread.Execute(() => OnGameReset?.Invoke());
            });

            socket.On("game-error", response =>
            {
                var data = response.GetValue<GameErrorData>();
                Debug.LogWarning($"[Network] Game error: {data.message}");
                UnityMainThread.Execute(() => OnGameError?.Invoke(data.message));
            });
        }

        // STB Actions
        public void CreateRoom()
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Not connected, cannot create room");
                return;
            }

            Debug.Log("[Network] Creating room...");
            socket.Emit("create-room");
        }

        public void StartGame(string winPattern = "line", int drawSpeed = 8)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Not connected, cannot start game");
                return;
            }

            Debug.Log($"[Network] Starting game (pattern: {winPattern}, speed: {drawSpeed}s)");
            socket.Emit("start-game", new { winPattern, drawSpeed });
        }

        public void DrawCard()
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Not connected, cannot draw card");
                return;
            }

            socket.Emit("draw-card");
        }

        public void PauseGame()
        {
            if (!IsConnected) return;
            socket.Emit("pause-game");
        }

        public void ResumeGame()
        {
            if (!IsConnected) return;
            socket.Emit("resume-game");
        }

        public void ResetGame()
        {
            if (!IsConnected) return;
            socket.Emit("reset-game");
        }

        public void CloseRoom()
        {
            if (!IsConnected) return;
            socket.Emit("disconnect-set-top-box");
            CurrentRoomCode = null;
        }

        // Debug Actions
        public void DebugForceWin(string playerId)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Not connected, cannot force win");
                return;
            }

            Debug.Log($"[Network] Debug: Forcing win for player {playerId}");
            socket.Emit("debug-force-win", new { playerId });
        }

        public void DebugTriggerLoteria(string playerId)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Not connected, cannot trigger loteria");
                return;
            }

            Debug.Log($"[Network] Debug: Triggering loteria for player {playerId}");
            socket.Emit("debug-trigger-loteria", new { playerId });
        }
    }

    /// <summary>
    /// Helper to execute actions on the Unity main thread
    /// </summary>
    public static class UnityMainThread
    {
        private static SynchronizationContext mainThreadContext;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            mainThreadContext = SynchronizationContext.Current;
        }

        public static void Execute(Action action)
        {
            if (mainThreadContext != null)
            {
                mainThreadContext.Post(_ => action?.Invoke(), null);
            }
            else
            {
                action?.Invoke();
            }
        }
    }
}
