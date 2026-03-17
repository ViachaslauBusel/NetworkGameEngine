using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    internal class InitComponentTests : WorldTestBase
    {
        public sealed class FirstInitComponent : Component
        {
            private readonly TaskCompletionSource<bool> _firstUpdateProbe;
            private TestModel _model;
            private SecondInitComponent _secondComponent;
            private int _startCallCount;
            private bool _isFirstUpdate = true;

            public bool IsLinked => _secondComponent != null;
            public int ModelValue => _model.value;
            public int StartCallCount => _startCallCount;
            public bool UpdateInvariantPassed { get; private set; }

            public FirstInitComponent(TaskCompletionSource<bool> firstUpdateProbe)
            {
                _firstUpdateProbe = firstUpdateProbe;
            }

            public override void Init()
            {
                _model = GetModel<TestModel>();
                _secondComponent = GetComponent<SecondInitComponent>();
            }

            public override void Start()
            {
                _startCallCount++;
            }

            public override void Update()
            {
                if (!_isFirstUpdate)
                {
                    return;
                }

                _isFirstUpdate = false;
                UpdateInvariantPassed = IsLinked && StartCallCount == 1;
                _firstUpdateProbe.TrySetResult(UpdateInvariantPassed);
            }
        }

        public sealed class SecondInitComponent : Component
        {
            private TestModel _model;
            private FirstInitComponent _firstComponent;
            private int _startCallCount;

            public bool IsLinked => _firstComponent != null;
            public int ModelValue => _model.value;
            public int StartCallCount => _startCallCount;

            public override void Init()
            {
                _model = GetModel<TestModel>();
                _firstComponent = GetComponent<FirstInitComponent>();
            }

            public override void Start()
            {
                _startCallCount++;
            }
        }

        public sealed class TestModel : LocalModel
        {
            public int value = 1;
        }

        [Test]
        public async Task Should_LinkComponents_ResolveModel_And_CallStartOnce()
        {
            var firstUpdateProbe = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            GameObject gameObject = new();
            var firstComponent = new FirstInitComponent(firstUpdateProbe);
            var secondComponent = new SecondInitComponent();

            gameObject.AddModel(new TestModel());
            gameObject.AddComponent(firstComponent);
            gameObject.AddComponent(secondComponent);

            await World.AddGameObject(gameObject);

            Task completed = await Task.WhenAny(firstUpdateProbe.Task, Task.Delay(2_000));
            Assert.That(completed, Is.EqualTo(firstUpdateProbe.Task), "First Update() was not called in time.");

            Assert.Multiple(() =>
            {
                Assert.That(firstComponent.IsLinked, Is.True);
                Assert.That(secondComponent.IsLinked, Is.True);

                Assert.That(firstComponent.ModelValue, Is.EqualTo(1));
                Assert.That(secondComponent.ModelValue, Is.EqualTo(1));

                Assert.That(firstComponent.StartCallCount, Is.EqualTo(1));
                Assert.That(secondComponent.StartCallCount, Is.EqualTo(1));

                Assert.That(firstComponent.UpdateInvariantPassed, Is.True);
            });
        }
    }
}
