using UnityEngine;

namespace Curling
{ 
    public class StoneIcon : MonoBehaviour
    {
        [Tooltip("Image component for the stone icon.")]
        [SerializeField]
        private UnityEngine.UI.Image StoneIconImage;

        private bool isBlinking = false;
        private float BlinkFactor = 0f;
        private bool BecomingMoreOpaque = true;
        private const float BLINK_DURATION_SECONDS = 2.0f;

        private void Update()
        {
            if (isBlinking)
            {
                if (BecomingMoreOpaque)
                {
                    BlinkFactor += Time.deltaTime;
                } else
                {
                    BlinkFactor -= Time.deltaTime;
                }

                float opacity = Utilities.MapToRange(BlinkFactor, 0f, BLINK_DURATION_SECONDS, 0f, 1f);
                StoneIconImage.color = new Color(StoneIconImage.color.r, StoneIconImage.color.g, StoneIconImage.color.b, opacity);

                if (BlinkFactor >= BLINK_DURATION_SECONDS)
                {
                    BecomingMoreOpaque = false;
                }

                if (BlinkFactor <= 0f)
                {
                    BecomingMoreOpaque = true;
                }
            }
        }

        public void SetEnabled(bool enabled)
        {
            // reset to full opacity
            StoneIconImage.color = new Color(StoneIconImage.color.r, StoneIconImage.color.g, StoneIconImage.color.b, 1.0f);
            gameObject.SetActive(enabled);
        }

        public void SetIsBlinking(bool blinking)
        {
            BlinkFactor = 0f;
            BecomingMoreOpaque = true;
            isBlinking = blinking;
        }
    }
}

