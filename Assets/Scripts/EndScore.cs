using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Curling
{
    public class EndScore : MonoBehaviour
    {
        [Tooltip("UI Image for the background.")]
        [SerializeField]
        private UnityEngine.UI.Image Background;

        [Tooltip("UI Text object for rendering the score for this end.")]
        [SerializeField]
        private UnityEngine.UI.Text Score;

        [Tooltip("UI Image for rendering the hammer icon.")]
        [SerializeField]
        private UnityEngine.UI.Image Hammer;

        private Color DEFAULT_BACKGROUND_COLOR = new Color32(200, 214, 229, 255);
        private Color HIGHLIGHTED_BACKGROUND_COLOR = new Color32(131, 149, 167, 255);

        private void Awake()
        {
            Score.text = "";
            SetHammerEnabled(false);
        }

        public void SetScore(int score)
        {
            Score.text = score.ToString();
        }

        public void SetHammerEnabled(bool enabled)
        {
            Hammer.enabled = enabled;
        }

        public void SetIsHighlighted(bool highlighted)
        {
            Background.color = highlighted ? HIGHLIGHTED_BACKGROUND_COLOR : DEFAULT_BACKGROUND_COLOR;
        }
    }
}
