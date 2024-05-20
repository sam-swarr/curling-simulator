using Photon.Pun;

namespace Curling
{
    public class NetworkedAimingBroom : AimingBroom
    {
        public PhotonView PhotonView { get; private set; }
        

        #region MonoBehaviour Callbacks

        public override void Awake()
        {
            PhotonView = this.GetComponent<PhotonView>();
            OwningPlayerID = PhotonView.Owner.UserId;
            base.Awake();
        }

        #endregion


        protected override bool AmOwner()
        {
            return PhotonView.AmOwner;
        }
    }
}
