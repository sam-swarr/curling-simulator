using UnityEngine;

namespace Curling
{
    public class LocalGameManager : GameManager
    {
        [Tooltip("Prefab for the LocalCurlingStone")]
        [SerializeField]
        private GameObject localCurlingStonePrefab;

        #region MonoBehaviour Callbacks

        public override void Awake()
        {
            GameManager.IsNetworked = false;
            base.Awake();
        }

        #endregion

        protected override CurlingStone CreateAndInitializeStone(
            Vector3 initialPosition,
            Quaternion initialRotation,
            PlayerColor stoneColor)
        {
            CurlingStone s = Instantiate(
                localCurlingStonePrefab,
                initialPosition,
                initialRotation).GetComponent<CurlingStone>();
            s.Initialize(GetCurrentPlayer().PlayerColor);
            return s;
        }
    }
}
