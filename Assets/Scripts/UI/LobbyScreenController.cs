using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PartyLoteria.Data;
using PartyLoteria.Game;
using PartyLoteria.Utils;

namespace PartyLoteria.UI
{
    public class LobbyScreenController : MonoBehaviour
    {
        private TextMeshProUGUI roomCodeText;
        private TextMeshProUGUI playerCountText;
        private Transform playerListContainer;
        private Button startGameButton;
        private Slider drawSpeedSlider;
        private TextMeshProUGUI drawSpeedValueText;
        private RawImage qrCodeImage;

        private List<GameObject> playerEntries = new List<GameObject>();
        private bool isSetup = false;
        private Texture2D qrCodeTexture;

        // Player client URL - set this in the inspector or via code
        // For local testing, call SetPlayerUrlBase() with your local IP (e.g., "http://192.168.1.100:5173")
        private static string playerUrlBase = "https://party-loteria-client.netlify.app";
        // Server URL for the player client to connect to (null = use client's default)
        private static string serverUrlOverride = null;
        private static LobbyScreenController activeInstance;

        /// <summary>
        /// Set the player client URL base for QR code generation.
        /// Call this before room creation to set the correct URL for your network.
        /// </summary>
        public static void SetPlayerUrlBase(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[Lobby] SetPlayerUrlBase called with null/empty URL, ignoring");
                return;
            }
            playerUrlBase = url.TrimEnd('/');
            Debug.Log($"[Lobby] Player URL base set to: {playerUrlBase}");

            // Refresh QR code if lobby is currently active
            if (activeInstance != null && activeInstance.gameObject.activeInHierarchy)
            {
                activeInstance.RefreshQRCode();
            }
        }

        /// <summary>
        /// Set the server URL that the player client should connect to.
        /// This is passed as a query parameter in the QR code URL.
        /// </summary>
        public static void SetServerUrl(string url)
        {
            serverUrlOverride = url?.TrimEnd('/');
            Debug.Log($"[Lobby] Server URL override set to: {serverUrlOverride}");

            // Refresh QR code if lobby is currently active
            if (activeInstance != null && activeInstance.gameObject.activeInHierarchy)
            {
                activeInstance.RefreshQRCode();
            }
        }

        private void RefreshQRCode()
        {
            var game = Game.GameManager.Instance;
            if (game != null && !string.IsNullOrEmpty(game.RoomCode))
            {
                UpdateQRCode(game.RoomCode);
            }
        }

        public void Setup(
            TextMeshProUGUI roomCode,
            TextMeshProUGUI playerCount,
            Transform playerList,
            Button startButton,
            Slider speedSlider,
            TextMeshProUGUI speedValue,
            RawImage qrCode)
        {
            roomCodeText = roomCode;
            playerCountText = playerCount;
            playerListContainer = playerList;
            startGameButton = startButton;
            drawSpeedSlider = speedSlider;
            drawSpeedValueText = speedValue;
            qrCodeImage = qrCode;
            isSetup = true;

            SetupControls();
        }

        private void OnEnable()
        {
            activeInstance = this;
            SubscribeToEvents();
            if (isSetup)
            {
                UpdateRoomCode();
                UpdatePlayerList();
            }
        }

        private void OnDisable()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            var game = GameManager.Instance;
            if (game != null)
            {
                game.OnRoomCodeChanged += HandleRoomCodeChanged;
                game.OnPlayersUpdated += HandlePlayersUpdated;
                game.OnPlayerJoined += HandlePlayerJoined;
                game.OnPlayerLeft += HandlePlayerLeft;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var game = GameManager.Instance;
            if (game != null)
            {
                game.OnRoomCodeChanged -= HandleRoomCodeChanged;
                game.OnPlayersUpdated -= HandlePlayersUpdated;
                game.OnPlayerJoined -= HandlePlayerJoined;
                game.OnPlayerLeft -= HandlePlayerLeft;
            }
        }

        private void SetupControls()
        {
            // NOTE: Start button and draw speed controls removed - host controls game from mobile device
            // Hide these controls since STB is now display-only
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(false);
            }

            if (drawSpeedSlider != null)
            {
                drawSpeedSlider.gameObject.SetActive(false);
            }

            if (drawSpeedValueText != null)
            {
                drawSpeedValueText.gameObject.SetActive(false);
            }
        }

        private void HandleRoomCodeChanged(string roomCode)
        {
            UpdateRoomCode();
        }

        private void HandlePlayersUpdated(List<Player> players)
        {
            UpdatePlayerList();
        }

        private void HandlePlayerJoined(Player player)
        {
            UpdatePlayerList();
        }

        private void HandlePlayerLeft(string playerName)
        {
            UpdatePlayerList();
        }

        private void UpdateRoomCode()
        {
            var game = GameManager.Instance;
            if (game == null) return;

            string roomCode = game.RoomCode ?? "----";

            if (roomCodeText != null)
            {
                roomCodeText.text = roomCode;
            }

            UpdateQRCode(roomCode);
        }

        private void UpdateQRCode(string roomCode)
        {
            if (qrCodeImage == null) return;

            // Don't generate QR for placeholder
            if (string.IsNullOrEmpty(roomCode) || roomCode == "----")
            {
                qrCodeImage.texture = null;
                return;
            }

            // Clean up previous texture
            if (qrCodeTexture != null)
            {
                Destroy(qrCodeTexture);
            }

            // Generate QR code with room URL (and optional server override)
            string roomUrl = $"{playerUrlBase}?room={roomCode}";
            if (!string.IsNullOrEmpty(serverUrlOverride))
            {
                roomUrl += $"&server={UnityEngine.Networking.UnityWebRequest.EscapeURL(serverUrlOverride)}";
            }
            qrCodeTexture = QRCodeGenerator.Generate(roomUrl, 8);
            qrCodeImage.texture = qrCodeTexture;

            Debug.Log($"[Lobby] QR code generated for: {roomUrl}");
        }

        private void OnDestroy()
        {
            // Clean up texture
            if (qrCodeTexture != null)
            {
                Destroy(qrCodeTexture);
            }
        }

        private void UpdatePlayerList()
        {
            // Clear existing entries
            foreach (var entry in playerEntries)
            {
                Destroy(entry);
            }
            playerEntries.Clear();

            var game = GameManager.Instance;
            if (game == null) return;

            // Create new entries
            foreach (var player in game.Players)
            {
                if (playerListContainer != null)
                {
                    var entry = CreatePlayerEntry(player.name);
                    playerEntries.Add(entry);
                }
            }

            // Update player count
            int count = game.Players.Count;
            if (playerCountText != null)
            {
                playerCountText.text = $"{count} Player{(count != 1 ? "s" : "")}";
            }
        }

        private GameObject CreatePlayerEntry(string playerName)
        {
            var entry = new GameObject("PlayerEntry");
            entry.transform.SetParent(playerListContainer, false);

            var rect = entry.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 50);

            var bg = entry.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f);

            var text = new GameObject("Name").AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(entry.transform, false);
            text.text = playerName;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return entry;
        }

        // NOTE: OnStartGameClicked and OnDrawSpeedChanged removed
        // Host now controls these from the mobile client
    }
}
