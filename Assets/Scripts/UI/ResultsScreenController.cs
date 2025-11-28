using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PartyLoteria.Data;
using PartyLoteria.Game;

namespace PartyLoteria.UI
{
    public class ResultsScreenController : MonoBehaviour
    {
        private TextMeshProUGUI winnerNameText;
        private TextMeshProUGUI winnerMessageText;
        private GameObject winnerPanel;
        private GameObject noWinnerPanel;
        private Button playAgainButton;

        private bool isSetup = false;

        public void Setup(
            TextMeshProUGUI winnerName,
            TextMeshProUGUI winnerMessage,
            GameObject winner,
            GameObject noWinner,
            Button playAgain)
        {
            winnerNameText = winnerName;
            winnerMessageText = winnerMessage;
            winnerPanel = winner;
            noWinnerPanel = noWinner;
            playAgainButton = playAgain;
            isSetup = true;

            SetupControls();
        }

        private void OnEnable()
        {
            if (isSetup) DisplayResults();
        }

        private void SetupControls()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }
        }

        private void DisplayResults()
        {
            var game = GameManager.Instance;
            if (game == null) return;

            bool hasWinner = game.GameWinner != null;

            if (winnerPanel != null)
            {
                winnerPanel.SetActive(hasWinner);
            }

            if (noWinnerPanel != null)
            {
                noWinnerPanel.SetActive(!hasWinner);
            }

            if (hasWinner)
            {
                DisplayWinner(game.GameWinner);
            }
        }

        private void DisplayWinner(Winner winner)
        {
            if (winnerNameText != null)
            {
                winnerNameText.text = winner.name;
            }

            if (winnerMessageText != null)
            {
                winnerMessageText.text = "¡LOTERÍA!";
            }
        }

        private void OnPlayAgainClicked()
        {
            GameManager.Instance?.ResetGame();
        }
    }
}
