using UnityEngine;

namespace Curling
{
    public enum CurlingStoneState
    {
        // Sitting in hack, waiting for shooter to set aim and power
        InHack,
        // Rock is being delivered
        InMotion,
        // Rock is in play but no longer moving
        AtRest,
        // Rock went out of play and is animating the dissolve shader
        Dissolving,
        // Rock went out of play
        OutOfBounds,
    }

    public abstract class CurlingStone : MonoBehaviour
    {
        public static CurlingStone CurrentStone;

        [field: SerializeField]
        public PhysicMaterial IceMaterial { get; private set; }
        public Rigidbody StoneRigidBody { get; private set; }
        [field: SerializeField]
        public bool SpinClockwise { get; private set; } = true;

        [field: SerializeField]
        public float ShotVelocity { get; private set; }

        [field: SerializeField]
        public Vector3 NormalizedVectorToBroom { get; private set; }
        [field: SerializeField]
        public bool HasBeenSpun { get; private set; } = false;
        [field: SerializeField]
        public float CurlingFactor { get; private set; } = DEFAULT_CURLING_FACTOR;

        [SerializeField]
        private CurlingStoneState _state = CurlingStoneState.InHack;
        public CurlingStoneState State
        {
            get => _state;
            set
            {
                _state = value;
            }
        }

        [SerializeField]
        private PlayerColor _color;
        public PlayerColor Color
        {
            get => _color;
            private set
            {
                _color = value;
            }
        }

        [Tooltip("Handle MeshRenderer so that it's material can be changed dynamically depending on color.")]
        [SerializeField]
        private MeshRenderer _handleMeshRenderer;
        [Tooltip("Stone MeshRenderer so that it's material can be changed dynamically.")]
        [SerializeField]
        private MeshRenderer _stoneMeshRenderer;
        [Tooltip("Material to use for red player's stone's handle.")]
        [SerializeField]
        private Material _handleMaterialRed;
        [Tooltip("Material to use for blue player's stone's handle.")]
        [SerializeField]
        private Material _handleMaterialBlue;
        [Tooltip("Material to use for red player's stone's handle dissolve effect.")]
        [SerializeField]
        private Material _handleMaterialRedDissolve;
        [Tooltip("Material to use for blue player's stone's handle dissolve effect.")]
        [SerializeField]
        private Material _handleMaterialBlueDissolve;
        [Tooltip("Material to use for red player's stone dissolve effect.")]
        [SerializeField]
        private Material _stoneMaterialRedDissolve;
        [Tooltip("Material to use for blue player's stone dissolve effect.")]
        [SerializeField]
        private Material _stoneMaterialBlueDissolve;
        [Tooltip("Broom handle MeshRenderer so that it's material can be changed dynamically depending on color.")]
        [SerializeField]
        private MeshRenderer _broomHandleMeshRenderer;
        [Tooltip("Broom pad MeshRenderer so that it's material can be changed dynamically depending on color.")]
        [SerializeField]
        private MeshRenderer _broomPadMeshRenderer;
        [Tooltip("Material to use for red player's broom handle.")]
        [SerializeField]
        private Material _broomHandleMaterialRed;
        [Tooltip("Material to use for blue player's broom handle.")]
        [SerializeField]
        private Material _broomHandleMaterialBlue;
        [Tooltip("Material to use for red player's broom pad.")]
        [SerializeField]
        private Material _broomPadMaterialRed;
        [Tooltip("Material to use for blue player's broom pad.")]
        [SerializeField]
        private Material _broomPadMaterialBlue;

        private bool setGameState = false;

        // Constants
        private Quaternion HANDLE_ANGLE_FOR_CW_ROTATION = Quaternion.Euler(0, -45, 0);
        private Quaternion HANDLE_ANGLE_FOR_CCW_ROTATION = Quaternion.Euler(0, 45, 0);

        private const float VELOCITY_MIN = 1.8f;
        private const float VELOCITY_MAX = 3.4f;

        private const float START_HANDLE_SPIN_Z = -14.13838f;
        private const float CLOSE_HOG_LINE_Z = -11.1239f;
        private const float FAR_HOG_LINE_Z = 11.124f;
        private const float FAR_BACK_LINE_Z = 19.389f;
        private const float X_LIMIT = 2.2f;

        private const float DEFAULT_CURLING_FACTOR = 10.0f;
        private const float MAX_SWEEPING_CURLING_FACTOR = 5.0f;
        private const float START_SWEEPING_CURLING_FACTOR_CHANGE_RATE = 0.1f;
        private const float STOP_SWEEPING_CURLING_FACTOR_CHANGE_RATE = 0.2f;

        private const float DEFAULT_FRICTION = 0.005f;
        private const float SWEEPING_FRICTION = 0.00465f;

        private Vector3 SPIN_TORQUE = new Vector3(0, 10.0f, 0);
        private const float INCREASE_ANGULAR_DRAG_THRESHOLD = 0.1f;
        private const float DEFAULT_ANGULAR_DRAG = 0.05f;
        private const float INCREASED_ANGULAR_DRAG = 1.0f;
        // A stone traveling at this velocity or higher is eligible to be swept.
        private const float SWEEPING_THRESHOLD = 0.005f;
        // At this velocity or less, the rock is considered to have stopped moving.
        // This should stay in sync with the sensitivity values on the NetworkTransform component.
        private const float MOVING_THRESHOLD = 0.001f;
        private const float BROOM_HOVER_Y_OFFSET = 0.13f;
        private Vector3 BROOM_DISTANCE_FROM_STONE = new Vector3(0, 0, 0.5f);
        private const float BROOM_SWEEP_X_MOVEMENT_RATE = 3f;
        private const float BROOM_SWEEP_X_MOVEMENT_RANGE = 0.25f;

        // Sweeping
        [field: SerializeField]
        public bool IsAboveSweepingVelocityThreshold { get; protected set; } = false;
        [field: SerializeField]
        public bool IsBeingSwept { get; private set; } = false;
        [field: SerializeField]
        public float LookRotationY { get; protected set; } = 0f;
        [field: SerializeField]
        public bool ShowSweepingBroom { get; private set; } = false;
        private GameObject SweepingHitbox;
        private GameObject SweepingBroom;
        private GameObject BroomLocation;
        private float BroomSweepXOffset = 0f;
        private float BroomSweepXMovementDirection = 1;

        // Dissolve
        private const float DISSOLVE_DURATION_SECONDS = 2.0f;
        private float DissolveDurationSoFar = 0f;


        #region MonoBehaviour Callbacks

        public virtual void Awake()
        {
            CurlingStone.CurrentStone = this;

            StoneRigidBody = this.GetComponent<Rigidbody>();
            IceMaterial = GameObject.FindWithTag("Ice").GetComponent<BoxCollider>().material;
            State = CurlingStoneState.InHack;
            SweepingHitbox = transform.GetChild(0).gameObject;
            SweepingBroom = transform.GetChild(1).gameObject;
            BroomLocation = transform.GetChild(2).gameObject;
        }

        public void Update()
        {
            // While in the hack, have each client slerp between handle angles as it changes.
            if (State == CurlingStoneState.InHack)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    SpinClockwise ? HANDLE_ANGLE_FOR_CW_ROTATION : HANDLE_ANGLE_FOR_CCW_ROTATION,
                    Time.deltaTime * 10);
            }
            // Animate the dissolve if the stone is going out of bounds
            else if (State == CurlingStoneState.Dissolving)
            {
                DissolveDurationSoFar += Time.deltaTime;
                float dissolveFactor = Utilities.MapToRange(DissolveDurationSoFar, 0f, DISSOLVE_DURATION_SECONDS, 0f, 1f);
                _stoneMeshRenderer.material.SetFloat("Threshold", dissolveFactor);
                _handleMeshRenderer.material.SetFloat("Threshold", dissolveFactor);

                // If we're done animating the dissolve, then transition to OutOfBounds.
                if (dissolveFactor >= 1.0f)
                {
                    State = CurlingStoneState.OutOfBounds;
                    gameObject.SetActive(false);
                }
            }
            // Else, when rock is moving have remote clients use snapshot interpolation to sync the position / rotation.
            else if (IsRemoteClient())
            {
                SyncPositionUsingSnapshots();
            }

            // Reposition broom location in front of stone's direction of movement.
            BroomLocation.transform.position = transform.position;
            BroomLocation.transform.eulerAngles = new Vector3(
                BroomLocation.transform.eulerAngles.x,
                LookRotationY,
                BroomLocation.transform.eulerAngles.z);
            BroomLocation.transform.Translate(BROOM_DISTANCE_FROM_STONE);

            // Keep sweeping hitbox pointed in direction of rock movement by having it match the broom location's y rotation.
            SweepingHitbox.transform.eulerAngles = new Vector3(
                SweepingHitbox.transform.eulerAngles.x,
                BroomLocation.transform.eulerAngles.y,
                SweepingHitbox.transform.eulerAngles.z);

            if (ShowSweepingBroom || IsBeingSwept)
            {
                SweepingBroom.SetActive(true);
                if (IsBeingSwept)
                {
                    // If we're sweeping, animate the broom moving back and forth.
                    SweepingBroom.transform.position = BroomLocation.transform.position;
                    SweepingBroom.transform.eulerAngles = BroomLocation.transform.eulerAngles;
                    SweepingBroom.transform.Translate(new Vector3(BroomSweepXOffset, 0, 0));

                    BroomSweepXOffset += BroomSweepXMovementDirection * BROOM_SWEEP_X_MOVEMENT_RATE * Time.deltaTime;
                    if (BroomSweepXOffset >= BROOM_SWEEP_X_MOVEMENT_RANGE)
                    {
                        BroomSweepXMovementDirection *= -1;
                        BroomSweepXOffset = BROOM_SWEEP_X_MOVEMENT_RANGE;
                    }
                    else if (BroomSweepXOffset <= -BROOM_SWEEP_X_MOVEMENT_RANGE)
                    {
                        BroomSweepXMovementDirection *= -1;
                        BroomSweepXOffset = -BROOM_SWEEP_X_MOVEMENT_RANGE;
                    }
                }
                else
                {
                    // Else the player is hovering in front of rock and not yet sweeping. Position the broom relative
                    // to stone, rotating to account for stone's direction of movement by having it mirror the broom
                    // location's position / rotation.
                    if (SweepingBroom.activeSelf)
                    {
                        SweepingBroom.transform.position = new Vector3(
                            BroomLocation.transform.position.x,
                            BroomLocation.transform.position.y + BROOM_HOVER_Y_OFFSET,
                            BroomLocation.transform.position.z);
                        SweepingBroom.transform.eulerAngles = BroomLocation.transform.eulerAngles;
                    }
                }
            }
            else
            {
                SweepingBroom.SetActive(false);
            }

            // Check if rock is out of bounds. We check here on both master and remote client and call DissolveStoneImpl() as
            // a non-RPC function so that each client animates the dissolve at the correct time. (Previously, master client
            // issued an RPC for DissolveStone when the stone went out of bounds, but because the remote client is a few
            // snapshots behind, it would begin animating the dissolve before the stone appeared to be out of bounds).
            if (transform.position.z > FAR_BACK_LINE_Z ||
                transform.position.x < -X_LIMIT ||
                transform.position.x > X_LIMIT)
            {
                if (!(State == CurlingStoneState.Dissolving || State == CurlingStoneState.OutOfBounds))
                {
                    DissolveStoneImpl();
                }
            }
        }

        public void FixedUpdate()
        {
            // Only simulate stone physics on master client. Stone positions will be synced to other clients.
            if (IsRemoteClient())
            {
                return;
            }

            if (State == CurlingStoneState.InMotion)
            {
                // stone is still being delivered; keep constant velocity up until hog line
                if (transform.position.z < CLOSE_HOG_LINE_Z)
                {
                    StoneRigidBody.velocity = NormalizedVectorToBroom * ShotVelocity;
                }

                // simulate curler spinning handle as rock is about to be released
                if (transform.position.z > START_HANDLE_SPIN_Z && transform.position.z < CLOSE_HOG_LINE_Z)
                {
                    float degrees = (CLOSE_HOG_LINE_Z - transform.position.z) / (CLOSE_HOG_LINE_Z - START_HANDLE_SPIN_Z) * 45;
                    transform.eulerAngles = new Vector3(transform.eulerAngles.x, SpinClockwise ? -degrees : degrees, transform.eulerAngles.z);
                }

                // apply spin as rock is released
                if (transform.position.z > CLOSE_HOG_LINE_Z && !HasBeenSpun)
                {
                    HasBeenSpun = true;
                    StoneRigidBody.AddTorque(SPIN_TORQUE * (SpinClockwise ? 1 : -1));
                }

                // switch to WaitingForStonesToStop game state, which will enable the moving camera
                if (transform.position.z > CLOSE_HOG_LINE_Z && !setGameState)
                {
                    setGameState = true;
                    GameManager.Instance.WaitingForStonesToStop();
                }

                // adjust the curling factor based on sweeping and make the stone curl by applying a force
                if (IsBeingSwept)
                {
                    CurlingFactor = Mathf.Max(CurlingFactor - START_SWEEPING_CURLING_FACTOR_CHANGE_RATE, MAX_SWEEPING_CURLING_FACTOR);
                }
                else
                {
                    CurlingFactor = Mathf.Min(CurlingFactor + STOP_SWEEPING_CURLING_FACTOR_CHANGE_RATE, DEFAULT_CURLING_FACTOR);
                }
                StoneRigidBody.AddForce(CurlingFactor * Time.deltaTime * (SpinClockwise ? Vector3.right : Vector3.left));
            }

            // increase angular drag at low speed to reduce spinning more realistically
            StoneRigidBody.angularDrag = StoneRigidBody.velocity.magnitude < INCREASE_ANGULAR_DRAG_THRESHOLD ?
                INCREASED_ANGULAR_DRAG : DEFAULT_ANGULAR_DRAG;

            if (StoneRigidBody.velocity.magnitude > SWEEPING_THRESHOLD)
            {
                IsAboveSweepingVelocityThreshold = true;
                LookRotationY = Quaternion.LookRotation(StoneRigidBody.velocity).eulerAngles.y;
            }
            else
            {
                IsAboveSweepingVelocityThreshold = false;
            }
        }

        #endregion

        protected virtual bool IsRemoteClient()
        {
            return false;
        }

        protected virtual void SyncPositionUsingSnapshots() 
        {
            throw new System.NotImplementedException();
        }

        #region Sweeping
        public bool CanBeSwept(PlayerColor sweeperColor)
        {
            // TODO: support being able to sweep opponent's stone if past tee line, etc.
            if (Color != sweeperColor)
            {
                return false;
            }

            return IsAboveSweepingVelocityThreshold;
        }

        public void SetShowSweepingBroom(bool show)
        {
            ShowSweepingBroom = show;
        }

        #endregion

        public bool CheckIfMoving()
        {
            if (!gameObject.activeSelf || State == CurlingStoneState.Dissolving || State == CurlingStoneState.OutOfBounds)
            {
                return false;
            }

            if (transform.position.z > CLOSE_HOG_LINE_Z)
            {
                if (StoneRigidBody.velocity.magnitude < MOVING_THRESHOLD)
                {
                    if (transform.position.z < FAR_HOG_LINE_Z)
                    {
                        // Call regular DissolveStone() here since in networked mode, only master client wil execute this code.
                        // That way we trigger the DissolveStone RPC and both clients will be updated.
                        DissolveStone();
                    }
                    else
                    {
                        State = CurlingStoneState.AtRest;
                    }
                    return false;
                }
                else
                {
                    State = CurlingStoneState.InMotion;
                    return true;
                }
            }

            return true;
        }

        private float GetVelocityFromShotPower(float shotPower)
        {
            // 2.22 --- guard
            // 2.31 --- top 12
            // 2.37 --- button
            // 2.77 --- takeout
            float velocity = 1.36579f * shotPower + 1.72579f;
            print("velocity: " + velocity);
            return velocity;
        }

        public void StopSweepingImpl()
        {
            IsBeingSwept = false;
            ShowSweepingBroom = false;
            IceMaterial.dynamicFriction = DEFAULT_FRICTION;
            IceMaterial.staticFriction = DEFAULT_FRICTION;
        }

        // Public since we need to directly call this in the unit tests.
        public void InitializeImpl(PlayerColor color)
        {
            Color = color;
            if (color == PlayerColor.Red)
            {
                _handleMeshRenderer.material = _handleMaterialRed;
                _broomHandleMeshRenderer.material = _broomHandleMaterialRed;
                _broomPadMeshRenderer.material = _broomPadMaterialRed;
            }
            else
            {
                _handleMeshRenderer.material = _handleMaterialBlue;
                _broomHandleMeshRenderer.material = _broomHandleMaterialBlue;
                _broomPadMeshRenderer.material = _broomPadMaterialBlue;
            }
        }


        #region Abstracted API for GameManager / CurlingPlayer

        public virtual void Initialize(PlayerColor color)
        {
            InitializeImpl(color);
        }

        public virtual void DissolveStone()
        {
            DissolveStoneImpl();
        }

        protected virtual void DissolveStoneImpl()
        {
            // Update state and materials. The Update loop will take care of animating the dissolve
            // and then transitioning the stone to the OutOfBounds state after the animation is done.
            State = CurlingStoneState.Dissolving;
            // Set this so physics no longer act on the stone.
            StoneRigidBody.isKinematic = true;
            if (Color == PlayerColor.Red)
            {
                _handleMeshRenderer.material = _handleMaterialRedDissolve;
                _stoneMeshRenderer.material = _stoneMaterialRedDissolve;
            }
            else
            {
                _handleMeshRenderer.material = _handleMaterialBlueDissolve;
                _stoneMeshRenderer.material = _stoneMaterialBlueDissolve;
            }
        }

        public virtual void ShootStone(float shotPower, Vector3 normalizedVectorToBroom)
        {
            State = CurlingStoneState.InMotion;
            ShotVelocity = GetVelocityFromShotPower(shotPower);
            NormalizedVectorToBroom = normalizedVectorToBroom;
        }

        public virtual void PlaceStoneForTesting(Vector3 position)
        {
            transform.position = position;
            State = CurlingStoneState.AtRest;
        }

        public virtual void SetSpinClockwise(bool spinClockwise)
        {
            SpinClockwise = spinClockwise;
        }

        public virtual void Destroy()
        {
            Destroy(gameObject);
        }

        public virtual void StartSweeping(PlayerColor sweeper)
        {
            if (CanBeSwept(sweeper))
            {
                BroomSweepXOffset = 0;
                IsBeingSwept = true;
                IceMaterial.dynamicFriction = SWEEPING_FRICTION;
                IceMaterial.staticFriction = SWEEPING_FRICTION;
            }
            else
            {
                Debug.LogWarning($"Received bad StartSweeping RPC from player ({sweeper})");
            }
        }

        public virtual void StopSweeping()
        {
            StopSweepingImpl();
        }

        #endregion
    }
}