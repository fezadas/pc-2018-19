using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;



namespace PC_SERIE_1
{

    class EventBus
    {
        private int maxPending;
        Dictionary<Type, List<IEvent>> events = new Dictionary<Type, List<IEvent>>();

        public EventBus(int maxPending) { this.maxPending = maxPending; }

        //ACQUIRE
        public void SubscribeEvent<T>(Action<T> handler) where T : class
        {

            //a thread que regista o handler do subscritor,
            //é a mesma que vai proceder à execução

            List<IEvent> subs;
            Consumer<T> consumer = new Consumer<T>(handler);
            if (events.TryGetValue(typeof(T), out subs))
            {
                subs.Add(consumer);
            }
            else
            {
                List<IEvent> list = new List<IEvent>();
                list.Add(consumer);
                events.Add(typeof(T), list);
            }

            //depois de registar o handler, fica à espera ( adormecia )
            //Monitor.Wait();

        }

        //RELEASE
        public void PublishEvent<E>(E message) where E : class
        {
            List<IEvent> subs;
            if (events.TryGetValue(typeof(E), out subs)) ;

            //é o publish que notifica cada uma das threads para executar o handler
            //com a mensagem E. a thread nunca é bloqueada
            //por cada publish, de alguma forma tem de ser incrementando um contador 
            //que indique se já chegamos ao maximo de trabalho pendente

        }

        public void Shutdown()
        { // bloquear a thread ate que todos os pedidos sejam atendidos }



        }
    }
}
