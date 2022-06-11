namespace queues;

public interface IQueue<T>
{
    int Count { get; }
    T FirstMessage { get; }
    T Dequeue(int timeout = 0, string search = "");
    void Enqueue(T item);
    IEnumerable<T> RemoveTop(int n, string search = "");
}
