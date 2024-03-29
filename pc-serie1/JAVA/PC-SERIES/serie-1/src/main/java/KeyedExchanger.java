import java.util.HashMap;
import java.util.Map;
import java.util.Optional;

public class KeyedExchanger<T> {

    private final Object monitor = new Object();
    private Map<Integer, Request> data = new HashMap<>();

    private static class Request<T>{
        boolean done;
        T value;
        Request(T value){
            this.value=value;
            done = false;
        }
    }

    public Optional<T> exchange(int ky, T mydata, int timeout) throws InterruptedException {
        synchronized(monitor) {
            Request request;
            //if first thread is ready for exchange
            if ((request = data.get(ky)) != null) {
                data.remove(ky); //remove the request from Map
                T aux = (T)request.value; //saves value from first thread
                request.value = mydata; //secon thread updates value
                request.done = true;
                monitor.notifyAll();
                return Optional.of(aux);
            } else {
                request = new Request(mydata);
                data.put(ky,request);
            }

            TimeoutHolder th = new TimeoutHolder(timeout);
            long millisTimeout;
            do {
                try {
                    if (th.isTimed()) {
                        if ((millisTimeout = th.value()) <= 0) {
                            // the timeout limit has expired - here we are sure that the
                            // acquire resquest is still pending.
                            data.remove(ky);
                            return  Optional.empty();
                        }
                        monitor.wait(millisTimeout);
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
					data.remove(ky);
                    throw ie;
                }
            } while (!request.done);
            // the request acquire operation completed successfully
            return Optional.of((T)request.value);
        }
    }
}
