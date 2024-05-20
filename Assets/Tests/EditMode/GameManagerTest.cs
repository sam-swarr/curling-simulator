using NUnit.Framework;
using UnityEngine;

namespace Curling
{
    public class GameManagerTest
    {
        [Test]
        public void CalculatePointsScoredTest()
        {
            // Simple case with two stones.
            CurlingStone[] stones = new CurlingStone[2];
            stones[0] = CreateStone(PlayerColor.Blue, new Vector3(0.39f, 0, 17.26f), CurlingStoneState.AtRest);
            stones[1] = CreateStone(PlayerColor.Red, new Vector3(0.54f, 0, 16.88f), CurlingStoneState.AtRest);
            (PlayerColor, int) result = GameManager.CalculatePointsScored(stones);
            Assert.AreEqual(PlayerColor.Blue, result.Item1);
            Assert.AreEqual(1, result.Item2);

            // Simple case with four stones.
            stones = new CurlingStone[4];
            // 4th stone
            stones[0] = CreateStone(PlayerColor.Red, new Vector3(-1.49559689f, 0, 16.8879719f), CurlingStoneState.AtRest);
            // 1st stone
            stones[1] = CreateStone(PlayerColor.Red, new Vector3(-0.375999987f, 0, 17.1270008f), CurlingStoneState.AtRest);
            // 2nd stone
            stones[2] = CreateStone(PlayerColor.Red, new Vector3(-0.180000007f, 0, 17.9810009f), CurlingStoneState.AtRest);
            // 3rd stone
            stones[3] = CreateStone(PlayerColor.Blue, new Vector3(0.547999978f, 0, 16.8880005f), CurlingStoneState.AtRest);
            result = GameManager.CalculatePointsScored(stones);
            Assert.AreEqual(PlayerColor.Red, result.Item1);
            Assert.AreEqual(2, result.Item2);

            // Case with four stones, two of them out of bounds.
            stones = new CurlingStone[4];
            stones[0] = CreateStone(PlayerColor.Blue, new Vector3(-0.375999987f, 0, 17.1270008f), CurlingStoneState.AtRest);
            // 2nd stone
            stones[1] = CreateStone(PlayerColor.Blue, new Vector3(-0.180000007f, 0, 17.9810009f), CurlingStoneState.AtRest);
            // OOB
            stones[2] = CreateStone(PlayerColor.Blue, new Vector3(0, 0, 0), CurlingStoneState.OutOfBounds);
            // OOB
            stones[3] = CreateStone(PlayerColor.Red, new Vector3(0, 0, 0), CurlingStoneState.OutOfBounds);
            result = GameManager.CalculatePointsScored(stones);
            Assert.AreEqual(PlayerColor.Blue, result.Item1);
            Assert.AreEqual(2, result.Item2);

            // Case with no stones in play.
            stones = new CurlingStone[4];
            // OOB
            stones[0] = CreateStone(PlayerColor.Blue, new Vector3(0, 0, 0), CurlingStoneState.OutOfBounds);
            // OOB
            stones[1] = CreateStone(PlayerColor.Blue, new Vector3(0, 0, 0), CurlingStoneState.OutOfBounds);
            // OOB
            stones[2] = CreateStone(PlayerColor.Blue, new Vector3(0, 0, 0), CurlingStoneState.OutOfBounds);
            // OOB
            stones[3] = CreateStone(PlayerColor.Red, new Vector3(0, 0, 0), CurlingStoneState.OutOfBounds);
            result = GameManager.CalculatePointsScored(stones);
            Assert.AreEqual(0, result.Item2);

            // Case with stones in play but not in the house.
            stones = new CurlingStone[4];
            // Outside of house
            stones[0] = CreateStone(PlayerColor.Red, new Vector3(-1.18700004f, 0, 15.7320004f), CurlingStoneState.AtRest);
            // Outside of house
            stones[1] = CreateStone(PlayerColor.Blue, new Vector3(1.24699998f, 0, 15.6110001f), CurlingStoneState.AtRest);
            // Outside of house
            stones[2] = CreateStone(PlayerColor.Blue, new Vector3(-2.11599994f, 0, 17.3950005f), CurlingStoneState.AtRest);
            // Outside of house
            stones[3] = CreateStone(PlayerColor.Red, new Vector3(1.94799995f, 0, 18.9640007f), CurlingStoneState.AtRest);
            result = GameManager.CalculatePointsScored(stones);
            Assert.AreEqual(0, result.Item2);

            // Case with stone barely in the house and one barely outside of house.
            stones = new CurlingStone[2];
            // Barely inside of house
            stones[0] = CreateStone(PlayerColor.Red, new Vector3(-0.00749707222f, 0, 15.4716644f), CurlingStoneState.AtRest);
            // Just outside of house
            stones[1] = CreateStone(PlayerColor.Red, new Vector3(0.307999998f, 0, 15.3789997f), CurlingStoneState.AtRest);
            result = GameManager.CalculatePointsScored(stones);
            Assert.AreEqual(PlayerColor.Red, result.Item1);
            Assert.AreEqual(1, result.Item2);
        }

        [Test]
        public void TestNextHammer()
        {
            // Test that hammer remains with same team when there's no score for the end.
            Assert.AreEqual(PlayerColor.Blue, GameManager.NextHammer(PlayerColor.Blue, (PlayerColor.Blue, 0)));
            Assert.AreEqual(PlayerColor.Blue, GameManager.NextHammer(PlayerColor.Blue, (PlayerColor.Red, 0)));
            Assert.AreEqual(PlayerColor.Red, GameManager.NextHammer(PlayerColor.Red, (PlayerColor.Blue, 0)));
            Assert.AreEqual(PlayerColor.Red, GameManager.NextHammer(PlayerColor.Red, (PlayerColor.Red, 0)));

            // Test that hammer goes to team that doesn't score.
            Assert.AreEqual(PlayerColor.Blue, GameManager.NextHammer(PlayerColor.Blue, (PlayerColor.Red, 1)));
            Assert.AreEqual(PlayerColor.Blue, GameManager.NextHammer(PlayerColor.Blue, (PlayerColor.Red, 2)));
            Assert.AreEqual(PlayerColor.Blue, GameManager.NextHammer(PlayerColor.Red, (PlayerColor.Red, 1)));
            Assert.AreEqual(PlayerColor.Blue, GameManager.NextHammer(PlayerColor.Red, (PlayerColor.Red, 2)));
            Assert.AreEqual(PlayerColor.Red, GameManager.NextHammer(PlayerColor.Blue, (PlayerColor.Blue, 1)));
            Assert.AreEqual(PlayerColor.Red, GameManager.NextHammer(PlayerColor.Blue, (PlayerColor.Blue, 2)));
            Assert.AreEqual(PlayerColor.Red, GameManager.NextHammer(PlayerColor.Red, (PlayerColor.Blue, 1)));
            Assert.AreEqual(PlayerColor.Red, GameManager.NextHammer(PlayerColor.Red, (PlayerColor.Blue, 2)));
        }

        // Helper function for instantiating a stone of the given color at the given position with the given state.
        private CurlingStone CreateStone(PlayerColor color, Vector3 position, CurlingStoneState state)
        {
            CurlingStone stone = MonoBehaviour.Instantiate(Resources.Load<GameObject>("NetworkedCurlingStone")).GetComponent<CurlingStone>();
            stone.transform.position = position;
            stone.InitializeImpl(color);
            stone.State = state;
            return stone;
        }
    }
}