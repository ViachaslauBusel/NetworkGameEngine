namespace NetworkGameEngine.UnitTests
{
    internal class ModelAttachDetachTest : WorldTestBase
    {
        private sealed class TestLocalModel : LocalModel
        {
            public int AttachedCount { get; private set; }
            public int DetachedCount { get; private set; }

            public override void OnAttached()
            {
                AttachedCount++;
            }

            public override void OnDetached()
            {
                DetachedCount++;
            }
        }

        [Test]
        public async Task OnAttached_And_OnDetached_AreCalled_WhenGameObjectAddedAndRemoved()
        {
            var gameObject = new GameObject();
            var model = new TestLocalModel();

            gameObject.AddModel(model);

            uint objectId = await World.AddGameObject(gameObject);

            bool attachedCalled = SpinWait.SpinUntil(
                () => model.AttachedCount == 1,
                TimeSpan.FromSeconds(2));

            Assert.IsTrue(attachedCalled, "LocalModel.OnAttached() was not called after adding GameObject to world.");
            Assert.AreEqual(0, model.DetachedCount, "LocalModel.OnDetached() should not be called before object removal.");

            World.RemoveGameObject(objectId);

            bool detachedCalled = SpinWait.SpinUntil(
                () => model.DetachedCount == 1,
                TimeSpan.FromSeconds(2));

            Assert.IsTrue(detachedCalled, "LocalModel.OnDetached() was not called after removing GameObject from world.");
            Assert.AreEqual(1, model.AttachedCount, "LocalModel.OnAttached() should be called exactly once.");
        }
    }
}
