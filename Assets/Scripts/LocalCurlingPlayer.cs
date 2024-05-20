using System.Collections.Generic;
using UnityEngine;

namespace Curling
{
    public class LocalCurlingPlayer : CurlingPlayer
    {
        public static List<CurlingPlayer> Instances = new List<CurlingPlayer>();

        [Tooltip("Prefab for the LocalAimingBroom")]
        [SerializeField]
        private GameObject localAimingBroomPrefab;

        #region MonoBehaviour Callbacks
        public override void Awake()
        {
            PlayerID = System.Guid.NewGuid().ToString();
            PlayerName = "";

            Instances.Add(this);

            // For local multiplayer, two LocalCurlingPlayer instances will be created, but they need to share
            // the same EndOfEndDialog. Have the first instance take care of initializing the static variable and
            // hiding the dialog.
            if (CurlingPlayer.EndOfEndDialog == null)
            {
                CurlingPlayer.EndOfEndDialog = GameObject.FindWithTag("End Of End Dialog").GetComponent<EndOfEndDialog>();
                CurlingPlayer.EndOfEndDialog.Hide();
            }

            base.Awake();
        }

        #endregion


        public override void SetColor(PlayerColor color)
        {
            PlayerName = color == PlayerColor.Red ? "Red Player" : "Blue Player";
            base.SetColor(color);
        }

        protected override void SpawnAimingBroom()
        {
            GameObject spawnLocation = GameObject.FindWithTag("Aiming Broom Spawn");
            LocalAimingBroom broom = Instantiate(
                localAimingBroomPrefab,
                spawnLocation.transform.position,
                spawnLocation.transform.rotation).GetComponent<LocalAimingBroom>();
            broom.SetOwningPlayerID(PlayerID);
        }
    }
}

