using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PC_SERIE_1;
using System.Threading;

namespace PC_SERIE_1_Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            EventBus eventBus = new EventBus(5);
            String[] res = new String[1];
            Object[] resObj = new Object[1];

            Thread t = new Thread(() =>
            {
                eventBus.SubscribeEvent<String>((value) => { res[0] = "doneStr"; });
            });
            t.Start();

            Thread t2 = new Thread(() =>
            {
                eventBus.SubscribeEvent<Object>((value) => { resObj[0] = "doneObj"; });
            });
            t2.Start();

            Thread.Sleep(1000);
            
            eventBus.PublishEvent("msg1");
            eventBus.PublishEvent(new Object());

            eventBus.Shutdown();
            String m = res[0];
            Assert.AreSame(res[0], "doneStr");
        }

    }
}
