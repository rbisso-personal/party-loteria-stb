using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using PartyLoteria.Utils;
using PartyLoteria.Config;

namespace PartyLoteria.UI
{
    /// <summary>
    /// Programmatically builds the entire UI at runtime using responsive layouts.
    /// Attach to a GameObject in your scene - it will create everything.
    /// </summary>
    public class UIBuilder : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        [SerializeField] private Color primaryColor = new Color(0.96f, 0.76f, 0.05f, 1f);
        [SerializeField] private Color secondaryColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color accentColor = new Color(0.3f, 0.7f, 0.4f, 1f);

        [Header("Typography Scale (base size 24)")]
        [SerializeField] private float fontScaleSmall = 0.75f;    // 18
        [SerializeField] private float fontScaleMedium = 1f;      // 24
        [SerializeField] private float fontScaleLarge = 1.5f;     // 36
        [SerializeField] private float fontScaleXLarge = 2f;      // 48
        [SerializeField] private float fontScaleHuge = 3f;        // 72
        [SerializeField] private float fontScaleGiant = 4f;       // 96
        private const float BASE_FONT_SIZE = 24f;

        [Header("Spacing")]
        [SerializeField] private float paddingSmall = 10f;
        [SerializeField] private float paddingMedium = 20f;
        [SerializeField] private float paddingLarge = 40f;

        [Header("Card Display")]
        [SerializeField] private float cardImageFlexibleHeight = 4f;

        private Canvas mainCanvas;
        private GameObject connectingScreen;
        private GameObject lobbyScreen;
        private GameObject gameScreen;
        private GameObject resultsScreen;

        // Controller references
        private TextMeshProUGUI roomCodeText;
        private TextMeshProUGUI playerCountText;
        private Transform playerListContainer;
        private Button startGameButton;
        private Slider drawSpeedSlider;
        private TextMeshProUGUI drawSpeedValueText;
        private RawImage qrCodeImage;
        private TextMeshProUGUI playerUrlText;

        private TextMeshProUGUI cardNameText;
        private TextMeshProUGUI cardVerseText;
        private TextMeshProUGUI cardNumberText;
        private Image cardBackgroundImage;
        private Image cardImage;
        private GameObject cardImageContainer;
        private TextMeshProUGUI progressText;
        private Slider progressSlider;
        private Button pauseButton;
        private Button resumeButton;
        private TextMeshProUGUI pausedText;
        private TextMeshProUGUI rulesText;

        private TextMeshProUGUI winnerNameText;
        private TextMeshProUGUI winnerMessageText;
        private GameObject winnerPanel;
        private GameObject noWinnerPanel;
        private Button playAgainButton;

        // Server status indicator
        private ServerStatusIndicator serverStatusIndicator;

        private void Awake()
        {
            BuildUI();
            SetupUIManager();
        }

        /// <summary>
        /// Update the displayed player URL (called by NetworkManager based on environment)
        /// </summary>
        public void SetPlayerUrl(string url)
        {
            if (playerUrlText != null && !string.IsNullOrEmpty(url))
            {
                // Strip protocol for cleaner display
                string displayUrl = url.Replace("https://", "").Replace("http://", "").TrimEnd('/');
                playerUrlText.text = displayUrl;
                Debug.Log($"[UIBuilder] Player URL updated to: {displayUrl}");
            }
        }

        private void BuildUI()
        {
            EnsureEventSystem();
            mainCanvas = CreateCanvas();

            connectingScreen = CreateConnectingScreen(mainCanvas.transform);
            lobbyScreen = CreateLobbyScreen(mainCanvas.transform);
            gameScreen = CreateGameScreen(mainCanvas.transform);
            resultsScreen = CreateResultsScreen(mainCanvas.transform);

            connectingScreen.SetActive(true);
            lobbyScreen.SetActive(false);
            gameScreen.SetActive(false);
            resultsScreen.SetActive(false);
        }

        private void SetupUIManager()
        {
            // GameConfig and NetworkManager are already on the Game object in the scene
            // GameManager is created dynamically as a root object (DontDestroyOnLoad requires root)
            var gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<Game.GameManager>();

            var uiManager = gameObject.AddComponent<UIManager>();
            uiManager.SetScreens(connectingScreen, lobbyScreen, gameScreen, resultsScreen);

            var lobbyController = lobbyScreen.AddComponent<LobbyScreenController>();
            var gameController = gameScreen.AddComponent<GameScreenController>();
            var resultsController = resultsScreen.AddComponent<ResultsScreenController>();

            SetupLobbyController(lobbyController);
            SetupGameController(gameController);
            SetupResultsController(resultsController);
        }

        private Canvas CreateCanvas()
        {
            var canvasObj = new GameObject("MainCanvas");
            canvasObj.transform.SetParent(transform);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[UIBuilder] Created EventSystem for UI input");
            }
        }

        private float FontSize(float scale) => BASE_FONT_SIZE * scale;

        #region Connecting Screen

        private GameObject CreateConnectingScreen(Transform parent)
        {
            var screen = CreateFullScreen(parent, "ConnectingScreen", backgroundColor);

            // Center container using layout
            var container = CreateCenteredPanel(screen.transform, "Container", secondaryColor, 400, 200);

            // Spinner
            var spinnerObj = new GameObject("Spinner");
            spinnerObj.transform.SetParent(container.transform, false);
            var spinnerImage = spinnerObj.AddComponent<Image>();
            spinnerImage.color = primaryColor;
            var spinnerRect = spinnerObj.GetComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.5f, 0.7f);
            spinnerRect.anchorMax = new Vector2(0.5f, 0.7f);
            spinnerRect.sizeDelta = new Vector2(60, 60);
            spinnerObj.AddComponent<SpinnerAnimation>();

            // Text
            var text = CreateText(container.transform, "ConnectingText", "Connecting to server...", FontSize(fontScaleMedium));
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.1f);
            textRect.anchorMax = new Vector2(1, 0.4f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return screen;
        }

        #endregion

        #region Lobby Screen

        private GameObject CreateLobbyScreen(Transform parent)
        {
            var screen = CreateFullScreen(parent, "LobbyScreen", backgroundColor);

            // Main vertical layout
            var mainLayout = CreateVerticalLayoutPanel(screen.transform, "MainLayout", Color.clear);
            StretchToParent(mainLayout, paddingMedium);
            var vlg = mainLayout.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = paddingMedium;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title row
            var titleRow = CreateLayoutElement(mainLayout.transform, "TitleRow", preferredHeight: 100);
            var title = CreateText(titleRow.transform, "Title", "LOTERÍA", FontSize(fontScaleGiant), FontStyles.Bold);
            title.color = primaryColor;
            StretchToParent(title.gameObject);

            // Join area row - QR + Room Code side by side
            var joinRow = CreateLayoutElement(mainLayout.transform, "JoinRow", flexibleHeight: 1);
            var joinBg = joinRow.AddComponent<Image>();
            joinBg.color = secondaryColor;

            var joinLayout = joinRow.AddComponent<HorizontalLayoutGroup>();
            joinLayout.padding = new RectOffset((int)paddingLarge, (int)paddingLarge, (int)paddingLarge, (int)paddingLarge);
            joinLayout.spacing = paddingLarge * 2;
            joinLayout.childAlignment = TextAnchor.MiddleCenter;
            // childControl = true: LayoutElement values are respected
            // childForceExpand = false: elements won't expand beyond preferred size
            joinLayout.childControlWidth = true;
            joinLayout.childControlHeight = true;
            joinLayout.childForceExpandWidth = false;
            joinLayout.childForceExpandHeight = false;

            // QR Code container - fixed size, NO AspectRatioFitter (breaks layout groups)
            var qrContainer = new GameObject("QRContainer");
            qrContainer.transform.SetParent(joinRow.transform, false);
            qrContainer.AddComponent<RectTransform>();
            var qrContainerLE = qrContainer.AddComponent<LayoutElement>();
            qrContainerLE.minWidth = 300;
            qrContainerLE.minHeight = 300;
            qrContainerLE.preferredWidth = 350;
            qrContainerLE.preferredHeight = 350;
            qrContainerLE.flexibleWidth = 0;
            qrContainerLE.flexibleHeight = 0;

            var qrPanel = new GameObject("QRCodePanel");
            qrPanel.transform.SetParent(qrContainer.transform, false);
            var qrPanelImage = qrPanel.AddComponent<Image>();
            qrPanelImage.color = Color.white;
            StretchToParent(qrPanel);

            var qrObj = new GameObject("QRCodeImage");
            qrObj.transform.SetParent(qrPanel.transform, false);
            qrCodeImage = qrObj.AddComponent<RawImage>();
            qrCodeImage.color = Color.white;
            StretchToParent(qrObj, paddingSmall);

            // Room code info (right side)
            var roomInfoPanel = new GameObject("RoomInfoPanel");
            roomInfoPanel.transform.SetParent(joinRow.transform, false);
            roomInfoPanel.AddComponent<RectTransform>();
            var roomInfoLE = roomInfoPanel.AddComponent<LayoutElement>();
            roomInfoLE.flexibleWidth = 1;
            roomInfoLE.flexibleHeight = 1;

            var roomInfoLayout = roomInfoPanel.AddComponent<VerticalLayoutGroup>();
            roomInfoLayout.spacing = paddingSmall;
            roomInfoLayout.childAlignment = TextAnchor.MiddleLeft;
            roomInfoLayout.childControlWidth = true;
            roomInfoLayout.childControlHeight = true;  // Respect LayoutElement heights
            roomInfoLayout.childForceExpandWidth = true;
            roomInfoLayout.childForceExpandHeight = false;

            var joinLabel = CreateText(roomInfoPanel.transform, "JoinLabel", "Scan QR code or visit:", FontSize(fontScaleLarge));
            joinLabel.alignment = TextAlignmentOptions.Left;
            var joinLabelLE = joinLabel.gameObject.AddComponent<LayoutElement>();
            joinLabelLE.preferredHeight = 50;

            playerUrlText = CreateText(roomInfoPanel.transform, "UrlLabel", "party-loteria-client.netlify.app", FontSize(fontScaleXLarge), FontStyles.Bold);
            playerUrlText.color = primaryColor;
            playerUrlText.alignment = TextAlignmentOptions.Left;
            var urlLabelLE = playerUrlText.gameObject.AddComponent<LayoutElement>();
            urlLabelLE.preferredHeight = 60;

            // Spacer
            var spacer = CreateLayoutElement(roomInfoPanel.transform, "Spacer", preferredHeight: 30);

            var codeLabel = CreateText(roomInfoPanel.transform, "CodeLabel", "Room Code:", FontSize(fontScaleLarge));
            codeLabel.alignment = TextAlignmentOptions.Left;
            var codeLabelLE = codeLabel.gameObject.AddComponent<LayoutElement>();
            codeLabelLE.preferredHeight = 50;

            roomCodeText = CreateText(roomInfoPanel.transform, "RoomCode", "----", FontSize(fontScaleGiant), FontStyles.Bold);
            roomCodeText.color = primaryColor;
            roomCodeText.alignment = TextAlignmentOptions.Left;
            roomCodeText.characterSpacing = 30;
            var roomCodeLE = roomCodeText.gameObject.AddComponent<LayoutElement>();
            roomCodeLE.preferredHeight = 120;

            // Bottom area - Players + Controls
            var bottomRow = CreateLayoutElement(mainLayout.transform, "BottomRow", preferredHeight: 150);
            var bottomLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.spacing = paddingMedium;
            bottomLayout.childControlWidth = true;
            bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = false;
            bottomLayout.childForceExpandHeight = true;

            // Player panel
            var playerPanel = new GameObject("PlayerPanel");
            playerPanel.transform.SetParent(bottomRow.transform, false);
            playerPanel.AddComponent<RectTransform>();
            var playerPanelBg = playerPanel.AddComponent<Image>();
            playerPanelBg.color = secondaryColor;
            var playerPanelLE = playerPanel.AddComponent<LayoutElement>();
            playerPanelLE.flexibleWidth = 2;

            var playerPanelVlg = playerPanel.AddComponent<VerticalLayoutGroup>();
            playerPanelVlg.padding = new RectOffset((int)paddingMedium, (int)paddingMedium, (int)paddingSmall, (int)paddingSmall);
            playerPanelVlg.spacing = paddingSmall;
            playerPanelVlg.childControlWidth = true;
            playerPanelVlg.childControlHeight = false;
            playerPanelVlg.childForceExpandWidth = true;
            playerPanelVlg.childForceExpandHeight = false;

            playerCountText = CreateText(playerPanel.transform, "PlayerCount", "0 Players", FontSize(fontScaleMedium));
            playerCountText.alignment = TextAlignmentOptions.Center;
            var playerCountLE = playerCountText.gameObject.AddComponent<LayoutElement>();
            playerCountLE.preferredHeight = 40;

            var listContainer = new GameObject("PlayerListContainer");
            listContainer.transform.SetParent(playerPanel.transform, false);
            listContainer.AddComponent<RectTransform>();
            var listVlg = listContainer.AddComponent<VerticalLayoutGroup>();
            listVlg.spacing = 5;
            listVlg.childAlignment = TextAnchor.UpperCenter;
            listVlg.childControlHeight = false;
            listVlg.childControlWidth = true;
            var listLE = listContainer.AddComponent<LayoutElement>();
            listLE.flexibleHeight = 1;
            playerListContainer = listContainer.transform;

            // Settings panel
            var settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(bottomRow.transform, false);
            settingsPanel.AddComponent<RectTransform>();
            var settingsPanelBg = settingsPanel.AddComponent<Image>();
            settingsPanelBg.color = secondaryColor;
            var settingsPanelLE = settingsPanel.AddComponent<LayoutElement>();
            settingsPanelLE.flexibleWidth = 1;

            var settingsVlg = settingsPanel.AddComponent<VerticalLayoutGroup>();
            settingsVlg.padding = new RectOffset((int)paddingMedium, (int)paddingMedium, (int)paddingSmall, (int)paddingSmall);
            settingsVlg.spacing = paddingSmall;
            settingsVlg.childAlignment = TextAnchor.MiddleCenter;
            settingsVlg.childControlWidth = true;
            settingsVlg.childControlHeight = false;
            settingsVlg.childForceExpandWidth = true;
            settingsVlg.childForceExpandHeight = false;

            var speedLabel = CreateText(settingsPanel.transform, "SpeedLabel", "Draw Speed", FontSize(fontScaleSmall));
            var speedLabelLE = speedLabel.gameObject.AddComponent<LayoutElement>();
            speedLabelLE.preferredHeight = 30;

            var sliderRow = new GameObject("SliderRow");
            sliderRow.transform.SetParent(settingsPanel.transform, false);
            sliderRow.AddComponent<RectTransform>();
            var sliderRowHlg = sliderRow.AddComponent<HorizontalLayoutGroup>();
            sliderRowHlg.spacing = paddingSmall;
            sliderRowHlg.childAlignment = TextAnchor.MiddleCenter;
            sliderRowHlg.childControlWidth = true;
            sliderRowHlg.childControlHeight = true;
            sliderRowHlg.childForceExpandWidth = false;
            sliderRowHlg.childForceExpandHeight = true;
            var sliderRowLE = sliderRow.AddComponent<LayoutElement>();
            sliderRowLE.flexibleHeight = 1;

            var sliderObj = new GameObject("DrawSpeedSlider");
            sliderObj.transform.SetParent(sliderRow.transform, false);
            sliderObj.AddComponent<RectTransform>();
            var sliderLE = sliderObj.AddComponent<LayoutElement>();
            sliderLE.flexibleWidth = 1;
            sliderLE.preferredHeight = 30;
            drawSpeedSlider = CreateSlider(sliderObj);

            drawSpeedValueText = CreateText(sliderRow.transform, "SpeedValue", "8s", FontSize(fontScaleSmall));
            var speedValueLE = drawSpeedValueText.gameObject.AddComponent<LayoutElement>();
            speedValueLE.preferredWidth = 50;
            speedValueLE.preferredHeight = 30;

            // Start button panel
            var startPanel = new GameObject("StartPanel");
            startPanel.transform.SetParent(bottomRow.transform, false);
            startPanel.AddComponent<RectTransform>();
            var startPanelBg = startPanel.AddComponent<Image>();
            startPanelBg.color = secondaryColor;
            var startPanelLE = startPanel.AddComponent<LayoutElement>();
            startPanelLE.flexibleWidth = 1;

            startGameButton = CreateButton(startPanel.transform, "StartButton", "START\nGAME", primaryColor, Color.black);
            StretchToParent(startGameButton.gameObject, paddingMedium);
            var startButtonText = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
            startButtonText.fontSize = FontSize(fontScaleLarge);
            startGameButton.interactable = false;

            // Server status indicator - floating button in top-right corner (outside mainLayout)
            var serverStatusBtn = new GameObject("ServerStatusButton");
            serverStatusBtn.transform.SetParent(screen.transform, false);
            var statusRect = serverStatusBtn.AddComponent<RectTransform>();
            // Anchor to top-right corner
            statusRect.anchorMin = new Vector2(1, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(1, 1);
            statusRect.anchoredPosition = new Vector2(-paddingSmall, -paddingSmall);
            statusRect.sizeDelta = new Vector2(60, 40);

            var statusBg = serverStatusBtn.AddComponent<Image>();
            statusBg.color = new Color(0.3f, 0.3f, 0.35f);

            var statusBtnComp = serverStatusBtn.AddComponent<Button>();
            statusBtnComp.targetGraphic = statusBg;

            var statusIconObj = new GameObject("Icon");
            statusIconObj.transform.SetParent(serverStatusBtn.transform, false);
            var statusIcon = statusIconObj.AddComponent<TextMeshProUGUI>();
            statusIcon.text = "zzZ";
            statusIcon.fontSize = 18;
            statusIcon.alignment = TextAlignmentOptions.Center;
            StretchToParent(statusIconObj);

            serverStatusIndicator = serverStatusBtn.AddComponent<ServerStatusIndicator>();

            return screen;
        }

        #endregion

        #region Game Screen

        private GameObject CreateGameScreen(Transform parent)
        {
            var screen = CreateFullScreen(parent, "GameScreen", backgroundColor);

            // Main layout - childControlHeight=true so LayoutElement values are respected
            var mainLayout = CreateVerticalLayoutPanel(screen.transform, "MainLayout", Color.clear);
            StretchToParent(mainLayout, paddingMedium);
            var vlg = mainLayout.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = paddingMedium;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;  // Required for LayoutElement to work
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;  // Don't force expand - respect LayoutElement values

            // Top bar with card number on left, pause button on right
            var topBar = CreateLayoutElement(mainLayout.transform, "TopBar", preferredHeight: 60);
            topBar.GetComponent<LayoutElement>().flexibleHeight = 0;
            var topBarHlg = topBar.AddComponent<HorizontalLayoutGroup>();
            topBarHlg.childAlignment = TextAnchor.MiddleCenter;
            topBarHlg.childControlWidth = true;
            topBarHlg.childControlHeight = true;
            topBarHlg.childForceExpandWidth = false;
            topBarHlg.childForceExpandHeight = true;

            // Card number on left side of top bar
            cardNumberText = CreateText(topBar.transform, "CardNumber", "#1", FontSize(fontScaleLarge), FontStyles.Bold);
            cardNumberText.alignment = TextAlignmentOptions.Left;
            var cardNumLE = cardNumberText.gameObject.AddComponent<LayoutElement>();
            cardNumLE.flexibleWidth = 1;

            pauseButton = CreateButton(topBar.transform, "PauseButton", "PAUSE", secondaryColor, textColor);
            var pauseLE = pauseButton.gameObject.AddComponent<LayoutElement>();
            pauseLE.preferredWidth = 150;
            pauseLE.preferredHeight = 50;
            pauseLE.flexibleWidth = 0;

            resumeButton = CreateButton(topBar.transform, "ResumeButton", "RESUME", accentColor, Color.black);
            var resumeLE = resumeButton.gameObject.AddComponent<LayoutElement>();
            resumeLE.preferredWidth = 150;
            resumeLE.preferredHeight = 50;
            resumeLE.flexibleWidth = 0;
            resumeButton.gameObject.SetActive(false);

            pausedText = CreateText(topBar.transform, "PausedText", "PAUSED", FontSize(fontScaleXLarge), FontStyles.Bold);
            pausedText.color = primaryColor;
            var pausedLE = pausedText.gameObject.AddComponent<LayoutElement>();
            pausedLE.flexibleWidth = 1;
            pausedText.gameObject.SetActive(false);

            // Rules bar - shows win patterns and draw speed
            var rulesBar = CreateLayoutElement(mainLayout.transform, "RulesBar", preferredHeight: 40);
            rulesBar.GetComponent<LayoutElement>().flexibleHeight = 0;
            var rulesBarBg = rulesBar.AddComponent<Image>();
            rulesBarBg.color = new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, 0.5f);

            rulesText = CreateText(rulesBar.transform, "RulesText", "", FontSize(fontScaleSmall));
            rulesText.color = new Color(0.7f, 0.7f, 0.7f);
            rulesText.alignment = TextAlignmentOptions.Center;
            StretchToParent(rulesText.gameObject, paddingSmall);

            // Card display area (main content) - expands to fill available space
            var cardPanel = CreateLayoutElement(mainLayout.transform, "CardPanel", flexibleHeight: 1);
            var cardPanelBg = cardPanel.AddComponent<Image>();
            cardPanelBg.color = secondaryColor;
            cardBackgroundImage = cardPanelBg;

            var cardLayout = cardPanel.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset((int)paddingLarge, (int)paddingLarge, (int)paddingLarge, (int)paddingLarge);
            cardLayout.spacing = paddingMedium;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;  // Required for LayoutElement to work
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            // Card image - expands to fill available space while maintaining aspect ratio
            cardImageContainer = CreateLayoutElement(cardPanel.transform, "CardImageContainer", flexibleHeight: cardImageFlexibleHeight);
            cardImageContainer.SetActive(false); // Hidden until first card is drawn
            
            var cardImageObj = new GameObject("CardImage");
            cardImageObj.transform.SetParent(cardImageContainer.transform, false);
            cardImage = cardImageObj.AddComponent<Image>();
            cardImage.preserveAspect = true;
            
            // Center the image in the container, don't stretch to parent
            var cardImageRect = cardImageObj.GetComponent<RectTransform>();
            cardImageRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardImageRect.pivot = new Vector2(0.5f, 0.5f);
            cardImageRect.sizeDelta = new Vector2(100, 100); // Initial size, will be updated at runtime

            // Card name - main focus, expands to fill center area
            var cardNameContainer = CreateLayoutElement(cardPanel.transform, "CardNameContainer", flexibleHeight: 1);
            cardNameText = CreateText(cardNameContainer.transform, "CardName", "El Gallo", FontSize(fontScaleHuge), FontStyles.Bold);
            cardNameText.color = primaryColor;
            cardNameText.enableAutoSizing = true;
            cardNameText.fontSizeMin = FontSize(fontScaleLarge);
            cardNameText.fontSizeMax = FontSize(fontScaleGiant);
            StretchToParent(cardNameText.gameObject);

            // Verse text - fixed height at bottom of card panel
            cardVerseText = CreateText(cardPanel.transform, "CardVerse", "El que le cantó a San Pedro...", FontSize(fontScaleLarge), FontStyles.Italic);
            cardVerseText.color = new Color(0.9f, 0.9f, 0.9f);
            var verseLE = cardVerseText.gameObject.AddComponent<LayoutElement>();
            verseLE.preferredHeight = 100;
            verseLE.flexibleHeight = 0;

            // Progress bar - fixed height at bottom
            var progressPanel = CreateLayoutElement(mainLayout.transform, "ProgressPanel", preferredHeight: 60);
            progressPanel.GetComponent<LayoutElement>().flexibleHeight = 0;
            var progressPanelBg = progressPanel.AddComponent<Image>();
            progressPanelBg.color = secondaryColor;

            var progressLayout = progressPanel.AddComponent<HorizontalLayoutGroup>();
            progressLayout.padding = new RectOffset((int)paddingMedium, (int)paddingMedium, (int)paddingSmall, (int)paddingSmall);
            progressLayout.spacing = paddingMedium;
            progressLayout.childAlignment = TextAnchor.MiddleCenter;
            progressLayout.childControlWidth = true;
            progressLayout.childControlHeight = true;
            progressLayout.childForceExpandWidth = false;
            progressLayout.childForceExpandHeight = true;

            var progressSliderObj = new GameObject("ProgressSlider");
            progressSliderObj.transform.SetParent(progressPanel.transform, false);
            progressSliderObj.AddComponent<RectTransform>();
            var progressSliderLE = progressSliderObj.AddComponent<LayoutElement>();
            progressSliderLE.flexibleWidth = 1;
            progressSliderLE.preferredHeight = 30;
            progressSlider = CreateSlider(progressSliderObj);
            progressSlider.interactable = false;

            progressText = CreateText(progressPanel.transform, "ProgressText", "0 / 54", FontSize(fontScaleMedium));
            var progressTextLE = progressText.gameObject.AddComponent<LayoutElement>();
            progressTextLE.preferredWidth = 120;

            return screen;
        }

        #endregion

        #region Results Screen

        private GameObject CreateResultsScreen(Transform parent)
        {
            var screen = CreateFullScreen(parent, "ResultsScreen", backgroundColor);

            // Main layout - childControlHeight=true so LayoutElement values are respected
            var mainLayout = CreateVerticalLayoutPanel(screen.transform, "MainLayout", Color.clear);
            StretchToParent(mainLayout, paddingLarge);
            var vlg = mainLayout.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = paddingLarge;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;  // Required for LayoutElement to work
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;  // Don't force expand - respect LayoutElement values

            // Winner Panel - expands to fill available space
            winnerPanel = CreateLayoutElement(mainLayout.transform, "WinnerPanel", flexibleHeight: 1);
            var winnerBg = winnerPanel.AddComponent<Image>();
            winnerBg.color = secondaryColor;

            var winnerLayout = winnerPanel.AddComponent<VerticalLayoutGroup>();
            winnerLayout.padding = new RectOffset((int)paddingLarge, (int)paddingLarge, (int)paddingLarge, (int)paddingLarge);
            winnerLayout.spacing = paddingMedium;
            winnerLayout.childAlignment = TextAnchor.MiddleCenter;
            winnerLayout.childControlWidth = true;
            winnerLayout.childControlHeight = true;  // Required for LayoutElement to work
            winnerLayout.childForceExpandWidth = true;
            winnerLayout.childForceExpandHeight = false;

            // Winner message - expands to fill center
            var msgContainer = CreateLayoutElement(winnerPanel.transform, "MessageContainer", flexibleHeight: 1);
            winnerMessageText = CreateText(msgContainer.transform, "WinnerMessage", "¡LOTERÍA!", FontSize(fontScaleGiant), FontStyles.Bold);
            winnerMessageText.color = primaryColor;
            winnerMessageText.enableAutoSizing = true;
            winnerMessageText.fontSizeMin = FontSize(fontScaleXLarge);
            winnerMessageText.fontSizeMax = FontSize(fontScaleGiant);
            StretchToParent(winnerMessageText.gameObject);

            // Winner name - fixed height
            winnerNameText = CreateText(winnerPanel.transform, "WinnerName", "Player Name", FontSize(fontScaleXLarge));
            var nameLE = winnerNameText.gameObject.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 100;
            nameLE.flexibleHeight = 0;

            // No Winner Panel - expands to fill available space
            noWinnerPanel = CreateLayoutElement(mainLayout.transform, "NoWinnerPanel", flexibleHeight: 1);
            var noWinnerBg = noWinnerPanel.AddComponent<Image>();
            noWinnerBg.color = secondaryColor;
            noWinnerPanel.SetActive(false);

            var noWinnerText = CreateText(noWinnerPanel.transform, "NoWinnerText", "All cards drawn!\nNo winner this round.", FontSize(fontScaleLarge));
            StretchToParent(noWinnerText.gameObject, paddingLarge);

            // Button row - fixed height at bottom
            var buttonRow = CreateLayoutElement(mainLayout.transform, "ButtonRow", preferredHeight: 100);
            buttonRow.GetComponent<LayoutElement>().flexibleHeight = 0;
            var buttonHlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonHlg.spacing = paddingLarge;
            buttonHlg.childAlignment = TextAnchor.MiddleCenter;
            buttonHlg.childControlWidth = true;
            buttonHlg.childControlHeight = true;
            buttonHlg.childForceExpandWidth = false;
            buttonHlg.childForceExpandHeight = true;

            playAgainButton = CreateButton(buttonRow.transform, "PlayAgainButton", "PLAY AGAIN", primaryColor, Color.black);
            var playAgainLE = playAgainButton.gameObject.AddComponent<LayoutElement>();
            playAgainLE.preferredWidth = 250;
            playAgainLE.preferredHeight = 80;
            playAgainLE.flexibleWidth = 0;

            var lobbyButton = CreateButton(buttonRow.transform, "LobbyButton", "BACK TO\nLOBBY", secondaryColor, textColor);
            var lobbyLE = lobbyButton.gameObject.AddComponent<LayoutElement>();
            lobbyLE.preferredWidth = 250;
            lobbyLE.preferredHeight = 80;
            lobbyLE.flexibleWidth = 0;

            return screen;
        }

        #endregion

        #region UI Helpers

        private GameObject CreateFullScreen(Transform parent, string name, Color bgColor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = obj.AddComponent<Image>();
            image.color = bgColor;

            return obj;
        }

        private GameObject CreateCenteredPanel(Transform parent, string name, Color color, float width, float height)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);

            var image = obj.AddComponent<Image>();
            image.color = color;

            return obj;
        }

        private GameObject CreateVerticalLayoutPanel(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            if (color.a > 0)
            {
                var image = obj.AddComponent<Image>();
                image.color = color;
            }

            var vlg = obj.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            return obj;
        }

        private GameObject CreateLayoutElement(Transform parent, string name, float preferredWidth = -1, float preferredHeight = -1, float flexibleWidth = -1, float flexibleHeight = -1)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            var le = obj.AddComponent<LayoutElement>();
            if (preferredWidth >= 0) le.preferredWidth = preferredWidth;
            if (preferredHeight >= 0) le.preferredHeight = preferredHeight;
            if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0) le.flexibleHeight = flexibleHeight;

            return obj;
        }

        private void StretchToParent(GameObject obj, float padding = 0)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, FontStyles style = FontStyles.Normal)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            var text = obj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;

            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, Color bgColor, Color labelColor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            var image = obj.AddComponent<Image>();
            image.color = bgColor;

            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.highlightedColor = bgColor * 1.1f;
            colors.pressedColor = bgColor * 0.9f;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
            button.colors = colors;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            StretchToParent(textObj);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = FontSize(fontScaleMedium);
            text.fontStyle = FontStyles.Bold;
            text.color = labelColor;
            text.alignment = TextAlignmentOptions.Center;

            return button;
        }

        private Slider CreateSlider(GameObject obj)
        {
            var slider = obj.AddComponent<Slider>();

            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(obj.transform, false);
            StretchToParent(bgObj);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(obj.transform, false);
            StretchToParent(fillArea);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            StretchToParent(fill);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = primaryColor;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;

            return slider;
        }

        #endregion

        #region Controller Setup

        private void SetupLobbyController(LobbyScreenController controller)
        {
            controller.Setup(roomCodeText, playerCountText, playerListContainer, startGameButton, drawSpeedSlider, drawSpeedValueText, qrCodeImage);
        }

        private void SetupGameController(GameScreenController controller)
        {
            controller.Setup(cardImage, cardImageContainer, cardNameText, cardVerseText, cardNumberText, progressText, progressSlider, pauseButton, resumeButton, pausedText, rulesText);
        }

        private void SetupResultsController(ResultsScreenController controller)
        {
            controller.Setup(winnerNameText, winnerMessageText, winnerPanel, noWinnerPanel, playAgainButton);
        }

        #endregion
    }

    public class SpinnerAnimation : MonoBehaviour
    {
        public float rotationSpeed = 200f;

        private void Update()
        {
            transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }
}
