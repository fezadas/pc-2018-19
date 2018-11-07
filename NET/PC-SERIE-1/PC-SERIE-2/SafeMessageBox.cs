using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PC_SERIE_2
{
    public class SafeMessageBox<M> where M : class
    {
        private class MsgHolder
        {
            internal M /*readonly*/ msg;
            internal volatile int lives;
        }

        private volatile MsgHolder msgHolder = null;

        public void Publish(M m, int lvs)
        {
            msgHolder = new MsgHolder { msg = m, lives = lvs };
        }

        public M TryConsume()
        {
            MsgHolder observed, newValue = null;
            do
            {
                observed = msgHolder;
                if (observed != null && observed.lives > 0) //1
                {
                    Interlocked.Decrement(ref observed.lives);
                    newValue = new MsgHolder { msg = observed.msg, lives = observed.lives };
                }
                else return null;

            } while (Interlocked.CompareExchange(ref msgHolder, newValue, observed) != observed);
            
            return newValue.msg;
        }
    }
}
