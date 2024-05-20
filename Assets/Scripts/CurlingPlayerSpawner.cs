using Photon.Pun;
using UnityEngine;

namespace Curling
{
    public class CurlingPlayerSpawner : MonoBehaviour
    {
        [Tooltip("Prefab for the LocalCurlingPlayer")]
        [SerializeField]
        private GameObject localCurlingPlayerPrefab;

        private void Awake()
        {
            if (GameManager.IsNetworked)
            {
                PhotonNetwork.Instantiate("NetworkedCurlingPlayer", Vector3.zero, Quaternion.identity);
            } else
            {
                // Create two player instances since this is local multiplayer.
                Object.Instantiate(localCurlingPlayerPrefab, Vector3.zero, Quaternion.identity);
                Object.Instantiate(localCurlingPlayerPrefab, Vector3.zero, Quaternion.identity);
            }
        }
    }
}
