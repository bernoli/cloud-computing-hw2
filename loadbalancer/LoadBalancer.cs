using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Text;

namespace LoadBalancer;

public class LoadBalancer
{
    private static readonly int WaitBeforeTryReconnectToQueuesMs = 5000;
    private static readonly int MAX_SCALE = 15;
    private static int Scaled = 0;
    private static readonly int TimesToSkipScale = 6;
    private static readonly int BlockScalingForMinutes = 3;
    private readonly IReadOnlyList<IPEndPoint> _endPoints;
    private readonly HttpClient _httpClient;
    private readonly int _waitingBeforeScaleSeconds;
    private bool _limitScale;
    private System.Timers.Timer _timer;

    private readonly string _ipsCommandLineScale;

    public LoadBalancer(IReadOnlyList<IPEndPoint> endPoints, int waitingBeforeScaleSeconds)
    {
        _waitingBeforeScaleSeconds = waitingBeforeScaleSeconds;
        _endPoints = endPoints;
        var sb = new StringBuilder();
        foreach (var endpoint in _endPoints)
        {
            sb.Append($"{endpoint} ");
        }
        _ipsCommandLineScale = sb.ToString().TrimEnd();
        Console.WriteLine($"Command line args for IPs: [{_ipsCommandLineScale}]");
        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
        _httpClient = new HttpClient(clientHandler);
        _timer = new System.Timers.Timer();
        _limitScale = true;
        _timer.Elapsed += (sender, e) =>
        {
            Scaled = 0;
            _limitScale = true;
        };
    }

    public Task StartWork(CancellationToken cancellationToken)
    {
        var task = Task.Run(() =>
        {
            var random = new Random();
            int count = _endPoints.Count;
            var skip = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var endpoint = _endPoints[random.Next(count)];
                var address = $"https://{endpoint}/head";
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, address);

                    var httpResponseMessage = _httpClient.SendAsync(request).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var workItem = httpResponseMessage.Content.ReadFromJsonAsync<WorkerItem>().Result;
                        if (workItem != null)
                        {
                            var waitingTime = (DateTime.UtcNow - workItem.RequestedtAt);
                            if (waitingTime.TotalSeconds > _waitingBeforeScaleSeconds && skip == 0)
                            {
                                if (Scaled < MAX_SCALE)
                                {
                                    Console.WriteLine("***** Should have scale now! *****");

                                    Console.WriteLine(ScaleWorker());
                                    skip = TimesToSkipScale;
                                }
                                else if (_limitScale)
                                {
                                    _limitScale = false;
                                    Console.WriteLine($"Reaching Workers scalled limit, restart load balancer to scale more or wait for {BlockScalingForMinutes} minutes.");
                                    _timer.Interval = TimeSpan.FromMinutes(BlockScalingForMinutes).TotalMilliseconds;
                                    _timer.AutoReset = false;
                                    _timer.Start();

                                }
                            }
                            else if (skip > 0)
                            {
                                // System.Console.WriteLine($"***** skip scaling [{skip}]");
                                Thread.Sleep(10000);
                                skip = Math.Max(0, (skip - 1));
                            }
                            else
                            {
                                //just wai before check
                                Thread.Sleep(1000);
                            }
                            // Console.WriteLine($"Last work item: [{workItem}], from [{address}], starting work, waiting for: [{waitingTime}]");
                        }
                    }
                    else
                    {
                        Thread.Sleep(2000);
                        skip = Math.Max(0, (skip - 1));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Load balancer failed to connect to queue [{address}], will try in [{WaitBeforeTryReconnectToQueuesMs}]");
                    Task.Delay(TimeSpan.FromMilliseconds(WaitBeforeTryReconnectToQueuesMs)).Wait();
                }
            }
        });

        return task;
    }

    private String ScaleWorker()
    {
        var ps = new ProcessStartInfo();
        Console.WriteLine($"Run scale worker from directory: {Environment.CurrentDirectory}");
        ps.FileName = "bash";
        ps.Arguments = $"spawn.sh {_ipsCommandLineScale}";
        ps.UseShellExecute = false;
        ps.RedirectStandardOutput = false;

        var process = Process.Start(ps);
        //process?.WaitForExit();

        Scaled++;
        return "Success";
    }
}

