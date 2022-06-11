using System.Net;

namespace Worker;

internal class Program
{
    private static void Main(string[] args)
    {
        var endpoints = new List<IPEndPoint>();
        foreach (var arg in args)
        {
            if (IPEndPoint.TryParse(arg, out IPEndPoint endpoint))
            {
                endpoints.Add(endpoint);
                System.Console.WriteLine($"Endpoint [{endpoint}] found");
            }
        }

        var worker = new Worker(endpoints, 30);

        worker.StartWork().Wait();

        System.Console.WriteLine("Worker is done.");

    }
}