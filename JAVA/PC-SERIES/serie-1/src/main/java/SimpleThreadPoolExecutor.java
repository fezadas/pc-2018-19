import java.util.LinkedList;
import java.util.concurrent.RejectedExecutionException;

public class SimpleThreadPoolExecutor {
/*
    private int maxPoolSize;
    private int keepAliveTime;
    private int totalWorkerThreads;
    private boolean toShutDown;

    private LinkedList<Request> pendingRequests = new LinkedList<>();
    private LinkedList<WorkerThread> threadPool = new LinkedList<>();

    public SimpleThreadPoolExecutor(int maxPoolSize, int keepAliveTime) {
        this.maxPoolSize = maxPoolSize;
        this.keepAliveTime = keepAliveTime;
    }

    private final Object monitor = new Object();

    private class WorkerThread extends Thread {

        public void setCommand(Runnable command) {
            this.command = command;
        }

        Runnable command;
        public WorkerThread(Runnable command){
            this.command = command;
        }

        @Override
        public void run(){
            command.run();
        }

    }

    public boolean execute(Runnable command, int timeout)throws InterruptedException{
        synchronized(monitor) {
            if(pendingRequests.size() == 0 && toShutDown) throw new RejectedExecutionException();
            if(pendingRequests.size() == 0 && canAcquire())
                return acquireSideEffect(command);
            if(totalWorkerThreads < maxPoolSize) {
                WorkerThread newThread = new WorkerThread(command);
                ++totalWorkerThreads;
                return true;
            }
            Request request = new Request(command,false);
            pendingRequests.addLast(request);  // enqueue "request" at the end of the request queue
            TimeoutHolder th = new TimeoutHolder(timeout);
            do {
                try {
                    if (th.isTimed()) {
                        if ((timeout = (int)th.value()) <= 0) {
                            pendingRequests.remove(request);
                            // After remove the request of the current thread from queue, *it is possible*
                            // that the current synhcronization allows now to satisfy another queued
                            // acquires.
                            if (pendingRequests.size() > 0 && threadPool.size() > 0 )
                                performPossibleAcquires();

                            return false;
                        }
                        monitor.wait(timeout);
                    } else
                        monitor.wait();
                } catch (InterruptedException ie) {
                    // the thread may be interrupted when the requested acquire operation
                    // is already performed, in which case you can no longer give up
                    if (request.done) {
                        // re-assert the interrupt and return normally, indicating to the
                        // caller that the operation was successfully completed
                        Thread.currentThread().interrupt();
                        break;
                    }
                    // remove the request from the queue and throw ThreadInterruptedException
                    pendingRequests.remove(request);

                    // After remove the request of the current thread from queue, *it is possible*
                    // that the current synhcronization allows now to satisfy another queued
                    // acquires.
                    if (pendingRequests.size() > 0 && threadPool.size()>0)
                        performPossibleAcquires();

                    throw ie;
                }
            } while (!request.done);
            // the request acquire operation completed successfully
            return true;
        }
    }

    private void performPossibleAcquires() {
        boolean notify = false;
        while (pendingRequests.size() > 0) {
            Request request = pendingRequests.peek();
            if (!canAcquire())
                break;
            pendingRequests.removeFirst();
            request.done = true;
            notify = true;
        }
        if (notify) {
            // even if we release only one thread, we do not know its position of the queue
            // of the condition variable, so it is necessary to notify all blocked threads,
            // to make sure that the thread(s) in question is notified.
            monitor.notifyAll();
        }
    }



    private boolean acquireSideEffect(Runnable command) {
        WorkerThread thread = threadPool.getFirst();
        threadPool.remove(thread);
        thread.setCommand(command);
        //thread.run();
        return true;
    }

    private boolean canAcquire() {
        return threadPool.size() > 0;
    }


    public void shutDown(){
        synchronized (monitor){
            toShutDown = true;
        }
    };

    public boolean awaitTermination(int timeout)throws InterruptedException{
        synchronized(monitor) {
            updateStateDueToRelease(releaseArgs);
            performPossibleAcquires();
        }
    }

    private class Request{

        Runnable command;
        boolean done;

        public Request(Runnable command, boolean done) {
            this.command = command;
            this.done = done;
        }
    }
*/
}
