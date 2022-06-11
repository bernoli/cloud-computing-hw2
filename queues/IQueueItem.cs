namespace queues;

public interface IQueueItem
{
    string Id { get; set; }
    DateTime RequestedtAt { get; set; }
}
