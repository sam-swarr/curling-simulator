using Photon.Pun;
using System.Collections;
using UnityEngine;

namespace Curling
{
    public class NetworkedGameManager : GameManager
    {
        public PhotonView PhotonView { get; private set; }

        #region MonoBehaviour Callbacks

        public override void Awake()
        {
            base.Awake();
            PhotonView = this.GetComponent<PhotonView>();
            GameManager.IsNetworked = true;
        }

        public override void Update()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                base.Update();
            }
        }

        #endregion


        #region Hooks

        protected override bool OnStartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("Calling StartGame() on client other than master!");
                return false;
            }
            return true;
        }

        protected override bool OnNextShot()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("Calling NextShot() on client other than master!");
                return false;
            }
            return true;
        }

        protected override bool OnSpawnStone()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("Calling SpawnStone() on client other than master!");
                return false;
            }
            return true;
        }

        #endregion


        private IEnumerator StartNextEndAfter2Seconds()
        {
            yield return new WaitForSeconds(2);
            StartNextEnd();
        }


        #region Virtual Overrides

        public override void SetCurlDirection(bool spinClockwise)
        {
            PhotonView.RPC("RPCSetCurlDirection", RpcTarget.MasterClient, spinClockwise);
        }

        public override void PlayerReadyForNextEnd()
        {
            PhotonView.RPC("RPCPlayerReadyForNextEnd", RpcTarget.All, NetworkedCurlingPlayer.LocalPlayerInstance.PlayerColor);
        }

        protected override void UpdateEndScores(int endNumber, int redPoints, int bluePoints)
        {
            PhotonView.RPC("RPCUpdateEndScores", RpcTarget.All, endNumber, redPoints, bluePoints);
        }

        protected override bool IsMasterClient()
        {
            return PhotonNetwork.IsMasterClient;
        }

        #endregion


        #region Abstract Function Implementations

        protected override void SetCurrentPlayer(string playerID)
        {
            PhotonView.RPC("RPCSetCurrentPlayer", RpcTarget.All, playerID);
        }

        protected override void SetEndNumberAndHammer(int endNumber, PlayerColor playerWithHammer)
        {
            PhotonView.RPC("RPCSetEndNumberAndHammer", RpcTarget.All, endNumber, playerWithHammer);
        }

        protected override void SetCurrentStoneIndex(int stoneIndex)
        {
            PhotonView.RPC("RPCSetCurrentStoneIndex", RpcTarget.All, stoneIndex);
        }

        protected override void SetCurrentGameState(GameState gameState)
        {
            PhotonView.RPC("RPCSetCurrentGameState", RpcTarget.All, gameState);
        }

        protected override CurlingStone CreateAndInitializeStone(
            Vector3 initialPosition,
            Quaternion initialRotation,
            PlayerColor stoneColor)
        {
            GameObject s = PhotonNetwork.InstantiateRoomObject("NetworkedCurlingStone", initialPosition, initialRotation);
            CurlingStone stone = s.GetComponent<CurlingStone>();
            stone.Initialize(GetCurrentPlayer().PlayerColor);
            return stone;
        }

        public override void SetAim(Vector3 normalizedAimingLineVector)
        {
            PhotonView.RPC("RPCSetAim", RpcTarget.All, normalizedAimingLineVector);
        }

        public override void ShootStone(float shotPower)
        {
            PhotonView.RPC("RPCShootStone", RpcTarget.All, shotPower);
        }

        public override void PlaceStoneForTesting(Vector3 position)
        {
            PhotonView.RPC("RPCPlaceStoneForTesting", RpcTarget.All, position);
        }

        #endregion


        #region RPCs

        [PunRPC]
        public void RPCSetCurrentGameState(GameState gameState)
        {
            base.SetCurrentGameState(gameState);
        }

        [PunRPC]
        public void RPCSetCurrentPlayer(string playerID)
        {
            base.SetCurrentPlayer(playerID);
        }

        [PunRPC]
        public void RPCSetCurrentStoneIndex(int stoneIndex)
        {
            base.SetCurrentStoneIndex(stoneIndex);
        }

        [PunRPC]
        public void RPCSetEndNumberAndHammer(int endNumber, PlayerColor hammer)
        {
            base.SetEndNumberAndHammer(endNumber, hammer);
        }

        [PunRPC]
        public void RPCSetCurlDirection(bool spinClockwise)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("Calling SetCurlDirection() on client other than master!");
                return;
            }
            base.SetCurlDirection(spinClockwise);
        }

        [PunRPC]
        public void RPCSetAim(Vector3 normalizedAimingLineVector)
        {
            base.SetAim(normalizedAimingLineVector);
        }

        [PunRPC]
        public void RPCShootStone(float shotPower)
        {
            base.ShootStone(shotPower);
        }

        [PunRPC]
        public void RPCUpdateEndScores(int endNumber, int redPoints, int bluePoints)
        {
            base.UpdateEndScores(endNumber, redPoints, bluePoints);
        }

        [PunRPC]
        public void RPCPlayerReadyForNextEnd(PlayerColor playerWhoIsReady)
        {
            if (playerWhoIsReady == PlayerColor.Red)
            {
                RedPlayerReadyForNextEnd = true;
            }
            if (playerWhoIsReady == PlayerColor.Blue)
            {
                BluePlayerReadyForNextEnd = true;
            }

            if (RedPlayerReadyForNextEnd && BluePlayerReadyForNextEnd)
            {
                StartCoroutine(StartNextEndAfter2Seconds());
            }
        }

        [PunRPC]
        public void RPCPlaceStoneForTesting(Vector3 position)
        {
            base.PlaceStoneForTesting(position);
        }

        #endregion
    }
}