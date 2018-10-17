using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PC_SERIE_1
{
    class Subscriber
    {
        public List<Object> events;
        public Object condition;
        public int maxPending;

        public Subscriber(int maxPending)
        {
            events = new List<Object>();
            this.condition = new object();
            this.maxPending = maxPending;
        }

        public void Cancel()
        {

        }

        public bool AddEvent(Object ev)
        {
            if (events.Count < maxPending)
            {
                events.Add(ev);
                return true;
            }
            return false;
        }
    }
}
