using UnityEngine;

namespace Curling
{
    public class StatusText : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.UI.Text textObject;

        private GameState _gameState;
        private string _playerID;

        void OnEnable()
        {
            // Wire up all events to handlers.
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnCurrentPlayerIDChanged += OnCurrentPlayerIDChanged;

            // Manually invoke handlers with initial data to avoid race conditions.
            OnGameStateChanged(GameManager.Instance.CurrentGameState);
            OnCurrentPlayerIDChanged(GameManager.Instance.CurrentPlayerID);
        }

        public void OnGameStateChanged(GameState newState)
        {
            _gameState = newState;
            textObject.text = "Current Player: " + _playerID + "\n" + _gameState;
        }

        public void OnCurrentPlayerIDChanged(string newID)
        {
            _playerID = newID;
            textObject.text = "Current Player: " + _playerID + "\n" + _gameState;
        }
    }
}