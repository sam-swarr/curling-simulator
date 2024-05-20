using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Curling
{
    public abstract class GameManager : MonoBehaviour
    {
        public static GameManager Instance = null;
        public static bool IsNetworked = false;

        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action<string> OnCurrentPlayerIDChanged;
        public event Action<bool> OnCurlDirectionChanged;
        public event Action<int /* end number */, PlayerColor /* who has hammer */> OnEndNumberChanged;
        public event Action<int /* num red stones */, int /* num blue stones */> OnNextStoneSpawned;
        public event Action<
            int, // end number
            int, // red score for end
            int, // blue score for end
            int, // red total score
            int // blue total score
        > OnEndScored;
        public event Action<
            bool, // red player ready
            bool // blue player ready
        > OnPlayerReadyForNextEnd;

        // GameState
        [SerializeField]
        private GameState _currentGameState = GameState.WaitingForPlayersToConnect;
        public GameState CurrentGameState
        {
            get => _currentGameState;
            protected set
            {
                _currentGameState = value;
                OnGameStateChanged?.Invoke(value);
            }
        }

        // Scoring
        [SerializeField]
        private int _endNumber;
        public int EndNumber
        {
            get => _endNumber;
            protected set
            {
                _endNumber = value;
                OnEndNumberChanged?.Invoke(value, Hammer);
            }
        }
        [SerializeField]
        private (int red, int blue)[] EndScores = new (int, int)[NUM_ENDS_IN_MATCH];

        public PlayerColor Hammer { get; protected set; }

        // Players
        [SerializeField]
        protected CurlingPlayer Player1;
        [SerializeField]
        protected CurlingPlayer Player2;
        [SerializeField]
        private string _currentPlayerID;
        public string CurrentPlayerID
        {
            get => _currentPlayerID;
            protected set
            {
                _currentPlayerID = value;
                OnCurrentPlayerIDChanged?.Invoke(value);
            }
        }
        [SerializeField]
        private bool _redPlayerReadyForNextEnd = false;
        public bool RedPlayerReadyForNextEnd
        {
            get => _redPlayerReadyForNextEnd;
            protected set
            {
                _redPlayerReadyForNextEnd = value;
                OnPlayerReadyForNextEnd?.Invoke(value, BluePlayerReadyForNextEnd);
            }
        }
        [SerializeField]
        private bool _bluePlayerReadyForNextEnd = false;
        public bool BluePlayerReadyForNextEnd
        {
            get => _bluePlayerReadyForNextEnd;
            protected set
            {
                _bluePlayerReadyForNextEnd = value;
                OnPlayerReadyForNextEnd?.Invoke(RedPlayerReadyForNextEnd, value);
            }
        }

        // Stones
        [SerializeField]
        private int _currentStoneIndex = 0;
        public int CurrentStoneIndex
        {
            get => _currentStoneIndex;
            protected set
            {
                _currentStoneIndex = value;
                float halfOfTotalStones = (NUM_STONES_IN_END - value) / 2.0f;
                int redStonesLeft = (int)(Hammer == PlayerColor.Red ? Math.Ceiling(halfOfTotalStones) : Math.Floor(halfOfTotalStones));
                int blueStonesLeft = (int)(Hammer == PlayerColor.Blue ? Math.Ceiling(halfOfTotalStones) : Math.Floor(halfOfTotalStones));
                OnNextStoneSpawned?.Invoke(redStonesLeft, blueStonesLeft);
            }
        }
        private const int NUM_STONES_IN_END = 16;
        private const int NUM_ENDS_IN_MATCH = 8;
        private static Vector3 CENTER_OF_HOUSE = new Vector3(0, 0, 17.375f);
        // A stone must be this distance or closer to the center of the house to count.
        // This is 6 feet + 5.5 inches in meters (distance from center to edge of 12ft circle + half diameter of a stone).
        private static float MAX_DISTANCE_FROM_CENTER_OF_HOUSE = 1.9685f;
        [SerializeField]
        private CurlingStone[] Stones = new CurlingStone[NUM_STONES_IN_END];
        private GameObject _stoneSpawnLocation;
        private bool _nextShotTimerActive = false;

        private Vector3 NormalizedVectorToBroom;

        // Curling Direction
        [SerializeField]
        private bool _spinClockwise = true;
        public bool SpinClockwise
        {
            get => _spinClockwise;
            private set
            {
                _spinClockwise = value;
                OnCurlDirectionChanged?.Invoke(value);
            }
        }


        #region MonoBehaviour Callbacks

        public virtual void Awake()
        {
            GameManager.Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public virtual void Update()
        {
            CheckForMovingStones();
        }

        #endregion


        #region Registering Players

        public void RegisterPlayer(CurlingPlayer cp)
        {
            if (Player1 == null)
            {
                Player1 = cp;
            }
            else if (Player2 == null)
            {
                Player2 = cp;
            }
            else
            {
                Debug.LogError("Trying to add a player when both players are already set.");
                return;
            }
        }

        public void RegisterBroom(AimingBroom broom)
        {
            if (Player1.GetPlayerID() == broom.GetOwningPlayerID())
            {
                Player1.AimingBroom = broom;
            }
            else if (Player2.GetPlayerID() == broom.GetOwningPlayerID())
            {
                Player2.AimingBroom = broom;
            }
            else
            {
                Debug.LogError("Trying to register a broom whose owner could not be found.");
                return;
            }

            if (Player1 != null && Player1.AimingBroom != null && Player2 != null && Player2.AimingBroom != null)
            {
                if (IsMasterClient())
                {
                    StartGame();
                }
            }
        }

        #endregion


        #region Player Getters

        public CurlingPlayer GetCurrentPlayer()
        {
            return CurrentPlayerID == Player1.GetPlayerID() ? Player1 : Player2;
        }

        public CurlingPlayer GetPlayer(PlayerColor color)
        {
            return Player1.PlayerColor == color ? Player1 : Player2;
        }

        #endregion


        #region Virtual / Abstract Functions

        protected virtual void SetCurrentGameState(GameState gameState)
        {
            CurrentGameState = gameState;
        }

        protected virtual void SetCurrentPlayer(string playerID)
        {
            CurrentPlayerID = playerID;
        }

        protected virtual void SetCurrentStoneIndex(int stoneIndex)
        {
            CurrentStoneIndex = stoneIndex;
        }

        protected virtual void SetEndNumberAndHammer(int endNumber, PlayerColor playerWithHammer)
        {
            // Make sure hammer is set before setting EndNumber so that OnEndNumberChanged event gets latest Hammer value
            Hammer = playerWithHammer;
            EndNumber = endNumber;
        }

        protected abstract CurlingStone CreateAndInitializeStone(
            Vector3 initialPosition,
            Quaternion initialRotation,
            PlayerColor stoneColor);

        #endregion


        #region Hooks

        protected virtual bool OnStartGame()
        {
            return true;
        }

        protected virtual bool OnNextShot()
        {
            return true;
        }

        protected virtual bool OnSpawnStone()
        {
            return true;
        }

        #endregion


        #region Game Flow

        protected void StartGame()
        {
            Debug.Log("Starting Game...");

            // Call hook and abort if it returns false.
            if (!OnStartGame()) { return; }
    
            // Initialize references that are expected to exist in the main game scene here
            _stoneSpawnLocation = GameObject.FindWithTag("Stone Spawn Location");

            // TODO: allow player color to be selected and randomize starting player
            CurlingPlayer startingPlayer = Player2;
            CurlingPlayer otherPlayer = Player1;
            Player1.SetColor(PlayerColor.Blue);
            Player2.SetColor(PlayerColor.Red);
            SetCurrentPlayer(startingPlayer.GetPlayerID());
            SetEndNumberAndHammer(1, otherPlayer.PlayerColor);

            NextShot(0, false);
        }

        protected void StartNextEnd()
        {
            Debug.Log("Starting Next End...");

            // Reset variables
            RedPlayerReadyForNextEnd = false;
            BluePlayerReadyForNextEnd = false;

            // Reset local CurlingPlayer state
            if (GameManager.IsNetworked)
            {
                NetworkedCurlingPlayer.LocalPlayerInstance.ResetForNextEnd();
            } else
            {
                foreach (LocalCurlingPlayer lcp in LocalCurlingPlayer.Instances)
                {
                    lcp.ResetForNextEnd();
                }
            }

            // Clean up old stone instances and call RPCs to kick off next end.
            // Only should be called by master client.
            if (IsMasterClient())
            {
                // Compute next hammer. Make sure this happens before we destroy the stones.
                (PlayerColor, int) endScore = GameManager.CalculatePointsScored(Stones);
                PlayerColor nextHammer = GameManager.NextHammer(Hammer, endScore);

                // Clean up old stones.
                for (int i = 0; i < Stones.Length; i++)
                {
                    Stones[i].Destroy();
                }
                Stones = new CurlingStone[NUM_STONES_IN_END];

                // Update current player, end number, and hammer for next end.
                CurlingPlayer nextStartingPlayer = nextHammer == PlayerColor.Red ? GetPlayer(PlayerColor.Blue) : GetPlayer(PlayerColor.Red);
                SetCurrentPlayer(nextStartingPlayer.GetPlayerID());
                SetEndNumberAndHammer(EndNumber + 1, nextHammer);

                NextShot(0, false);
            }
        }

        protected void NextShot(int currentStoneIndex, bool switchPlayers = true)
        {
            Debug.Log("Next Shot...");

            // Call hook and abort if it returns false.
            if (!OnNextShot()) { return; }

            // Check if the end is over
            if (currentStoneIndex >= NUM_STONES_IN_END)
            {
                Debug.Log("End of end...");

                (PlayerColor, int) endScore = GameManager.CalculatePointsScored(Stones);

                // TODO: check if game is over
                if (EndNumber >= NUM_ENDS_IN_MATCH)
                {
                    print("GAME OVER");
                    return;
                }

                int redScore = endScore.Item1 == PlayerColor.Red ? endScore.Item2 : 0;
                int blueScore = endScore.Item1 == PlayerColor.Blue ? endScore.Item2 : 0;
                UpdateEndScores(EndNumber, redScore, blueScore);
                SetCurrentGameState(GameState.EndOfEnd);
                return;
            }

            // Ensure sweeping is disabled before next shot just in case.
            foreach (CurlingStone stone in Stones)
            {
                if (stone != null)
                {
                    stone.StopSweepingImpl();
                }
            }

            if (switchPlayers)
            {
                SwitchCurrentPlayer();
            }

            SpinClockwise = true;

            SetCurrentStoneIndex(currentStoneIndex);
            SpawnStone();
            SetCurrentGameState(GameState.YourTurnDialogActive);
            _nextShotTimerActive = false;
        }

        private void SpawnStone()
        {
            Debug.Log("Spawn Stone...");

            // Call hook and abort if it returns false.
            if (!OnSpawnStone()) { return; }

            Stones[CurrentStoneIndex] = CreateAndInitializeStone(
                _stoneSpawnLocation.transform.position,
                _stoneSpawnLocation.transform.rotation,
                GetCurrentPlayer().PlayerColor);
        }

        protected void CheckForMovingStones()
        {
            if (CurrentGameState == GameState.WaitingForStonesToStop)
            {
                // See if all stones have stopped moving
                if (!Stones.Any(stone =>
                {
                    if (stone == null)
                    {
                        return false;
                    }
                    else
                    {
                        return stone.CheckIfMoving();
                    }
                }))
                {
                    if (!_nextShotTimerActive)
                    {
                        StartCoroutine(NextShotAfter3Seconds());
                    }
                }
            }
        }

        private IEnumerator NextShotAfter3Seconds()
        {
            _nextShotTimerActive = true;
            yield return new WaitForSeconds(3);
            NextShot(CurrentStoneIndex + 1);
        }

        private void SwitchCurrentPlayer()
        {
            string otherPlayerID = CurrentPlayerID == Player1.GetPlayerID() ? Player2.GetPlayerID() : Player1.GetPlayerID();
            SetCurrentPlayer(otherPlayerID);
        }

        public virtual void SetCurlDirection(bool spinClockwise)
        {
            SpinClockwise = spinClockwise;
            Stones[CurrentStoneIndex].SetSpinClockwise(spinClockwise);
        }

        protected virtual void UpdateEndScores(int endNumber, int redPoints, int bluePoints)
        {
            EndScores[endNumber] = (redPoints, bluePoints);

            int redTotalScore = 0;
            int blueTotalScore = 0;

            for (int i = 0; i < EndScores.Length; i++)
            {
                redTotalScore += EndScores[i].red;
                blueTotalScore += EndScores[i].blue;
            }

            OnEndScored?.Invoke(endNumber, redPoints, bluePoints, redTotalScore, blueTotalScore);
        }

        public virtual void PlayerReadyForNextEnd()
        {
            StartNextEnd();
        }

        protected virtual bool IsMasterClient()
        {
            return true;
        }

        public void PlaceBroom()
        {
            SetCurrentGameState(GameState.AccuracyMeterActive);
        }

        public void OnYourTurnDialogClosed()
        {
            SetCurrentGameState(GameState.PlacingBroom);
        }

        public void WaitingForStonesToStop()
        {
            SetCurrentGameState(GameState.WaitingForStonesToStop);
        }

        public virtual void SetAim(Vector3 normalizedAimingLineVector)
        {
            CurrentGameState = GameState.ShootingMeterActive;
            NormalizedVectorToBroom = normalizedAimingLineVector;
        }

        public virtual void ShootStone(float shotPower)
        {
            CurrentGameState = GameState.DeliveringStone;
            if (IsMasterClient())
            {
                Stones[CurrentStoneIndex].ShootStone(shotPower, NormalizedVectorToBroom);
            }
        }

        public virtual void PlaceStoneForTesting(Vector3 position)
        {
            if (IsMasterClient())
            {
                Stones[CurrentStoneIndex].PlaceStoneForTesting(position);
                NextShot(CurrentStoneIndex + 1);
            }
        }

        #endregion


        #region Score Calculation Functions

        // Returns the color of the scoring player and the number of points they scored.
        public static (PlayerColor, int) CalculatePointsScored(CurlingStone[] stones)
        {
            HashSet<int> countedStoneIndices = new HashSet<int>();
            (PlayerColor, int) closestStoneInfo = FindClosestStone(stones, countedStoneIndices);

            // In some cases, there will be no stones in the house. Return 0 for the score in that case.
            // The PlayerColor is not meaningful.
            if (closestStoneInfo.Item2 == -1)
            {
                return (PlayerColor.Red, 0);
            }

            PlayerColor scoringPlayer = closestStoneInfo.Item1;
            countedStoneIndices.Add(closestStoneInfo.Item2);

            while (true)
            {
                (PlayerColor, int) nextClosestStoneInfo = FindClosestStone(stones, countedStoneIndices);
                // If no other stones were found or if the next closest stone is a different color, then break out of the loop.
                if (nextClosestStoneInfo.Item2 == -1 || nextClosestStoneInfo.Item1 != scoringPlayer)
                {
                    break;
                }
                // Otherwise include that stone in the count and iterate again to find the next closest stone.
                else
                {
                    countedStoneIndices.Add(nextClosestStoneInfo.Item2);
                }
            }

            return (scoringPlayer, countedStoneIndices.Count);
        }

        // Returns the color and index of the closest stone in the supplied array, ignoring stones at the given indices.
        private static (PlayerColor, int) FindClosestStone(CurlingStone[] stones, HashSet<int> indicesToIgnore)
        {
            int currClosestIndex = -1;
            float currClosestDistance = float.MaxValue;

            for (int i = 0; i < stones.Length; i++)
            {
                if (indicesToIgnore.Contains(i) || !(stones[i].State == CurlingStoneState.AtRest))
                {
                    continue;
                }

                float distance = Vector3.Distance(CENTER_OF_HOUSE, stones[i].transform.position);
                if (distance <= MAX_DISTANCE_FROM_CENTER_OF_HOUSE && distance < currClosestDistance)
                {
                    currClosestDistance = distance;
                    currClosestIndex = i;
                }
            }

            // If there are no other stones to consider, return -1 as the index as an indicator.
            // The returned PlayerColor is not meaningful in this case.
            if (currClosestIndex == -1)
            {
                return (PlayerColor.Red, -1);
            }
            return (stones[currClosestIndex].Color, currClosestIndex);
        }

        public static PlayerColor NextHammer(PlayerColor previousHammer, (PlayerColor, int) endScore)
        {
            if (endScore.Item2 == 0)
            {
                return previousHammer;
            }

            return endScore.Item1 == PlayerColor.Red ? PlayerColor.Blue : PlayerColor.Red;
        }

        public (string playerName, int numPoints) GetMostRecentScoreInfo()
        {
            (int redScore, int blueScore) = EndScores[EndNumber];
            if (redScore > 0)
            {
                return (GetPlayer(PlayerColor.Red).GetPlayerName(), redScore);
            }
            else
            {
                return (GetPlayer(PlayerColor.Blue).GetPlayerName(), blueScore);
            }
        }

        #endregion

    }
}

