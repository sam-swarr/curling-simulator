using UnityEngine;

namespace Curling
{
    public class Scoreboard : MonoBehaviour
    {
        [Tooltip("UI Text object for rendering the red player's name.")]
        [SerializeField]
        private UnityEngine.UI.Text RedPlayerName;

        [Tooltip("Image for the ready checkmark that appears when red player is ready for next end.")]
        [SerializeField]
        private UnityEngine.UI.Image RedReadyCheckmark;

        [Tooltip("UI Text object for rendering the blue player's name.")]
        [SerializeField]
        private UnityEngine.UI.Text BluePlayerName;

        [Tooltip("Image for the ready checkmark that appears when blue player is ready for next end.")]
        [SerializeField]
        private UnityEngine.UI.Image BlueReadyCheckmark;

        [Tooltip("Panel containing the remaining stone icons for the red player.")]
        [SerializeField]
        private GameObject RedStoneIconsPanel;

        [Tooltip("Panel containing the remaining stone icons for the blue player.")]
        [SerializeField]
        private GameObject BlueStoneIconsPanel;

        [Tooltip("Panel containing the EndScore components for the red player.")]
        [SerializeField]
        private GameObject RedEndScoresPanel;

        [Tooltip("Panel containing the EndScore components for the blue player.")]
        [SerializeField]
        private GameObject BlueEndScoresPanel;

        [Tooltip("UI Text object for rendering the red player's total score.")]
        [SerializeField]
        private UnityEngine.UI.Text RedTotalScore;

        [Tooltip("UI Text object for rendering the blue player's total score.")]
        [SerializeField]
        private UnityEngine.UI.Text BlueTotalScore;

        private EndScore[] RedEndScores;
        private EndScore[] BlueEndScores;

        private StoneIcon[] RedStoneIcons;
        private StoneIcon[] BlueStoneIcons;

        private bool IsRedPlayersTurn;
        private bool IsBluePlayersTurn;

        void OnEnable()
        {
            RedEndScores = RedEndScoresPanel.GetComponentsInChildren<EndScore>();
            BlueEndScores = BlueEndScoresPanel.GetComponentsInChildren<EndScore>();

            RedStoneIcons = RedStoneIconsPanel.GetComponentsInChildren<StoneIcon>();
            BlueStoneIcons = BlueStoneIconsPanel.GetComponentsInChildren<StoneIcon>();

            // Wire up event handlers.
            GameManager.Instance.OnCurrentPlayerIDChanged += OnCurrentPlayerIDChanged;
            GameManager.Instance.OnEndNumberChanged += OnEndNumberChanged;
            GameManager.Instance.OnNextStoneSpawned += OnNextStoneSpawned;
            GameManager.Instance.OnEndScored += OnEndScored;
            GameManager.Instance.OnPlayerReadyForNextEnd += OnPlayerReadyForNextEnd;

            // Initially disable ready checkmarks.
            OnPlayerReadyForNextEnd(GameManager.Instance.RedPlayerReadyForNextEnd, GameManager.Instance.BluePlayerReadyForNextEnd);
        }

        public void OnCurrentPlayerIDChanged(string currentPlayerID)
        {
            CurlingPlayer redPlayer = GameManager.Instance.GetPlayer(PlayerColor.Red);
            CurlingPlayer bluePlayer = GameManager.Instance.GetPlayer(PlayerColor.Blue);

            IsRedPlayersTurn = redPlayer.GetPlayerID() == currentPlayerID;
            IsBluePlayersTurn = bluePlayer.GetPlayerID() == currentPlayerID;

            RedPlayerName.text = redPlayer.GetPlayerName();
            BluePlayerName.text = bluePlayer.GetPlayerName();

            RedPlayerName.fontStyle = IsRedPlayersTurn ? FontStyle.Bold : FontStyle.Normal;
            BluePlayerName.fontStyle = IsBluePlayersTurn ? FontStyle.Bold : FontStyle.Normal;
        }

        public void OnEndNumberChanged(int endNumber, PlayerColor hammer)
        {
            // Reset highlighted / hammer state
            foreach (EndScore endScore in RedEndScores)
            {
                endScore.SetIsHighlighted(false);
                endScore.SetHammerEnabled(false);
            }
            foreach (EndScore endScore in BlueEndScores)
            {
                endScore.SetIsHighlighted(false);
                endScore.SetHammerEnabled(false);
            }

            RedEndScores[endNumber - 1].SetIsHighlighted(true);
            BlueEndScores[endNumber - 1].SetIsHighlighted(true);

            EndScore[] endScores = hammer == PlayerColor.Red ? RedEndScores : BlueEndScores;
            endScores[endNumber - 1].SetHammerEnabled(true);
        }

        public void OnNextStoneSpawned(int redStonesLeft, int blueStonesLeft)
        {
            for (int i = 0; i < RedStoneIcons.Length; i++)
            {
                StoneIcon icon = RedStoneIcons[i];
                if (i == redStonesLeft - 1 && IsRedPlayersTurn)
                {
                    icon.SetEnabled(true);
                    icon.SetIsBlinking(true);
                }
                else
                {
                    icon.SetIsBlinking(false);
                    icon.SetEnabled(i < redStonesLeft);
                }
            }

            for (int i = 0; i < BlueStoneIcons.Length; i++)
            {
                StoneIcon icon = BlueStoneIcons[i];
                if (i == blueStonesLeft - 1 && IsBluePlayersTurn)
                {
                    icon.SetEnabled(true);
                    icon.SetIsBlinking(true);
                }
                else
                {
                    icon.SetIsBlinking(false);
                    icon.SetEnabled(i < blueStonesLeft);
                }
            }
        }

        public void OnEndScored(
            int endNumber,
            int redPointsForEnd,
            int bluePointsForEnd,
            int totalRedPoints,
            int totalBluePoints)
        {
            // Call this to disable all stone icons.
            OnNextStoneSpawned(0, 0);
            RedEndScores[endNumber - 1].SetHammerEnabled(false);
            RedEndScores[endNumber - 1].SetScore(redPointsForEnd);
            BlueEndScores[endNumber - 1].SetHammerEnabled(false);
            BlueEndScores[endNumber - 1].SetScore(bluePointsForEnd);

            RedTotalScore.text = totalRedPoints.ToString();
            BlueTotalScore.text = totalBluePoints.ToString();
        }

        public void OnPlayerReadyForNextEnd(bool redPlayerReady, bool bluePlayerReady)
        {
            RedReadyCheckmark.enabled = redPlayerReady;
            BlueReadyCheckmark.enabled = bluePlayerReady;
        }
    }
}
