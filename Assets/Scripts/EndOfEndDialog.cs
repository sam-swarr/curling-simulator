using UnityEngine;

namespace Curling
{
    public class EndOfEndDialog : MonoBehaviour
    {

        [Tooltip("Text object for the dialog.")]
        [SerializeField]
        private UnityEngine.UI.Text Text;

        public void Show(string scoringPlayerName, int points)
        {
            if (points == 0)
            {
                Text.text = "No one scored.";
            } else
            {
                Text.text = $"{scoringPlayerName} scored {points} points!";
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnClick()
        {
            Hide();
            GameManager.Instance.PlayerReadyForNextEnd();
        }
    }
}
