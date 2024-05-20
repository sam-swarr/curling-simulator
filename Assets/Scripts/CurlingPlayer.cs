using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Curling
{
    public abstract class CurlingPlayer : MonoBehaviour
    {
        [field: SerializeField]
        public PlayerColor PlayerColor { get; private set; }

        [field: SerializeField]
        protected string PlayerID;

        [field: SerializeField]
        protected string PlayerName;

        [field: SerializeField]
        public AimingBroom AimingBroom { get; set; }

        // Cameras
        [field: SerializeField]
        public Camera CurrentCamera { get; private set; }
        [SerializeField]
        private Camera ShootingCamera;
        [SerializeField]
        private Camera MovingStoneCamera;
        [SerializeField]
        private Camera OverheadCamera;
        private Vector3 SHOOTING_CAMERA_STARTING_POSITION = new Vector3(0, 0.55f, -23.16f);
        private Quaternion SHOOTING_CAMERA_STARTING_ROTATION = new Quaternion(0.00398700312f, 0, 0, 0.999992073f);
        private Vector3 MOVING_CAMERA_OFFSET = new Vector3(0, 4.05f, -6.65f);
        private Vector3 MOVING_CAMERA_STARTING_POSITION = new Vector3(0f, 5.72f, -16f);
        private Quaternion MOVING_CAMERA_STARTING_ROTATION = new Quaternion(0.311066717f, 0, 0, 0.950388134f);
        private const float MOVING_CAMERA_Z_LIMIT = 6.2f;
        private Vector3 MOVING_CAMERA_END_OF_END_POSITION = new Vector3(0, 6, 15.25f);
        private Quaternion MOVING_CAMERA_END_OF_END_ROTATION = new Quaternion(0.585502505f, 0, 0, 0.810670614f);
        private const float MOVING_CAMERA_END_OF_END_PAN_DURATION_SECONDS = 3.0f;
        private Vector3 MovingCameraEndOfEndStartingPosition;
        private Quaternion MovingCameraEndOfEndStartingRotation;
        private float MovingCameraEndOfEndPanDurationSoFar = 0f;
        private bool HasDisplayedEndOfEndDialog = false;
        protected static EndOfEndDialog EndOfEndDialog;

        // Sweeping
        private RaycastHit SweepHit;
        private CurlingStone SweepableStone;
        private CurlingStone StoneBeingSwept;

        #region MonoBehaviour Callbacks

        public virtual void Awake()
        {
            ShootingCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            MovingStoneCamera = GameObject.FindWithTag("Moving Stone Camera").GetComponent<Camera>();
            OverheadCamera = GameObject.FindWithTag("Overhead House Camera").GetComponent<Camera>();
            CurrentCamera = ShootingCamera;

            GameManager.Instance.RegisterPlayer(this);

            // Wire up event listener.
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            // Manually invoke listener with initial data to avoid race conditions.
            OnGameStateChanged(GameManager.Instance.CurrentGameState);
        }

        public void Start()
        {
            if (IsMine())
            {
                SpawnAimingBroom();
            }
        }

        public void Update()
        {
            // Early return if this is not the local player instance.
            if (!AmOwner())
            {
                return;
            }

            // SHORTCUT FOR PLACING ROCKS QUICKLY IN HOUSE FOR TESTING
            // TODO: remove / compile this out for actual production release
            if (
                (GameManager.Instance.CurrentGameState == GameState.PlacingBroom || GameManager.Instance.CurrentGameState == GameState.YourTurnDialogActive) &&
                Input.GetKey(KeyCode.LeftControl) &&
                Input.GetMouseButtonDown(0) &&
                Utilities.IsMouseOnCameraViewport(OverheadCamera) &&
                (GameManager.IsNetworked || GameManager.Instance.CurrentPlayerID == this.GetPlayerID()))
            {
                Vector3 stoneLocation = OverheadCamera.ScreenToWorldPoint(
                    new Vector3(Input.mousePosition.x, Input.mousePosition.y, OverheadCamera.transform.position.y));

                // Calling this asynchronously otherwise in local multiplayer, we end up placing two stones per click
                // since GameManager.CurrentPlayerID is updated in between the LocalCurlingPlayer instances' Update() calls.
                StartCoroutine(PlaceStoneAfterShortDelay(stoneLocation));
            }

            // Pan camera to house if end is over
            if (GameManager.Instance.CurrentGameState == GameState.EndOfEnd && !HasDisplayedEndOfEndDialog)
            {
                if (MovingCameraEndOfEndPanDurationSoFar == 0f)
                {
                    // Disable the overhead camera view while we pan to house and display end of end dialog.
                    OverheadCamera.enabled = false;

                    // Keep track of initial camera position / rotation so we can smoothly lerp to the final position / rotation.
                    MovingCameraEndOfEndStartingPosition = CurrentCamera.transform.position;
                    MovingCameraEndOfEndStartingRotation = CurrentCamera.transform.rotation;
                }

                MovingCameraEndOfEndPanDurationSoFar += Time.deltaTime;
                float lerpFactor = Utilities.MapToRange(
                    MovingCameraEndOfEndPanDurationSoFar,
                    0f,
                    MOVING_CAMERA_END_OF_END_PAN_DURATION_SECONDS,
                    0f,
                    1f);


                CurrentCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(
                        MovingCameraEndOfEndStartingPosition,
                        MOVING_CAMERA_END_OF_END_POSITION,
                        lerpFactor),
                    Quaternion.Lerp(
                        MovingCameraEndOfEndStartingRotation,
                        MOVING_CAMERA_END_OF_END_ROTATION,
                        lerpFactor));

                if (MovingCameraEndOfEndPanDurationSoFar >= MOVING_CAMERA_END_OF_END_PAN_DURATION_SECONDS)
                {
                    // Display the end of end results dialog. When both players dismiss this
                    // dialog, the GameManager will transition to the next end.
                    HasDisplayedEndOfEndDialog = true;
                    (string playerName, int score) = GameManager.Instance.GetMostRecentScoreInfo();
                    EndOfEndDialog.Show(playerName, score);
                }

                return;
            }

            // Update the moving camera if applicable.
            if (CurrentCamera == MovingStoneCamera && GameManager.Instance.CurrentGameState == GameState.WaitingForStonesToStop)
            {
                CurrentCamera.transform.position = new Vector3(
                    CurrentCamera.transform.position.x,
                    CurrentCamera.transform.position.y,
                    Mathf.Min(CurlingStone.CurrentStone.transform.position.z + MOVING_CAMERA_OFFSET.z, MOVING_CAMERA_Z_LIMIT));

                CheckForSweeping();
            }

            // Listen for mouse click.
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                // This is the current player instance.
                if (GameManager.Instance.CurrentPlayerID == this.GetPlayerID())
                {
                    GameState state = GameManager.Instance.CurrentGameState;
                    if (state == GameState.PlacingBroom)
                    {
                        GameManager.Instance.PlaceBroom();
                    }
                    else if (state == GameState.AccuracyMeterActive)
                    {
                        // Grab aiming vector from client instance of the broom so that it accurately reflects
                        // what the player is seeing on their screen.
                        GameManager.Instance.SetAim(AimingBroom.GetNormalizedAimingLineVector());
                        PowerMeter.Instance.Enable();
                    }
                    else if (state == GameState.ShootingMeterActive)
                    {
                        GameManager.Instance.ShootStone(PowerMeter.Instance.GetShotPower());
                    }
                }

                if (SweepableStone != null)
                {
                    StoneBeingSwept = SweepableStone;
                    StoneBeingSwept.StartSweeping(PlayerColor);
                }
            }

            // Listen for mouse up
            if (StoneBeingSwept != null && Input.GetMouseButtonUp(0))
            {
                StoneBeingSwept.StopSweeping();
                StoneBeingSwept = null;
                SweepableStone = null;
            }
        }

        #endregion

        private IEnumerator PlaceStoneAfterShortDelay(Vector3 stoneLocation)
        {
            yield return new WaitForSeconds(0.05f);
            GameManager.Instance.PlaceStoneForTesting(stoneLocation);
        }

        public string GetPlayerID()
        {
            return PlayerID;
        }

        public string GetPlayerName()
        {
            return PlayerName;
        }

        private void OnGameStateChanged(GameState newGameState)
        {
            if (
                newGameState == GameState.WaitingForStonesToStop ||
                newGameState == GameState.EndOfEnd)
            {
                SwitchToMovingStoneCamera();
            }
            else
            {
                SwitchToShootingCamera();
            }

            if (newGameState == GameState.EndOfEnd)
            {
                // Reset this flag here to avoid race conditions.
                HasDisplayedEndOfEndDialog = false;
            }
        }

        private void SwitchToMovingStoneCamera()
        {
            if (CurrentCamera != MovingStoneCamera)
            {
                PowerMeter.Instance.Disable();
                AimingBroom.Disable();

                CurrentCamera.enabled = false;
                CurrentCamera = MovingStoneCamera;
                CurrentCamera.enabled = true;
                CurrentCamera.transform.SetPositionAndRotation(
                    MOVING_CAMERA_STARTING_POSITION,
                    MOVING_CAMERA_STARTING_ROTATION);
            }
        }

        private void SwitchToShootingCamera()
        {
            if (CurrentCamera != ShootingCamera)
            {
                CurrentCamera.enabled = false;
                CurrentCamera = ShootingCamera;
                CurrentCamera.enabled = true;
                CurrentCamera.transform.SetPositionAndRotation(
                    SHOOTING_CAMERA_STARTING_POSITION,
                    SHOOTING_CAMERA_STARTING_ROTATION);
            }
        }

        public void ResetForNextEnd()
        {
            MovingCameraEndOfEndPanDurationSoFar = 0f;
            OverheadCamera.enabled = true;
            SwitchToShootingCamera();
        }

        private void CheckForSweeping()
        {
            Ray ray = CurrentCamera.ScreenPointToRay(Input.mousePosition);
            bool isHit = Physics.Raycast(ray, out SweepHit, 15.0f, LayerMask.GetMask("StoneSweepingHitbox"));
            if (isHit)
            {
                CurlingStone stone = SweepHit.collider.gameObject.GetComponentInParent<CurlingStone>();
                if (stone.CanBeSwept(PlayerColor))
                {
                    SweepableStone = stone;
                    SweepableStone.SetShowSweepingBroom(true);
                }
                else
                {
                    if (SweepableStone != null)
                    {
                        SweepableStone.SetShowSweepingBroom(false);
                        SweepableStone = null;
                    }
                }
            }
            else
            {
                if (SweepableStone != null)
                {
                    SweepableStone.SetShowSweepingBroom(false);
                    SweepableStone = null;
                }
            }
        }


        #region Abstract / Virtual Functions

        protected virtual bool AmOwner()
        {
            return true;
        }

        protected virtual bool IsMine()
        {
            return true;
        }

        protected abstract void SpawnAimingBroom();

        public virtual void SetColor(PlayerColor color)
        {
            PlayerColor = color;
            AimingBroom.SetColor(color);
        }

        #endregion
    }
}
