using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    internal class TestInitComponent : WorldTestBase
    {
        public class FirstInitTestComponent : Component
        {
            private TestModel _model;
            private SecondInitTestComponent _secondComponent;
            private int _startCalled = 0;

            public bool isLinked => _secondComponent != null;
            public int modelValue => _model.value;
            public int startCalled => _startCalled;

            public override void Init()
            {
                _model = GetModel<TestModel>();
                _secondComponent = GetComponent<SecondInitTestComponent>();
            }

            public override void Start()
            {
                _startCalled++;
            }
        }
        public class SecondInitTestComponent : Component
        {
            private TestModel _model;
            private FirstInitTestComponent _firstComponent;
            private int _startCalled = 0;

            public bool isLinked => _firstComponent != null;
            public int modelValue => _model.value;
            public int startCalled => _startCalled;

            public override void Init()
            {
                _model = GetModel<TestModel>();
                _firstComponent = GetComponent<FirstInitTestComponent>();
            }

            public override void Start()
            {
                _startCalled++;
            }
        }
        public class TestModel : LocalModel
        {
            public int value = 1;
        }
        [Test]
        public async Task Test1()
        {
            bool isInit = false;
            GameObject gameObject = new GameObject();
            var firstComp = new FirstInitTestComponent();
            var secondComp = new SecondInitTestComponent();
            gameObject.AddModel(new TestModel());
            gameObject.AddComponent(firstComp);
            gameObject.AddComponent(secondComp);
            uint objID = await World.AddGameObject(gameObject);
            Thread.Sleep(200);
            Assert.IsTrue(firstComp.isLinked && secondComp.isLinked);
            Assert.AreEqual(1, firstComp.modelValue);
            Assert.AreEqual(1, secondComp.modelValue);
            Assert.AreEqual(1, firstComp.startCalled);
            Assert.AreEqual(1, secondComp.startCalled);
        }
    }
}
