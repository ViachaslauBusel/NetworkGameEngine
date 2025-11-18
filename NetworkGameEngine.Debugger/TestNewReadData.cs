using System.Numerics;

namespace NetworkGameEngine.UnitTests
{
    internal class TestNewReadData : WorldTestBase
    {
        public class TestSharedData : LocalModel
        {
            protected Vector3 _position;

            public int value;
            public Vector3 Position
            {
                get => _position;
                set
                {
                    if (_position != value)
                    {
                        _position = value;
                        MakeAllDirty();
                    }
                }
            }
        }

        public class TestShareDataContainer : TestSharedData
        {
            
        }

        public class TestWriteDataComponent : Component
        {
            private TestSharedData _data;

            public override void Init()
            {
                _data = GetModel<TestSharedData>();
            }

            public override void LateUpdate()
            {
                _data.value = 48;
                _data.Position = new Vector3(4, 2, 3);
            }
        }

        public class TestReadDataComponent : Component
        {
            private readonly GameObject _obj;

            public int TestValue { get; private set; } = 0;
            public Vector3 TestPosition { get; private set; } = Vector3.Zero;

            public TestReadDataComponent(GameObject obj)
            {
                _obj = obj;
            }

            public override void LateUpdate()
            {
                _obj.TryGetModel(out TestSharedData data);
                TestValue = data?.value ?? -1;
                TestPosition = data?.Position ?? Vector3.Zero;
            }
        }

        [Test]
        public async Task Test1()
        {
            var gameObject_0 = new GameObject();
            TestShareDataContainer data = new TestShareDataContainer();
            data.value = 42;
            data.Position = new Vector3(1, 2, 3);
            gameObject_0.AddModel(data);
            gameObject_0.AddComponent<TestWriteDataComponent>();

            var gameObject_1 = new GameObject();
            var readDataComponent = new TestReadDataComponent(gameObject_0);
            gameObject_1.AddComponent(readDataComponent);

            World.AddGameObject(gameObject_0);
            World.AddGameObject(gameObject_1);


            // Wait up to 500ms for TestValue to become 42
            var timeout = Task.Delay(50000);
            while (readDataComponent.TestValue != 48 && !timeout.IsCompleted)
            {
                await Task.Delay(10);
            }

            Assert.IsTrue(readDataComponent.TestValue == 48, $"Expected 42, got {readDataComponent.TestValue}");
            Assert.IsTrue(readDataComponent.TestPosition == new Vector3(4, 2, 3), $"Expected (1,2,3), got {readDataComponent.TestPosition}");
        }
    }
}