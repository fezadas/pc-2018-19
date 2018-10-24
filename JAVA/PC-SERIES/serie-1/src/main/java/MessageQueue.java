import java.util.LinkedList;
import java.util.Optional;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class MessageQueue<T> {

    private final Lock monitor = new ReentrantLock();

    private final LinkedList<OperationStatus> pendingMessages = new LinkedList<OperationStatus>();
    private final LinkedList<Request> requestsQueue = new LinkedList<Request>();

    public SendStatus send(T sentMsg){
        try {
            monitor.lock();

            //quando ainda não há requests feitos pelo receiver
            if(requestsQueue.size() == 0){
                OperationStatus operation = new OperationStatus(false, sentMsg, monitor.newCondition());
                pendingMessages.add(operation);
                return operation;
            }
            else {
                //operation = new OperationStatus(true);
                Request request = requestsQueue.removeLast();
                request.done = true;
                request.message = sentMsg;
                request.cond.signal();
                return new SendStatus() {
                    @Override public boolean isSent() { return true; }
                    @Override public boolean tryCancel() { return false; }
                    @Override public boolean await(int timeout) throws InterruptedException { return false; }
                };
            }
        }finally {
            monitor.unlock();
        }
    }

    public Optional<T> receive(int timeout) throws InterruptedException {
        monitor.lock();
         try {

            //Quando não há requests previamente registados
            //e há mensagens na fila de espera, este pedido pode ser logo processado
            if (requestsQueue.size() == 0 && canAcquire())
                return Optional.of(acquireSideEffect()); //altera estado de operationstatus e retorna menssagem

            //Quando send mete uma mensagem na pendingMgs e ainda não há requests.
            Request request = new Request(false, monitor.newCondition());
            requestsQueue.addLast(request);

            TimeoutHolder th = new TimeoutHolder(timeout);
            do {
                try {
                    if (th.isTimed()) {
                        if ((timeout = (int)th.value()) <= 0) {
                            requestsQueue.remove(request);
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
                    requestsQueue.remove(request);
                    throw ie;
                }
            } while (!request.done);
            return Optional.of(request.message);

         } finally {
             monitor.unlock();
         }
    }

    private boolean canAcquire() {
        return pendingMessages.size() != 0;
    }

    private T acquireSideEffect() {
        OperationStatus operation = pendingMessages.removeFirst(); //gets the message
        operation.completed = true; //the message was sent
        operation.cond.signal();
        return operation.message;
    }

    //-------------------------------

    private class Request{

        public boolean done;
        public T message;
        public Condition cond;

        public Request(boolean done, Condition cond){
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

        public OperationStatus(boolean completed , T message, Condition cond){
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
        public boolean tryCancel() {
            monitor.lock();
            try {
                return !completed && pendingMessages.remove(this);
            } finally {
                monitor.unlock();
            }
        }

        @Override
        public boolean await(int timeout) throws InterruptedException {
            try {
                monitor.lock();
                do {
                    TimeoutHolder th = new TimeoutHolder(timeout);
                    try {
                        if (th.isTimed()) {
                            if ((timeout = (int)th.value()) <= 0) {
                                pendingMessages.remove(this);
                                return false;
                            }
                            this.cond.await(timeout, TimeUnit.MILLISECONDS);
                        }
                        this.cond.await();
                    }catch (InterruptedException ie) {
                        if (this.completed) {
                            Thread.currentThread().interrupt();
                        }
                        pendingMessages.remove(this);
                        throw ie;
                    }
                } while(!this.completed);

                return true;

            } finally {
                monitor.unlock();
            }
        }
    }
}
