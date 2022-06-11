using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

namespace Worker;

public partial class Worker
{
    private readonly IReadOnlyList<IPEndPoint> _endPoints;
    private readonly HttpClient _httpClient;
    private readonly int _timeoutBeforeTerminateSeconds;

    public Worker(IReadOnlyList<IPEndPoint> endPoints, int timeoutBeforeTerminateSeconds)
    {
        _timeoutBeforeTerminateSeconds = timeoutBeforeTerminateSeconds;
        _endPoints = endPoints;
        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
        _httpClient = new HttpClient(clientHandler);
    }

    public Task StartWork()
    {
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var task = Task.Run(() =>
        {
            var random = new Random();
            int count = _endPoints.Count;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var endpoint = _endPoints[random.Next(count)];
                    var address = $"https://{endpoint}/dequeue?timeout={_timeoutBeforeTerminateSeconds * 1000}";
                    var request = new HttpRequestMessage(HttpMethod.Get, address);

                    var httpResponseMessage = _httpClient.SendAsync(request).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var workItem = httpResponseMessage.Content.ReadFromJsonAsync<WorkerItem>().Result;
                        if (workItem != null)
                        {
                            var sw = new Stopwatch();
                            sw.Start();
                            Console.WriteLine($"start working on: [{workItem}]");
                            var result = Work.ComputeSha512(workItem.Buffer, workItem.Iterations);
                            sw.Stop();
                            Console.WriteLine($"End working on: [{workItem}], took [{sw.ElapsedMilliseconds}] ms, [{sw.ElapsedMilliseconds / 1000} sec]");

                            var data = JsonContent.Create(result);
                            var addressCompleted = $"https://{endpoint}/result?id={workItem.Id}";
                            var sendCompletedRequest = new HttpRequestMessage(HttpMethod.Put, addressCompleted);
                            var response = _httpClient.PutAsync(addressCompleted, data).Result;
                        }

                    }
                    else
                    {
                        Console.WriteLine($"Should terminate due to: [{_timeoutBeforeTerminateSeconds}] seconds without work to handle.");
                        TerminateMachine();
                        cts.Cancel();
                    }
                }
                catch (Exception ex)
                {
                    var exp = (ex is AggregateException) ? ((AggregateException)ex).Flatten() : ex;
                    Console.WriteLine($"An error occurred, worker will terminate.{exp}");
                    TerminateMachine();
                    cts.Cancel();
                }
            }
        });

        return task;
    }

    private String TerminateMachine()
    {
        var ps = new ProcessStartInfo();
        Console.WriteLine($"Run terminate worker from directory: {Environment.CurrentDirectory}");
        ps.FileName = "bash";
        ps.Arguments = "terminate.sh";
        ps.UseShellExecute = false;
        ps.RedirectStandardOutput = false;

        var process = Process.Start(ps);
        process?.WaitForExit();
        return "Success terminating worker";
    }

}