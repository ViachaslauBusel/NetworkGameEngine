using System.Diagnostics;

namespace NetworkGameEngine.UnitTests
{
    internal sealed class GameObjectAddPerformanceTests : WorldTestBase
    {
        public class ComponentA : Component
        {
            private ComponentB _componentB;
            private ComponentC _componentC;
            private ComponentD _componentD;
            private ComponentE _componentE;
            private ComponentF _componentF;

            public override void Init()
            {
                _componentB = GetComponent<ComponentB>();
                _componentC = GetComponent<ComponentC>();
                _componentD = GetComponent<ComponentD>();
                _componentE = GetComponent<ComponentE>();
                _componentF = GetComponent<ComponentF>();
            }
        }

        public class ComponentB : Component
        {
            private ComponentA _componentA;
            private ComponentC _componentC;
            private ComponentD _componentD;
            private ComponentE _componentE;
            private ComponentF _componentF;

            public override void Init()
            {
                _componentA = GetComponent<ComponentA>();
                _componentC = GetComponent<ComponentC>();
                _componentD = GetComponent<ComponentD>();
                _componentE = GetComponent<ComponentE>();
                _componentF = GetComponent<ComponentF>();
            }
        }

        public class ComponentC : Component
        {
            private ComponentA _componentA;
            private ComponentB _componentB;
            private ComponentD _componentD;
            private ComponentE _componentE;
            private ComponentF _componentF;

            public override void Init()
            {
                _componentA = GetComponent<ComponentA>();
                _componentB = GetComponent<ComponentB>();
                _componentD = GetComponent<ComponentD>();
                _componentE = GetComponent<ComponentE>();
                _componentF = GetComponent<ComponentF>();
            }
        }

        public class ComponentD : Component
        {
            private ComponentA _componentA;
            private ComponentB _componentB;
            private ComponentC _componentC;
            private ComponentE _componentE;
            private ComponentF _componentF;

            public override void Init()
            {
                _componentA = GetComponent<ComponentA>();
                _componentB = GetComponent<ComponentB>();
                _componentC = GetComponent<ComponentC>();
                _componentE = GetComponent<ComponentE>();
                _componentF = GetComponent<ComponentF>();
            }
        }

        public class ComponentE : Component
        {
            private ComponentA _componentA;
            private ComponentB _componentB;
            private ComponentC _componentC;
            private ComponentD _componentD;
            private ComponentF _componentF;

            public override void Init()
            {
                _componentA = GetComponent<ComponentA>();
                _componentB = GetComponent<ComponentB>();
                _componentC = GetComponent<ComponentC>();
                _componentD = GetComponent<ComponentD>();
                _componentF = GetComponent<ComponentF>();
            }
        }

        public class ComponentF : Component
        {
            private ComponentA _componentA;
            private ComponentB _componentB;
            private ComponentC _componentC;
            private ComponentD _componentD;
            private ComponentE _componentE;

            public override void Init()
            {
                _componentA = GetComponent<ComponentA>();
                _componentB = GetComponent<ComponentB>();
                _componentC = GetComponent<ComponentC>();
                _componentD = GetComponent<ComponentD>();
                _componentE = GetComponent<ComponentE>();
            }
        }

        [Test]
        public async Task AddGameObjects_Performance()
        {
            var profiler = World.StartExecutionProfiler();
            int gameObjectCount = 50_000;
            Task<uint>[] addTasks = new Task<uint>[gameObjectCount];

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < gameObjectCount; i++)
            {
                GameObject newObj = new GameObject($"Perf_{i}");
                newObj.AddComponent<ComponentA>();
                newObj.AddComponent<ComponentB>();
                newObj.AddComponent<ComponentC>();
                newObj.AddComponent<ComponentD>();
                newObj.AddComponent<ComponentE>();
                newObj.AddComponent<ComponentF>();
                addTasks[i] = World.AddGameObject(newObj);
            }

            uint[] createdIds = await Task.WhenAll(addTasks);

            stopwatch.Stop();

            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            double objectsPerSecond = gameObjectCount / stopwatch.Elapsed.TotalSeconds;

            TestContext.WriteLine(
                $"Added {gameObjectCount} GameObjects in {elapsedMs:F2} ms " +
                $"({objectsPerSecond:F2} obj/s).");
            Thread.Sleep(1000); // Ensure profiler has time to process samples
            TestContext.WriteLine($"Profiler Max: {profiler.GetMaxTime(MethodType.InitComponent)} ms");

            Assert.That(createdIds.Length, Is.EqualTo(gameObjectCount));
            Assert.That(World.ActiveGameObjectCount, Is.EqualTo(gameObjectCount));
        }
    }
}
