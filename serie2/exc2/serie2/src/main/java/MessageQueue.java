
import java.util.Optional;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class MessageQueue<T> {

    private final Lock monitor = new ReentrantLock();
    private final Condition requestCondition = monitor.newCondition();

    private final LinkedQueue<OperationStatus> pendingMessages = new LinkedQueue<>();
    private volatile int waiters;

    public Optional<T> receive(long timeout) throws InterruptedException {

        OperationStatus operation;

        //Quando não há requests previamente registados
        //e há mensagens na fila de espera, este pedido pode ser logo processado

        if ((operation = pendingMessages.tryRemove()) != null) {
            if (!operation.completed) {
                operation.completed = true;
                if (operation.waiters != 0) { //é preciso adquirir o lock porque existem threads em espera
                    monitor.lock();
                    try {
                        if (operation.waiters > 0) { //existe alguem que precise de ser desbloqueado, neste caso mensagens a espera de receivers
                            operation.cond.signal();
                            return Optional.of(operation.message);
                        }
                    } finally {
                        monitor.unlock();
                    }
                }
            }
        }
        if (timeout == 0)
            return Optional.empty();

        // if a time out was specified, get a time reference
        boolean timed = timeout > 0;
        long nanosTimeout = timed ? TimeUnit.NANOSECONDS.toNanos(timeout) : 0L;

        monitor.lock();
        try {
            // the current thread declares itself as a waiter..
            waiters++;
            try {
                do {
                    if ((operation = pendingMessages.tryRemove()) != null) { //se existirem mensagens para serem recebidas
                        operation.completed = true; //the message was sent
                        operation.cond.signalAll(); //estando já na posse do lock, podemos sinalizar logo
                        return Optional.of(operation.message);
                    }
                    // check if the specified timeout expired
                    if (timed && nanosTimeout <= 0)
                        return Optional.empty();
                    if (timed)
                        nanosTimeout = requestCondition.awaitNanos(nanosTimeout);
                    else
                        requestCondition.await();

                } while (true);
            } finally {
                // the current thread is no longer a waiter
                waiters--;
            }
        } finally {
            monitor.unlock();
        }
    }

    public SendStatus send(T sentMsg) {
        OperationStatus operationStatus = new OperationStatus(false, sentMsg, monitor.newCondition());
        pendingMessages.put(operationStatus);
        if(waiters > 0) {
            monitor.lock();
            try {
                if(waiters > 0){
                    requestCondition.signal(); // only one thread can proceed execution
                    return operationStatus;
                }
            } finally {
                monitor.unlock();
            }
        }
        return operationStatus;
    }

    //-------------------------------

    private class OperationStatus implements SendStatus {

        private volatile boolean completed;
        private volatile int waiters;
        private T message;
        private Condition cond;

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
            //verificar se a mensagem já foi recebida
            if (this.completed) return true;
            // the event is not signalled; if a null time out was specified, return failure.
            if (timeout == 0)
                return false;

            // process timeout
            boolean timed = timeout > 0;
            long nanosTimeout = timed ? TimeUnit.NANOSECONDS.toNanos(timeout) : 0L;

            monitor.lock();
            try {
                ++this.waiters; //declarar propria thread como uma que espera
                try{
                    do {
                        if (this.completed) //verificar se é mesmo preciso bloquear
                            return true;
                        // check for timeout
                        if (timed) {
                            if (nanosTimeout <= 0)
                                // the specified time out elapsed, so return failure
                                return false;
                            nanosTimeout = this.cond.awaitNanos(nanosTimeout);
                        } else
                            this.cond.await();
                    } while (true);
                }finally {
                    this.waiters--;
                }
            } finally {
                monitor.unlock();
            }
        }
    }
}
