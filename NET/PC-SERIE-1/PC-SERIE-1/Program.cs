using System;
using System.Collections.Generic;
using System.Threading;

namespace PC_SERIE_1
{

    class EventBus
    {
        private readonly Object monitor = new Object();
        private int maxPending;
        private Boolean toShutdown;
        private Object shutdownCondition;
        Dictionary<Type, List<Subscriber>> subscribers = new Dictionary<Type, List<Subscriber>>();

        public EventBus(int maxPending) {
            this.maxPending = maxPending;
            toShutdown = false;
            shutdownCondition = new object();
        }

        //ACQUIRE
        public void SubscribeEvent<T>(Action<T> handler) where T : class
        {
            Subscriber sub;
            lock (monitor)
            {
                List<Subscriber> subs;
                if (subscribers.TryGetValue(typeof(T), out subs))
                {
                    subs.Add((sub = new Subscriber(maxPending)));
                }
                else
                {
                    subs = new List<Subscriber>();
                    subs.Add((sub = new Subscriber(maxPending)));
                    subscribers.Add(typeof(T), subs);
                }
            }

            List<Object> events;
            while((events = GetEvents(sub)) != null) {
                foreach(Object ev in events)
                {
                    try
                    {
                        handler((T)ev);
                    }
                    catch (ThreadInterruptedException)
                    {
                        break; //code below!     |
                    }          //                V                       
                }
            }

            //retirar subscritor do mapa depois de terem sido processados todos os eventos
            //e tiver sido chamado o shutDown ou houve interrupcao

            lock (monitor)
            {
                RemoveSubscriber(typeof(T), sub);

                if (subscribers.Count == 0)
                    Monitor.Pulse(shutdownCondition);
            }
        }
        
        public void PublishEvent<E>(E message) where E : class
        {
            lock (monitor)
            {
                if (toShutdown)
                    throw new InvalidOperationException();

                List<Subscriber> subs;
                if (subscribers.TryGetValue(typeof(E), out subs)) {
                    foreach (Subscriber s in subs) {
                        s.TryAddEvent(message);
                    }
                    Monitor.Pulse(subscribers);
                }

            }
        }

        public void Shutdown()
        { 
            lock(monitor)
            {
                toShutdown = true;
                do {
                    try
                    {
                        Monitor.Wait(shutdownCondition);
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (subscribers.Count==0)
                        {
                            Thread.CurrentThread.Interrupt();
                            break;
                        }
                        throw;
                    }
                } while (true);
            }
        }

        public void RemoveSubscriber(Type type, Subscriber subscriber)
        {
            List<Subscriber> subs;
           
            if (subscribers.TryGetValue(type, out subs))
            {
                subs.Remove(subscriber);
                //se foi o ultimo subscritor a ser removido, eliminar a entrada no dicionario
                if(subs.Count == 0) subscribers.Remove(type);
            }
            
        }

        public List<Object> GetEvents(Subscriber subscriber)
        {
            lock (monitor)
            {

                List<Object> eventsToProcess;

                do
                {
                    //não existem eventos e foi chamado o shutDown
                    if (subscriber.events.Count == 0 && toShutdown)
                        return null;

                    //foi chamado shutDown mas existem eventos a processar
                    if (toShutdown && subscriber.events.Count != 0)
                        return subscriber.RemoveSubscriberEvents();

                    try
                    {
                        Monitor.Wait(subscribers);
                        if (toShutdown)
                        {
                            return null;
                        }
                        return subscriber.RemoveSubscriberEvents();
                    }
                    catch (ThreadInterruptedException)
                    {
                        //thread interrompida mas existem eventos a processar
                        if (subscriber.events.Count != 0)
                        {
                            eventsToProcess = subscriber.RemoveSubscriberEvents();
                            //cancelar na posse do lock
                            Thread.CurrentThread.Interrupt();
                            break;
                        }
                        throw;
                    }
                } while (true);
                return eventsToProcess;
            }
        }
    }
}
