using UnityEngine;
using UnityEngine.UI;

namespace Curling
{
    public class CurlArrowButton : MonoBehaviour
    {
        [Tooltip("The other curl direction button.")]
        [SerializeField]
        private CurlArrowButton OtherButton;

        [Tooltip("Whether this is the right arrow button or left arrow button.")]
        [SerializeField]
        private bool isRightArrow;

        [Tooltip("The sprite for the red right arrow.")]
        [SerializeField]
        private Sprite RedArrowRight;

        [Tooltip("The sprite for the red right arrow (fade).")]
        [SerializeField]
        private Sprite RedArrowRightFade;

        [Tooltip("The sprite for the red left arrow.")]
        [SerializeField]
        private Sprite RedArrowLeft;

        [Tooltip("The sprite for the red left arrow (fade).")]
        [SerializeField]
        private Sprite RedArrowLeftFade;

        [Tooltip("The sprite for the blue right arrow.")]
        [SerializeField]
        private Sprite BlueArrowRight;

        [Tooltip("The sprite for the blue right arrow (fade).")]
        [SerializeField]
        private Sprite BlueArrowRightFade;

        [Tooltip("The sprite for the blue left arrow.")]
        [SerializeField]
        private Sprite BlueArrowLeft;

        [Tooltip("The sprite for the blue left arrow (fade).")]
        [SerializeField]
        private Sprite BlueArrowLeftFade;

        private Button Button;
        private Sprite RegularSprite;
        private Sprite FadeSprite;

        void Start()
        {
            Button = GetComponent<Button>();

            // Subscribe to events that would affect button's dislay.
            GameManager.Instance.OnCurrentPlayerIDChanged += OnCurrentPlayerIDChanged;
            GameManager.Instance.OnCurlDirectionChanged += OnCurlDirectionChanged;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.PlacingBroom)
            {
                // Show the curl buttons for the current player and hide all others.
                bool isLocalPlayersTurn = (
                    !GameManager.IsNetworked ||
                    NetworkedCurlingPlayer.LocalPlayerInstance.GetPlayerID() == GameManager.Instance.CurrentPlayerID);
                if (isLocalPlayersTurn)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }
            else
            {
                Hide();
            }

            // Invoke this manually here to ensure button state matches GameManager state.
            OnCurlDirectionChanged(GameManager.Instance.SpinClockwise);
        }

        private void OnCurlDirectionChanged(bool spinClockwise)
        {
            if (isRightArrow)
            {
                SetUseFadeSprite(!spinClockwise);
            }
            else
            {
                SetUseFadeSprite(spinClockwise);
            }
        }

        private void OnCurrentPlayerIDChanged(string _)
        {
            if (GameManager.Instance.GetCurrentPlayer().PlayerColor == PlayerColor.Red)
            {
                RegularSprite = isRightArrow ? RedArrowRight : RedArrowLeft;
                FadeSprite = isRightArrow ? RedArrowRightFade : RedArrowLeftFade;
            }
            else
            {
                RegularSprite = isRightArrow ? BlueArrowRight : BlueArrowLeft;
                FadeSprite = isRightArrow ? BlueArrowRightFade : BlueArrowLeftFade;
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

        public void SetUseFadeSprite(bool useFadeSprite)
        {
            Button.image.sprite = useFadeSprite ? FadeSprite : RegularSprite;
            float opacity = useFadeSprite ? 0.8f : 1.0f;
            Button.image.color = new Color(Button.image.color.r, Button.image.color.g, Button.image.color.b, opacity);
        }

        public void HandleClick()
        {
            GameManager.Instance.SetCurlDirection(isRightArrow);
            OtherButton.SetUseFadeSprite(true);
            SetUseFadeSprite(false);
        }
    }
}

