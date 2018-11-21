package main.java;

import java.util.concurrent.atomic.AtomicReference;

public class LinkedQueue<E> {

    private static class Node<T>{
        final T item;
        final AtomicReference<Node<T>> next;
        public Node(T item, Node<T> next){
            this.item = item;
            this.next = new AtomicReference<Node<T>>(next);
        }
    }
    private final AtomicReference<Node<E>> head,tail;

    public LinkedQueue(){
        Node<E> dummy = new Node<E>(null,null);
        head = new AtomicReference<Node<E>>(dummy);
        tail = new AtomicReference<Node<E>>(dummy);
    }

    public void put(E item){
        Node<E> newNode = new Node<>(item,null);
        Node<E> observedTail,observedTailNext;

        do{
            observedTail = tail.get();
            observedTailNext = observedTail.next.get();
            if(observedTail == tail.get()){
                if(observedTailNext != null)
                    tail.compareAndSet(observedTail,observedTailNext);
                else {
                    if(observedTail.next.compareAndSet(null,newNode)){
                        tail.compareAndSet(observedTail,newNode);
                        return;
                    }
                }
            }
        }while (true);
    }
    public E remove(){
        Node<E> observedHead,observedTail,dummyNext;

        do{
            observedHead = head.get();
            observedTail = tail.get();
            dummyNext = observedHead.next.get();

            if(observedHead == head.get()){
                if(observedHead == observedTail){
                    if(observedHead.next.get() == null) return null;
                    //tail.compareAndSet(observedTail,dummyNext);
                }else {
                    E item = dummyNext.item;
                    head.compareAndSet(observedHead,dummyNext);
                    return item;
                }
            }
        }while (true);
    }
}
