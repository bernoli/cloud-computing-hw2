namespace queues;

public class CompletedWork : IQueueItem
{
    public string Id { get; set; }

    public byte[] Result { get; set; }

    public DateTime RequestedtAt { get; set; }


    public override string ToString()
    {
        return $"Completed work: [{Id}], Result [{Result?.Length}] bytes";
    }
}