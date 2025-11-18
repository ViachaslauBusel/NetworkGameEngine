using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    internal class OnEnableOnDisableTest : WorldTestBase
    {
        public class TestComponent : Component
        {
            public int EnableCount { get; private set; } = 0;
            public int DisableCount { get; private set; } = 0;
            public override void OnEnable()
            {
                EnableCount++;
            }
            public override void OnDisable()
            {
                DisableCount++;
            }
        }

        [Test]
        public async Task Test1()
        {
            var gameObject = new GameObject();
            var testComponent = new TestComponent();
            gameObject.AddComponent(testComponent);
            uint objID = await World.AddGameObject(gameObject);
            Thread.Sleep(150);
            Assert.AreEqual(1, testComponent.EnableCount, "OnEnable was not called.");
            gameObject.SetActive(false);
            Thread.Sleep(150);
            Assert.AreEqual(1, testComponent.DisableCount, "OnDisable was not called.");
            gameObject.SetActive(true);
            Thread.Sleep(150);
            Assert.AreEqual(2, testComponent.EnableCount, "OnEnable was not called again.");
            World.RemoveGameObject(objID);
            Thread.Sleep(150);
            Assert.AreEqual(2, testComponent.DisableCount, "OnDisable was not called again.");
        }
    }
}
