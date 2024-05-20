using UnityEngine;

namespace Curling
{
    public class YourTurnDialog : MonoBehaviour
    {
        [Tooltip("UI Text object that renders the dialog header text.")]
        [SerializeField]
        private UnityEngine.UI.Text HeaderText;

        void Start()
        {
            // To determine whether or not we show the button depends on:
            //   1. GameState
            //   2. Current Player
            // So make sure to subscribe to those events.
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnCurrentPlayerIDChanged += OnCurrentPlayerIDChanged;

            // Manually invoke listener with initial data to avoid race conditions.
            OnGameStateChanged(GameManager.Instance.CurrentGameState);
            OnCurrentPlayerIDChanged(GameManager.Instance.CurrentPlayerID);
        }

        private void OnGameStateChanged(GameState _)
        {
            UpdateVisibility();
        }

        private void OnCurrentPlayerIDChanged(string _)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (GameManager.Instance.CurrentGameState == GameState.YourTurnDialogActive)
            {
                // For local multiplayer, always show dialog. For networked multiplayer, show the dialog for the current player and hide it for the other player.
                if (!GameManager.IsNetworked || (NetworkedCurlingPlayer.LocalPlayerInstance.GetPlayerID() == GameManager.Instance.CurrentPlayerID))
                {
                    this.Show();
                }
                else
                {
                    this.Hide();
                }
            }
            else
            {
                this.Hide();
            }
        }

        public void Show()
        {
            if (!GameManager.IsNetworked)
            {
                HeaderText.text = $"{GameManager.Instance.GetCurrentPlayer().GetPlayerName()}'s Turn";
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnClick()
        {
            GameManager.Instance.OnYourTurnDialogClosed();
        }
    }
}
