using System;
using System.Collections.Generic;

namespace PC_SERIE_1
{
    public class Subscriber
    {
        public List<Object> events;
        public Object condition;
        public int maxPending;

        public Subscriber(int maxPending)
        {
            events = new List<Object>();
            condition = new Object();
            this.maxPending = maxPending;
        }
        public void TryAddEvent(Object ev)
        {
            if (events.Count < maxPending)
            {
                events.Add(ev);
            }
        }

        public List<Object> RemoveSubscriberEvents()
        {
            List<Object> ret = events;
            events = new List<Object>();
            return ret;
        }
    }
}
