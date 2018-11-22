package main.java;

import java.util.Optional;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class MessageQueueNonBlocking<T> {

    private final Lock monitor = new ReentrantLock();

    private final LinkedQueue<OperationStatus> pendingMessages = new LinkedQueue<>();
    private final LinkedQueue<Request> requestsQueue = new LinkedQueue<Request>();

    public SendStatus sendOptimized(T sentMsg) {

        Request request;
        if ((request = requestsQueue.tryRemove()) == null) {
            OperationStatus operation = new OperationStatus(false, sentMsg, monitor.newCondition());
            pendingMessages.put(operation);
            return operation;
        } else {
            try {
                monitor.lock();
                request.done = true;
                request.message = sentMsg;
                request.cond.signal();
                return new SendStatus() {
                    @Override
                    public boolean isSent() {
                        return true;
                    }

                    @Override
                    public boolean await(int timeout) throws InterruptedException {
                        return false;
                    }
                };
            } finally {
                monitor.unlock();
            }
        }
    }

    public Optional<T> receiveOptimized(int timeout) throws InterruptedException {
        OperationStatus operation;

        //Quando não há requests previamente registados
        //e há mensagens na fila de espera, este pedido pode ser logo processado

        if ((operation = pendingMessages.tryRemove()) != null) { //gets the message
            operation.completed = true; //the message was sent
            operation.cond.signal();
            return Optional.of(operation.message);
        }

        Request request = new Request(false, monitor.newCondition());
        requestsQueue.put(request);

        try {
            monitor.lock();
            TimeoutHolder th = new TimeoutHolder(timeout);
            do {
                try {
                    if (th.isTimed()) {
                        if ((timeout = (int) th.value()) <= 0) {
                            requestsQueue.tryRemove();
                            return Optional.empty();
                        }
                        request.cond.await(timeout, TimeUnit.MILLISECONDS);
                    } else
                        request.cond.await();
                } catch (InterruptedException ie) {
                    if (request.done) {
                        Thread.currentThread().interrupt();
                        break;
                    }
                    requestsQueue.tryRemove();
                    throw ie;
                }
            } while (!request.done);
            return Optional.of(request.message);

        } finally {
            monitor.unlock();
        }
    }

    //-------------------------------

    private class Request {

        public boolean done;
        public T message;
        public Condition cond;

        public Request(boolean done, Condition cond) {
            this.done = done;
            this.message = null;
            this.cond = cond;
        }
    }

    //-------------------------------

    private class OperationStatus implements SendStatus {

        private boolean completed;
        public T message;
        public Condition cond;

        public OperationStatus(boolean completed, T message, Condition cond) {
            this.completed = completed;
            this.message = message;
            this.cond = cond;
        }

        @Override
        public boolean isSent() {
            monitor.lock();
            try {
                return completed;
            } finally {
                monitor.unlock();
            }
        }

        @Override
        public boolean await(int timeout) throws InterruptedException {
            try {
                monitor.lock();
                TimeoutHolder th = new TimeoutHolder(timeout);
                do {
                    try {
                        if (th.isTimed()) {
                            if ((timeout = (int) th.value()) <= 0) {
                                pendingMessages.tryRemove(this);
                                return false;
                            }
                            this.cond.await(timeout, TimeUnit.MILLISECONDS);
                        }
                        this.cond.await();
                    } catch (InterruptedException ie) {
                        if (this.completed) {
                            Thread.currentThread().interrupt();
                        }
                        pendingMessages.tryRemove(this);
                        throw ie;
                    }
                } while (!this.completed);

                return true;

            } finally {
                monitor.unlock();
            }
        }
    }
}
