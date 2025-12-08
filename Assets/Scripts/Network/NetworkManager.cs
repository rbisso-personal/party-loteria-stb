using System;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using PartyLoteria.Data;

#if !UNITY_WEBGL || UNITY_EDITOR
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Cysharp.Threading.Tasks;
#endif

namespace PartyLoteria.Network
{
    public enum ServerEnvironment
    {
        Production,
        Staging,
        Local
    }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Environment")]
        [Tooltip("Production: itch.io live servers\nStaging: Test servers\nLocal: Your dev machine")]
        [SerializeField] private ServerEnvironment environment = ServerEnvironment.Production;

        [Header("Connection")]
        [SerializeField] private bool autoConnect = true;

        [Header("Local Development Ports")]
        [SerializeField] private int localServerPort = 3001;
        [SerializeField] private int localPlayerClientPort = 5173;

        // Environment URLs
        private const string PROD_SERVER = "https://party-loteria-ircg2u7krq-uc.a.run.app";
        private const string PROD_CLIENT = "https://party-loteria-client.netlify.app";
        private const string STAGING_SERVER = "https://party-loteria-staging-866817394440.us-central1.run.app";
        private const string STAGING_CLIENT = "https://staging-party-loteria.netlify.app";

        public ServerEnvironment Environment => environment;

#if !UNITY_WEBGL || UNITY_EDITOR
        private SocketIOUnity socket;
#else
        private WebGLSocketBridge webglBridge;
#endif

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

#if !UNITY_WEBGL || UNITY_EDITOR
        public bool IsConnected => socket?.Connected ?? false;
#else
        public bool IsConnected => webglBridge?.IsConnected ?? false;
#endif

        public string CurrentRoomCode { get; private set; }

        private string localIpCache;

        /// <summary>
        /// Returns the server URL based on environment setting
        /// </summary>
        public string EffectiveServerUrl
        {
            get
            {
                switch (environment)
                {
                    case ServerEnvironment.Staging:
                        return STAGING_SERVER;
                    case ServerEnvironment.Local:
                        return $"http://{GetLocalIP()}:{localServerPort}";
                    default:
                        return PROD_SERVER;
                }
            }
        }

        /// <summary>
        /// Returns the client URL based on environment setting
        /// </summary>
        public string EffectiveClientUrl
        {
            get
            {
                switch (environment)
                {
                    case ServerEnvironment.Staging:
                        return STAGING_CLIENT;
                    case ServerEnvironment.Local:
                        return $"http://{GetLocalIP()}:{localPlayerClientPort}";
                    default:
                        return PROD_CLIENT;
                }
            }
        }

        private string GetLocalIP()
        {
            if (localIpCache == null)
            {
                localIpCache = GetLocalIPAddress() ?? "localhost";
            }
            return localIpCache;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Show environment indicator for non-production
            if (environment != ServerEnvironment.Production)
            {
                CreateEnvironmentIndicator();
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            webglBridge = gameObject.AddComponent<WebGLSocketBridge>();
            SetupWebGLEventHandlers();
#endif
        }

        private void Start()
        {
            // Setup player client URL for QR codes
            SetupPlayerClientUrl();

            if (autoConnect)
            {
                Connect();
            }
        }

        private void SetupPlayerClientUrl()
        {
            UI.LobbyScreenController.SetPlayerUrlBase(EffectiveClientUrl);
            Debug.Log($"[Network] Player client URL: {EffectiveClientUrl}");

            // Update the visible URL text in UIBuilder
            var uiBuilder = GetComponent<UI.UIBuilder>();
            if (uiBuilder != null)
            {
                uiBuilder.SetPlayerUrl(EffectiveClientUrl);
            }

            // Only pass server URL in QR code for local testing
            if (environment == ServerEnvironment.Local)
            {
                UI.LobbyScreenController.SetServerUrl(EffectiveServerUrl);
                Debug.Log($"[Network] Server URL for QR: {EffectiveServerUrl}");
            }
            else
            {
                UI.LobbyScreenController.SetServerUrl(null);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            ConnectDesktop();
#else
            ConnectWebGL();
#endif
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private void ConnectDesktop()
        {
            if (socket != null && socket.Connected)
            {
                Debug.Log("[Network] Already connected");
                return;
            }

            Debug.Log($"[Network] Connecting to {EffectiveServerUrl}...");

            var uri = new Uri(EffectiveServerUrl);
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

            SetupDesktopEventHandlers();
            socket.Connect();
        }

        private void SetupDesktopEventHandlers()
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
                // Server sends { players, hostId } object
                var data = response.GetValue<LobbyUpdateData>();
                var players = data?.players ?? new Player[0];
                Debug.Log($"[Network] Lobby update: {players.Length} players, hostId={data?.hostId}");
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
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        private void ConnectWebGL()
        {
            if (webglBridge != null && webglBridge.IsConnected)
            {
                Debug.Log("[Network] Already connected");
                return;
            }

            Debug.Log($"[Network] WebGL connecting to {EffectiveServerUrl}...");
            webglBridge.Connect(EffectiveServerUrl);
        }

        private void SetupWebGLEventHandlers()
        {
            webglBridge.OnConnected += () => OnConnected?.Invoke();
            webglBridge.OnDisconnected += () => OnDisconnected?.Invoke();
            webglBridge.OnConnectionError += (error) => OnConnectionError?.Invoke(error);

            webglBridge.OnRoomCreated += (roomCode) =>
            {
                CurrentRoomCode = roomCode;
                OnRoomCreated?.Invoke(roomCode);
            };
            webglBridge.OnPlayerJoined += (player) => OnPlayerJoined?.Invoke(player);
            webglBridge.OnPlayerLeft += (playerId, playerName) => OnPlayerLeft?.Invoke(playerId, playerName);
            webglBridge.OnLobbyUpdate += (players) => OnLobbyUpdate?.Invoke(players);

            webglBridge.OnGameStarted += (data) => OnGameStarted?.Invoke(data);
            webglBridge.OnCardDrawn += (card, cardNum, total) => OnCardDrawn?.Invoke(card, cardNum, total);
            webglBridge.OnGamePaused += () => OnGamePaused?.Invoke();
            webglBridge.OnGameResumed += () => OnGameResumed?.Invoke();
            webglBridge.OnGameOver += (data) => OnGameOver?.Invoke(data);
            webglBridge.OnGameReset += () => OnGameReset?.Invoke();
            webglBridge.OnGameError += (msg) => OnGameError?.Invoke(msg);
        }
#endif

        public void Disconnect()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (socket != null)
            {
                socket.Disconnect();
                socket.Dispose();
                socket = null;
            }
#else
            webglBridge?.Disconnect();
#endif
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
#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("create-room");
#else
            webglBridge.Emit("create-room");
#endif
        }

        public void StartGame(string winPattern = "line", int drawSpeed = 8)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Not connected, cannot start game");
                return;
            }

            Debug.Log($"[Network] Starting game (pattern: {winPattern}, speed: {drawSpeed}s)");
#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("start-game", new { winPattern, drawSpeed });
#else
            webglBridge.Emit("start-game", new { winPattern, drawSpeed });
#endif
        }

        public void DrawCard()
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Not connected, cannot draw card");
                return;
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("draw-card");
#else
            webglBridge.Emit("draw-card");
#endif
        }

        public void PauseGame()
        {
            if (!IsConnected) return;
#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("pause-game");
#else
            webglBridge.Emit("pause-game");
#endif
        }

        public void ResumeGame()
        {
            if (!IsConnected) return;
#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("resume-game");
#else
            webglBridge.Emit("resume-game");
#endif
        }

        public void ResetGame()
        {
            if (!IsConnected) return;
#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("reset-game");
#else
            webglBridge.Emit("reset-game");
#endif
        }

        public void CloseRoom()
        {
            if (!IsConnected) return;
#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("disconnect-set-top-box");
#else
            webglBridge.Emit("disconnect-set-top-box");
#endif
            CurrentRoomCode = null;
        }

        /// <summary>
        /// Gets the local IP address by detecting the active network interface via UDP socket.
        /// </summary>
        private string GetLocalIPAddress()
        {
            try
            {
                using (var socket = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.InterNetwork,
                    System.Net.Sockets.SocketType.Dgram,
                    System.Net.Sockets.ProtocolType.Udp))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                    if (endPoint != null)
                    {
                        string ip = endPoint.Address.ToString();
                        Debug.Log($"[Network] Detected local IP: {ip}");
                        return ip;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Network] UDP IP detection failed: {ex.Message}");
            }

            // Fallback to DNS lookup
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string ipStr = ip.ToString();
                        if (ipStr.StartsWith("192.168.") || ipStr.StartsWith("10.") || ipStr.StartsWith("172."))
                        {
                            return ipStr;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Network] DNS fallback failed: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Creates a UGUI overlay showing environment indicator
        /// </summary>
        private void CreateEnvironmentIndicator()
        {
            var canvasObj = new GameObject("EnvironmentIndicator");
            canvasObj.transform.SetParent(transform);
            // No DontDestroyOnLoad needed - parent (NetworkManager) already has it

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

            var panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);

            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(200, 40);

            var panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();

            string envText;
            switch (environment)
            {
                case ServerEnvironment.Staging:
                    panelImage.color = new Color(1f, 0.6f, 0f, 0.9f); // Orange
                    envText = "STAGING";
                    break;
                case ServerEnvironment.Local:
                    panelImage.color = new Color(1f, 0.3f, 0.3f, 0.9f); // Red
                    envText = "LOCAL";
                    break;
                default:
                    return;
            }

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(panelObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = envText;
            text.fontSize = 24;
            text.color = Color.white;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;

            Debug.Log($"[Network] Environment: {environment} → {EffectiveServerUrl}");
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
#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("debug-force-win", new { playerId });
#else
            webglBridge.Emit("debug-force-win", new { playerId });
#endif
        }

        public void DebugTriggerLoteria(string playerId)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Not connected, cannot trigger loteria");
                return;
            }

            Debug.Log($"[Network] Debug: Triggering loteria for player {playerId}");
#if !UNITY_WEBGL || UNITY_EDITOR
            socket.Emit("debug-trigger-loteria", new { playerId });
#else
            webglBridge.Emit("debug-trigger-loteria", new { playerId });
#endif
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
