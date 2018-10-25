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
    public int totalWorkerThreads;

    private boolean toShutdown, doneshutdown;
    private Condition shutdownCondition;

    public LinkedList<Request> pendingRequests = new LinkedList<>();
    public LinkedList<WorkerThread> availableThreads = new LinkedList<>();

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

            Request request = new Request(command);
            pendingRequests.addLast(request);  // enqueue "request" at the end of the request queue
            TimeoutHolder th = new TimeoutHolder(timeout);
            do {
                try {
                    if (th.isTimed()) {
                        if ((timeout = (int)th.value()) <= 0) {
                            pendingRequests.remove(request);
                            return false;
                        }
                        request.condition.await(timeout, TimeUnit.MILLISECONDS);
                    } else
                        request.condition.await();
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

            TimeoutHolder th = new TimeoutHolder(timeout);
            do {
                try
                {
                    if (th.isTimed()) {
                        if ((timeout = (int)th.value()) <= 0) {
                            totalWorkerThreads = 0;
                            availableThreads.removeAll(availableThreads);
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
        Condition condition;
        boolean done;

        public Request(Runnable command) {
            this.command = command;
            done = false;
            condition = monitor.newCondition();
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
                Request request = pendingRequests.removeFirst();
                workerThread.cmd = request.command;
                request.done = true;
                request.condition.signal();
                return true;
            }
            TimeoutHolder th = new TimeoutHolder(keepAliveTime);
            do
            {
                if (pendingRequests.size() == 0 && toShutdown) {
                    unlockedTerminateThread();
                    return false;
                }

                if (toShutdown) { //but still has work to finish
                    Request request = pendingRequests.removeFirst();
                    workerThread.cmd = request.command;
                    request.done = true;
                    request.condition.signal();
                    return true;
                }

                if (th.isTimed()) {
                    if ((keepAliveTime = (int)th.value()) <= 0) {
                        availableThreads.remove(workerThread);
                        --totalWorkerThreads;
                        return false;
                    }
                    availableThreads.add(workerThread);
                    workerThread.condition.await(keepAliveTime, TimeUnit.MILLISECONDS);

                } else {
                    availableThreads.add(workerThread);
                    workerThread.condition.await();
                }

                if(pendingRequests.size() > 0){
                    workerThread.cmd = pendingRequests.removeFirst().command;
                    return true;
                }
            } while (true);

        } catch (InterruptedException e) {

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
