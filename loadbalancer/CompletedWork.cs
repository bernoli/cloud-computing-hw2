namespace LoadBalancer;

public class CompletedWork
{
    public string Id { get; set; }

    public byte[] Result { get; set; }


    public override string ToString()
    {
        return $"Completed work: [{Id}], Result [{Result?.Length}] bytes";
    }
}

