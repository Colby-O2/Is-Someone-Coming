using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PlazmaGames.Core;
using PlazmaGames.Core.MonoSystem;
using PlazmaGames.Core.Events;
using UnityEngine;
using UnityEngine.TestTools;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace PlazmaGames.Tests
{
    internal interface ITestMonoSystem : IMonoSystem { }
    internal class TestMonoSystem : MonoBehaviour, ITestMonoSystem { }
    internal record TestEvent(string value = "") {}


    public class GameManagerTests
    {
        private static string _eventMsg = string.Empty;

        private TestMonoSystem _testMS;

        private EventResponse _responseEmpty = new EventResponse();
        private EventResponse _response1 = new EventResponse(TestEventResponse1);
        private EventResponse _response2 = new EventResponse(TestEventResponse2);

        private static void TestEventResponse1(Component sender, object data) => _eventMsg += ((TestEvent)data).value;
        private static void TestEventResponse2(Component sender, object data) => _eventMsg += " : " + ((TestEvent)data).value;
        
        [Test, Order(1)]
        public void HasGameManagerTest()
        {
            Assert.AreNotEqual(GameManager.Instance, null);
        }

        [Test, Order(2)]
        public void AddMonoSystemTest()
        {
            _testMS = new TestMonoSystem();
            GameManager.AddMonoSystem<TestMonoSystem, ITestMonoSystem>(_testMS);
            Assert.IsTrue(GameManager.HasMonoSystem<ITestMonoSystem>());
        }

        [Test, Order(3)]
        public void GetMonoSystemTest() 
        {
            Assert.AreEqual(GameManager.GetMonoSystem<ITestMonoSystem>(), _testMS);
        }

        [Test, Order(4)]
        public void RemoveMonoSystemTest()
        {
            GameManager.RemoveMonoSystem<ITestMonoSystem>();
            Assert.IsFalse(GameManager.HasMonoSystem<ITestMonoSystem>());
        }

        [Test, Order(5)]
        public void AddEventTest()
        {
            GameManager.AddEventListener<TestEvent>(_responseEmpty);
            Assert.IsTrue(GameManager.HasEvent<TestEvent>());
        }

        [Test, Order(6)]
        public void AddEventResponseTest()
        {
            _eventMsg = string.Empty;
            GameManager.AddEventListener<TestEvent>(_response1);
            GameManager.EmitEvent(new TestEvent("a"));
            Assert.AreEqual(_eventMsg, "a");
        }

        [Test, Order(7)]
        public void AddAdditionalEventResponseTest()
        {
            _eventMsg = string.Empty;
            GameManager.AddEventListener<TestEvent>(_response2);
            GameManager.EmitEvent(new TestEvent("b"));
            Assert.AreEqual(_eventMsg, "b : b");
        }

        [Test, Order(8)]
        public void EventResponsePriorityTest()
        {
            _eventMsg = string.Empty;
            _response1.priority = 0;
            _response2.priority = 1;
            GameManager.EmitEvent(new TestEvent("c"));
            Assert.AreEqual(_eventMsg, " : cc");
        }

        [Test, Order(9)]
        public void RemoveEventListenerTest()
        {
            _eventMsg = string.Empty;
            GameManager.RemoveEventListener<TestEvent>(_response1);
            GameManager.EmitEvent(new TestEvent("d"));
            Assert.AreEqual(_eventMsg, " : d");
        }

        [Test, Order(10)]
        public void RemoveEventTest()
        {
            GameManager.RemoveEventListener<TestEvent>(_response2);
            GameManager.RemoveEventListener<TestEvent>(_responseEmpty);
            Assert.IsFalse(GameManager.HasEvent<TestEvent>());
        }
    }
}
