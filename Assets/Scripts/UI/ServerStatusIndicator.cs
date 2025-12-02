using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using PartyLoteria.Network;

namespace PartyLoteria.UI
{
    /// <summary>
    /// Small indicator button that shows server connection status.
    /// Clicking it wakes up the server (useful for Cloud Run cold starts).
    /// </summary>
    public class ServerStatusIndicator : MonoBehaviour
    {
        [Header("Status Icons")]
        [SerializeField] private string sleepingIcon = "zzZ";
        [SerializeField] private string connectingIcon = "...";
        [SerializeField] private string awakeIcon = "OK";

        [Header("Colors")]
        [SerializeField] private Color sleepingColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color connectingColor = new Color(0.96f, 0.76f, 0.05f);
        [SerializeField] private Color awakeColor = new Color(0.3f, 0.8f, 0.4f);

        [Header("Settings")]
        [SerializeField] private float pingTimeout = 10f;
        [SerializeField] private string serverUrl = "https://party-loteria-ircg2u7krq-uc.a.run.app";

        private Button button;
        private TextMeshProUGUI iconText;
        private Image backgroundImage;

        private bool isPinging = false;
        private Coroutine pingCoroutine;

        private void Start()
        {
            button = GetComponent<Button>();
            iconText = GetComponentInChildren<TextMeshProUGUI>();
            backgroundImage = GetComponent<Image>();

            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }

            SetStatus(ServerStatus.Sleeping);

            // Subscribe to network events when NetworkManager becomes available
            StartCoroutine(WaitForNetworkManager());
        }

        private IEnumerator WaitForNetworkManager()
        {
            // Wait for NetworkManager to be created
            while (NetworkManager.Instance == null)
            {
                yield return null;
            }

            NetworkManager.Instance.OnConnected += OnServerConnected;
            NetworkManager.Instance.OnDisconnected += OnServerDisconnected;

            // If already connected, show awake status
            if (NetworkManager.Instance.IsConnected)
            {
                SetStatus(ServerStatus.Awake);
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnected -= OnServerConnected;
                NetworkManager.Instance.OnDisconnected -= OnServerDisconnected;
            }

            if (pingCoroutine != null)
            {
                StopCoroutine(pingCoroutine);
            }
        }

        private void OnServerConnected()
        {
            SetStatus(ServerStatus.Awake);
        }

        private void OnServerDisconnected()
        {
            SetStatus(ServerStatus.Sleeping);
        }

        private void OnButtonClick()
        {
            if (!isPinging)
            {
                WakeServer();
            }
        }

        /// <summary>
        /// Ping the server's health endpoint to wake it up from cold start
        /// </summary>
        public void WakeServer()
        {
            // Skip only if already pinging
            if (isPinging) return;

            if (pingCoroutine != null)
            {
                StopCoroutine(pingCoroutine);
            }
            pingCoroutine = StartCoroutine(PingServerCoroutine());
        }

        private IEnumerator PingServerCoroutine()
        {
            isPinging = true;
            SetStatus(ServerStatus.Connecting);

            string healthUrl = serverUrl.TrimEnd('/') + "/api/health";
            Debug.Log($"[ServerStatus] Pinging {healthUrl}...");

            using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
            {
                request.timeout = (int)pingTimeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[ServerStatus] Server is warm: {request.downloadHandler.text}");
                    SetStatus(ServerStatus.Awake);
                }
                else
                {
                    Debug.LogWarning($"[ServerStatus] Ping failed: {request.error}");
                    SetStatus(ServerStatus.Sleeping);
                }
            }

            isPinging = false;
        }

        private void SetStatus(ServerStatus status)
        {
            if (iconText == null) return;

            switch (status)
            {
                case ServerStatus.Sleeping:
                    iconText.text = sleepingIcon;
                    if (backgroundImage != null) backgroundImage.color = sleepingColor;
                    break;

                case ServerStatus.Connecting:
                    iconText.text = connectingIcon;
                    if (backgroundImage != null) backgroundImage.color = connectingColor;
                    break;

                case ServerStatus.Awake:
                    iconText.text = awakeIcon;
                    if (backgroundImage != null) backgroundImage.color = awakeColor;
                    break;
            }
        }

        private enum ServerStatus
        {
            Sleeping,
            Connecting,
            Awake
        }
    }
}
