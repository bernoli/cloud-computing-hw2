namespace LoadBalancer;

public class WorkerItem
{
    public DateTime RequestedtAt { get; set; }

    public int Iterations { get; set; }

    public byte[] Buffer { get; set; }

    public string Id { get; set; }

    public override string ToString()
    {
        return $"Worker Item: [{Id}], Iterations: [{Iterations}]";
    }
}

