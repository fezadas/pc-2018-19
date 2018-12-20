using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using TCPServer;

namespace JsonEchoServer
{
    internal class MessageQueue
    {
        private readonly int capacity;
        private readonly Message[] room;
        private readonly SemaphoreAsync freeSlots, filledSlots;
        private int putIdx, takeIdx;

        // construct the blocking queue
        public MessageQueue(int capacity)
        {
            this.capacity = capacity;
            this.room = new Message[capacity];
            this.putIdx = this.takeIdx = 0;
            this.freeSlots = new SemaphoreAsync(capacity, capacity);
            this.filledSlots = new SemaphoreAsync(0, capacity);
        }

        // Put an item in the queue asynchronously enabling timeout and cancellation
        public async Task<bool> PutAsync(Message item, int timeout = Timeout.Infinite,
                                            CancellationToken cToken = default(CancellationToken))
        {
            if (!await freeSlots.WaitAsync(timeout: timeout, cToken: cToken))
                return false;       // timed out
            lock (room)
                room[putIdx++ % capacity] = item;
            filledSlots.Release();
            return true;
        }

        // Put an item in the queue synchronously enabling timeout and cancellation
        public bool Put(Message item, int timeout = Timeout.Infinite,
                        CancellationToken cToken = default(CancellationToken))
        {
            if (freeSlots.Wait(1, timeout, cToken))
            {
                lock (room)
                    room[putIdx++ % capacity] = item;
                filledSlots.Release();
                return true;
            }
            else
                return false;
        }

        // Take an item from the queue asynchronously enabling timeout and cancellation
        public async Task<Message> TakeAsync(int timeout, CancellationToken cToken)
        {
            if (await filledSlots.WaitAsync(timeout: timeout, cToken: cToken))
            {
                Message item;
                lock (room)
                    item = room[takeIdx++ % capacity];
                freeSlots.Release();
                return item;
            }
            else
                return null;        // timed out
        }

        // Take an item from the queue synchronously enabling timeout and cancellation
        public Message Take(int timeout = Timeout.Infinite,
                        CancellationToken cToken = default(CancellationToken))
        {
            if (filledSlots.Wait(1, timeout: timeout, cToken: cToken))
            {
                Message item;
                lock (room)
                    item = room[takeIdx++ % capacity];
                freeSlots.Release();
                return item;
            }
            else
                return null;
        }

        // Returns the number of filled positions in the queue
        public int Count
        {
            get
            {
                lock (room)
                    return putIdx - takeIdx;
            }
        }
    }
}