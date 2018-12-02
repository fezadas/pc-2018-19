package main.java;

public interface SendStatus {

    boolean isSent();
    boolean await(int timeout)throws InterruptedException;
}
