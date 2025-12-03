using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PartyLoteria.Data;
using PartyLoteria.Game;

namespace PartyLoteria.UI
{
    public class GameScreenController : MonoBehaviour
    {
        private Image cardImage;
        private GameObject cardImageContainer;
        private TextMeshProUGUI cardNameText;
        private TextMeshProUGUI cardVerseText;
        private TextMeshProUGUI cardNumberText;
        private TextMeshProUGUI progressText;
        private Slider progressSlider;
        private Button pauseButton;
        private Button resumeButton;
        private TextMeshProUGUI pausedText;

        private bool isSetup = false;

        public void Setup(
            Image cardImg,
            GameObject cardImgContainer,
            TextMeshProUGUI cardName,
            TextMeshProUGUI cardVerse,
            TextMeshProUGUI cardNumber,
            TextMeshProUGUI progress,
            Slider progressBar,
            Button pause,
            Button resume,
            TextMeshProUGUI paused)
        {
            cardImage = cardImg;
            cardImageContainer = cardImgContainer;
            cardNameText = cardName;
            cardVerseText = cardVerse;
            cardNumberText = cardNumber;
            progressText = progress;
            progressSlider = progressBar;
            pauseButton = pause;
            resumeButton = resume;
            pausedText = paused;
            isSetup = true;

            SetupControls();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            if (isSetup) UpdateUI();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            var game = GameManager.Instance;
            if (game != null)
            {
                game.OnCardDrawn += HandleCardDrawn;
                game.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var game = GameManager.Instance;
            if (game != null)
            {
                game.OnCardDrawn -= HandleCardDrawn;
                game.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void SetupControls()
        {
            // NOTE: Pause/Resume controls removed - host controls game from mobile device
            // Hide the buttons since STB is now display-only
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(false);
            }

            if (resumeButton != null)
            {
                resumeButton.gameObject.SetActive(false);
            }
        }

        private void HandleCardDrawn(Card card, int cardNumber, int totalCards)
        {
            UpdateCardDisplay(card);
            UpdateProgress(cardNumber, totalCards);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            UpdatePauseControls(phase);
        }

        private void UpdateUI()
        {
            var game = GameManager.Instance;
            if (game == null) return;

            if (game.CurrentCard != null)
            {
                UpdateCardDisplay(game.CurrentCard);
            }
            else
            {
                // Clear display when no card
                if (cardNameText != null) cardNameText.text = "";
                if (cardVerseText != null) cardVerseText.text = "Waiting for first card...";
                if (cardNumberText != null) cardNumberText.text = "";
                if (cardImageContainer != null) cardImageContainer.SetActive(false);
            }

            UpdateProgress(game.CardsDrawn, game.TotalCards);
            UpdatePauseControls(game.CurrentPhase);
        }

        private void UpdateCardDisplay(Card card)
        {
            var game = GameManager.Instance;
            string lang = game?.Language ?? "es";

            if (cardNameText != null)
            {
                cardNameText.text = card.GetName(lang);
            }

            if (cardVerseText != null)
            {
                cardVerseText.text = card.GetVerse(lang);
            }

            if (cardNumberText != null)
            {
                cardNumberText.text = $"#{card.id}";
            }

            // Load and display card image
            if (cardImage != null && !string.IsNullOrEmpty(card.image))
            {
                var sprite = CardImageLoader.LoadCardSprite(card.image);
                if (sprite != null)
                {
                    cardImage.sprite = sprite;
                    cardImage.preserveAspect = true;
                    if (cardImageContainer != null) cardImageContainer.SetActive(true);

                    // Size the image to fit container while maintaining aspect ratio
                    if (cardImageContainer != null)
                    {
                        // Rebuild parent layout first to get accurate container dimensions
                        var parentRect = cardImageContainer.transform.parent?.GetComponent<RectTransform>();
                        if (parentRect != null)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                        }
                        LayoutRebuilder.ForceRebuildLayoutImmediate(cardImageContainer.GetComponent<RectTransform>());

                        float spriteAspect = sprite.rect.width / sprite.rect.height;
                        var containerRect = cardImageContainer.GetComponent<RectTransform>();
                        float availableHeight = containerRect.rect.height;
                        float availableWidth = containerRect.rect.width;

                        float targetWidth, targetHeight;
                        if (availableHeight > 0 && availableWidth > 0)
                        {
                            float containerAspect = availableWidth / availableHeight;
                            if (containerAspect > spriteAspect)
                            {
                                // Container is wider - fit to height
                                targetHeight = availableHeight;
                                targetWidth = targetHeight * spriteAspect;
                            }
                            else
                            {
                                // Container is taller - fit to width
                                targetWidth = availableWidth;
                                targetHeight = targetWidth / spriteAspect;
                            }
                            cardImage.rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
                        }
                        else
                        {
                            // Fallback size maintaining aspect ratio
                            cardImage.rectTransform.sizeDelta = new Vector2(368, 500);
                        }
                    }
                }
                else
                {
                    if (cardImageContainer != null) cardImageContainer.SetActive(false);
                }
            }
        }

        private void UpdateProgress(int current, int total)
        {
            if (progressSlider != null)
            {
                progressSlider.value = total > 0 ? (float)current / total : 0;
            }

            if (progressText != null)
            {
                progressText.text = total > 0 ? $"{current} / {total}" : "0 / 54";
            }
        }

        private void UpdatePauseControls(GamePhase phase)
        {
            bool isPaused = phase == GamePhase.Paused;

            // NOTE: Pause/Resume buttons are hidden (host controls from mobile)
            // Only show the paused text indicator
            if (pausedText != null)
            {
                pausedText.gameObject.SetActive(isPaused);
            }
        }
    }
}
