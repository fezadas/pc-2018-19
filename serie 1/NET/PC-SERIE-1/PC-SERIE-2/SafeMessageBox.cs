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
            internal readonly M msg;
            internal readonly int lives;
            public MsgHolder(M m, int ls)
            {
                msg = m;
                lives = ls;
            }
        }

        private volatile MsgHolder msgHolder = null;

        public void Publish(M m, int lvs)
        {
            msgHolder = new MsgHolder (m, lvs);
        }

        public M TryConsume()
        {
            MsgHolder observed, newValue = null;
            do
            {
                observed = msgHolder;
                if (observed != null && observed.lives > 0) 
                    newValue = new MsgHolder(observed.msg, observed.lives - 1);
                else
                    return null;
            } while (Interlocked.CompareExchange(ref msgHolder, newValue, observed) != observed);
            
            return newValue.msg;
        }
    }
}
