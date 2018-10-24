import java.util.LinkedList;
import java.util.concurrent.RejectedExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class SimpleThreadPoolExecutor {

    private final Lock monitor = new ReentrantLock();

    private int maxPoolSize;
    private int keepAliveTime;
    private int totalWorkerThreads;

    private boolean toShutdown, doneshutdown;
    private Condition shutdownCondition;

    private LinkedList<Request> pendingRequests = new LinkedList<>();
    private LinkedList<WorkerThread> availableThreads = new LinkedList<>();

    public SimpleThreadPoolExecutor(int maxPoolSize, int keepAliveTime) {
        this.maxPoolSize = maxPoolSize;
        this.keepAliveTime = keepAliveTime;
        shutdownCondition = monitor.newCondition();
    }

    public boolean execute(Runnable command, int timeout) throws InterruptedException{

        monitor.lock();
        try {

            if(toShutdown)
                throw new RejectedExecutionException();

            if(pendingRequests.size() == 0 && availableThreads.size() > 0){
                WorkerThread thread = availableThreads.getFirst();
                availableThreads.remove(thread);
                thread.cmd = command;
                thread.condition.signal();
                return true;
            }

            if(totalWorkerThreads < maxPoolSize) {
                WorkerThread newThread = new WorkerThread(command);
                ++totalWorkerThreads;
                newThread.start();
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
                            return false;
                        }
                        monitor.wait(timeout);
                    } else
                        monitor.wait();
                } catch (InterruptedException ie) {
                    if (request.done) {
                        Thread.currentThread().interrupt();
                        break;
                    }
                    throw ie;
                }
            } while (!request.done);

            return true;
        } finally {
            monitor.unlock();
        }
    }

    public void shutDown(){
        monitor.lock();
        try {
            unlockedShutDown();
        } finally {
            monitor.unlock();
        }
    };

    private void unlockedShutDown(){
        toShutdown = true;
        for (WorkerThread w : availableThreads) {
            w.condition.signalAll();
        }
    }

    public boolean awaitTermination(int timeout)throws InterruptedException{
        monitor.lock();
        try {
            if (doneshutdown) return true;
            if(!toShutdown) unlockedShutDown();

            do {
                try
                {
                    TimeoutHolder th = new TimeoutHolder(timeout);
                    if (th.isTimed()) {
                        if ((timeout = (int)th.value()) <= 0) {
                            // the timeout limit has expired - here we are sure that the
                            // acquire resquest is still pending. So, we remove the request
                            // from the queue and return failure
                            return false;
                        }
                        shutdownCondition.await(timeout, TimeUnit.MILLISECONDS);

                    } else
                        shutdownCondition.await();
                }
                catch (InterruptedException e)
                {
                    if (totalWorkerThreads == 0)
                    {
                        Thread.currentThread().interrupt();
                        break;
                    }
                    throw e;
                }
            } while (!doneshutdown);
        } finally {
            monitor.unlock();
        }
        return true;
    }

    //--------------------------------------------

    private class Request{

        Runnable command;
        boolean done;

        public Request(Runnable command, boolean done) {
            this.command = command;
            this.done = done;
        }
    }

    private class WorkerThread extends Thread {
        public Runnable cmd;
        public Condition condition;

        public WorkerThread(Runnable cmd){
            this.cmd = cmd;
            condition = monitor.newCondition();
        }
        @Override
        public void run(){
            do {
                try {
                    cmd.run();
                } catch (Exception ex) {
                    lockedTerminateThread();
                }
            } while(getWork(this));
        }
    }



    private boolean getWork(WorkerThread workerThread){
        monitor.lock();
        try {
            if(pendingRequests.size() > 0){
                workerThread.cmd = pendingRequests.removeFirst().command;
                return true;
            }
            do
            {
                if (pendingRequests.size() == 0 && toShutdown) {
                    unlockedTerminateThread();
                    return false;
                }

                if (toShutdown) { //but still has work to finish
                    workerThread.cmd = pendingRequests.removeFirst().command;
                    return true;
                }

                availableThreads.add(workerThread);

                TimeoutHolder th = new TimeoutHolder(keepAliveTime);
                if (th.isTimed()) {
                    if ((keepAliveTime = (int)th.value()) <= 0) {
                        // the timeout limit has expired - here we are sure that the
                        // acquire resquest is still pending. So, we remove the request
                        // from the queue and return failure
                        availableThreads.remove(workerThread);
                        --totalWorkerThreads;
                        return false;
                    }
                    workerThread.condition.await(keepAliveTime, TimeUnit.MILLISECONDS);

                } else
                    workerThread.condition.await();

                if(pendingRequests.size() > 0){
                    workerThread.cmd = pendingRequests.removeFirst().command;
                    return true;
                }
            } while (true);

        } catch (InterruptedException e) {
            //NOTHING (WE DONT HANDLE WITH THIS)
        } finally {
            monitor.unlock();
        }

        return false;
    }

    private void lockedTerminateThread(){
        monitor.lock();
        try {
            unlockedTerminateThread();
        } finally {
          monitor.unlock();
        }
    }

    private void unlockedTerminateThread(){
        if(--totalWorkerThreads == 0)
            doneshutdown = true;
        shutdownCondition.signal();
    }
}
