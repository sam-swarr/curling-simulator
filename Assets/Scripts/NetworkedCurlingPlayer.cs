using Photon.Pun;
using UnityEngine;

namespace Curling
{
    public class NetworkedCurlingPlayer : CurlingPlayer
    {
        public static CurlingPlayer LocalPlayerInstance = null;

        public PhotonView PhotonView { get; private set; }


        #region MonoBehaviour Callbacks

        public override void Awake()
        {
            PhotonView = this.GetComponent<PhotonView>();
            PlayerID = PhotonView.Owner.UserId;
            PlayerName = PhotonView.Owner.NickName;

            if (AmOwner())
            {
                LocalPlayerInstance = this;

                // Two instances of NetworkedCurlingPlayer will exist, so we take care of initializing and hiding
                // the EndOfEndDialog just once here.
                CurlingPlayer.EndOfEndDialog = GameObject.FindWithTag("End Of End Dialog").GetComponent<EndOfEndDialog>();
                CurlingPlayer.EndOfEndDialog.Hide();
            }
            base.Awake();
        }

        #endregion


        #region Virtual / Abstract Overrides

        protected override bool AmOwner()
        {
            return PhotonView.AmOwner;
        }

        protected override bool IsMine()
        {
            return PhotonView.IsMine;
        }

        protected override void SpawnAimingBroom()
        {
            GameObject spawnLocation = GameObject.FindWithTag("Aiming Broom Spawn");
            PhotonNetwork.Instantiate("NetworkedAimingBroom", spawnLocation.transform.position, spawnLocation.transform.rotation);
        }
        public override void SetColor(PlayerColor color)
        {
            PhotonView.RPC("RPCSetColor", RpcTarget.All, color);
        }

        #endregion


        #region RPCs

        [PunRPC]
        public void RPCSetColor(PlayerColor color)
        {
            base.SetColor(color);
        }

        #endregion
    }
}