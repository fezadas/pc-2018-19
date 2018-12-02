
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
    public E tryRemove(){
        Node<E> observedHead,observedTail,observedHeadNext;

        do{
            observedHead = head.get();
            observedTail = tail.get();
            observedHeadNext = observedHead.next.get();

            if(observedHead == head.get()){
                if(observedHead == observedTail){
                    if(observedHeadNext == null) return null;
                    tail.compareAndSet(observedTail,observedHeadNext);
                }else {
                    E item = observedHeadNext.item;
                    if(head.compareAndSet(observedHead,observedHeadNext))
                        return item;
                }
            }
        }while (true);
    }

}
