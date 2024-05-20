using UnityEngine;

namespace Curling
{
    public class Utilities : MonoBehaviour
    {
        /*
         * Useful remapping values from one range of values to another range of values.
         * Values outside of the input range will clamp to the min/max of the output range.
         */
        public static float MapToRange(float input, float inputMin, float inputMax, float outputMin, float outputMax)
        {
            input = Mathf.Clamp(input, inputMin, inputMax);

            var inputAbs = input - inputMin;
            var inputMaxAbs = inputMax - inputMin;

            var normal = inputAbs / inputMaxAbs;

            var outputMaxAbs = outputMax - outputMin;
            var outputAbs = outputMaxAbs * normal;

            return outputAbs + outputMin;
        }

        public static bool IsMouseOnCameraViewport(Camera camera)
        {
            Rect camRect = camera.pixelRect;
            float mouseX = Input.mousePosition.x;
            float mouseY = Input.mousePosition.y;

            return (mouseX >= camRect.x && mouseX <= camRect.x + camRect.width) &&
                   (mouseY >= camRect.y && mouseY <= camRect.y + camRect.height);
        }
    }
}
