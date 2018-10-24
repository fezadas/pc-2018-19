using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PC_SERIE_1;

namespace PC_SERIE_1_Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            EventBus eventBus = new EventBus(5);
            String m1 = "Message1";
            String m2 = "Message2";
            eventBus.PublishEvent(m1);
            eventBus.PublishEvent(m2);

            eventBus.SubscribeEvent<String>((value) => { value.Substring(7); });

            Assert.AreEqual(m1, "1");
            Assert.AreEqual(m2, "2");
        }
    }
}
