using UnityEngine;
using UnityEngine.UI;

namespace Curling
{
    public class PowerMeter : MonoBehaviour
    {
        public static PowerMeter Instance;

        [Tooltip("The meter mask that will show/hide the meter dynamically.")]
        [SerializeField]
        private GameObject MaskGameObject;

        // Number of seconds it takes for the meter to fill one direction.
        private const float METER_SPEED = 1.0f;

        private RectMask2D Mask;
        private float MaskWidth;

        private int MeterDirection = -1;
        private float MeterFullness = 1.0f;

        void Start()
        {
            Instance = this;
            Mask = MaskGameObject.GetComponent<RectMask2D>();
            MaskWidth = Mask.canvasRect.width;
            Disable();
        }

        void Update()
        {
            if (GameManager.Instance.CurrentGameState == GameState.ShootingMeterActive)
            {
                // For some reason, the transform's z value gets more negative as the screen size increases. Once it goes past -1000,
                // the meter disappears (supposedly due to being less than the camera's z value?). As a workaround, make sure the z
                // value stays above -1000.
                if (gameObject.transform.position.z <= -1000.0)
                {
                    gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -1000);
                }

                MeterFullness += MeterDirection * Time.deltaTime;
                float padding = Utilities.MapToRange(MeterFullness, 0f, METER_SPEED, 0, MaskWidth);
                Mask.padding = new Vector4(0, 0, padding, 0);

                if (MeterFullness >= METER_SPEED)
                {
                    MeterDirection *= -1;
                }
                if (MeterFullness <= 0f)
                {
                    MeterDirection *= -1;
                }
            }
        }

        public void Enable()
        {
            // Reset to initial values
            MeterDirection = -1;
            MeterFullness = 1.0f;
            gameObject.SetActive(true);
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }

        public float GetShotPower()
        {
            print("SHOT POWER: " + (1.0f - MeterFullness));
            return 1.0f - MeterFullness;
        }
    }
}
