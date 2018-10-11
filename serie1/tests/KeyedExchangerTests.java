import org.junit.Before;
import org.junit.jupiter.api.Test;

import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.assertEquals;

public class KeyedExchangerTests {

    KeyedExchanger<String> exchanger = new KeyedExchanger<>();
    String[] res;

    @Test
    public void test_simple_exchange() throws InterruptedException {

        Thread t = new Thread(() -> {
            try {
                res[0] = exchanger.exchange(1, "data1", 2000).get();
            } catch (InterruptedException e) {
                //TODO
            }
        });

        Thread t2 = new Thread(() -> {
            try {
                res[1] = exchanger.exchange(1, "data2", 2000).get();
            } catch (InterruptedException e) {
                //TODO
            }
        });
        t.start();
        t2.start();

        TimeUnit.SECONDS.sleep(1);
        assertEquals("data2", res[0]);
        assertEquals("data1", res[1]);
    }

    @Test
    public void test_timeout_exchange() throws InterruptedException {
        String[] res = new String[2];
        Thread t = new Thread(() -> {
            try {
                res[0] = exchanger.exchange(1, "data1", 2000).get();
            } catch (InterruptedException e) {
                //TODO
            }
        });
        Thread t2 = new Thread(() -> {
            try {
                res[1] = exchanger.exchange(1, "data2", 2000).get();
            } catch (InterruptedException e) {
                //TODO
            }
        });
        t.start();
        t2.start();

        TimeUnit.SECONDS.sleep(3);
        assertEquals(null, res[0]);
        assertEquals(null, res[1]); //FIXME ????
    }

    @Test
    public void test_exchange_for_different_keys() throws InterruptedException {
        String[] res = new String[4];
        Thread t = new Thread(() -> {
            try {
                res[0] = exchanger.exchange(1, "data1", 2000).get();
            } catch (InterruptedException e) {
                //TODO
            }
        });
        Thread t2 = new Thread(() -> {
            try {
                res[1] = exchanger.exchange(1, "data2", 2000).get();
            } catch (InterruptedException e) {
                //TODO
            }
        });
        Thread t3 = new Thread(() -> {
            try {
                res[2] = exchanger.exchange(3, "data3", 2000).get();
            } catch (InterruptedException e) {
                //TODO
            }
        });
        Thread t4 = new Thread(() -> {
            try {
                res[3] = exchanger.exchange(3, "data4", 2000).get();
            } catch (InterruptedException e) {
                //TODO
            }
        });
        t.start();
        t2.start();
        t3.start();
        t4.start();

        TimeUnit.SECONDS.sleep(1);
        assertEquals("data2", res[0]);
        assertEquals("data1", res[1]);
        assertEquals("data4", res[2]);
        assertEquals("data3", res[3]);
    }
}
