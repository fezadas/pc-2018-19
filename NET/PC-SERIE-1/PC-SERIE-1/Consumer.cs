using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PC_SERIE_1
{
     public class Consumer<R> : IEvent
    {

        private Action<R> consumer;

        public Consumer(Action<R> consumer)
        {
            this.consumer = consumer;
        }

        public void consume(Object value)
        {
            consumer((R)value);
        }
    }
}
