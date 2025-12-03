using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Newtonsoft.Json;
using PartyLoteria.Data;

namespace PartyLoteria.Network
{
    /// <summary>
    /// Bridge between Unity C# and the WebGL JavaScript Socket.IO plugin.
    /// This MonoBehaviour receives callbacks from JavaScript via SendMessage.
    /// </summary>
    public class WebGLSocketBridge : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SocketIO_Init(string gameObjectName);

        [DllImport("__Internal")]
        private static extern void SocketIO_Connect(string url);

        [DllImport("__Internal")]
        private static extern void SocketIO_Disconnect();

        [DllImport("__Internal")]
        private static extern void SocketIO_Emit(string eventName, string data);

        [DllImport("__Internal")]
        private static extern int SocketIO_IsConnected();
#endif

        public static WebGLSocketBridge Instance { get; private set; }

        // Connection state
        public bool IsConnected { get; private set; }
        public string CurrentRoomCode { get; private set; }

        // Connection events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnConnectionError;

        // Room events
        public event Action<string> OnRoomCreated;
        public event Action<Player> OnPlayerJoined;
        public event Action<string, string> OnPlayerLeft;
        public event Action<Player[]> OnLobbyUpdate;

        // Game events
        public event Action<GameStartedData> OnGameStarted;
        public event Action<Card, int, int> OnCardDrawn;
        public event Action<WinClaimedData> OnWinClaimed;
        public event Action<WinVerifiedData> OnWinVerified;
        public event Action<WinRejectedData> OnWinRejected;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<GameOverData> OnGameOver;
        public event Action OnGameReset;
        public event Action<string> OnGameError;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
            // Register this GameObject with the JavaScript plugin
            SocketIO_Init(gameObject.name);
            Debug.Log($"[WebGLSocket] Initialized bridge on GameObject: {gameObject.name}");
#endif
        }

        public void Connect(string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log($"[WebGLSocket] Connecting to {url}...");
            SocketIO_Connect(url);
#else
            Debug.LogWarning("[WebGLSocket] WebGL socket only works in WebGL builds");
#endif
        }

        public void Disconnect()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[WebGLSocket] Disconnecting...");
            SocketIO_Disconnect();
            IsConnected = false;
#endif
        }

        public void Emit(string eventName, object data = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string jsonData = data != null ? JsonConvert.SerializeObject(data) : "";
            Debug.Log($"[WebGLSocket] Emit: {eventName} {jsonData}");
            SocketIO_Emit(eventName, jsonData);
#else
            Debug.LogWarning($"[WebGLSocket] WebGL emit only works in WebGL builds: {eventName}");
#endif
        }

        public bool CheckConnected()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return SocketIO_IsConnected() == 1;
#else
            return false;
#endif
        }

        // ===== JavaScript Callbacks (called via SendMessage from jslib) =====

        public void OnWebGLConnected(string socketId)
        {
            Debug.Log($"[WebGLSocket] Connected! Socket ID: {socketId}");
            IsConnected = true;
            OnConnected?.Invoke();
        }

        public void OnWebGLDisconnected(string reason)
        {
            Debug.Log($"[WebGLSocket] Disconnected: {reason}");
            IsConnected = false;
            OnDisconnected?.Invoke();
        }

        public void OnWebGLConnectionError(string error)
        {
            Debug.LogError($"[WebGLSocket] Connection error: {error}");
            OnConnectionError?.Invoke(error);
        }

        public void OnWebGLRoomCreated(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<RoomCreatedData>(jsonData);
                CurrentRoomCode = data.roomCode;
                Debug.Log($"[WebGLSocket] Room created: {data.roomCode}");
                OnRoomCreated?.Invoke(data.roomCode);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing room-created: {ex.Message}");
            }
        }

        public void OnWebGLPlayerJoined(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<PlayerJoinedData>(jsonData);
                Debug.Log($"[WebGLSocket] Player joined: {data.player.name}");
                OnPlayerJoined?.Invoke(data.player);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing player-joined: {ex.Message}");
            }
        }

        public void OnWebGLPlayerLeft(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<PlayerLeftData>(jsonData);
                Debug.Log($"[WebGLSocket] Player left: {data.playerName}");
                OnPlayerLeft?.Invoke(data.playerId, data.playerName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing player-left: {ex.Message}");
            }
        }

        public void OnWebGLLobbyUpdate(string jsonData)
        {
            try
            {
                // Server sends { players, hostId } object
                var data = JsonConvert.DeserializeObject<LobbyUpdateData>(jsonData);
                var players = data?.players ?? new Player[0];
                Debug.Log($"[WebGLSocket] Lobby update: {players.Length} players, hostId={data?.hostId}");
                OnLobbyUpdate?.Invoke(players);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing lobby-update: {ex.Message}");
            }
        }

        public void OnWebGLGameStarted(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<GameStartedData>(jsonData);
                Debug.Log($"[WebGLSocket] Game started with {data.playerCount} players");
                OnGameStarted?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing game-started: {ex.Message}");
            }
        }

        public void OnWebGLCardDrawn(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<CardDrawnData>(jsonData);
                Debug.Log($"[WebGLSocket] Card drawn: {data.card.name_es} ({data.cardNumber}/{data.totalCards})");
                OnCardDrawn?.Invoke(data.card, data.cardNumber, data.totalCards);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing card-drawn: {ex.Message}");
            }
        }

        public void OnWebGLWinClaimed(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<WinClaimedData>(jsonData);
                Debug.Log($"[WebGLSocket] Win claimed by: {data.playerName}");
                OnWinClaimed?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing win-claimed: {ex.Message}");
            }
        }

        public void OnWebGLWinVerified(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<WinVerifiedData>(jsonData);
                Debug.Log($"[WebGLSocket] Win verified for: {data.playerName}");
                OnWinVerified?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing win-verified: {ex.Message}");
            }
        }

        public void OnWebGLWinRejected(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<WinRejectedData>(jsonData);
                Debug.Log($"[WebGLSocket] Win rejected: {data.reason}");
                OnWinRejected?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing win-rejected: {ex.Message}");
            }
        }

        public void OnWebGLGamePaused(string unused)
        {
            Debug.Log("[WebGLSocket] Game paused");
            OnGamePaused?.Invoke();
        }

        public void OnWebGLGameResumed(string unused)
        {
            Debug.Log("[WebGLSocket] Game resumed");
            OnGameResumed?.Invoke();
        }

        public void OnWebGLGameOver(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<GameOverData>(jsonData);
                Debug.Log($"[WebGLSocket] Game over: {data.reason}");
                OnGameOver?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSocket] Error parsing game-over: {ex.Message}");
            }
        }

        public void OnWebGLGameReset(string unused)
        {
            Debug.Log("[WebGLSocket] Game reset");
            CurrentRoomCode = null;
            OnGameReset?.Invoke();
        }

        public void OnWebGLGameError(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<GameErrorData>(jsonData);
                Debug.LogWarning($"[WebGLSocket] Game error: {data.message}");
                OnGameError?.Invoke(data.message);
            }
            catch (Exception ex)
            {
                // If it's just a plain string
                Debug.LogWarning($"[WebGLSocket] Game error: {jsonData}");
                OnGameError?.Invoke(jsonData);
            }
        }
    }
}
