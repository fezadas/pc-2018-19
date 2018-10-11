using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PC_SERIE_1
{
    interface IEvent
    {
        void consume(Object value);
    }
}
