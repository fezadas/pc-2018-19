import org.junit.jupiter.api.Test;

import java.sql.Time;
import java.util.concurrent.RejectedExecutionException;
import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

public class SimpleThreadPoolExecutorTests {

    @Test
    public void test_threadPool() throws InterruptedException {

        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(3, 10000);
        boolean cont[] = new boolean[1];
        cont[0] = false;
        Runnable r1 = () -> {
            while(!cont[0])
                System.out.println("work");
        };
        Runnable r2 = () -> { System.out.println("4th execute"); };

        Thread t1 = new Thread(() -> {
            try {
                threadPool.execute(r2, 2000);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });

        assertEquals(0, threadPool.totalWorkerThreads);

        threadPool.execute(r1, -1);
        threadPool.execute(r1, -1);
        threadPool.execute(r1, -1);

        assertEquals(0, threadPool.availableThreads.size());
        assertEquals(3, threadPool.totalWorkerThreads);

        t1.start();
        TimeUnit.SECONDS.sleep(1);
        assertEquals(1, threadPool.pendingRequests.size());

        cont[0] = true;
        TimeUnit.SECONDS.sleep(1);
        assertEquals(0, threadPool.pendingRequests.size());

        threadPool.shutDown();
    }

    @Test
    public void test_keepAliveTime() throws InterruptedException {
        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(2, 2000);
        Runnable r = () -> { System.out.println("work"); };
        threadPool.execute(r, -1);

        TimeUnit.SECONDS.sleep(1);
        assertEquals(1, threadPool.totalWorkerThreads);
        assertEquals(1,threadPool.availableThreads.size());
        TimeUnit.SECONDS.sleep(2);
        assertEquals(0, threadPool.availableThreads.size());
        assertEquals(0, threadPool.totalWorkerThreads);
    }

    @Test
    public void test_shutDown() throws InterruptedException {
        boolean flag = false;
        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(2, 2000);
        Runnable r = () -> { System.out.println("work"); };
        threadPool.execute(r, -1);

        TimeUnit.SECONDS.sleep(1);

        threadPool.shutDown();

        try {
            threadPool.execute(r,-1);
        }catch (RejectedExecutionException e){
            flag = true;
        }
        assertTrue(flag);
    }

    @Test
    public void test_awaitTermination_with_timeout() throws InterruptedException {

        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(2, 5000);


        boolean cont[] = new boolean[1];
        cont[0] = false;
        Runnable r1 = () -> {
            while(!cont[0]);
        };

        Thread t1 = new Thread(() -> {
            try {
                threadPool.execute(r1, 2000);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });
        t1.start();

        TimeUnit.SECONDS.sleep(1);
        threadPool.shutDown();

        boolean[] resAwait = new boolean[1];

        Thread t2 = new Thread(() -> {
            try {
                resAwait[0] = threadPool.awaitTermination(3000);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });
        t2.start();

        TimeUnit.SECONDS.sleep(4);
        assertEquals(0,threadPool.totalWorkerThreads);
        assertEquals(0, threadPool.availableThreads.size());
        assertTrue(!resAwait[0]);
    }

    @Test
    public void test_awaitTermination_without_timeout() throws InterruptedException {

        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(2, 5000);


        boolean cont[] = new boolean[1];
        cont[0] = false;
        Runnable r1 = () -> {
            while(!cont[0]) System.out.println("work");
        };

        Thread t1 = new Thread(() -> {
            try {
                threadPool.execute(r1, 2000);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });
        t1.start();

        TimeUnit.SECONDS.sleep(1);
        threadPool.shutDown();

        boolean[] resAwait = new boolean[1];

        Thread t2 = new Thread(() -> {
            try {
                resAwait[0] = threadPool.awaitTermination(-1);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });
        t2.start();

        TimeUnit.SECONDS.sleep(1);
        cont[0] = true;
        TimeUnit.SECONDS.sleep(1);
        assertTrue(resAwait[0]);
        assertEquals(0, threadPool.availableThreads.size());
        assertEquals(0,threadPool.totalWorkerThreads);
    }
}
