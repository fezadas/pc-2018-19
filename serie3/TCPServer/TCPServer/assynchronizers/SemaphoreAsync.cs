/**
 *
 *  ISEL, LEIC, Concurrent Programming
 *
 *  Semaphore with asynchronous and synchronous interface
 *
 *  Carlos Martins, December 2018
 *
 **/

#define SEND_INTERRUPTS

// Comment/Uncomment to select tests
//#define AS_LOCK_SYNCH
#define AS_LOCK_ASYNC		
//#define ON_PRODUCER_CONSUMER_SYNC	
//#define ON_PRODUCER_CONSUMER_ASYNC		

// Uncomment to run the test continously until <enter>
#define RUN_CONTINOUSLY		

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

public class SemaphoreAsync {
			
	// The type used to represent each asynchronous acquire request
	private class Request: TaskCompletionSource<bool> {
		internal readonly int acquires;
		internal readonly CancellationToken cToken;
		internal CancellationTokenRegistration cTokenRegistration;
		internal Timer timer;
		const int PENDING = 0, OWNED = 1;
		private volatile int state; 
		
		internal Request(int acquires, CancellationToken cToken) : base() {
			this.acquires = acquires;
			this.cToken = cToken;
			this.state = PENDING;
		}
		internal bool TryLock() {
			return state == PENDING &&
				   Interlocked.CompareExchange(ref state, OWNED, PENDING) == PENDING;
		}
	}

	// the lock
	private readonly object _lock = new object();
	
	// available and maximum permits	
	private int permits;
	private readonly int maxPermits;

	// the requests queue
	private readonly LinkedList<Request> requests;

    /**
	 * Delegates with cancellation handlers for asynchrounous requests 
	 */
    private readonly Action<object> cancellationHandler;
	private readonly TimerCallback timeoutHandler;

    /**
	 *  completed tasks use to return true and false results
	 */
    private static readonly Task<bool> trueTask = Task.FromResult<bool>(true);
	private static readonly Task<bool> falseTask = Task.FromResult<bool>(false);
    
	/**
     * Constructor
     */
    public SemaphoreAsync(int initial = 0, int maximum = Int32.MaxValue) {
        if (initial < 0 || initial > maximum)
            throw new ArgumentOutOfRangeException("initial");
        //validate input
        if (maximum <= 0)
            throw new ArgumentOutOfRangeException("maximum");
        // ...
        permits = initial;
        maxPermits = maximum;
        // initialize the list of pending requests
        requests = new LinkedList<Request>();
        // construct delegates to describe cancellation handlers
        cancellationHandler = new Action<object>(CancellationHandler);
        timeoutHandler = new TimerCallback(TimeoutHandler);
    }

    /**
     * Cancellation handlers
     */

    // Cancel a request due to cancellation through CancellationToken
    private void CancellationHandler(object requestNode) {
		Request request = ((LinkedListNode<Request>)requestNode).Value;
		if (request.TryLock()) {
			// to access shared state we must acquire the lock
			lock(_lock) {
				if (((LinkedListNode<Request>)requestNode).List != null)
					requests.Remove((LinkedListNode<Request>)requestNode);
			}
			// The CancellationTokenRegistration is disposed after the cancellation
			// handler is called; since we acquired the lock we ensure that the
			// field request.timer is set.
			request.timer?.Dispose();
            // Complete the TaskCompletionSource to Canceled state
            request.SetCanceled();
        }
	}

	// Cancels a request due to timeout
	private void TimeoutHandler(object requestNode) {
		Request request = ((LinkedListNode<Request>)requestNode).Value;
		if (request.TryLock()) {
			lock(_lock) {
                if (((LinkedListNode<Request>)requestNode).List != null)
                    requests.Remove((LinkedListNode<Request>)requestNode);
			}
			// Dispose the possible cancelers.
			// Since we acquire the lock, we ensure the  correct visibility for
			// the request.cTokenRegistration and request.timer fields
			if (request.cToken.CanBeCanceled)
                request.cTokenRegistration.Dispose();
			request.timer.Dispose();
            // complete the TaskCompletionSource with RunToCompletion state, Result = false
            request.SetResult(false);
        }
	}
		
	// Try to cancel an asynchronous request identified by its task
	public bool TryCancelAcquire(Task<bool> requestTask) {
        Request request = null;
        lock(_lock) {
			foreach (Request req in requests) {
				if (req.Task == requestTask) {
                    if (req.TryLock()) {
						request = req;
                        requests.Remove(req);
					}
					break;
				}
			}
		}
		if (request != null) {
            if (request.cToken.CanBeCanceled)
                request.cTokenRegistration.Dispose();
			request.timer?.Dispose();
            request.SetCanceled();
        }
		return request != null;
	}

	/**
	 * Auxiliary methods that defines the synchronizer semantics
	 */
	// Returns true if the acquire is possible
	 private bool CanAcquire(int acquires) { return permits >= acquires; }

	// Updates synchronization state after an successful acquire 
	 private void AcquireSideEffect(int acquires) { permits -= acquires; }

	// Updates the synchronization state due to a release
	private void UpdateDueToRelease(int releases) { permits += releases; }

    /**
	 * Asynchronous TAP interface
	 */

    // Acquire multiple permits asynchronously enabling timeout and cancellation
    public Task<bool> WaitAsync(int acquires = 1, int timeout = Timeout.Infinite,
							    CancellationToken cToken = default(CancellationToken)) {
		lock(_lock) {
			if (requests.Count == 0 && CanAcquire(acquires)) {
				AcquireSideEffect(acquires);
				return trueTask;
			}
            // if the a cquire was specified as immediate, return failure
            if (timeout == 0)
				return falseTask;
			
			// If a cancellation was requested return a task in the Canceled state
			if (cToken.IsCancellationRequested)
				return Task.FromCanceled<bool>(cToken);
						
			// Create a request node and insert it in requests queue
			Request request = new Request(acquires, cToken);
			LinkedListNode<Request> requestNode = requests.AddLast(request);
		
			// Activate the specified cancelers owning the lock.
			// Since the timeout handler acquires the lock before use the request.timer and
			// request.cTokenRegistration the assignements will be visible.
			if (timeout != Timeout.Infinite)
				request.timer = new Timer(timeoutHandler, requestNode, timeout, Timeout.Infinite);
			
			// If the cancellation token is already in the canceled state, the delegate will
			// run immediately and synchronously, which causes no damage because the implicit
			// locks can be acquired recursively.
			if (cToken.CanBeCanceled)
            	request.cTokenRegistration = cToken.Register(cancellationHandler, requestNode);
	
			// Return the task that represents the asynchronous operation
			return request.Task;
		}
    }

	/**
	 * Releases the specified number of permits
	 */
	public void Release(int releases) {
        // A list to hold temporarily satisfied asynchronous operations 
        List<Request> released = new List<Request>();
		lock(_lock) {
			if (permits + releases > maxPermits)
				throw new InvalidOperationException("Exceeded maximum number of permits");	
			UpdateDueToRelease(releases);
			while (requests.Count > 0) {
                Request request = requests.First.Value;
            	if (!CanAcquire(request.acquires))
					break;
                // Remove the request from the queue
				requests.RemoveFirst();
				// Try lock the request and complete it if succeeded
				if (request.TryLock()) {
					AcquireSideEffect(request.acquires);
					released.Add(request);
				}
			} 
		}
		// Cleanup cancellers for the waiters released and complete the
		// underlying TaskCompletionSource
		foreach (Request request in released) {
            if (request.cToken.CanBeCanceled)
                request.cTokenRegistration.Dispose();
            request.timer?.Dispose();
            request.SetResult(true);
        }
	}
		
	// Release one permit
	public void Release() { Release(1);	}

    /**
	 *	Synchronous interface based on asynchronous TAP interface
	 */

    // Acquire multiple permits synchronously enabling timeout and cancellation
    public bool Wait(int acquires = 1, int timeout = Timeout.Infinite,
					 CancellationToken cToken = default(CancellationToken)) {
		Task<bool> waitTask = WaitAsync(acquires, timeout, cToken); 
		try {
            return waitTask.Result;
        } catch (ThreadInterruptedException) {
			// Try to cancel the asynchronous request
			if (TryCancelAcquire(waitTask))
				throw;
			// The request was already completed or cancelled, return the
			// underlying result
			try {
				return waitTask.Result;
			} catch (AggregateException ae) {
                throw ae.InnerException;
            } finally {
				// Anyway re-assert the interrupt
                Thread.CurrentThread.Interrupt();
            }
        } catch (AggregateException ae) {
            throw ae.InnerException;
        }
	}
}

/**
 * Test code
 */

/**
 * A blocking queue with synchronous and asynchronous TAP interface
 */
internal class BlockingQueueAsync<T> where T : class {
    private readonly int capacity;
    private readonly T[] room;
    private readonly SemaphoreAsync freeSlots, filledSlots;
    private int putIdx, takeIdx;

    // construct the blocking queue
    public BlockingQueueAsync(int capacity) {
        this.capacity = capacity;
        this.room = new T[capacity];
        this.putIdx = this.takeIdx = 0;
        this.freeSlots = new SemaphoreAsync(capacity, capacity);
        this.filledSlots = new SemaphoreAsync(0, capacity);
    }

    // Put an item in the queue asynchronously enabling timeout and cancellation
    public async Task<bool> PutAsync(T item, int timeout = Timeout.Infinite,
								     CancellationToken cToken = default(CancellationToken)) {
        if (!await freeSlots.WaitAsync(timeout: timeout, cToken: cToken))
            return false;       // timed out
        lock (room)
            room[putIdx++ % capacity] = item;
        filledSlots.Release();
        return true;
    }

    // Put an item in the queue synchronously enabling timeout and cancellation
    public bool Put(T item, int timeout = Timeout.Infinite,
                    CancellationToken cToken = default(CancellationToken)) {
		if (freeSlots.Wait(1, timeout, cToken)) {
	    	lock (room)
            	room[putIdx++ % capacity] = item;
        	filledSlots.Release();
        	return true;
		} else
			return false;
    }

    // Take an item from the queue asynchronously enabling timeout and cancellation
    public async Task<T> TakeAsync(int timeout, CancellationToken cToken) {
        if (await filledSlots.WaitAsync(timeout: timeout, cToken: cToken)) {
        	T item;
        	lock (room)
            	item = room[takeIdx++ % capacity];
        	freeSlots.Release();
        	return item;
		} else
            return null;        // timed out
    }
	
	// Take an item from the queue synchronously enabling timeout and cancellation
    public T Take(int timeout = Timeout.Infinite,
				  CancellationToken cToken = default(CancellationToken)) {
        if (filledSlots.Wait(1, timeout: timeout, cToken: cToken)) {
        	T item;
        	lock (room)
            	item = room[takeIdx++ % capacity];
        	freeSlots.Release();
        	return item;
		} else
			return null;
    }

    // Returns the number of filled positions in the queue
    public int Count {
        get {
            lock (room)
                return putIdx - takeIdx;
        }
    }
}


