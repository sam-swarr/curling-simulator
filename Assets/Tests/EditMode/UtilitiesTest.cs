using NUnit.Framework;

namespace Curling
{
    public class UtilitiesTest
    {
        [Test]
        public void MapToRangeTest()
        {
            Assert.AreEqual(0f, Utilities.MapToRange(0, 0, 10, 0, 100));
            Assert.AreEqual(20, Utilities.MapToRange(2, 0, 10, 0, 100));
            Assert.AreEqual(50, Utilities.MapToRange(5, 0, 10, 0, 100));
            Assert.AreEqual(75, Utilities.MapToRange(7.5f, 0, 10, 0, 100));
            Assert.AreEqual(89.9f, Utilities.MapToRange(8.99f, 0, 10, 0, 100));
            Assert.AreEqual(100, Utilities.MapToRange(10, 0, 10, 0, 100));

            // values outside of input range are clamped
            Assert.AreEqual(0, Utilities.MapToRange(-1, 0, 10, 0, 100));
            Assert.AreEqual(100, Utilities.MapToRange(11, 0, 10, 0, 100));

            // handles negative values and ranges
            Assert.AreEqual(-500, Utilities.MapToRange(0, -5, 15, -1000, 1000));
            Assert.AreEqual(-1000, Utilities.MapToRange(-5, -5, 15, -1000, 1000));
            Assert.AreEqual(500, Utilities.MapToRange(10, -5, 15, -1000, 1000));
        }
    }
}
