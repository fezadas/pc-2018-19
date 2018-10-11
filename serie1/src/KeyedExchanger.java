import java.util.HashMap;
import java.util.Map;
import java.util.Optional;

public class KeyedExchanger<T> {

    private final Object monitor = new Object();
    Map<Integer, T> data = new HashMap<>();

    public Optional<T> exchange(int ky, T mydata, int timeout) throws InterruptedException {
        synchronized(monitor) {

            T kyData;
            //if first thread is ready for exchange
            if ((kyData = data.get(ky)) != null) {
                data.put(ky, mydata); //data.remove(ky);
                monitor.notifyAll();
                return Optional.of(kyData);
            } else
                data.put(ky, mydata);

            TimeoutHolder th = new TimeoutHolder(timeout);
            long millisTimeout;
            do {
                try {
                    if (th.isTimed()) {
                        if ((millisTimeout = th.value()) <= 0) {
                            // the timeout limit has expired - here we are sure that the
                            // acquire resquest is still pending.
                            return  Optional.empty();
                        }
                        monitor.wait(millisTimeout);
                    } else
                        monitor.wait();
                } catch (InterruptedException ie) {
                    // the thread may be interrupted when the requested acquire operation
                    // is already performed, in which case you can no longer give up
                    if (data.get(ky) == null) {
                        // re-assert the interrupt and return normally, indicating to the
                        // caller that the operation was successfully completed
                        Thread.currentThread().interrupt();
                        break;
                    }
                    throw ie;
                }
            } while (data.get(ky) == mydata);

            kyData = data.get(ky);
            data.remove(ky);
            // the request acquire operation completed successfully
            return Optional.of(kyData);
        }
    }
}
