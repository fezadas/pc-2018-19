using System;
using System.Collections.Generic;
using System.Threading;

namespace PC_SERIE_1
{

    class EventBus
    {
        private readonly Object monitor = new Object();
        private int maxPending;
        Dictionary<Type, List<Subscriber>> subscribers = new Dictionary<Type, List<Subscriber>>();

        public EventBus(int maxPending) { this.maxPending = maxPending; }

        //ACQUIRE
        public void SubscribeEvent<T>(Action<T> handler) where T : class
        {
            lock (monitor)
            {
                List<Subscriber> subs;
                if (subscribers.TryGetValue(typeof(T), out subs))
                {
                    subs.Add(new Subscriber(maxPending));
                }
                else
                {
                    subs = new List<Subscriber>();
                    subs.Add(new Subscriber(maxPending));
                    subscribers.Add(typeof(T), subs);
                }
            }

            List<Object> events;
            while((events = GetEvents(typeof(T))) != null){
                foreach(Object ev in events)
                {
                    try
                    {
                        handler((T)ev);
                    }
                    catch (ThreadInterruptedException)
                    {

                    }
                }
            }
        }
        
        public void PublishEvent<E>(E message) where E : class
        {
            lock (monitor)
            {
                List<Subscriber> subs;
                if (subscribers.TryGetValue(typeof(E), out subs)) {
                    foreach (Subscriber s in subs) {
                        if (s.AddEvent(message)) // se não ultrapassa o maxPending
                            Monitor.Pulse(s.condition);
                    }
                }
            }
        }

        public void Shutdown()
        { // bloquear a thread ate que todos os pedidos sejam atendidos }



        }

        public List<Object> GetEvents(Type type)
        {
            lock (monitor)
            {
                /**
                 
                 * /

                //TimeoutHolder th = new TimeoutHolder(timeout);
                do
                {
                    /*if ((timeout = th.Value) == 0)
                    {
                        // the timeout limit has expired - here we are sure that the acquire resquest
                        // is still pending. So, we remove the request from the queue and return failure.
                        reqQueue.Remove(request);

                        // After remove the request of the current thread from queue, *it is possible*
                        // that the current synhcronization allows now to satisfy another queued acquires.
                        if (CurrentSynchStateAllowsAquire())
                            PerformPossibleAcquires();

                        result = default(AcquireResult);
                        return false;
                    }*/
                    try
                    {
                        Monitor.Wait(monitor);
                    }
                    catch (ThreadInterruptedException)
                    {
                        // the thread may be interrupted when the requested acquire operation
                        // is already performed, in which case you can no longer give up
                        if (request.done)
                        {
                            // re-assert the interrupt and return normally, indicating to the
                            // caller that the operation was successfully completed
                            Thread.CurrentThread.Interrupt();
                            break;
                        }
                        // remove the request from the queue and throw ThreadInterruptedException
                        reqQueue.Remove(request);

                        // After remove the request of the current thread from queue, *it is possible*
                        // that the current synhcronization allows now to satisfy another queued acquires.
                        if (CurrentSynchStateAllowsAquire())
                            PerformPossibleAcquires();

                        throw;      // ThreadInterruptedException
                    }
                } while (!request.done);
                // the request acquire operation completed successfully
                result = request.acquireResult;
                return true;
            }
        }
    }
}
