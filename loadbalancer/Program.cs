using System.Net;

namespace LoadBalancer;

internal class Program
{
    private static void Main(string[] args)
    {
        const int waitBeforeScaleSeconds = 30;

        var endpoints = new List<IPEndPoint>();
        foreach (var arg in args)
        {
            if (IPEndPoint.TryParse(arg, out var endpoint))
            {
                endpoints.Add(endpoint);
                System.Console.WriteLine($"Endpoint [{endpoint}] found");
            }
        }

        var loadBalancer = new LoadBalancer(endpoints, waitBeforeScaleSeconds);
        loadBalancer.StartWork(CancellationToken.None).Wait();
        System.Console.WriteLine("load balancer terminated.");
    }
}