﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test;

namespace NetworkGameEngine.Debugger
{
    public class TestCMD_0 : ICommand
    {
        public string testValue = "Hello World_0!!!";
    }
    public class TestCMD_1 : ICommand
    {
        public string testValue = "Hello World_1!!!";
    }
    public struct TestData_0 : IData
    {
        public int OutValue;
    }
    public class TestComponent : Component, IReactCommand<TestCMD_0>, IReactCommand<TestCMD_1>, IReadData<TestData_0>
    {

        public void ReactCommand(ref TestCMD_0 command)
        {
            Console.WriteLine(command.testValue);
        }

        public void ReactCommand(ref TestCMD_1 command)
        {
            Console.WriteLine(command.testValue);
        }

        public void UpdateData(ref TestData_0 data)
        {
            data.OutValue = 1;
        }
    }
    public class TestCommand
    {
        private World m_world;

        [SetUp]
        public void Setup()
        {
            m_world = new World();
            m_world.Init(8);
            Thread.Sleep(100);
            Thread th = new Thread(WorldThread);
            th.Start();
        }

        private void WorldThread()
        {

            while (true)
            {
                m_world.Update();
                Thread.Sleep(100);
            }
        }

        [Test]
        public async Task Test1()
        {
            GameObject gameObject = new GameObject();
            gameObject.AddComponent<TestComponent>();
          //  gameObject.AddComponent<TestInternalComponent>();
            int objID = await m_world.AddGameObject(gameObject);
            Thread.Sleep(150);
            gameObject.SendCommand(new TestCMD_0());
            gameObject.SendCommand(new TestCMD_1());

            gameObject.ReadData(out TestData_0 data);
          
            Thread.Sleep(800);
            Assert.IsTrue(data.OutValue == 1);
        }
    }
}
