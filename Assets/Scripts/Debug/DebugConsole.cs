using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using PartyLoteria.DevConsole.Commands;
using PartyLoteria.Game;
using PartyLoteria.Network;

namespace PartyLoteria.DevConsole
{
    /// <summary>
    /// In-game debug console for the STB client.
    /// Toggle with backtick (`) key.
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        // Configuration constants
        private const float CONSOLE_HEIGHT_PERCENT = 0.5f;
        private const int FONT_SIZE = 18;
        private const int INPUT_HEIGHT = 28;
        private const int MAX_OUTPUT_LINES = 100;
        private const int MAX_HISTORY = 50;

        // Colors
        private static readonly Color CONSOLE_BG_COLOR = new Color(0f, 0f, 0f, 0.95f);
        private static readonly Color TEXT_COLOR = new Color(0f, 1f, 0f, 1f); // Green
        private static readonly Color ERROR_COLOR = new Color(1f, 0.3f, 0.3f, 1f); // Red
        private static readonly Color WARNING_COLOR = new Color(1f, 1f, 0f, 1f); // Yellow
        private static readonly Color INFO_COLOR = new Color(0.5f, 0.8f, 1f, 1f); // Light blue

        public static DebugConsole Instance { get; private set; }

        // UI References
        private GameObject consolePanel;
        private TMP_Text outputText;
        private TMP_InputField inputField;
        private ScrollRect scrollRect;

        // State
        private bool isShown;
        private List<string> outputLines = new List<string>();
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1;

        // Commands
        private Dictionary<string, ConsoleCommand> commands = new Dictionary<string, ConsoleCommand>();

        // Public accessors for commands
        public GameManager GameManager => GameManager.Instance;
        public NetworkManager NetworkManager => NetworkManager.Instance;
        public IReadOnlyDictionary<string, ConsoleCommand> Commands => commands;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateUI();
            RegisterCommands();
            Hide();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Toggle with backtick key
            if (keyboard.backquoteKey.wasPressedThisFrame)
            {
                Toggle();
            }

            // Handle input when shown
            if (isShown && inputField != null)
            {
                // History navigation
                if (keyboard.upArrowKey.wasPressedThisFrame)
                {
                    NavigateHistory(1);
                }
                else if (keyboard.downArrowKey.wasPressedThisFrame)
                {
                    NavigateHistory(-1);
                }

                // Submit on Enter
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    SubmitCommand();
                }

                // Close on Escape
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    Hide();
                }
            }
        }

        private void CreateUI()
        {
            // Create canvas if needed
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("DebugCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999; // Always on top
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Main panel
            consolePanel = new GameObject("DebugConsole");
            consolePanel.transform.SetParent(canvas.transform, false);

            var panelRect = consolePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1 - CONSOLE_HEIGHT_PERCENT);
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = consolePanel.AddComponent<Image>();
            panelImage.color = CONSOLE_BG_COLOR;

            var vlg = consolePanel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Output area with scroll
            var scrollArea = new GameObject("ScrollArea");
            scrollArea.transform.SetParent(consolePanel.transform, false);

            var scrollAreaRect = scrollArea.AddComponent<RectTransform>();
            var scrollAreaLayout = scrollArea.AddComponent<LayoutElement>();
            scrollAreaLayout.flexibleHeight = 1;

            scrollRect = scrollArea.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20;

            var scrollBg = scrollArea.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.3f);

            // Content container
            var content = new GameObject("Content");
            content.transform.SetParent(scrollArea.transform, false);

            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.padding = new RectOffset(5, 5, 5, 5);

            scrollRect.content = contentRect;

            // Output text
            var outputObj = new GameObject("OutputText");
            outputObj.transform.SetParent(content.transform, false);

            outputText = outputObj.AddComponent<TextMeshProUGUI>();
            outputText.fontSize = FONT_SIZE;
            outputText.color = TEXT_COLOR;
            outputText.font = TMP_Settings.defaultFontAsset;
            outputText.textWrappingMode = TextWrappingModes.Normal;
            outputText.overflowMode = TextOverflowModes.Truncate;
            outputText.richText = true;

            // Input area
            var inputArea = new GameObject("InputArea");
            inputArea.transform.SetParent(consolePanel.transform, false);

            var inputAreaRect = inputArea.AddComponent<RectTransform>();
            var inputAreaLayout = inputArea.AddComponent<LayoutElement>();
            inputAreaLayout.preferredHeight = INPUT_HEIGHT;
            inputAreaLayout.flexibleHeight = 0;  // Don't expand - stay at preferred height

            var inputAreaHlg = inputArea.AddComponent<HorizontalLayoutGroup>();
            inputAreaHlg.childControlWidth = true;
            inputAreaHlg.childControlHeight = true;
            inputAreaHlg.childForceExpandWidth = false;  // Don't force expand - respect LayoutElement values
            inputAreaHlg.childForceExpandHeight = true;
            inputAreaHlg.spacing = 5;

            // Prompt (fixed width, doesn't expand)
            var promptObj = new GameObject("Prompt");
            promptObj.transform.SetParent(inputArea.transform, false);

            var promptText = promptObj.AddComponent<TextMeshProUGUI>();
            promptText.text = ">";
            promptText.fontSize = FONT_SIZE;
            promptText.color = TEXT_COLOR;
            promptText.font = TMP_Settings.defaultFontAsset;
            promptText.alignment = TextAlignmentOptions.MidlineLeft;

            var promptLayout = promptObj.AddComponent<LayoutElement>();
            promptLayout.preferredWidth = 20;
            promptLayout.flexibleWidth = 0;  // Don't expand

            // Input field (expands to fill remaining space)
            var inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(inputArea.transform, false);

            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.flexibleWidth = 1;  // Expand to fill available space

            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textViewport = inputObj.GetComponent<RectTransform>();
            inputField.onFocusSelectAll = false;
            inputField.restoreOriginalTextOnEscape = false;

            // Input text area
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);

            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(5, 0);
            textAreaRect.offsetMax = new Vector2(-5, 0);

            inputField.textViewport = textAreaRect;

            // Input text
            var inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(textArea.transform, false);

            var inputTextRect = inputTextObj.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            var inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = FONT_SIZE;
            inputText.color = TEXT_COLOR;
            inputText.font = TMP_Settings.defaultFontAsset;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            inputField.textComponent = inputText;

            // Placeholder
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);

            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            var placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Type 'help' for commands...";
            placeholderText.fontSize = FONT_SIZE;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholderText.font = TMP_Settings.defaultFontAsset;
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderText.fontStyle = FontStyles.Italic;

            inputField.placeholder = placeholderText;
        }

        private void RegisterCommands()
        {
            // Built-in commands
            RegisterCommand(new HelpCommand());
            RegisterCommand(new ClearCommand());
            RegisterCommand(new StatusCommand());

            // Game commands
            RegisterCommand(new DrawCommand());
            RegisterCommand(new SpeedCommand());
            RegisterCommand(new ResetCommand());
            RegisterCommand(new PatternCommand());
            RegisterCommand(new AutoDrawCommand());
            RegisterCommand(new WinCommand());
            RegisterCommand(new LoteriaCommand());
        }

        public void RegisterCommand(ConsoleCommand command)
        {
            string name = command.Name.ToLower();
            if (commands.ContainsKey(name))
            {
                UnityEngine.Debug.LogWarning($"[DebugConsole] Command '{name}' already registered, overwriting");
            }
            commands[name] = command;
        }

        public void Toggle()
        {
            if (isShown) Hide();
            else Show();
        }

        public void Show()
        {
            isShown = true;
            consolePanel.SetActive(true);

            // Focus input
            if (inputField != null)
            {
                inputField.ActivateInputField();
                inputField.Select();
            }

            // Pause game when console is open
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            isShown = false;
            if (consolePanel != null)
            {
                consolePanel.SetActive(false);
            }

            // Resume game
            Time.timeScale = 1f;
        }

        private void SubmitCommand()
        {
            string commandLine = inputField.text.Trim();
            inputField.text = "";
            inputField.ActivateInputField();

            if (string.IsNullOrEmpty(commandLine))
                return;

            // Add to history
            commandHistory.Insert(0, commandLine);
            if (commandHistory.Count > MAX_HISTORY)
            {
                commandHistory.RemoveAt(commandHistory.Count - 1);
            }
            historyIndex = -1;

            // Echo command
            Print($"> {commandLine}");

            // Parse and execute
            string[] parts = commandLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string commandName = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (commands.TryGetValue(commandName, out ConsoleCommand command))
            {
                try
                {
                    command.Execute(args, this);
                }
                catch (Exception e)
                {
                    PrintError($"Error: {e.Message}");
                    UnityEngine.Debug.LogException(e);
                }
            }
            else
            {
                PrintError($"Unknown command: {commandName}");
                Print("Type 'help' for available commands");
            }
        }

        private void NavigateHistory(int direction)
        {
            if (commandHistory.Count == 0) return;

            historyIndex += direction;
            historyIndex = Mathf.Clamp(historyIndex, -1, commandHistory.Count - 1);

            if (historyIndex >= 0)
            {
                inputField.text = commandHistory[historyIndex];
                inputField.caretPosition = inputField.text.Length;
            }
            else
            {
                inputField.text = "";
            }
        }

        // Output methods
        public void Print(string message)
        {
            Print(message, TEXT_COLOR);
        }

        public void Print(string message, Color color)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            outputLines.Add($"<color=#{colorHex}>{message}</color>");

            // Trim old lines
            while (outputLines.Count > MAX_OUTPUT_LINES)
            {
                outputLines.RemoveAt(0);
            }

            RefreshOutput();
        }

        public void PrintError(string message)
        {
            Print(message, ERROR_COLOR);
        }

        public void PrintWarning(string message)
        {
            Print(message, WARNING_COLOR);
        }

        public void PrintInfo(string message)
        {
            Print(message, INFO_COLOR);
        }

        public void Clear()
        {
            outputLines.Clear();
            RefreshOutput();
        }

        private void RefreshOutput()
        {
            if (outputText != null)
            {
                outputText.text = string.Join("\n", outputLines);

                // Scroll to bottom
                Canvas.ForceUpdateCanvases();
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
    }
}
