using UnityEngine;

namespace Curling
{
    public class CurlButton : MonoBehaviour
    {
        [Tooltip("The other curl direction button.")]
        [SerializeField]
        private CurlButton _otherButton;

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
            if (GameManager.Instance.CurrentGameState == GameState.PlacingBroom)
            {
                // Show the inward curl button for the current player and hide all others.
                bool isLocalPlayersTurn = (!GameManager.IsNetworked || NetworkedCurlingPlayer.LocalPlayerInstance.GetPlayerID() == GameManager.Instance.CurrentPlayerID);
                if (isLocalPlayersTurn && this.CompareTag("Inward Curl Button"))
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
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void HandleInwardCurlClick()
        {
            GameManager.Instance.SetCurlDirection(false);
            _otherButton.Show();
            Hide();
        }

        public void HandleOutwardCurlClick()
        {
            GameManager.Instance.SetCurlDirection(true);
            _otherButton.Show();
            Hide();
        }
    }
}
