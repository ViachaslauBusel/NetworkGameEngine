﻿using NetworkGameEngine.JobsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    public class ExceptionTest
    {
        public abstract class TestComponentBase : Component
        {
            protected int _value;
            protected bool _isTested = false;

            public bool IsTested => _isTested;

            protected TestComponentBase(int value)
            {
                _value = value;
            }

            public override async void Update()
            {
                await JobsManager.Execute(Task.Run(() =>
                {
                    _isTested = false;
                    if (_value % 2 == 0)
                    {
                        throw new Exception("Test exception");
                    }
                    _isTested = true;
                }));
            }
        }

        public class TestComponent1 : TestComponentBase
        {
            public TestComponent1() : base(1) { }
        }

        public class TestComponent2 : TestComponentBase
        {
            public TestComponent2() : base(2) { }
        }

        public class TestComponent3 : TestComponentBase
        {
            public TestComponent3() : base(3) { }
        }

        private World m_world;

        [SetUp]
        public void Setup()
        {
            m_world = new World();
            m_world.Init(8);
            Thread.Sleep(100);
            Thread th = new Thread(WorldThread);
            th.IsBackground = true;
            th.Start();
        }

        private void WorldThread(object? obj)
        {
            while (true)
            {
                m_world.Update();
                Thread.Sleep(100);
            }
        }

        [Test]
        public void Test1()
        {
            GameObject obj_0 = new GameObject();
            GameObject obj_1 = new GameObject();
            GameObject obj_2 = new GameObject();

            var component_0 = new TestComponent1();
            var component_1 = new TestComponent2();
            var component_2 = new TestComponent3();

            obj_0.AddComponent(component_0);
            obj_1.AddComponent(component_1);
            obj_2.AddComponent(component_2);

            m_world.AddGameObject(obj_0).Wait();
            m_world.AddGameObject(obj_1).Wait();
            m_world.AddGameObject(obj_2).Wait();

            Thread.Sleep(2_000);

            Assert.IsTrue(component_0.IsTested);
            Assert.IsFalse(component_1.IsTested);
            Assert.IsTrue(component_2.IsTested);
        }
    }
}
