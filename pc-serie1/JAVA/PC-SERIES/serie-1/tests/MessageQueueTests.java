import org.junit.jupiter.api.Test;

import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.assertEquals;

public class MessageQueueTests {

    @Test
    public void send_first() throws InterruptedException {

        MessageQueue<String> messageQueue = new MessageQueue<>();
        String msg = "Sent was called first.";

        SendStatus[] sendStatus = new SendStatus[1];
        String[] receivedMsg = new String[1];
        boolean[] awaitRes = new boolean[1];

        Thread t1 = new Thread(() -> {
            sendStatus[0] = messageQueue.send(msg);
            try {
                awaitRes[0] = sendStatus[0].await(-1);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });

        Thread t2 = new Thread(() -> {
            try {
                receivedMsg[0] = messageQueue.receive(2000).get();
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });

        t1.start();
        TimeUnit.SECONDS.sleep(1);
        assertEquals(false, sendStatus[0].isSent());


        t2.start();
        TimeUnit.SECONDS.sleep(1);
        assertEquals(true, sendStatus[0].isSent());
        assertEquals(msg, receivedMsg[0]);

        assertEquals(true, awaitRes[0]);
    }

    @Test
    public void fist_receive() throws InterruptedException {

        MessageQueue<String> messageQueue = new MessageQueue<>();
        String msg = "Receive was called first.";

        SendStatus[] sendStatus = new SendStatus[1];
        String[] receivedMsg = new String[1];

        Thread t1 = new Thread(() -> {
            try {
                receivedMsg[0] = messageQueue.receive(3000).get();
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });

        Thread t2 = new Thread(() -> {
            sendStatus[0] = messageQueue.send(msg);
        });
        t1.start();
        TimeUnit.SECONDS.sleep(1);
        t2.start();

        TimeUnit.SECONDS.sleep(1);
        assertEquals(true, sendStatus[0].isSent());
        assertEquals(msg, receivedMsg[0]);
    }
}
