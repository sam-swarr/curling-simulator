using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Curling
{
    public class NetworkedCurlingStone : CurlingStone, IPunObservable
    {
        public PhotonView PhotonView { get; private set; }

        /*
         * Snapshot Interpolation
         */
        private readonly SortedList<double, CurlingSnapshot> remoteClientBuffer = new SortedList<double, CurlingSnapshot>();
        double clientInterpolationTime;
        // only convert the static Interpolation function to Func<T> once to avoid allocations
        readonly Func<CurlingSnapshot, CurlingSnapshot, double, CurlingSnapshot> Interpolate = CurlingSnapshot.Interpolate;

        [Header("Buffering")]
        [Tooltip("Snapshots are buffered for sendInterval * multiplier seconds. If your expected client base is to run at non-ideal connection quality (2-5% packet loss), 3x supposedly works best.")]
        public int bufferTimeMultiplier = 1;
        [Tooltip("Buffer size limit to avoid ever growing list memory consumption attacks.")]
        public int bufferSizeLimit = 64;
        [Tooltip("Start to accelerate interpolation if buffer size is >= threshold. Needs to be larger than bufferTimeMultiplier.")]
        public int catchupThreshold = 4;
        [Tooltip("Once buffer is larger catchupThreshold, accelerate by multiplier % per excess entry.")]
        [Range(0, 1)] public float catchupMultiplier = 0.10f;
        [Tooltip("How much time, as a multiple of send interval, has passed before clearing buffers.")]
        public float bufferResetMultiplier = 5;

        [Header("Sensitivity"), Tooltip("Sensitivity of changes needed before an updated state is sent over the network")]
        public float positionSensitivity = 0.001f;
        public float rotationSensitivity = 0.001f;

        protected bool positionChanged;
        protected bool rotationChanged;

        // Used to store last sent snapshots
        protected CurlingSnapshot lastSnapshot;
        protected bool cachedSnapshotComparison;
        protected bool hasSentUnchangedPosition;

        // amount of time in seconds between serialized messages being sent between clients
        private readonly float SEND_INTERVAL = (1.0f / 10.0f); // PhotonNetwork.SerializationRate); <--- this ref to PhotonNetwork was causing the WebGL issues...
        private float BUFFER_TIME => SEND_INTERVAL * bufferTimeMultiplier;

        #region MonoBehaviour Callbacks

        public override void Awake()
        {
            base.Awake();
            PhotonView = this.GetComponent<PhotonView>();
        }

        public void OnEnable()
        {
            ResetSnapshots();
        }

        public void OnDisable()
        {
            ResetSnapshots();
        }

        #endregion


        #region Snapshot Interpolation Functions

        protected override bool IsRemoteClient()
        {
            return !PhotonNetwork.IsMasterClient;
        }

        protected override void SyncPositionUsingSnapshots()
        {
            if (SnapshotInterpolation.Compute(
                Time.timeAsDouble,
                Time.deltaTime,
                ref clientInterpolationTime,
                BUFFER_TIME,
                remoteClientBuffer,
                catchupThreshold,
                catchupMultiplier,
                Interpolate,
                out CurlingSnapshot computed))
            {
                transform.SetPositionAndRotation(computed.position, computed.rotation);
            }
        }

        private void ResetSnapshots()
        {
            remoteClientBuffer.Clear();
            // Reset interpolation time, so we start at t=0 next time.
            clientInterpolationTime = 0;
        }

        // Returns true if position and rotation are both unchanged within the configured sensitivity ranges.
        private bool CompareSnapshots(CurlingSnapshot currentSnapshot)
        {
            positionChanged = Vector3.SqrMagnitude(lastSnapshot.position - currentSnapshot.position) > positionSensitivity * positionSensitivity;
            rotationChanged = Quaternion.Angle(lastSnapshot.rotation, currentSnapshot.rotation) > rotationSensitivity;

            return (!positionChanged && !rotationChanged);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // Don't bother syncing anything while stone is still in hack.
            if (State == CurlingStoneState.InHack)
            {
                return;
            }

            if (stream.IsWriting)
            {
                CurlingSnapshot snapshot = new CurlingSnapshot(
                    0, // timestamp is irrelevant for comparison
                    0, // timestamp is irrelevant for comparison
                    transform.position,
                    transform.rotation);

                cachedSnapshotComparison = CompareSnapshots(snapshot);

                // If state has not changed meaningfully and we've already synced this state in previous update,
                // then skip writing anything over the wire.
                if (cachedSnapshotComparison && hasSentUnchangedPosition)
                {
                    return;
                }
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(LookRotationY);
                stream.SendNext(IsAboveSweepingVelocityThreshold);

                if (cachedSnapshotComparison)
                {
                    hasSentUnchangedPosition = true;
                }
                else
                {
                    hasSentUnchangedPosition = false;
                    lastSnapshot = snapshot;
                }
            }
            else if (stream.IsReading)
            {
                // info.SentServerTime is the value of PhotonNetwork.Time when the sender sent the message
                Vector3 position = (Vector3)stream.ReceiveNext();
                Quaternion rotation = (Quaternion)stream.ReceiveNext();
                LookRotationY = (float)stream.ReceiveNext();
                IsAboveSweepingVelocityThreshold = (bool)stream.ReceiveNext();
                OnRemoteClientReceiveUpdateFromMaster(position, rotation, info.SentServerTime);
            }
        }

        private void OnRemoteClientReceiveUpdateFromMaster(Vector3 position, Quaternion rotation, double masterTimestamp)
        {
            if (remoteClientBuffer.Count >= bufferSizeLimit)
            {
                return;
            }

            double timeIntervalCheck = bufferResetMultiplier * SEND_INTERVAL;

            if (remoteClientBuffer.Count > 0 && remoteClientBuffer.Values[remoteClientBuffer.Count - 1].remoteTimestamp + timeIntervalCheck < masterTimestamp)
            {
                ResetSnapshots();
            }

            CurlingSnapshot snapshot = new CurlingSnapshot(
                masterTimestamp,
                Time.timeAsDouble,
                position,
                rotation);

            SnapshotInterpolation.InsertIfNewEnough(snapshot, remoteClientBuffer);
        }

        #endregion

        public override void Initialize(PlayerColor color)
        {
            PhotonView.RPC("RPCInitialize", RpcTarget.All, color);
        }


        public override void DissolveStone()
        {
            PhotonView.RPC("RPCDissolveStone", RpcTarget.All);
        }

        public override void ShootStone(float shotPower, Vector3 normalizedVectorToBroom)
        {
            PhotonView.RPC(
                "RPCShootStone",
                RpcTarget.All,
                shotPower,
                normalizedVectorToBroom);
        }

        public override void PlaceStoneForTesting(Vector3 position)
        {
            PhotonView.RPC("RPCPlaceStoneForTesting", RpcTarget.All, position);
        }

        public override void SetSpinClockwise(bool spinClockwise)
        {
            PhotonView.RPC("RPCSetSpinClockwise", RpcTarget.All, spinClockwise);
        }

        public override void Destroy()
        {
            PhotonNetwork.Destroy(PhotonView);
        }

        public override void StartSweeping(PlayerColor sweeper)
        {
            PhotonView.RPC("RPCStartSweeping", RpcTarget.All, sweeper);
        }

        public override void StopSweeping()
        {
            PhotonView.RPC("RPCStopSweeping", RpcTarget.All);
        }

        #region RPCs

        [PunRPC]
        public void RPCInitialize(PlayerColor color, PhotonMessageInfo _)
        {
            base.Initialize(color);
        }

        [PunRPC]
        public void RPCSetSpinClockwise(bool spinClockwise)
        {
            base.SetSpinClockwise(spinClockwise);
        }

        [PunRPC]
        public void RPCDissolveStone()
        {
            base.DissolveStoneImpl();
        }

        [PunRPC]
        public void RPCShootStone(float shotPower, Vector3 normalizedVectorToBroom)
        {
            base.ShootStone(shotPower, normalizedVectorToBroom);
        }

        [PunRPC]
        public void RPCStartSweeping(PlayerColor sweeper)
        {
            base.StartSweeping(sweeper);
        }

        [PunRPC]
        public void RPCStopSweeping()
        {
            base.StopSweeping();
        }

        [PunRPC]
        public void RPCPlaceStoneForTesting(Vector3 stoneLocation)
        {
            base.PlaceStoneForTesting(stoneLocation);
        }

        #endregion
    }
}
