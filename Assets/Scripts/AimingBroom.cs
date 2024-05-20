using UnityEngine;

namespace Curling
{
    public abstract class AimingBroom : MonoBehaviour
    {
        [field: SerializeField]
        public PlayerColor Color { get; private set; }

        [SerializeField]
        protected string OwningPlayerID;

        [SerializeField]
        private Camera _overheadHouseCamera;
        [SerializeField]
        private GameObject _stoneSpawnLocation;

        [Tooltip("LineRenderer for the colored cone that makes up the range of possible aims.")]
        [SerializeField]
        private LineRenderer _coneRenderer;
        [Tooltip("LineRenderer for the colored line down the center of the aiming cone.")]
        [SerializeField]
        private LineRenderer _lineRenderer;
        [Tooltip("LineRenderer for the white moving line that oscillates while aiming.")]
        [SerializeField]
        private LineRenderer _movingLineRenderer;
        [Tooltip("Endpoint of the moving aiming line. Will oscillate back and forth.")]
        [SerializeField]
        private GameObject _movingLineEndpoint;
        [Tooltip("Material for the red player's aiming cone.")]
        [SerializeField]
        private Material _coneMaterialRed;
        [Tooltip("Material for the blue player's aiming cone.")]
        [SerializeField]
        private Material _coneMaterialBlue;
        [Tooltip("Material for the red player's aiming line.")]
        [SerializeField]
        private Material _lineMaterialRed;
        [Tooltip("Material for the blue player's aiming line.")]
        [SerializeField]
        private Material _lineMaterialBlue;
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

        // how far left/right the broom can move on the x-axis
        private const float X_RANGE = 2.28f;
        // Z-value to use when aiming using the main camera
        private const float FIXED_Z = 17.3735f;

        private float _movingLineXOffset = -MOVING_LINE_RANGE;
        private float _movingLineDirection = 1;

        private const float MOVING_LINE_RANGE = 1.43f;
        private const float MOVING_LINE_SPEED = 5f;
        private const float LINE_RENDERER_Y = 0.001f;
        private const float LINE_RENDERER_Z_OFFSET_FROM_STONE_SPAWN = 0.05f;


        #region Monobehaviour Callbacks
        public virtual void Awake()
        {
            _overheadHouseCamera = GameObject.FindWithTag("Overhead House Camera").GetComponent<Camera>();
            _stoneSpawnLocation = GameObject.FindWithTag("Stone Spawn Location");

            _coneRenderer.enabled = false;
            _lineRenderer.enabled = false;
            _movingLineRenderer.enabled = false;

            // Wire up event listener.
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            // Manually invoke listener with initial data to avoid race conditions.
            OnGameStateChanged(GameManager.Instance.CurrentGameState);
        }

        public void Start()
        {
            // Register broom here so that LocalCurlingPlayer has a chance to initialize
            // SetOwningPlayerID() beforehand.
            GameManager.Instance.RegisterBroom(this);
        }

        public void Update()
        {
            GameState gameState = GameManager.Instance.CurrentGameState;
            if (gameState == GameState.PlacingBroom)
            {
                // Only the owning client can move the broom.
                if (AmOwner())
                {
                    if (Utilities.IsMouseOnCameraViewport(_overheadHouseCamera))
                    {
                        transform.position = _overheadHouseCamera.ScreenToWorldPoint(
                            new Vector3(Input.mousePosition.x, Input.mousePosition.y, _overheadHouseCamera.transform.position.y));
                    }
                    else
                    {
                        float xPercent = Mathf.Clamp(((2 * Input.mousePosition.x) / Screen.width) - 0.5f, 0.0f, 1.0f);
                        float newXValue = -X_RANGE + (xPercent * X_RANGE * 2);
                        transform.position = new Vector3(newXValue, transform.position.y, FIXED_Z);
                    }
                }
            }

            // Render the aiming cone / line on all clients. Do this during PlacingBroom as well as the AccuracyMeterActive
            // in case there's some delayed updates to the broom's position that sync after state has switched.
            if (gameState == GameState.PlacingBroom || gameState == GameState.AccuracyMeterActive)
            {
                _coneRenderer.enabled = true;
                _coneRenderer.SetPosition(0, new Vector3(_stoneSpawnLocation.transform.position.x, LINE_RENDERER_Y, _stoneSpawnLocation.transform.position.z + LINE_RENDERER_Z_OFFSET_FROM_STONE_SPAWN));
                _coneRenderer.SetPosition(1, new Vector3(transform.position.x, LINE_RENDERER_Y, transform.position.z));
                _lineRenderer.enabled = true;
                _lineRenderer.SetPosition(0, new Vector3(_stoneSpawnLocation.transform.position.x, LINE_RENDERER_Y * 2, _stoneSpawnLocation.transform.position.z + LINE_RENDERER_Z_OFFSET_FROM_STONE_SPAWN));
                _lineRenderer.SetPosition(1, new Vector3(transform.position.x, LINE_RENDERER_Y * 2, transform.position.z));
            }

            if (gameState == GameState.AccuracyMeterActive)
            {
                if (AmOwner())
                {
                    _movingLineXOffset += (_movingLineDirection * MOVING_LINE_SPEED * Time.deltaTime);

                    if (_movingLineXOffset > MOVING_LINE_RANGE)
                    {
                        _movingLineXOffset = MOVING_LINE_RANGE;
                        _movingLineDirection *= -1;
                    }
                    if (_movingLineXOffset < -MOVING_LINE_RANGE)
                    {
                        _movingLineXOffset = -MOVING_LINE_RANGE;
                        _movingLineDirection *= -1;
                    }

                    _movingLineEndpoint.transform.position = new Vector3(
                        _movingLineXOffset,
                        _movingLineEndpoint.transform.position.y,
                        _movingLineEndpoint.transform.position.z);
                }
            }

            // Render the moving line on all clients. Do this during AccuracyMeterActive as well as the ShootingMeterActive
            // in case there's some delayed updates to the line endpoint's position that sync after state has switched.
            if (gameState == GameState.AccuracyMeterActive || gameState == GameState.ShootingMeterActive)
            {
                _movingLineRenderer.enabled = true;
                _movingLineRenderer.SetPosition(0, new Vector3(_stoneSpawnLocation.transform.position.x, LINE_RENDERER_Y * 3, _stoneSpawnLocation.transform.position.z + LINE_RENDERER_Z_OFFSET_FROM_STONE_SPAWN));
                _movingLineRenderer.SetPosition(1, new Vector3(transform.position.x + _movingLineEndpoint.transform.position.x, LINE_RENDERER_Y * 3, transform.position.z));
            }
        }

        #endregion


        protected virtual bool AmOwner()
        {
            return true;
        }

        public string GetOwningPlayerID()
        {
            return OwningPlayerID;
        }

        public void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.PlacingBroom)
            {
                if (GameManager.Instance.GetCurrentPlayer().PlayerColor == Color)
                {
                    Enable();
                }
                else
                {
                    Disable();
                }
            }
        }

        public Vector3 GetNormalizedAimingLineVector()
        {
            Vector3 aim = _movingLineRenderer.GetPosition(1);
            return new Vector3(aim.x - _stoneSpawnLocation.transform.position.x, 0, aim.z - _stoneSpawnLocation.transform.position.z).normalized;
        }

        public void Enable()
        {
            gameObject.SetActive(true);
            _coneRenderer.positionCount = 2;
            _lineRenderer.positionCount = 2;
            _movingLineRenderer.positionCount = 2;
            _movingLineXOffset = -MOVING_LINE_RANGE;
            _movingLineDirection = 1;
        }

        public void Disable()
        {
            _coneRenderer.enabled = false;
            _lineRenderer.enabled = false;
            _movingLineRenderer.enabled = false;
            gameObject.SetActive(false);
        }

        public void SetColor(PlayerColor color)
        {
            Color = color;
            if (color == PlayerColor.Red)
            {
                _coneRenderer.material = _coneMaterialRed;
                _lineRenderer.material = _lineMaterialRed;
                _broomHandleMeshRenderer.material = _broomHandleMaterialRed;
                _broomPadMeshRenderer.material = _broomPadMaterialRed;
            }
            else
            {
                _coneRenderer.material = _coneMaterialBlue;
                _lineRenderer.material = _lineMaterialBlue;
                _broomHandleMeshRenderer.material = _broomHandleMaterialBlue;
                _broomPadMeshRenderer.material = _broomPadMaterialBlue;
            }
        }
    }
}

