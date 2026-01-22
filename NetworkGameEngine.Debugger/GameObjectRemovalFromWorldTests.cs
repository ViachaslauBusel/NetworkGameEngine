namespace NetworkGameEngine.UnitTests
{
    internal class GameObjectRemovalFromWorldTests : WorldTestBase
    {
        [Test]
        public async Task Test1()
        {
            var gameObject = new GameObject();
            uint objID = await World.AddGameObject(gameObject);
            Assert.AreEqual(gameObject.ID, objID, "GameObject ID does not match returned ID.");
            Assert.IsTrue(gameObject.IsActive, "GameObject should be active after being added to the world.");
            gameObject.Destroy();
            Thread.Sleep(100);
            Assert.IsFalse(gameObject.IsActive, "GameObject should be inactive after being destroyed.");
        }
    }
}
